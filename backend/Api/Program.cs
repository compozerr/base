using Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

builder.Services.AddCore();

var app = builder.Build();

app.MapCarter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
