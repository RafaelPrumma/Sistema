namespace Sistema.MVC.Models;

public class SidebarMenuViewModel
{
    public string TextClass { get; set; } = "text-white";
    public bool MenuExpanded { get; set; } = true;
    public int UnreadMessages { get; set; }
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
