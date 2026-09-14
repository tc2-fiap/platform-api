using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Services;
using FiapGames.Platform.Api.Consumers;
using FiapGames.Platform.Api.Endpoints;
using FiapGames.Platform.Api.Infrastructure.Kubernetes;
using FiapGames.Shared.Infrastructure.Extensions;
using k8s;
using MassTransit;
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
builder.Services.AddScoped<IDeploymentRestarter>(sp => new KubernetesDeploymentRestarter(sp.GetRequiredService<IKubernetes>(), podsNamespace));
builder.Services.AddScoped<IDeploymentService, DeploymentService>();

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

// platform-api otherwise has no RabbitMQ integration at all — it owns no
// schema and sits outside the purchase-flow event system entirely (see
// notes.md 75). This is the one exception, added purely to receive
// TokenRevokedEvent so a revoked Admin token stops working against this
// service's pod-introspection endpoints too.
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TokenRevokedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"] ?? "localhost",
            builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });

        // Explicit, service-scoped endpoint name — see orders-api's
        // Program.cs for why relying on MassTransit's default naming
        // (which ignores the namespace) is unsafe once two services
        // declare a same-named consumer class for the same event.
        cfg.ReceiveEndpoint("platform-api-token-revoked", e =>
        {
            e.ConfigureConsumer<TokenRevokedConsumer>(context);
        });
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

// Shared by two routes below: the bare /version (unauthenticated, reached
// only via kubectl port-forward — not under /api/* so the Ingress can't
// route to it) and /api/platform/version (Admin-gated, reached through the
// Ingress like any other route — the admin dashboard's source for this).
// Same handler, not duplicated logic, registered at two paths because
// nothing else makes the unauthenticated one reachable from a browser.
static IResult GetVersion() => Results.Ok(new
{
    sha = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "unknown",
    buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown"
});

app.MapHealthChecks("/health");
app.MapGet("/version", GetVersion);

app.MapPlatformEndpoints(GetVersion);

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
