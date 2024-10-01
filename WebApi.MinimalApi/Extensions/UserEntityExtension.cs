using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Extensions;

public static class UserEntityExtension
{
    public static UserDto ToUserDto(this UserEntity user) 
        => new()
        {
            Id = user.Id,
            CurrentGameId = user.CurrentGameId,
            FullName = $"{user.LastName} {user.FirstName}",
            GamesPlayed = user.GamesPlayed,
            Login = user.Login
        };
}