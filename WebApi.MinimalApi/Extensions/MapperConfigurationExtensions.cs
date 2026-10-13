using AutoMapper;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Extensions;

public static class MapperConfigurationExtensions
{
    public static void Setup(this IMapperConfigurationExpression config)
    {
        config.SetupUserEntityToResponse();
        config.SetupUserCreationRequestToEntity();
        config.SetupUserUpdateRequestToEntity();
        config.SetupUserToUserUpdateRequest();
    }

    private static void SetupUserEntityToResponse(this IMapperConfigurationExpression config)
    {
        config
            .CreateMap<UserEntity, UserResponse>()
            .ForMember(
                dest => dest.FullName,
                options => options.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    }

    private static void SetupUserCreationRequestToEntity(this IMapperConfigurationExpression config)
    {
        config.CreateMap<UserCreationRequest, UserEntity>();
    }

    private static void SetupUserUpdateRequestToEntity(this IMapperConfigurationExpression config)
    {
        config.CreateMap<UserUpdateRequest, UserEntity>();
    }

    private static void SetupUserToUserUpdateRequest(this IMapperConfigurationExpression config)
    {
        config.CreateMap<UserEntity, UserUpdateRequest>();
    }
}