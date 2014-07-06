open ServiceStack.OrmLite
open ServiceStack.OrmLite.PostgreSQL
open ServiceStack.DataAnnotations
open Suave.Web
open Suave.Http.Successful
open DotLiquid

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

let post = {
  Id = 0
  Title = "Hello world"
  Body = "foo bar baz"
  Date = System.DateTime.Now
}

web_server default_config  (OK "Hello World")
