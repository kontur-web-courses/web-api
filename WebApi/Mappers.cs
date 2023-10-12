using AutoMapper;
using Game.Domain;
using WebApi.Models;

namespace WebApi;

public class Mappers : Profile
{
    public Mappers()
    {
        CreateMap<UserEntity, UserDto>()
            .ForMember(dest => dest.FullName,
                opt
                    => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    }
}