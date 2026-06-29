namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco/IO) da detecção de arquivos novos numa pasta monitorada.
/// Usada por <see cref="FinancasImportador.GarantirCargaInicialAsync"/> para decidir, de forma
/// BARATA, se a varredura precisa rodar de novo — sem recalcular Sha256 de todos os arquivos.
/// Critério: existe na pasta algum arquivo suportado cujo caminho ainda NÃO foi importado
/// (não consta em <c>DocumentoFinanceiro.StoredPath</c>). Para arquivos normais o StoredPath é o
/// próprio caminho; para .zip é o caminho do zip (cada entrada vira um documento, mas todas
/// compartilham o StoredPath do zip) → comparar pelo StoredPath cobre os dois casos sem reler bytes.
/// </summary>
public static class DeteccaoNovosArquivos
{
    /// <summary>
    /// True quando há pelo menos um arquivo na pasta cujo caminho ainda não foi importado.
    /// Comparação de caminho case-insensitive (Windows). Conjuntos vazios → false (nada a fazer).
    /// </summary>
    /// <param name="arquivosNaPasta">Caminhos dos arquivos suportados encontrados na pasta agora.</param>
    /// <param name="caminhosJaImportados">StoredPaths dos documentos já importados daquela pasta.</param>
    public static bool HaArquivoNovo(IEnumerable<string> arquivosNaPasta, ISet<string> caminhosJaImportados)
    {
        ArgumentNullException.ThrowIfNull(arquivosNaPasta);
        ArgumentNullException.ThrowIfNull(caminhosJaImportados);

        foreach (var arquivo in arquivosNaPasta)
        {
            if (string.IsNullOrWhiteSpace(arquivo))
                continue;
            if (!caminhosJaImportados.Contains(arquivo))
                return true;
        }

        return false;
    }
}
