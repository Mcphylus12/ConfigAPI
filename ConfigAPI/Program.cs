using ConfigAPI;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json.Nodes;

var site = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Index.html"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddHostedService<BootLoader>();

var app = builder.Build();

app.MapOpenApi();

app.UseCors();

app.MapGet("/", () => Results.Content(site, MediaTypeNames.Text.Html));

//MapSchemaRoutes(app);

MapConfigRoutes(app);


app.Run();

//static void MapSchemaRoutes(WebApplication app)
//{
//    app.MapPost("/api/config/{configKey}", async ([FromRoute] string configKey, [FromBody] JsonNode updateDoc, [FromServices] ISchemaService configService) =>
//    {
//        await configService.Set(configKey, new JsonSchema(updateDoc));
//        return Results.Ok();
//    });

//    app.MapGet("/api/config", async ([FromServices] ISchemaService configService, [FromQuery] string? prefix) =>
//    {
//        var configs = await configService.List(prefix);
//        return Results.Ok(configs);
//    });

//    app.MapGet("/api/config/{configKey}", async ([FromRoute] string configKey, [FromServices] ISchemaService configService) =>
//    {
//        var config = await configService.Get(configKey);
//        if (config is null) return Results.NotFound();
//        return Results.Ok(config.ToJsonString());
//    });

//    app.MapDelete("/api/config/{configKey}", ([FromRoute] string configKey, [FromServices] ISchemaService configService) => configService.Delete(configKey));
//}

static void MapConfigRoutes(WebApplication app)
{
    app.MapPost("/api/config/{configKey}", async ([FromRoute] string configKey, [FromBody] JsonNode updateDoc, [FromServices] ConfigService configService) =>
    {
        await configService.Set(configKey, new JsonConfig(updateDoc));
        return Results.Ok();
    });

    app.MapGet("/api/config", async ([FromServices] ConfigService configService, [FromQuery] string? prefix) =>
    {
        var configs = await configService.List(prefix);
        return Results.Ok(configs);
    });

    app.MapGet("/api/config/{configKey}", async ([FromRoute] string configKey, [FromQuery]bool shallow = false, [FromServices] ConfigService configService) =>
    {
        var config = await configService.Get(configKey, shallow);
        if (config is null) return Results.NotFound();
        return Results.Ok(config.ToJsonString());
    });

    app.MapDelete("/api/config/{configKey}", ([FromRoute] string configKey, [FromServices] ConfigService configService) => configService.Delete(configKey));
}