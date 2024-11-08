using Template;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => ExampleClass.ExampleMethod());

app.Run();
