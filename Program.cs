using Dociq.DependencyInjection;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/dociq-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddAIChatServices();
builder.Services.AddRagServices();

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title   = "Dociq";
        document.Info.Version = "v1";
        document.Info.Description =
            "Dociq is an intelligent document querying API built on Microsoft Extensions AI and Semantic Kernel. " +
            "Upload PDF documents and interact with them through natural language — powered by hybrid vector search " +
            "(dense + sparse RRF fusion), semantic embeddings, and retrieval-augmented generation with citations.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Raw OpenAPI spec: GET /openapi/v1.json
    app.MapOpenApi();

    // Interactive API docs: GET /scalar/v1
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Dociq API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
