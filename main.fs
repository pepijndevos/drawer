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

let all_posts conn =
  let cmd = new NpgsqlCommand("SELECT id, title, body, date FROM blogpost ORDER BY date DESC LIMIT 4", conn)
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

let compiler = FormatCompiler()
let index_template = compiler.Compile (System.IO.File.ReadAllText "templates/index.html")
type IndexData = {
  posts : Blogpost list
}

let index_page conn req =
  let data = {posts = all_posts conn}
  let html = index_template.Render data
  OK html

let webhook conn req =
  let mtitle = get_first req.multipart_fields "subject"
  let mplain = get_first req.multipart_fields "body-plain"
  let mhtml  = get_first req.multipart_fields "body-html"
  match (mtitle, mplain, mhtml) with
  | (Some title, _, Some html) ->
    create_post conn title html
    OK "Posted"
  | (Some title, Some text, None) ->
    let html = Markdown().Transform(text)
    create_post conn title html
    OK "Posted"
  | _ ->
    NOT_ACCEPTABLE "Missing parameters"

let app = choose [url "/" >>= (index_page |> with_db |> request)
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
