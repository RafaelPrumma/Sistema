namespace Sistema.CORE.Entities;

public enum PublicacaoTipo
{
    MensagemDireta = 1,
    PostSetor = 2,
    Aviso = 3
}

public enum PublicacaoStatus
{
    Ativa = 1,
    Oculta = 2,
    Excluida = 3,
    Expirada = 4
}

public enum AvisoAudiencia
{
    Todos = 1,
    Setor = 2,
    Grupo = 3,
    Usuarios = 4
}

public enum AvisoPrioridade
{
    Baixa = 1,
    Normal = 2,
    Alta = 3,
    Critica = 4
}

public enum TipoReacao
{
    Curtir = 1,
    Aplaudir = 2,
    Apoiar = 3,
    Interessante = 4
}
