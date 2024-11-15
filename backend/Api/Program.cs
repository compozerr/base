using Core;
using Core.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

builder.Services.AddCore();

var app = builder.Build();

app.MapCarter();
app.UseCors(AppConstants.CorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
