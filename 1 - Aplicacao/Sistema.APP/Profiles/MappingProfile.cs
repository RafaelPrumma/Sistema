using AutoMapper;
using Sistema.CORE.Entities;
using Sistema.APP.DTOs;

namespace Sistema.APP.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Perfil, PerfilDto>().ReverseMap();
        CreateMap<Funcionalidade, FuncionalidadeDto>().ReverseMap();
        CreateMap<Usuario, UsuarioDto>()
            .ForMember(d => d.SenhaHash, o => o.MapFrom(s => s.SenhaHash))
            .ForMember(d => d.Ativo, o => o.MapFrom(s => s.Ativo))
            .ReverseMap();
        CreateMap<Configuracao, ConfiguracaoDto>().ReverseMap();

        CreateMap<Mensagem, MensagemDto>()
            .ForMember(d => d.RemetenteNome, o => o.MapFrom(s => s.Remetente != null ? s.Remetente.Nome : "Sistema"))
            .ForMember(d => d.DestinatarioNome, o => o.MapFrom(s => s.Destinatario != null ? s.Destinatario.Nome : "Audiência"))
            .ForMember(d => d.PerfilNome, o => o.MapFrom(s => s.Perfil != null ? s.Perfil.Nome : string.Empty))
            .ForMember(d => d.Reacoes, o => o.MapFrom(s => s.Reacoes
                .GroupBy(r => r.TipoReacao)
                .ToDictionary(g => g.Key, g => g.Count())));

        CreateMap<Mensagem, MensagemThreadDto>()
            .ForMember(d => d.RemetenteNome, o => o.MapFrom(s => s.Remetente != null ? s.Remetente.Nome : "Sistema"))
            .ForMember(d => d.DestinatarioNome, o => o.MapFrom(s => s.Destinatario != null ? s.Destinatario.Nome : "Audiência"))
            .ForMember(d => d.PerfilNome, o => o.MapFrom(s => s.Perfil != null ? s.Perfil.Nome : string.Empty))
            .ForMember(d => d.Reacoes, o => o.MapFrom(s => s.Reacoes
                .GroupBy(r => r.TipoReacao)
                .ToDictionary(g => g.Key, g => g.Count())))
            .ForMember(d => d.Respostas, o => o.MapFrom(s => s.Respostas));

        CreateMap<NovaMensagemDto, Mensagem>();
    }
}
