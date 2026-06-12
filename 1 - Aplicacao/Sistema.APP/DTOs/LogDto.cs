namespace Sistema.APP.DTOs;

public record LogDto(
    int Id,
    DateTime DataOperacao,
    string Modulo,
    string Tipo,
    string Entidade,
    string Operacao,
    bool Sucesso,
    string Usuario,
    string Mensagem);
