using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public static class CalculadoraPosicaoAtivo
{
    private const decimal Epsilon = 0.000000000001m;

    public static IReadOnlyList<ResultadoPosicaoAtivo> Calcular(IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var estado = new Dictionary<int, ResultadoPosicaoAtivo>();
        foreach (var transacao in transacoes.Where(t => t.Asset is not null).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            if (!estado.TryGetValue(transacao.AssetId, out var posicao))
            {
                posicao = new ResultadoPosicaoAtivo(transacao.AssetId, transacao.Asset!);
                estado[transacao.AssetId] = posicao;
            }

            posicao.UltimaOperacaoEm = transacao.Date;
            var delta = DeltaQuantidade(transacao);
            if (delta > 0m)
            {
                var custoEntrada = transacao.Quantity * transacao.UnitPrice + transacao.Fees;
                posicao.CustoTotal += custoEntrada;
                posicao.TotalComprado += custoEntrada;
                posicao.Quantidade += transacao.Quantity;
            }
            else if (delta < 0m)
            {
                var precoMedio = posicao.Quantidade > 0m ? posicao.CustoTotal / posicao.Quantidade : 0m;
                var quantidadeBaixada = Math.Min(transacao.Quantity, posicao.Quantidade);
                var custoBaixado = quantidadeBaixada * precoMedio;
                var valorVenda = transacao.Quantity * transacao.UnitPrice - transacao.Fees;

                posicao.CustoTotal -= custoBaixado;
                posicao.TotalVendido += valorVenda;
                posicao.ResultadoRealizado += valorVenda - custoBaixado;
                posicao.Quantidade -= transacao.Quantity;

                if (posicao.Quantidade <= Epsilon)
                {
                    posicao.Quantidade = 0m;
                    posicao.CustoTotal = 0m;
                }
            }
        }

        foreach (var posicao in estado.Values)
        {
            posicao.PrecoMedio = posicao.Quantidade > 0m ? posicao.CustoTotal / posicao.Quantidade : 0m;
            posicao.Status = posicao.Quantidade > Epsilon
                ? StatusPosicaoAtivo.Aberta
                : posicao.Quantidade < 0m ? StatusPosicaoAtivo.Inconsistente : StatusPosicaoAtivo.Encerrada;
        }

        return estado.Values.OrderBy(x => x.AtivoFinanceiro.Sigla ?? x.AtivoFinanceiro.Chave).ToList();
    }

    private static decimal DeltaQuantidade(TransacaoFinanceira transacao) => transacao.OperationType switch
    {
        TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => transacao.Quantity,
        TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -transacao.Quantity,
        _ => 0m
    };
}

public sealed class ResultadoPosicaoAtivo(int ativoFinanceiroId, AtivoFinanceiro ativoFinanceiro)
{
    public int AtivoFinanceiroId { get; } = ativoFinanceiroId;
    public AtivoFinanceiro AtivoFinanceiro { get; } = ativoFinanceiro;
    public decimal Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal CustoTotal { get; set; }
    public decimal TotalComprado { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal ResultadoRealizado { get; set; }
    public DateTime? UltimaOperacaoEm { get; set; }
    public StatusPosicaoAtivo Status { get; set; } = StatusPosicaoAtivo.Encerrada;
}
