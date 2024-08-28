
using FluentValidation;
using FluentValidation.Results;
using Library.Api;
using Library.Api.Auth;
using Library.Api.Data;
using Library.Api.Endpoints.Internal;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    //WebRootPath = "./wwwroot",
    //EnvironmentName = Environment.GetEnvironmentVariable("env"),
    //ApplicationName = "Library.Api"
});

//builder.Services.Configure<JsonOptions>(options =>
//{
//    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
//    options.JsonSerializerOptions.IncludeFields = true;
//});

// Define Auth based on ApiKey scheme
builder.Services.AddAuthentication(ApiKeySchemeConstants.SchemeName)
    .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(ApiKeySchemeConstants.SchemeName, _ => { });
builder.Services.AddAuthorization();
//****************


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//DI Registration
builder.Services.AddSingleton<IDbConnectionFactory>(_ => 
    new SqliteConnectionFactory(
        builder.Configuration.GetValue<string>("Database:ConnectionString")!));
builder.Services.AddSingleton<DatabaseInitializer>();

builder.Services.AddEndpoints<IApiMarker>(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<IApiMarker>();
//****************


var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.UseEndpoints<IApiMarker>();

//Db Init
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();
//****************

app.Run();
