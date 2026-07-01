using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;

namespace Sistema.INFRA.Data.Seeds;

/// <summary>
/// Seed do submódulo Gastos (G1): categorias comuns + regras de auto-categorização iniciais.
/// Idempotente via ChaveNatural (categoria = nome normalizado; regra = padrão + tipo de match).
/// A tela de correção que APRENDE novas regras é a fase G2 — aqui só o seed inicial.
/// </summary>
public static class GastosSeed
{
    /// <summary>Categorias comuns (Despesa e Receita). Nome é a chave natural normalizada.</summary>
    public static IEnumerable<CategoriaGasto> GetCategorias()
    {
        (string Nome, TipoCategoriaGasto Tipo, string Icone, string Cor)[] defs =
        [
            // Despesas
            ("Alimentação", TipoCategoriaGasto.Despesa, "bi-egg-fried", "#E67E22"),
            ("Mercado", TipoCategoriaGasto.Despesa, "bi-cart", "#27AE60"),
            ("Transporte", TipoCategoriaGasto.Despesa, "bi-car-front", "#2980B9"),
            ("Moradia", TipoCategoriaGasto.Despesa, "bi-house", "#8E44AD"),
            ("Saúde", TipoCategoriaGasto.Despesa, "bi-heart-pulse", "#C0392B"),
            ("Lazer", TipoCategoriaGasto.Despesa, "bi-controller", "#F39C12"),
            ("Assinaturas", TipoCategoriaGasto.Despesa, "bi-repeat", "#16A085"),
            ("Compras", TipoCategoriaGasto.Despesa, "bi-bag", "#D35400"),
            ("Educação", TipoCategoriaGasto.Despesa, "bi-book", "#2C3E50"),
            ("Serviços", TipoCategoriaGasto.Despesa, "bi-tools", "#7F8C8D"),
            ("Transferências", TipoCategoriaGasto.Despesa, "bi-arrow-left-right", "#95A5A6"),
            ("Outros", TipoCategoriaGasto.Despesa, "bi-three-dots", "#BDC3C7"),
            // Receitas
            ("Salário", TipoCategoriaGasto.Receita, "bi-cash-stack", "#27AE60"),
            ("Rendimentos", TipoCategoriaGasto.Receita, "bi-graph-up-arrow", "#2ECC71"),
            ("Investimento/Aporte", TipoCategoriaGasto.Receita, "bi-piggy-bank", "#1ABC9C"),
            ("Outras receitas", TipoCategoriaGasto.Receita, "bi-plus-circle", "#58D68D"),
        ];

        foreach (var (nome, tipo, icone, cor) in defs)
        {
            yield return new CategoriaGasto
            {
                Nome = nome,
                Tipo = tipo,
                Icone = icone,
                Cor = cor,
                Ativo = true,
                ChaveNatural = CategoriaGasto.GerarChaveNatural(nome),
                UsuarioInclusao = "seed"
            };
        }
    }

    /// <summary>
    /// Regras iniciais "Contém" → nome da categoria-alvo. O DbInitializer resolve o nome para o
    /// CategoriaId em runtime. Prioridade menor vence (a 1ª que casar). Padrão é casado em
    /// MAIÚSCULAS (a categorização normaliza a descrição).
    /// </summary>
    public static IEnumerable<(string Padrao, TipoMatchRegra TipoMatch, string Categoria, int Prioridade)> GetRegras() =>
    [
        // Transporte
        ("UBER", TipoMatchRegra.Contem, "Transporte", 10),
        ("99 RIDE", TipoMatchRegra.Contem, "Transporte", 10),
        ("DL*99", TipoMatchRegra.Contem, "Transporte", 10),
        ("METRORIO", TipoMatchRegra.Contem, "Transporte", 10),
        ("RIOCARD", TipoMatchRegra.Contem, "Transporte", 10),
        ("MOBI", TipoMatchRegra.Contem, "Transporte", 15),
        // Alimentação
        ("IFOOD", TipoMatchRegra.Contem, "Alimentação", 10),
        ("RAPPI", TipoMatchRegra.Contem, "Alimentação", 10),
        ("RESTAURANTE", TipoMatchRegra.Contem, "Alimentação", 20),
        ("LANCHONETE", TipoMatchRegra.Contem, "Alimentação", 20),
        ("BAR ", TipoMatchRegra.Contem, "Alimentação", 25),
        ("CHOPERIA", TipoMatchRegra.Contem, "Alimentação", 20),
        ("PASTELARI", TipoMatchRegra.Contem, "Alimentação", 20),
        // Mercado
        ("MERCEARIA", TipoMatchRegra.Contem, "Mercado", 10),
        ("MERCADO", TipoMatchRegra.Contem, "Mercado", 30),
        ("SUPERMERCADO", TipoMatchRegra.Contem, "Mercado", 10),
        // Saúde
        ("DROGARIA", TipoMatchRegra.Contem, "Saúde", 10),
        ("FARMACIA", TipoMatchRegra.Contem, "Saúde", 10),
        ("TOTALPASS", TipoMatchRegra.Contem, "Saúde", 10),
        // Assinaturas
        ("NETFLIX", TipoMatchRegra.Contem, "Assinaturas", 10),
        ("YOUTUBE", TipoMatchRegra.Contem, "Assinaturas", 10),
        ("AMAZONPRIME", TipoMatchRegra.Contem, "Assinaturas", 10),
        ("SPOTIFY", TipoMatchRegra.Contem, "Assinaturas", 10),
        ("GOOGLE", TipoMatchRegra.Contem, "Assinaturas", 40),
        // Compras
        ("MERCADOLIVRE", TipoMatchRegra.Contem, "Compras", 20),
        ("AMAZON", TipoMatchRegra.Contem, "Compras", 30),
        // Receitas / movimentos
        ("SAL", TipoMatchRegra.Contem, "Salário", 50),
        ("RENDIMENTO", TipoMatchRegra.Contem, "Rendimentos", 10),
        ("COMPRA DE A", TipoMatchRegra.Contem, "Investimento/Aporte", 5),
        ("RDB", TipoMatchRegra.Contem, "Investimento/Aporte", 5),
        ("TESOURO", TipoMatchRegra.Contem, "Investimento/Aporte", 5),
        ("NUINVEST", TipoMatchRegra.Contem, "Investimento/Aporte", 5),
        ("PAGAMENTO DE FATURA", TipoMatchRegra.Contem, "Transferências", 5),
    ];
}
