using Dociq.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAIChatServices();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Dociq";
        document.Info.Version = "v1";
        document.Info.Description =
            "Dociq is an intelligent document querying API built on Microsoft Extensions AI and Semantic Kernel." +
            " Upload your documents and interact with them through natural language — powered by vector search, " +
            "semantic embeddings, and retrieval-augmented generation.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Raw OpenAPI spec: GET /openapi/v1.json
    app.MapOpenApi();

    // Interactive Swagger UI: GET /scalar/v1
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
