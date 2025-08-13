using Microsoft.AspNetCore.Mvc;
using Sistema.MVC.Models;
using System.Net.Http.Json;

namespace Sistema.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IHttpClientFactory httpClientFactory,
        ILogger<AccountController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var response = await client.PostAsJsonAsync("api/auth/login", model);
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadAsStringAsync();
                HttpContext.Session.SetString("AuthToken", token.Trim('"'));
                return RedirectToAction("Index", "Home");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar");
        }

        ModelState.AddModelError(string.Empty, "Credenciais inválidas");
        return View(model);
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("AuthToken");
        return RedirectToAction("Login");
    }
}
