open ServiceStack.OrmLite
open ServiceStack.OrmLite.PostgreSQL
open ServiceStack.DataAnnotations
open Suave
open Suave.Web
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Types
open Suave.Session
open Suave.Log
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
    db.Insert {
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

let index_page wp =
    let data = {Posts = db.Select<Blogpost>()}
    let html = index_template.Render data
    OK html wp

let app = choose [url "/" >>= index_page
                  url "/random" >>= (OK "test")]

web_server default_config  app
