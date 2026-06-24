using Sistema.CORE.Entities;

namespace Sistema.CORE.Common;

// F-L(b): projeção de rastreabilidade de cada documento importado. O repositório resolve as contagens
// (linhas/abas de ConteudoBruto e alertas) em SQL; a derivação de fonte/período (parse do RawMetadataJson)
// fica na camada de aplicação. Mantida fora das entidades porque é um read model só de leitura.
public record RastreabilidadeDocumentoProjecao(
    int Id,
    string FileName,
    TipoDocumentoFinanceiro DocumentKind,
    StatusParseDocumentoFinanceiro ParseStatus,
    StatusDocumentoFinanceiro Status,
    int? ReferenceYear,
    string RawMetadataJson,
    int LinhasLidas,
    int Abas,
    int Alertas);
