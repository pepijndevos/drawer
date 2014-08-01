open System.Text.RegularExpressions
open Suave.Web
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Http.RequestErrors
open Suave.Types
open Suave.Utils
open Suave.Log
open Mustache
open MarkdownSharp
open Npgsql

let toOption a =
  match a with
  | null -> None
  | _ -> Some a

let env =
  toOption << System.Environment.GetEnvironmentVariable

let (|HtmlBody|_|) (inp:string) =
    let opt = RegexOptions.IgnoreCase + RegexOptions.Multiline
    let m = Regex.Match(inp, "<body[^>]*>((?:\r|\n|.)*?)<\/body>", opt)
    if m.Success
    then Some m.Groups.[1].Value
    else None

let dburl = Option.map (fun url -> System.Uri url) <| env "DATABASE_URL"

let format_connect_string (uri : System.Uri) =
  let [|username; password|] = uri.UserInfo.Split(':')
  sprintf "Server=%s;Port=%d;User Id=%s;Password=%s;Database=%s;"
          uri.Host uri.Port username password (uri.AbsolutePath.Trim '/')

let connect_string =
  defaultArg (Option.map format_connect_string dburl)
             "Server=127.0.0.1;Port=5432;User Id=pepijndevos;Database=suaveblog;"

type Blogpost = {
  id : int64
  title : string
  body : string
  date : System.DateTime
}

let rec read_posts (reader : NpgsqlDataReader) =
  if reader.Read() then
    {
      id = reader.GetInt64 0
      title = reader.GetString 1
      body = reader.GetString 2
      date = reader.GetDateTime 3
    } :: read_posts reader
  else
    []

let all_posts conn page =
  let page_size = 4
  let cmd = new NpgsqlCommand("SELECT id, title, body, date FROM blogpost \
                               ORDER BY date DESC LIMIT :count OFFSET :start", conn)
  ignore <| cmd.Parameters.Add(new NpgsqlParameter("count", page_size))
  ignore <| cmd.Parameters.Add(new NpgsqlParameter("start", page * page_size))
  let reader = cmd.ExecuteReader()
  read_posts reader

let one_posts conn (id:int) =
  let cmd = new NpgsqlCommand("SELECT id, title, body, date FROM blogpost WHERE id = :id", conn)
  ignore <| cmd.Parameters.Add(new NpgsqlParameter("id", id))
  let reader = cmd.ExecuteReader()
  read_posts reader

let create_post conn (title : string) (body : string) =
  let cmd = new NpgsqlCommand("INSERT INTO blogpost (title, body) VALUES (:title, :body)", conn)
  ignore <| cmd.Parameters.Add(new NpgsqlParameter("title", title))
  ignore <| cmd.Parameters.Add(new NpgsqlParameter("body", body))
  ignore <| cmd.ExecuteNonQuery()

let with_db fn req =
  let conn = new NpgsqlConnection(connect_string)
  conn.Open()
  fn conn req

let (?||) opt1 opt2 =
  match opt1 with
  | Some x -> Some x
  | None -> opt2

let get_data req key =
  let mp = get_first req.multipart_fields key
  let uc = (form req) ^^ key
  mp ?|| uc

let compiler = FormatCompiler()
let index_template = compiler.Compile (System.IO.File.ReadAllText "templates/index.html")
type IndexData = {
  posts : Blogpost list
  next : int option
  previous : int option
}

let index_page conn req =
  let data = {posts = all_posts conn 0
              next = Some 1
              previous = None}
  let html = index_template.Render data
  OK html

let archive_page nr conn req =
  let data = {posts = all_posts conn nr
              next = Some (nr + 1)
              previous = match nr with
                         | 0 -> None
                         | _ -> Some (nr - 1)}
  let html = index_template.Render data
  OK html

let post_page id conn req =
  let data = {posts = one_posts conn id
              next = None
              previous = None}
  let html = index_template.Render data
  OK html

let webhook conn req =
  let mtitle = get_data req "subject"
  let mplain = get_data req "body-plain"
  let mhtml  = get_data req "body-html"
  match (mtitle, mplain, mhtml) with
  | (Some title, _, Some (HtmlBody html)) ->
    create_post conn title html
    OK "Posted"
  | (Some title, Some text, None) ->
    let html = Markdown().Transform(text)
    create_post conn title html
    OK "Posted"
  | _ ->
    NOT_ACCEPTABLE "Missing parameters"

let app = choose [url "/" >>= (index_page |> with_db |> request)
                  url_scan "/page/%d" (fun nr -> archive_page nr |> with_db |> request)
                  url_scan "/post/%d" (fun id -> post_page id |> with_db |> request)
                  url "/webhook" >>= POST >>= (webhook |> with_db |> request)
                  NOT_FOUND "Found no handlers"]

let logger = Loggers.sane_defaults_for Verbose

let port = defaultArg (Option.map uint16 <| env "PORT") 8080us

let cfg = {
  default_config with
    logger = logger
    bindings = [ { scheme = HTTP
                   ip     = System.Net.IPAddress.Parse "0.0.0.0"
                   port   = port } ]
}

web_server cfg app
