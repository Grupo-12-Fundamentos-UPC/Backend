using System.Diagnostics;
using HairyPaws.Contracts.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Common.Extensions;

public static class ApiBehaviorExtensions
{
    public static IServiceCollection AddApiBehavior(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(static entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        static entry => entry.Key,
                        static entry => entry.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The input was invalid." : error.ErrorMessage)
                            .ToArray());

                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                var response = new ErrorResponse(
                    "VALIDATION_ERROR",
                    "One or more validation errors occurred.",
                    details,
                    traceId);

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }
}
