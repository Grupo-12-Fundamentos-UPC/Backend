using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Notifications.Requests;
using HairyPaws.Domain.Notifications.Enums;

namespace HairyPaws.Application.Notifications.Queries.GetNotifications;

public sealed class NotificationsQueryParametersValidator : AbstractValidator<NotificationsQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "type", "isRead"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public NotificationsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Type)
            .MustBeEnumValueWhenProvided<NotificationsQueryParameters, NotificationType>();

        RuleFor(static query => query.SortBy)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(static query => query.SortDirection)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortDirections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
