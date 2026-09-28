using System.Text.Json.Serialization;
using Asp.Versioning;
using TronderLeikan.Application.Common;
using TronderLeikan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("TronderLeikan.Sender"))
    .WithMetrics(m => m.AddMeter("TronderLeikan.Sender"));
builder.Services.AddApplication();
// Enums som navn i JSON (f.eks. "Simracing"), tall aksepteres fortsatt på input
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// Versjonen ligger i URL-segmentet (/api/v1/), så en UrlSegmentApiVersionReader er den eneste leseren som trengs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
}).AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("tronderleikan")
    ?? throw new InvalidOperationException("Connection string 'tronderleikan' ikke konfigurert.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi().WithDocumentPerVersion();
else
    // Lokalt kalles API-et over http fra frontend via Aspire; en redirect til https med dev-sertifikat feiler i Node
    app.UseHttpsRedirection();
app.UseStatusCodePages();
app.MapDefaultEndpoints();
app.MapControllers();
app.Run();

// Gjør Program tilgjengelig for WebApplicationFactory i testprosjektet
public partial class Program { }
