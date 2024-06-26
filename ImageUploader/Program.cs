using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using Newtonsoft.Json;
using System;
using System.IO;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5500");
        });
});

var app = builder.Build();
app.UseStaticFiles();
app.UseCors();


app.MapGet("/test", () => "Hello World!");

var uploadsDirectory = Path.Combine("wwwroot", "uploads");
if (!Directory.Exists(uploadsDirectory))
{
    Directory.CreateDirectory(uploadsDirectory);
}

var dataDirectory = "Data";
if (!Directory.Exists(dataDirectory))
{
    Directory.CreateDirectory(dataDirectory);
}

app.MapPost("/upload", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    string title = context.Request.Form["title"];
    var file = context.Request.Form.Files["file"];

    if (file is null || file.Length == 0 || string.IsNullOrEmpty(title))
    {
        return Results.BadRequest("Title and image are required.");
    }

    var id = Guid.NewGuid().ToString();

    var fileName = $"{id}_{file.FileName}";
    var filePath = Path.Combine("wwwroot", "uploads", fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var imageData = new UploadedImage
    {
        Id = id,
        Title = title,
        FileName = fileName
    };

    var jsonData = JsonConvert.SerializeObject(imageData);
    var jsonFilePath = Path.Combine("Data", $"{id}.json");
    File.WriteAllText(jsonFilePath, jsonData);

    return Results.Ok(new { id = id });
});

app.MapGet("/picture/{id}", (string id) =>
{
    var jsonFilePath = Path.Combine("Data", $"{id}.json");
    if (!File.Exists(jsonFilePath))
    {
        return Results.NotFound();
    }

    var jsonData = File.ReadAllText(jsonFilePath);
    var imageData = JsonConvert.DeserializeObject<UploadedImage>(jsonData);

    var encodedTitle = HtmlEncoder.Default.Encode(imageData.Title); // Escape HTML entities
    var htmlContent = $@"<!DOCTYPE html>
        <html>
        <head>
            <title>{encodedTitle}</title>
        </head>
        <body>
            <h1>{encodedTitle}</h1>
            <img src=""/uploads/{imageData.FileName}"" alt=""{encodedTitle}"" />
        </body>
        </html>";

    return Results.Ok(htmlContent);
});

app.Run("http://localhost:3000");


public class UploadedImage
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string FileName { get; set; }
}