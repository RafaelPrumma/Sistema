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
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var response = await client.PostAsJsonAsync("api/auth/login", model);
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadAsStringAsync();
                HttpContext.Session.SetString("AuthToken", token.Trim('"'));
                return Ok(new { success = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar");
        }

        return Unauthorized(new { message = "Credenciais inválidas" });
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("AuthToken");
        return RedirectToAction("Login");
    }
}
