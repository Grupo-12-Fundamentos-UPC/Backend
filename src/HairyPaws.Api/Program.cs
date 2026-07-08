using FluentValidation.AspNetCore;
using HairyPaws.Api.Common.Health;
using HairyPaws.Api.Common.Extensions;
using HairyPaws.Api.Common.Middleware;
using HairyPaws.Api.Common.Options;
using HairyPaws.Application;
using HairyPaws.Infrastructure;
using HairyPaws.Infrastructure.Persistence;
using HairyPaws.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiBehavior();
builder.Services.AddJwtAuthentication();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();
var storageOptions = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
var uploadsPath = storageOptions.GetUploadsRoot(app.Environment.ContentRootPath);
Directory.CreateDirectory(uploadsPath);
var deploymentOptions = builder.Configuration.GetSection(DeploymentOptions.SectionName).Get<DeploymentOptions>() ?? new DeploymentOptions();
var enableDeveloperDefaults = app.Environment.IsDevelopment()
    || app.Environment.IsEnvironment("Testing")
    || app.Environment.IsEnvironment("Academic");
var enableSwagger = deploymentOptions.EnableSwagger || enableDeveloperDefaults;
var runMigrations = deploymentOptions.RunMigrations || enableDeveloperDefaults;
var seedAdmin = deploymentOptions.SeedAdmin || enableDeveloperDefaults;

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

if (runMigrations || seedAdmin)
{
    await app.Services.InitializeAsync(runMigrations, seedAdmin);
}

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hairy Paws API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapGet("/", static () => Results.Redirect("/swagger")).WithTags("Documentation");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = static _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync
}).WithTags("Health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = static registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
}).WithTags("Health");
app.MapControllers();

app.Run();

public partial class Program;
