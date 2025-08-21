
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NCEZ.Simulator.Services;
using NCEZ.Simulator.Filters;

var builder = WebApplication.CreateBuilder(args);

// Config
var dataRoot = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Simulator:DataRoot"] ?? "Data/runtime");
Directory.CreateDirectory(dataRoot);

// Services
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => { o.SuppressModelStateInvalidFilter = true; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NCEZ Simulator API",
        Version = "v1",
        Description = "Simulátor standardů EZ: Notifikační služby, Registr oprávnění, Žurnál činností, Dočasné úložiště, ePosudky, EZCA, EZKarta, eŽádanky, Katalog služeb EZ, Kmenové registry, NPEZ, Sdílený zdravotní záznam, Afinitní domény, Testovací rámec."
    });
});

builder.Services.AddSingleton<SystemClock>();
builder.Services.AddSingleton<IdGenerator>();
builder.Services.AddSingleton(new StorageOptions(dataRoot));
builder.Services.AddSingleton(typeof(IJsonRepository<>), typeof(JsonRepository<>));
builder.Services.AddSingleton<CodeSetService>();

builder.Services.AddMvc(options =>
{
    options.Filters.Add<ValidateModelFilter>();
});

var app = builder.Build();

// Pipeline
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        var problem = new ProblemDetails
        {
            Type = "https://http.dev/errors/internal-error",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? feature?.Error.ToString() : "Unexpected error."
        };
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? 500;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NCEZ Simulator v1");
    c.RoutePrefix = "swagger"; // místo string.Empty
});

app.UseStaticFiles();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
   .WithTags("System");

app.Run();

namespace NCEZ.Simulator.Services
{
    public sealed class StorageOptions
    {
        public StorageOptions(string dataRoot) => DataRoot = dataRoot;
        public string DataRoot { get; }
    }
    public sealed class SystemClock
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }
    public sealed class IdGenerator
    {
        public string NewId() => Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }
}
