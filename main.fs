open ServiceStack.OrmLite
open ServiceStack.OrmLite.PostgreSQL
open ServiceStack.DataAnnotations

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

System.Console.Write("Hallo world\n")
let result = db.Insert(post)
System.Console.Write("Inserted {0}\n", result)
