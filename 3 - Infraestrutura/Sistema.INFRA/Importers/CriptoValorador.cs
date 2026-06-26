using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco) da VALORAÇÃO em BRL das pernas cripto netadas (F2 — specs/cripto.spec.md §4/§5).
/// Mantida separada do <see cref="FinancasImportador"/> para ser testável sem DbContext, no estilo de
/// <see cref="CriptoNetting"/>.
///
/// O netting (F1) só acerta a QUANTIDADE; a perna saía com preço placeholder (earn = 0; venda = PM
/// corrente; compra de troca = PM corrente ou 1). Isso deixa custo/realizado irreais — e é o que o IR (F3)
/// consome. Aqui cada perna recebe o <b>preço de mercado em BRL na data da operação</b>:
/// - <b>Compra com fiat (BRL):</b> se a linha tem o BRL pago (<c>Total</c>), o custo é esse valor.
/// - <b>Compra / permuta-destino:</b> custo = preço de mercado BRL do ativo na data (em permuta a-mercado
///   ≈ o valor da saída correspondente; aceitável pro IR sem parear pernas).
/// - <b>Venda / permuta-origem:</b> <c>UnitPrice</c> = preço de mercado BRL na data = valor de alienação
///   (ganho = valor − custo médio corrente, resolvido na persistência pelo PM).
/// - <b>Rendimento (earn):</b> custo de aquisição = preço de mercado BRL na data (NÃO 0) — §4.
///
/// Quando não há preço algum (histórico nem cotação) NÃO chuta: marca <see cref="PrecoFaltante"/> (a
/// persistência registra um <see cref="AlertaConfiabilidade"/>), usa um valor neutro (PM corrente ou 0) e
/// rebaixa a confiança para <see cref="NivelConfianca.Baixa"/>.
/// </summary>
public static class CriptoValorador
{
    /// <summary>
    /// Valora uma perna cripto. <paramref name="precoMercadoNaData"/> é o preço de mercado em BRL do ativo
    /// na data (histórico diário com fallback de cotação); <c>null</c> quando não há preço. <paramref
    /// name="totalFiatBrl"/> é o BRL efetivamente pago/recebido na linha (só faz sentido em compra com
    /// fiat); <paramref name="pmCorrente"/> é o preço médio atual do ativo (custo/qtd) usado como neutro.
    /// </summary>
    public static PrecoPerna Valorar(
        MovimentoCriptoCanonico mov,
        decimal? precoMercadoNaData,
        decimal? totalFiatBrl,
        decimal pmCorrente)
    {
        var precoMercado = precoMercadoNaData is > 0m ? precoMercadoNaData.Value : (decimal?)null;

        if (mov.OperationType == TipoOperacaoFinanceira.Rendimento)
        {
            // Earn: custo de aquisição da POSIÇÃO = mercado na data (§4). Sem preço → 0 (neutro) + alerta.
            return precoMercado.HasValue
                ? new PrecoPerna(precoMercado.Value, NivelConfianca.Media, PrecoFaltante: false)
                : new PrecoPerna(0m, NivelConfianca.Baixa, PrecoFaltante: true);
        }

        if (mov.OperationType == TipoOperacaoFinanceira.Compra)
        {
            // Compra com fiat: o BRL pago é o custo direto (mais fiel que o close diário).
            if (totalFiatBrl is > 0m && mov.Quantity > 0m)
                return new PrecoPerna(totalFiatBrl.Value / mov.Quantity, NivelConfianca.Alta, PrecoFaltante: false);
            // Permuta-destino: custo = mercado na data. Sem preço → PM corrente se houver, senão neutro 1.
            if (precoMercado.HasValue)
                return new PrecoPerna(precoMercado.Value, NivelConfianca.Media, PrecoFaltante: false);
            return new PrecoPerna(pmCorrente > 0m ? pmCorrente : 1m, NivelConfianca.Baixa, PrecoFaltante: true);
        }

        // Venda / permuta-origem: valor de alienação = mercado na data. Sem preço → PM corrente + alerta
        // (realizado ≈ 0, não chuta um ganho).
        if (precoMercado.HasValue)
            return new PrecoPerna(precoMercado.Value, NivelConfianca.Media, PrecoFaltante: false);
        return new PrecoPerna(pmCorrente, NivelConfianca.Baixa, PrecoFaltante: true);
    }
}

/// <summary>
/// Resultado da valoração de uma perna: o preço unitário em BRL, o nível de confiança e se faltou preço
/// (a persistência registra o alerta e usa o valor neutro).
/// </summary>
public sealed record PrecoPerna(decimal UnitPrice, NivelConfianca Confianca, bool PrecoFaltante);
