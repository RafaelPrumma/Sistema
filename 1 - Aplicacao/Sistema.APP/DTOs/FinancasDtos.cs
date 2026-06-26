namespace Sistema.APP.DTOs;

public record FinanceiroKpiDto(string Titulo, decimal Valor, string Subtitulo, string Icone, bool Monetario = true);

public record FinanceiroSerieDto(string Label, decimal ValorCompra, decimal ValorVenda, decimal Saldo, int Operacoes);

public record FinanceiroDistribuicaoDto(string Label, decimal Valor, int Operacoes);

public record DocumentoFinanceiroDto(
    int Id,
    string FileName,
    string FileType,
    string Source,
    string Sha256,
    long SizeBytes,
    int? ReferenceYear,
    int? PageCount,
    string Status);

public record ConteudoBrutoFinanceiroDto(
    int Id,
    string ContentType,
    int? PageNumber,
    string? SheetName,
    int? RowNumber,
    string? RawText,
    string? RawJson);

public record OperacaoB3Dto(
    int Id,
    DateTime? TradeDate,
    string Tipo,
    string Ativo,
    string Classe,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal ValorBruto,
    string Mercado,
    string Arquivo,
    int? Pagina,
    bool Canonica,
    string Confianca);

public record TransacaoCriptoDto(
    int Id,
    DateTime? Data,
    string Tipo,
    string Ativo,
    string? Par,
    decimal Quantidade,
    decimal? Preco,
    decimal? Total,
    string Fonte,
    string RawType);

public record PosicaoFinanceiraDto(
    int Id,
    string Ativo,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal TotalInvestido,
    decimal TotalVendido,
    decimal ResultadoRealizado,
    string Status,
    string Confianca,
    DateTime? UltimaOperacao);

public record RendimentoInvestimentoDto(string Tipo, string Fonte, decimal Valor, string Tributacao);

public record ProventoDto(
    int Id,
    DateTime? DataPagamento,
    DateTime? DataCom,
    string Ticker,
    string Nome,
    string Classe,
    string Tipo,
    decimal? Quantidade,
    decimal? ValorPorAcao,
    decimal Valor,
    decimal ImpostoRetido,
    decimal ValorLiquido,
    string Tributacao,
    string Fonte);

public record ProventosResumoDto(
    decimal RecebidoMes,
    decimal RecebidoAno,
    decimal RecebidoTotal,
    decimal AReceber,
    int Quantidade);

public record ProventoPeriodoDto(
    string Codigo,
    string Rotulo,
    decimal Recebido,
    decimal AReceber,
    int Quantidade,
    decimal Percentual);

public record ProventoBaldeDto(
    string Codigo,
    string Rotulo,
    decimal Valor,
    decimal Percentual,
    int Quantidade,
    string Status);

public record ProventoMensalDto(
    string Rotulo,
    int Ano,
    int Mes,
    decimal Recebido,
    decimal AReceber);

public record ProventosPaginaDto(
    IReadOnlyList<ProventoDto> Itens,
    int Page,
    int PageSize,
    int TotalCount,
    ProventosResumoDto Resumo,
    IReadOnlyList<ProventoPeriodoDto> Periodos,
    IReadOnlyList<ProventoBaldeDto> Baldes,
    IReadOnlyList<ProventoMensalDto> Mensais);

public record AlertaConfiabilidadeDto(
    int Id,
    string Entidade,
    string Severidade,
    string Codigo,
    string Mensagem,
    string? Detalhes,
    DateTime CriadoEm);

public record CotacaoAtivoDto(
    int AtivoId,
    string Ativo,
    string Classe,
    string Symbol,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal? PrecoAtual,
    decimal ValorMercado,
    decimal CustoEstimado,
    decimal ResultadoNaoRealizado,
    decimal ResultadoNaoRealizadoPercentual,
    decimal? VariacaoDiaPercentual,
    DateTime? AtualizadoEm,
    string Status,
    string Confianca,
    // F-L: de onde veio o preço que valorou a posição — "Cotação" (Brapi/Binance ao vivo),
    // "B3Custódia" (fechamento da aba Posição) ou "Custo" (fallback ao preço médio, sem cotação).
    string FontePreco = "Custo");

public record CarteiraAtivoResumoDto(
    int AtivoId,
    string Ativo,
    string Classe,
    string Symbol,
    decimal Quantidade,
    decimal ValorMercado,
    decimal PercentualCarteira,
    decimal? VariacaoDiaPercentual,
    decimal ResultadoNaoRealizado,
    decimal ResultadoNaoRealizadoPercentual,
    string Status);

public record CarteiraFinanceiraResumoDto(
    int Id,
    string Nome,
    string Tipo,
    decimal ValorMercado,
    decimal CustoEstimado,
    decimal ResultadoNaoRealizado,
    decimal ResultadoNaoRealizadoPercentual,
    decimal VariacaoDiaPercentual,
    decimal PercentualPatrimonio,
    int Ativos,
    IReadOnlyList<CarteiraAtivoResumoDto> Itens,
    // F-I: subcarteiras (folhas) de uma carteira-topo. O valor/custo/resultado do pai agrega os filhos
    // (soma para cima). Carteira-folha ou flat tem a lista vazia.
    IReadOnlyList<CarteiraFinanceiraResumoDto> Subcarteiras);

public record PeriodoPerformanceDto(string Codigo, string Label, decimal VariacaoPercentual, decimal VariacaoValor);

public record ValidacaoAtivoResultado(
    bool Valido,
    string Ticker,
    string Nome,
    string Classe,
    string Provedor,
    bool IsCrypto,
    decimal? PrecoAtual,
    string? Mensagem);

public record TransacaoFinanceiraDto(
    int Id,
    string Origem,
    string Fonte,
    string Ativo,
    string Ticker,
    string Classe,
    DateTime Data,
    string Tipo,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal ValorTotal,
    decimal Taxas,
    string Corretora,
    string? Observacao);

public record NovaTransacaoInput(
    string Ticker,
    string Tipo,
    decimal Quantidade,
    decimal PrecoUnitario,
    DateTime Data,
    decimal Taxas = 0m,
    string? Corretora = null,
    string? Observacao = null);

public record ResultadoOperacao(bool Sucesso, string? Mensagem, int? Id = null);

public record EventoCorporativoDto(
    int Id,
    string Ticker,
    string AtivoNome,
    string Tipo,
    DateTime Data,
    decimal Fator,
    string Fonte);

public record NovoEventoCorporativoInput(
    string Ticker,
    string Tipo,
    DateTime Data,
    decimal Fator,
    string? Fonte = null);

// F-H — alerta de preço (listagem). DispararadoEm != null = já disparou (armado), aguardando re-arme.
public record AlertaPrecoDto(
    int Id,
    string Ticker,
    string AtivoNome,
    decimal Limiar,
    string Direcao,
    bool Ativo,
    DateTime? DispararadoEm,
    decimal? UltimoPreco,
    string? Observacao);

public record NovoAlertaPrecoInput(
    string Ticker,
    decimal Limiar,
    string Direcao,
    bool Ativo = true,
    string? Observacao = null);

// Série de evolução: eixo de datas compartilhado + arrays de valores paralelos (payload enxuto).
// VariacaoDia e ValorAtual vêm das cotações ao vivo (não do histórico diário).
public record SerieEvolucaoDto(
    string Chave,
    string Rotulo,
    IReadOnlyList<decimal> Valores,
    decimal VariacaoDia,
    decimal ValorAtual);

public record EvolucaoPatrimonioDto(
    IReadOnlyList<string> Datas,
    IReadOnlyList<decimal> Total,
    // Custo acumulado (aportes líquidos = compras − vendas, acumulado por data) alinhado a Datas;
    // sobreposto ao patrimônio no gráfico para comparar mercado × quanto foi aportado.
    IReadOnlyList<decimal> CustoAcumulado,
    decimal VariacaoDiaTotal,
    decimal ValorAtualTotal,
    IReadOnlyList<SerieEvolucaoDto> Setores,
    IReadOnlyList<PeriodoPerformanceDto> Periodos);

public record ResumoAtivoDto(
    string Ticker,
    string Nome,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal Custo,
    decimal? PrecoAtual,
    decimal ValorMercado,
    decimal PlNaoRealizado,
    decimal PlPercentual,
    decimal ResultadoRealizadoPeriodo,
    decimal ProventosPeriodo,
    decimal RetornoTotalPeriodo);

public record VendaRealizadaDto(
    DateTime Data,
    string Ticker,
    decimal Quantidade,
    decimal PrecoVenda,
    decimal PrecoMedio,
    decimal Resultado,
    bool Boa);

public record ResumoAnaliticoDto(
    string PeriodoLabel,
    DateTime Inicio,
    DateTime Fim,
    decimal TotalComprado,
    decimal TotalVendido,
    decimal AporteLiquido,
    decimal ResultadoRealizado,
    int NumeroOperacoes,
    decimal CustoTotal,
    decimal ValorMercadoTotal,
    decimal PlNaoRealizadoTotal,
    decimal ProventosRecebidos,
    decimal RetornoTotal,
    IReadOnlyList<ResumoAtivoDto> Ativos,
    IReadOnlyList<VendaRealizadaDto> Vendas);

public record ImportacaoFinanceiraResumoDto(
    DateTime? UltimaImportacao,
    int DocumentosMonitorados,
    int DocumentosProcessados,
    int DocumentosComAlerta,
    string? PastaMonitorada);

public record FinancasPatrimonioDto(
    decimal ValorMercadoTotal,
    decimal CustoEstimadoTotal,
    decimal ResultadoNaoRealizadoTotal,
    EvolucaoPatrimonioDto Evolucao);

public record FinancasCarteirasDto(
    IReadOnlyList<CarteiraFinanceiraResumoDto> Carteiras,
    // F-O: cripto ainda não tem saldo de abertura/snapshot real da Binance (ver cripto.spec.md F2).
    // Quando há posição cripto, sinalizamos "parcialmente reconciliado" — honestidade > número cego.
    bool CriptoParcialmenteReconciliada = false);

// F-G: acompanhamento de metas (peso-alvo) por carteira-topo.
//  - PesoAtual  = participação no patrimônio hoje (%).
//  - PesoAlvo   = soma dos PesoAlvo dos ativos da carteira (e subcarteiras), em % do patrimônio.
//  - Desvio*    = atual − alvo, em pontos percentuais (p.p.) e relativo (%).
//  - FaltaParaAlvo / SobraSobreAlvo = quanto em R$ falta aportar (ou está acima) para bater o alvo,
//    no patrimônio atual. AporteSugerido = fatia de um aporte hipotético direcionada a esta carteira
//    para reduzir o desvio (>= 0; carteira acima do alvo não recebe aporte).
public record MetaCarteiraDto(
    int CarteiraId,
    string Nome,
    decimal ValorMercado,
    decimal PesoAtual,
    decimal PesoAlvo,
    decimal DesvioPontos,
    decimal DesvioPercentual,
    decimal FaltaParaAlvo,
    decimal SobraSobreAlvo,
    decimal AporteSugerido);

public record FinancasMetasDto(
    IReadOnlyList<MetaCarteiraDto> Carteiras,
    decimal PatrimonioTotal,
    // Soma dos pesos-alvo definidos (sanidade: idealmente ~100%). SemMetas = nenhuma carteira tem alvo
    // → a ilha não aparece. AlvoForaDeCem sinaliza soma de alvos ≠ 100 (avisa, mas não trava).
    decimal SomaPesoAlvo,
    decimal AporteHipotetico,
    bool SemMetas,
    bool AlvoForaDeCem);

public record FinancasImportacaoDto(
    IReadOnlyList<FinanceiroKpiDto> Kpis,
    ImportacaoFinanceiraResumoDto ImportacaoArquivos,
    DateTime? CotacoesAtualizadasEm,
    // F-L(b): rastreabilidade dos arquivos por fonte/status + saúde da custódia B3.
    IReadOnlyList<RastreabilidadeFonteDto> RastreabilidadeFontes = null!,
    RastreabilidadeB3Dto? RastreabilidadeB3 = null);

// F-L(b): um documento importado, com o que dá para rastrear dele (tipo, período, status, linhas/abas
// lidas e nº de sinais de alerta/erro/duplicidade ligados ao documento).
public record RastreabilidadeDocumentoDto(
    string Arquivo,
    string Tipo,            // rótulo do DocumentKind (ex.: "B3 Extrato", "Nota Nubank")
    string? Periodo,        // referencePeriod (yyyy-MM) quando houver; senão o ano de referência
    string StatusParse,     // rótulo amigável do ParseStatus
    string Status,          // rótulo amigável do Status
    int LinhasLidas,
    int Abas,
    int Alertas);

// F-L(b): resumo de uma fonte (B3 / Nubank / Binance / IR / Outros) com a contagem por status de parse
// e a lista de documentos. "Parciais"/"Falhos" sinalizam o que não entrou inteiro.
public record RastreabilidadeFonteDto(
    string Fonte,
    int Documentos,
    int Processados,
    int Parciais,
    int Falhos,
    int LinhasLidas,
    int Alertas,
    IReadOnlyList<RastreabilidadeDocumentoDto> Itens);

// F-L(b): saúde da custódia B3 — última Posição usada na reconciliação (período do snapshot) e meses
// faltantes entre o primeiro e o último extrato consolidado (lacunas na série mensal).
public record RastreabilidadeB3Dto(
    string? UltimoPeriodoPosicao,        // maior referencePeriod entre os extratos B3 (yyyy-MM)
    string? PrimeiroPeriodoExtrato,
    int ExtratosImportados,
    IReadOnlyList<string> MesesFaltantes);

public record ProventoTopPagadorDto(
    string Ticker,
    string Nome,
    decimal Valor);

// F-N: quanto do provento recebido (12M) veio de cada fonte (B3 Extrato, Brapi, Binance Earn, IR).
// É informação de confiança: FII vem da B3 porque o informe de IR só cobre ações.
public record ProventoFonteDto(
    string Fonte,
    decimal Valor,
    decimal Percentual,
    int Quantidade);

// F-K: resumo de proventos para a ilha lazy-loaded do dashboard.
// Reaproveita os mesmos cálculos da tela de Proventos (resumo do período + série mensal).
// F-N: PorFonte separa o recebido (12M) por origem do dado.
public record FinancasProventosDashboardDto(
    ProventosResumoDto Resumo,
    IReadOnlyList<ProventoMensalDto> Mensais,
    IReadOnlyList<ProventoTopPagadorDto> TopPagadores,
    IReadOnlyList<ProventoFonteDto> PorFonte);

// F-L: painel de saúde/transparência. Posição calculada (das transações) confrontada com a custódia
// oficial da B3 (Preço de Fechamento da aba Posição), com a fonte do preço que valorou cada ativo.
public record PosicaoCalculadaDto(
    string Ticker,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal ValorMercado,
    string FontePreco,
    decimal? PrecoB3,        // Preço de Fechamento B3Custódia (null = sem snapshot da B3 p/ o ativo)
    decimal? DiferencaB3,    // valorMercado − (qtd × precoB3); null quando não há preço B3
    string Status);

// F-L (a): composição do valor de mercado por fonte do preço — cotação ao vivo × fechamento B3 × custo.
public record ComposicaoValorDto(
    decimal ComCotacao,      // valorado por cotação ao vivo (Brapi/Binance)
    decimal ComFechamentoB3, // valorado pelo fechamento B3Custódia
    decimal ComCusto,        // fallback ao custo (sem cotação utilizável)
    decimal Total);

public record FinancasPosicoesDashboardDto(
    ComposicaoValorDto Composicao,
    IReadOnlyList<PosicaoCalculadaDto> Posicoes);

// F-M: card de reconciliação B3. Torna o ReconciliadorPosicaoB3 explícito para o usuário confiar
// no número: alvo da custódia vs calculado por transações, nº de ajustes e o valor que foi parar no
// ativo virtual VARIACAO (a diferença não explicada pelos relatórios).
public record ReconciliacaoAtivoDto(
    string Ticker,
    string Nome,
    decimal Alvo,
    decimal Calculado,
    decimal Diferenca,
    decimal ValorAjuste);

public record FinancasReconciliacaoDto(
    bool TemDados,
    int NumeroAjustes,
    decimal ValorTotalVariacao,
    decimal AlvoTotalCustodia,
    decimal CalculadoTotal,
    IReadOnlyList<ReconciliacaoAtivoDto> PrincipaisAtivos);

public class FinancasDashboardDto
{
    public string GeradoEm { get; set; } = string.Empty;
    public string Fonte { get; set; } = string.Empty;
    public IReadOnlyList<FinanceiroKpiDto> Kpis { get; set; } = [];
    public IReadOnlyList<FinanceiroSerieDto> B3PorAno { get; set; } = [];
    public IReadOnlyList<FinanceiroSerieDto> B3PorMes { get; set; } = [];
    public IReadOnlyList<FinanceiroDistribuicaoDto> B3PorClasse { get; set; } = [];
    public IReadOnlyList<FinanceiroDistribuicaoDto> BinanceMoedas { get; set; } = [];
    public IReadOnlyList<OperacaoB3Dto> UltimasOperacoesB3 { get; set; } = [];
    public IReadOnlyList<TransacaoCriptoDto> UltimasTransacoesCripto { get; set; } = [];
    public IReadOnlyList<PosicaoFinanceiraDto> PosicoesAbertas { get; set; } = [];
    public IReadOnlyList<PosicaoFinanceiraDto> PosicoesEncerradas { get; set; } = [];
    public IReadOnlyList<AlertaConfiabilidadeDto> Alertas { get; set; } = [];
    public IReadOnlyList<CotacaoAtivoDto> AtivosCotados { get; set; } = [];
    public IReadOnlyList<CarteiraFinanceiraResumoDto> Carteiras { get; set; } = [];
    public IReadOnlyList<PeriodoPerformanceDto> Periodos { get; set; } = [];
    public ImportacaoFinanceiraResumoDto? ImportacaoArquivos { get; set; }
    public DateTime? CotacoesAtualizadasEm { get; set; }
    public decimal ValorMercadoTotal { get; set; }
    public decimal CustoEstimadoTotal { get; set; }
    public decimal ResultadoNaoRealizadoTotal { get; set; }
    public string? DashboardJson { get; set; }
    public IReadOnlyList<ProventoMensalDto> ProventosMensais { get; set; } = [];
}
