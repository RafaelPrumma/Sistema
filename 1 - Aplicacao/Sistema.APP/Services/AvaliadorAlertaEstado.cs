namespace Sistema.APP.Services;

// F-H — lógica PURA (sem DbContext) dos alertas de ESTADO acrescentados no F-H:
//  • quais ativos detidos não estão em nenhuma carteira ativa ("sem carteira");
//  • se a divergência calculado×custódia da reconciliação B3 passou do limiar configurado.
// Mantida isolada para ser testável sem banco (estilo AvaliadorAlertaPreco/ClassificadorSaudeCotacao).
// O serviço (camada INFRA) só lê os read models, monta os conjuntos e delega a DECISÃO aqui.
public static class AvaliadorAlertaEstado
{
    // Ativos detidos (posição > 0) que NÃO aparecem em nenhuma carteira ativa.
    //  detidos     = ids de ativos com Quantidade > 0 (já filtrado pelo chamador).
    //  emCarteira  = ids de ativos vinculados a alguma CarteiraAtivoFinanceiro com Ativo=true.
    // Devolve, em ordem estável, os ids detidos ausentes das carteiras (candidatos ao alerta "sem carteira").
    public static IReadOnlyList<int> AtivosSemCarteira(IEnumerable<int> detidos, ISet<int> emCarteira)
    {
        var resultado = new List<int>();
        foreach (var id in detidos)
        {
            if (!emCarteira.Contains(id))
                resultado.Add(id);
        }
        return resultado;
    }

    // Decide se a divergência calculado×custódia (reconciliação B3) merece alerta.
    //  valorDivergencia = valor (BRL) parado no ativo VARIAÇÃO = a diferença não explicada (pode ser ±).
    //  patrimonioReferencia = base BRL (>0) para o cálculo do percentual; <=0 desabilita a regra de %.
    //  limiarAbsoluto = limiar em BRL no MÓDULO da divergência (<=0 desabilita a regra absoluta).
    //  limiarPercentual = limiar em % (ex.: 5 = 5%) sobre o patrimônio de referência (<=0 desabilita).
    // Dispara se QUALQUER limiar habilitado for ultrapassado (em módulo). Sem limiar habilitado, não dispara.
    public static bool DivergenciaAcimaDoLimiar(
        decimal valorDivergencia,
        decimal patrimonioReferencia,
        decimal limiarAbsoluto,
        decimal limiarPercentual)
    {
        var modulo = Math.Abs(valorDivergencia);
        if (modulo <= 0m)
            return false;

        if (limiarAbsoluto > 0m && modulo >= limiarAbsoluto)
            return true;

        if (limiarPercentual > 0m && patrimonioReferencia > 0m)
        {
            var pct = modulo / patrimonioReferencia * 100m;
            if (pct >= limiarPercentual)
                return true;
        }

        return false;
    }
}
