using AutoMapper;
using Sistema.CORE.Entities;
using Sistema.APP.DTOs;

namespace Sistema.APP.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Perfil, PerfilDto>().ReverseMap();
    }
}
