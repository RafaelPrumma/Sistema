using AutoMapper;
using Sistema.CORE.Entities;
using Sistema.APP.DTOs;

namespace Sistema.APP.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Perfil, PerfilDto>().ReverseMap();
        CreateMap<Usuario, UsuarioDto>().ReverseMap();
        CreateMap<Funcionalidade, FuncionalidadeDto>().ReverseMap();
    }
}
