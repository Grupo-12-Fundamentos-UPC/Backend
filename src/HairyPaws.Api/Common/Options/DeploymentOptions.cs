namespace HairyPaws.Api.Common.Options;

public sealed class DeploymentOptions
{
    public const string SectionName = "Deployment";

    public bool EnableSwagger { get; init; }

    public bool RunMigrations { get; init; }

    public bool SeedAdmin { get; init; }
}
