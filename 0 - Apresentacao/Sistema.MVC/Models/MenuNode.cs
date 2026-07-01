namespace Sistema.MVC.Models;

/// <summary>
/// Nó da árvore do menu lateral. Um nó com <see cref="Children"/> vira um ramo
/// (accordion que expande/recolhe); um nó folha vira um link. A recursão em
/// <see cref="_MenuNode"/> permite profundidade ilimitada.
/// </summary>
public class MenuNode
{
    public string Label { get; set; } = string.Empty;

    /// <summary>Classe do ícone (ex.: "bi bi-house"). Opcional.</summary>
    public string? Icon { get; set; }

    /// <summary>Controller MVC do link (folha). Usado com <see cref="Action"/>.</summary>
    public string? Controller { get; set; }

    /// <summary>Action MVC do link (folha).</summary>
    public string? Action { get; set; }

    /// <summary>Href absoluto/externo do link (folha), alternativa a Controller/Action.</summary>
    public string? Href { get; set; }

    /// <summary>Fragmento (#ancora) para asp-fragment.</summary>
    public string? Fragment { get; set; }

    /// <summary>
    /// Chave dinâmica de badge (ex.: "unread"). Resolvida na renderização para um
    /// valor numérico; mantém o modelo desacoplado da contagem de mensagens.
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>Filhos do nó. Não vazio => nó é um ramo.</summary>
    public List<MenuNode> Children { get; } = new();

    /// <summary>Rótulo de seção (cabeçalho não clicável, ex.: "Modulos").</summary>
    public bool IsSectionLabel { get; set; }

    public bool HasChildren => Children.Count > 0;

    public bool IsLeaf => !HasChildren && !IsSectionLabel;

    public MenuNode Add(MenuNode child)
    {
        Children.Add(child);
        return this;
    }

    public static MenuNode Section(string label) => new() { Label = label, IsSectionLabel = true };

    public static MenuNode Branch(string label, string? icon = null) => new() { Label = label, Icon = icon };

    public static MenuNode Link(string label, string controller, string action, string? icon = null, string? fragment = null)
        => new() { Label = label, Controller = controller, Action = action, Icon = icon, Fragment = fragment };

    public static MenuNode LinkHref(string label, string href, string? icon = null)
        => new() { Label = label, Href = href, Icon = icon };
}
