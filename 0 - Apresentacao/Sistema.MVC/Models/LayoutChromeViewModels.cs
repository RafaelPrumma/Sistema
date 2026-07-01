namespace Sistema.MVC.Models;

public class SidebarMenuViewModel
{
    public string TextClass { get; set; } = "text-white";
    public bool MenuExpanded { get; set; } = true;
    public int UnreadMessages { get; set; }

    /// <summary>Nós raiz da árvore do menu, renderizados recursivamente.</summary>
    public IReadOnlyList<MenuNode> Nodes { get; set; } = System.Array.Empty<MenuNode>();

    /// <summary>Monta a hierarquia do menu lateral (níveis ilimitados).</summary>
    public static IReadOnlyList<MenuNode> BuildMenu()
    {
        var financas = MenuNode.Branch("Finanças", "bi bi-graph-up-arrow");

        financas.Add(MenuNode.Branch("Investimentos", "bi bi-bar-chart-line")
            .Add(MenuNode.Link("Dashboard", "Financas", "Index"))
            .Add(MenuNode.Link("Transações", "Financas", "Transacoes"))
            .Add(MenuNode.Link("Resumo analítico", "Financas", "Resumo"))
            .Add(MenuNode.Link("Proventos", "Financas", "Proventos"))
            .Add(MenuNode.Link("Documentos", "Financas", "Documentos"))
            .Add(MenuNode.Link("Operações B3", "Financas", "OperacoesB3"))
            .Add(MenuNode.Link("Cripto", "Financas", "OperacoesCripto"))
            .Add(MenuNode.Link("Posições estimadas", "Financas", "Posicoes"))
            .Add(MenuNode.Link("Alertas", "Financas", "Alertas"))
            .Add(MenuNode.Link("Eventos corporativos", "Financas", "Eventos"))
            .Add(MenuNode.Link("Alertas de preço", "Financas", "AlertasPreco"))
            .Add(MenuNode.Link("Peso-alvo (metas)", "Financas", "PesoAlvo")));

        financas.Add(MenuNode.Branch("Imposto de Renda", "bi bi-receipt")
            .Add(MenuNode.Link("Apuração de IR", "Financas", "IR")));

        financas.Add(MenuNode.Branch("Gastos", "bi bi-wallet2")
            .Add(MenuNode.Link("Visão geral", "Gastos", "Index")));

        return new List<MenuNode>
        {
            MenuNode.Section("Início"),
            MenuNode.Link("Dashboard", "Home", "Index", "bi bi-house"),

            MenuNode.Section("Módulos"),
            financas,

            MenuNode.Branch("Acesso", "bi bi-shield-lock")
                .Add(MenuNode.Link("Segurança", "Account", "ChangePassword"))
                .Add(MenuNode.LinkHref("AcessoLog (API)", "/api/log/acesso")),

            new MenuNode { Label = "Comunicação", Icon = "bi bi-envelope", Badge = "unread" }
                .Add(MenuNode.Link("Feed", "Mensagem", "Index"))
                .Add(MenuNode.Link("Caixa de saída", "Mensagem", "CaixaSaida"))
                .Add(MenuNode.Link("Nova publicação", "Mensagem", "Nova"))
                .Add(MenuNode.LinkHref("ComunicacaoLog (API)", "/api/log/comunicacao")),

            MenuNode.Branch("Administração", "bi bi-gear")
                .Add(MenuNode.Link("Configurações", "Configuracao", "Index"))
                .Add(MenuNode.Link("Tema", "Tema", "Edit"))
                .Add(MenuNode.Link("Logs", "Log", "Index"))
                .Add(MenuNode.Link("Fila de jobs", "Fila", "Index")),

            MenuNode.Branch("Documentação", "bi bi-book")
                .Add(MenuNode.Link("Mensagens", "Documentacao", "Index", fragment: "mensagens"))
                .Add(MenuNode.Link("Logs e auditoria", "Documentacao", "Index", fragment: "logs"))
                .Add(MenuNode.Link("Scripts", "Documentacao", "Index", fragment: "scripts")),
        };
    }
}

public class AppHeaderViewModel
{
    public string NavbarClass { get; set; } = "navbar-light";
    public string TextClass { get; set; } = "text-dark";
    public string FixedClass { get; set; } = string.Empty;
    public string UserName { get; set; } = "Usuario";
    public string UserInitial => string.IsNullOrWhiteSpace(UserName) ? "U" : UserName[..1].ToUpperInvariant();
    public int UnreadMessages { get; set; }
}

public class ThemePanelViewModel
{
    public string TextClass { get; set; } = "text-dark";
    public string HeaderColor { get; set; } = "#0d6efd";
    public string SidebarColor { get; set; } = "#0d6efd";
    public string RightbarColor { get; set; } = "#f8f9fa";
    public string FooterColor { get; set; } = "#0d6efd";
    public bool DarkMode { get; set; }
    public bool HeaderFixed { get; set; }
    public bool FooterFixed { get; set; }
    public bool MenuExpanded { get; set; } = true;
}
