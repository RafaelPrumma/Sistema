namespace Sistema.APP.Services.Interfaces;

// F-H — motor de alertas. Roda no job recorrente "financas-alertas". Tipos:
//  • Preço: regras de AlertaPreco contra a cotação atual (BRL) com histerese de re-disparo.
//  • Provento: provento recém-registrado de ativo detido.
//  • Cotação vencida/sem fonte: ativo detido cuja cotação está em estado crítico (ClassificadorSaudeCotacao).
//  • Ativo sem carteira: ativo detido (posição>0) fora de qualquer carteira ativa.
//  • Divergência custódia: reconciliação B3 (valor no ativo VARIAÇÃO) acima de limiar configurável.
// Os três últimos são condições de ESTADO: dedup/re-arme por marcador AlertaConfiabilidade.
// À prova de falha (try-catch por bloco e por item): uma regra ruim nunca derruba o job nem impede as demais.
public interface IFinancasAlertaService
{
    Task ProcessarAlertasAsync(CancellationToken cancellationToken = default);
}
