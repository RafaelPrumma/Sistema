using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using System;

namespace Sistema.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUsuarioService usuarioService,
        IPasswordHasher<Usuario> hasher,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
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
    public async Task<IActionResult> Login([FromForm] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _usuarioService.BuscarPorCpfAsync(model.Cpf);
        if (usuario is null)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        var result = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, model.Senha);
        if (result != PasswordVerificationResult.Success)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        if (!usuario.Ativo)
        {
            return Unauthorized(new { message = "Usuário inativo" });
        }

        HttpContext.Session.SetString("AuthToken", Guid.NewGuid().ToString());
        HttpContext.Session.SetInt32("UserId", usuario.Id);
        HttpContext.Session.SetString("UserName", usuario.Nome);
        return Ok(new { success = true });
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
                await _emailService.EnviarAsync(model.Email, "Bem-vindo", "Seu cadastro foi realizado com sucesso.");
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

            var token = Guid.NewGuid().ToString("N");
            usuario.ResetToken = token;
            usuario.ResetTokenExpiration = DateTime.UtcNow.AddHours(1);
            usuario.UsuarioAlteracao = "system";
            var result = await _usuarioService.AtualizarAsync(usuario);
            if (result.Success)
            {
                var resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);
                await _emailService.EnviarAsync(model.Email, "Recuperação de Senha", $"Clique no link para redefinir sua senha: {resetLink}");
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
    public async Task<IActionResult> ResetPassword(string token)
    {
        var usuario = await _usuarioService.BuscarPorResetTokenAsync(token);
        if (usuario is null)
        {
            return BadRequest("Token inválido ou expirado");
        }
        var model = new ResetPasswordViewModel { Token = token };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var usuario = await _usuarioService.BuscarPorResetTokenAsync(model.Token);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Token inválido ou expirado");
            return View(model);
        }

        usuario.SenhaHash = _hasher.HashPassword(usuario, model.Senha);
        usuario.ResetToken = null;
        usuario.ResetTokenExpiration = null;
        usuario.UsuarioAlteracao = "system";
        var result = await _usuarioService.AtualizarAsync(usuario);
        if (result.Success)
        {
            return RedirectToAction("Login");
        }
        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("AuthToken");
        HttpContext.Session.Remove("UserId");
        HttpContext.Session.Remove("UserName");
        return RedirectToAction("Login");
    }
}
