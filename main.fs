open ServiceStack.OrmLite
open ServiceStack.OrmLite.PostgreSQL
open ServiceStack.DataAnnotations
open Suave.Web
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Http.RequestErrors
open Suave.Types
open Suave.Utils
open Mustache

let factory = new OrmLiteConnectionFactory("Server=127.0.0.1;Port=5432;User Id=pepijn;Password=password;Database=suaveblog;", PostgreSqlDialect.Provider)

let db = factory.OpenDbConnection()

[<CLIMutable>]
type Blogpost = {
  [<AutoIncrement>]
  Id : int
  Title : string
  Body : string
  Date : System.DateTime
}
db.CreateTable<Blogpost>()

let create_post title body =
  ignore <| db.Insert {
    Id = 0
    Title = title
    Body = body
    Date = System.DateTime.Now
  }

let compiler = FormatCompiler()
let index_template = compiler.Compile (System.IO.File.ReadAllText "templates/index.html")
type IndexData = {
  Posts : System.Collections.Generic.List<Blogpost>
}

let index_page req =
  let data = {Posts = db.Select<Blogpost>("SELECT * FROM blogpost ORDER BY date DESC LIMIT 3")}
  let html = index_template.Render data
  OK html

let webhook req =
  let mtitle = look_up req.form "subject"
  let mtext = look_up req.form "body-plain"
  match [mtitle; mtext] with
  | [Some title; Some text] -> create_post title text
  | _ -> ()
  OK "OK"

let app = choose [url "/" >>= request index_page
                  url "/webhook" >>= POST >>= request webhook
                  NOT_FOUND "Found no handlers"]

let cfg = {
  default_config with
    bindings = [ { scheme = HTTP
                   ip     = System.Net.IPAddress.Parse "0.0.0.0"
                   port   = 8080us } ]
}

web_server cfg app
