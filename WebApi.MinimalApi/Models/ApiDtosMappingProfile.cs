using AutoMapper;
using WebApi.MinimalApi.Domain;

namespace WebApi.MinimalApi.Models;

public class ApiDtosMappingProfile : Profile
{
    public ApiDtosMappingProfile()
    {
        CreateMap<UserEntity, UserDto>()
            .ForMember(x => x.FullName,
                x => x.MapFrom(entity => $"{entity.LastName} {entity.FirstName}"));
        
        CreateMap<CreateUserRequest, UserEntity>();

        CreateMap<UpdateUserRequest, UserEntity>();

        CreateMap<PatchUserRequest, UserEntity>();
    }
}