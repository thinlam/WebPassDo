namespace PassDo.Api.Contracts.Users;

public record UpdateUserRequest(
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl);
