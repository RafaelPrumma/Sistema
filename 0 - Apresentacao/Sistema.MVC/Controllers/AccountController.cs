using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.MVC.Models;

namespace Sistema.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUsuarioService _usuarioService;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService,
        IUsuarioService usuarioService,
        IPasswordHasher<Usuario> hasher,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _usuarioService = usuarioService;
        _hasher = hasher;
        _emailService = emailService;
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
            var token = await _authService.AutenticarAsync(model.Cpf, model.Senha);
            if (token is not null)
            {
                HttpContext.Session.SetString("AuthToken", token);
                return Ok(new { success = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar");
        }

        return Unauthorized(new { message = "Credenciais inválidas" });
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuario = new Usuario
            {
                Nome = model.Nome,
                Cpf = model.Cpf,
                PerfilId = 2,
                UsuarioInclusao = model.Cpf,
                Ativo = true
            };
            usuario.SenhaHash = _hasher.HashPassword(usuario, model.Senha);
            var result = await _usuarioService.AdicionarAsync(usuario);
            if (result.Success)
            {
                await _emailService.EnviarAsync(model.Email, "Bem-vindo", $"Sua senha é: {model.Senha}");
                return Ok(new { success = true });
            }
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar");
            return StatusCode(500, new { message = "Erro ao registrar" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RecuperarSenha([FromBody] ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuario = await _usuarioService.BuscarPorCpfAsync(model.Cpf);
            if (usuario is null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }
            var novaSenha = Guid.NewGuid().ToString("N").Substring(0, 8);
            usuario.SenhaHash = _hasher.HashPassword(usuario, novaSenha);
            usuario.UsuarioAlteracao = "system";
            var result = await _usuarioService.AtualizarAsync(usuario);
            if (result.Success)
            {
                await _emailService.EnviarAsync(model.Email, "Recuperação de Senha", $"Sua nova senha é: {novaSenha}");
                return Ok(new { success = true });
            }
            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar senha");
            return StatusCode(500, new { message = "Erro ao recuperar senha" });
        }
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("AuthToken");
        return RedirectToAction("Login");
    }
}
