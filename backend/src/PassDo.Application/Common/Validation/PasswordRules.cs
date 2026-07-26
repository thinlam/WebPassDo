using FluentValidation;

namespace PassDo.Application.Common.Validation;

/// <summary>
/// Shared password strength policy used by Register and ChangePassword.
/// Requires: min 8 chars, at least one lowercase, one uppercase, one digit and one special character.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 100;
    public const string SpecialCharacters = "!@#$%^&*";

    public const string RequirementsMessage =
        "Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a digit, and a special character (!@#$%^&*).";

    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(MinimumLength)
            .MaximumLength(MaximumLength)
            .Must(HasLowercase).WithMessage(RequirementsMessage)
            .Must(HasUppercase).WithMessage(RequirementsMessage)
            .Must(HasDigit).WithMessage(RequirementsMessage)
            .Must(HasSpecialCharacter).WithMessage(RequirementsMessage);
    }

    private static bool HasLowercase(string value) => value.Any(char.IsLower);

    private static bool HasUppercase(string value) => value.Any(char.IsUpper);

    private static bool HasDigit(string value) => value.Any(char.IsDigit);

    private static bool HasSpecialCharacter(string value) => value.Any(SpecialCharacters.Contains);
}
