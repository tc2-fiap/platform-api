using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Services;
using FiapGames.Platform.Api.Endpoints;
using FiapGames.Platform.Api.Infrastructure.Kubernetes;
using FiapGames.Shared.Infrastructure.Extensions;
using k8s;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddGlobalExceptionHandling();

// In-cluster only — reads the mounted ServiceAccount token/CA cert
// automatically. This service has no meaningful standalone/local-dev mode:
// listing pods only makes sense when actually running inside the cluster
// whose pods it's listing.
builder.Services.AddSingleton<IKubernetes>(_ =>
    new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));

var podsNamespace = builder.Configuration["Kubernetes:Namespace"] ?? "fiap-games";
builder.Services.AddScoped<IPodReader>(sp => new KubernetesPodReader(sp.GetRequiredService<IKubernetes>(), podsNamespace));
builder.Services.AddScoped<IPodService, PodService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FIAP Games — Platform API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT token."
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(document =>
    {
        var requirement = new OpenApiSecurityRequirement();
        requirement.Add(new OpenApiSecuritySchemeReference("Bearer", document, null), []);
        return requirement;
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/version", () => Results.Ok(new
{
    sha = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "unknown",
    buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown"
}));

app.MapPlatformEndpoints();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
