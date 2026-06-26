namespace Sistema.APP.Services.Interfaces;

// F-H — motor de alertas de preço/provento. Roda no job recorrente "financas-alertas":
// avalia as regras de AlertaPreco contra a cotação atual (BRL) e, quando o preço cruza o limiar na
// direção configurada, dispara notificação interna (IMensagemAppService) + marca o re-disparo; e
// detecta proventos recém-registrados de ativos detidos para notificar. À prova de falha
// (try-catch por regra/provento): uma regra ruim nunca derruba o job nem impede as demais.
public interface IFinancasAlertaService
{
    Task ProcessarAlertasAsync(CancellationToken cancellationToken = default);
}
