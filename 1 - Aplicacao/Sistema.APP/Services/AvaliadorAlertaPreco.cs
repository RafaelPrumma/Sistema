using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

// F-H — lógica pura do "o preço cruzou o limiar?" (sem DbContext, testável isolada).
// Modela a histerese de re-disparo: um alerta dispara UMA vez quando o preço cruza o limiar na
// direção configurada e só volta a poder disparar ("re-arma") quando o preço retorna para o outro
// lado do limiar. Isso evita spam a cada execução do job recorrente.
public static class AvaliadorAlertaPreco
{
    // O que o avaliador decidiu para uma regra, dado o preço atual.
    //  Disparar = deve notificar agora (cruzou o limiar e estava armável).
    //  Rearmar  = o preço voltou para o outro lado; limpar o estado de disparo (sem notificar).
    // Quando ambos são false, nada muda (mantém o estado atual).
    public readonly record struct Decisao(bool Disparar, bool Rearmar);

    // jaDisparado: o alerta já está "disparado/armado" (DispararadoEm != null) — não notifica de novo
    // até re-armar. precoAtual: cotação corrente em BRL (> 0 para ser considerada válida).
    public static Decisao Avaliar(DirecaoAlertaPreco direcao, decimal limiar, decimal precoAtual, bool jaDisparado)
    {
        // Sem cotação válida não há decisão (não dispara nem re-arma — evita falso positivo com preço 0).
        if (precoAtual <= 0m)
            return new Decisao(false, false);

        var cruzou = direcao switch
        {
            DirecaoAlertaPreco.Acima => precoAtual >= limiar,
            DirecaoAlertaPreco.Abaixo => precoAtual <= limiar,
            _ => false
        };

        if (cruzou)
            // Cruzou: só dispara se ainda não estava disparado (1 notificação por cruzamento).
            return new Decisao(Disparar: !jaDisparado, Rearmar: false);

        // Não cruzou (voltou do outro lado): re-arma se estava disparado, para o próximo cruzamento notificar.
        return new Decisao(Disparar: false, Rearmar: jaDisparado);
    }

    // Conveniência: avalia direto sobre a entidade (usa o estado persistido DispararadoEm).
    public static Decisao Avaliar(AlertaPreco alerta, decimal precoAtual)
        => Avaliar(alerta.Direcao, alerta.Limiar, precoAtual, alerta.DispararadoEm.HasValue);
}
