using AutoMapper;
using Sistema.CORE.Entities;
using Sistema.APP.DTOs;

namespace Sistema.APP.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Perfil, PerfilDto>().ReverseMap();
        CreateMap<Usuario, UsuarioDto>()
            .ForMember(d => d.SenhaHash, o => o.MapFrom(s => s.SenhaHash))
            .ForMember(d => d.Ativo, o => o.MapFrom(s => s.Ativo))
            .ReverseMap();
    }
}
