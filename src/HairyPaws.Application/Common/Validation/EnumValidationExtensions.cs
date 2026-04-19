using FluentValidation;

namespace HairyPaws.Application.Common.Validation;

public static class EnumValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeEnumValue<T, TEnum>(this IRuleBuilder<T, string> ruleBuilder)
        where TEnum : struct, Enum
    {
        return ruleBuilder.Must(static value => Enum.TryParse<TEnum>(value, true, out _))
            .WithMessage($"'{typeof(TEnum).Name}' has an invalid value.");
    }

    public static IRuleBuilderOptions<T, string?> MustBeEnumValueWhenProvided<T, TEnum>(this IRuleBuilder<T, string?> ruleBuilder)
        where TEnum : struct, Enum
    {
        return ruleBuilder.Must(static value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<TEnum>(value, true, out _))
            .WithMessage($"'{typeof(TEnum).Name}' has an invalid value.");
    }
}
