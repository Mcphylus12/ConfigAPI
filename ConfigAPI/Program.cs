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
builder.Services.AddSingleton<IConfigCache, MemoryConfigCache>();
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddSingleton<ISchemaService, SchemaService>();
builder.Services.AddKeyedSingleton<IStore>("configStore", new FileStore("./configs"));
builder.Services.AddKeyedSingleton<IStore>("schemaStore", new FileStore("./schemas"));
builder.Services.AddHostedService<StartupConfigLoader>();
builder.Services.AddTransient<IUpdateNotifier, ConsoleNotifier>();

var app = builder.Build();

app.MapOpenApi();

app.UseCors();

app.MapGet("/", () => Results.Content(site, MediaTypeNames.Text.Html));

app.MapPost("/api/schema/{configKey}", async ([FromRoute] string configKey, [FromBody] JsonNode updateDoc, [FromServices] ISchemaService schemaService) =>
{
    await schemaService.Set(configKey, updateDoc);
    return Results.Ok();
});

app.MapGet("/api/schema", async ([FromServices] ISchemaService schemaService) =>
{
    var schemas = await schemaService.Get();
    return Results.Ok(schemas.Select(schema => schema.Key));
});

app.MapGet("/api/schema/{configKey}", async ([FromRoute] string configKey, [FromServices] ISchemaService schemaService) =>
{
    var schema = await schemaService.GetFromSchemaKey(configKey);

    if (schema is null) return Results.NotFound();

    return Results.Ok(schema.Node);
});

app.MapGet("/api/configs", async ([FromServices] IConfigService configService) =>
{
    return Results.Ok(await configService.List());
});

app.MapGet("/api/{configKey}", async ([FromRoute] string configKey, [FromServices] IConfigService configService, [FromQuery] bool shallow = false) =>
{
    return Results.Ok(await configService.Get(configKey, shallow));
});

app.MapPost("/api/{configKey}", async ([FromRoute] string configKey, [FromBody]JsonNode updateDoc, [FromServices] IConfigService configService) =>
{
    try
    {
        await configService.Set(configKey, updateDoc);
        return Results.Ok();
    }
    catch (SchemaValidationException sve)
    {
        return Results.BadRequest(sve.Result);
    }
});


app.Run();