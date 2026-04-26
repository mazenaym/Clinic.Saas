using Microsoft.AspNetCore.Identity;

var password = args.Length > 0 ? args[0] : "Swagger123!";
var hasher = new PasswordHasher<object>();
Console.WriteLine(hasher.HashPassword(new object(), password));
