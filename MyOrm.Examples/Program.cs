using MyOrm.SqlClient.Repository;
using MyOrm.Examples;
using MyOrm.Examples.Models;

var connectionString = "your-sql-connection-string-here";
var repo = new SqlRepository<User>(connectionString);

var newUser = new User
{
    Name = "John Doe",
    Age = 30
};

repo.Insert(newUser);
Console.WriteLine("User inserted!");
