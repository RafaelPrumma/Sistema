using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;

namespace Sistema.APP.Services;

public class PosicaoAtivoProjectionService(IUnitOfWork uow) : IPosicaoAtivoProjectionService
{
    private const string VersaoCalculo = "posicao-ativo-v1";

    private readonly IUnitOfWork _uow = uow;

    public async Task RecalcularAsync(CancellationToken cancellationToken = default)
    {
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var calculadas = CalculadoraPosicaoAtivo.Calcular(transacoes);
        var agora = DateTime.UtcNow;

        var posicoes = calculadas.Select(posicao => new PosicaoAtivo
        {
            AtivoFinanceiroId = posicao.AtivoFinanceiroId,
            Quantidade = posicao.Quantidade,
            PrecoMedio = posicao.PrecoMedio,
            CustoTotal = posicao.CustoTotal,
            TotalComprado = posicao.TotalComprado,
            TotalVendido = posicao.TotalVendido,
            ResultadoRealizado = posicao.ResultadoRealizado,
            UltimaOperacaoEm = posicao.UltimaOperacaoEm,
            Status = posicao.Status,
            CalculadoEm = agora,
            VersaoCalculo = VersaoCalculo,
            UsuarioInclusao = "financeiro-projecao"
        }).ToList();

        await _uow.Financas.SubstituirPosicoesAtivosAsync(posicoes, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
    }
}
