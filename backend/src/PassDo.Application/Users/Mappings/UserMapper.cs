using PassDo.Application.Users.DTOs;
using PassDo.Domain.Entities;

namespace PassDo.Application.Users.Mappings;

public static class UserMapper
{
    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        Role = user.Role.ToString()
    };
}
