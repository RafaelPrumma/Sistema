using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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

    private int? ObterUsuarioId()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            return userId.Value;
        }

        var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimId, out var parsedId))
        {
            HttpContext.Session.SetInt32("UserId", parsedId);
            return parsedId;
        }

        return null;
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

        var usuario = await _usuarioService.BuscarPorCpfAsync(model.Cpf);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Credenciais inválidas");
            return View(model);
        }

        var result = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, model.Senha);
        if (result != PasswordVerificationResult.Success)
        {
            ModelState.AddModelError(string.Empty, "Credenciais inválidas");
            return View(model);
        }

        if (!usuario.Ativo)
        {
            ModelState.AddModelError(string.Empty, "Usuário inativo");
            return View(model);
        }

        HttpContext.Session.SetString("AuthToken", Guid.NewGuid().ToString());
        HttpContext.Session.SetInt32("UserId", usuario.Id);
        HttpContext.Session.SetString("UserName", usuario.Nome);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        return RedirectToAction("Index", "Home");
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
                Email = model.Email,
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
            if (usuario is null || !string.Equals(usuario.Email, model.Email, StringComparison.OrdinalIgnoreCase))
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
    public IActionResult ChangePassword()
    {
        if (ObterUsuarioId() is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await _usuarioService.BuscarPorIdAsync(usuarioId.Value);
        if (usuario is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        var senhaAtualValida = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, model.SenhaAtual);
        if (senhaAtualValida != PasswordVerificationResult.Success)
        {
            ModelState.AddModelError(nameof(model.SenhaAtual), "Senha atual incorreta");
            return View(model);
        }

        var senhaIgualAtual = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, model.NovaSenha) == PasswordVerificationResult.Success;
        if (senhaIgualAtual)
        {
            ModelState.AddModelError(nameof(model.NovaSenha), "A nova senha deve ser diferente da atual");
            return View(model);
        }

        usuario.SenhaHash = _hasher.HashPassword(usuario, model.NovaSenha);
        usuario.UsuarioAlteracao = usuario.Cpf;
        usuario.ResetToken = null;
        usuario.ResetTokenExpiration = null;

        var result = await _usuarioService.AtualizarAsync(usuario);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        ViewBag.Sucesso = "Senha alterada com sucesso.";
        ModelState.Clear();
        return View(new ChangePasswordViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
