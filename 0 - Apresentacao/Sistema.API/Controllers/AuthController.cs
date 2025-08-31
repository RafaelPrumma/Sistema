using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.CORE.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(LoginDto dto)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var token = await _authService.AutenticarAsync(dto.Cpf, dto.Senha, cancellationToken);
        if (token is null) return Unauthorized();
        return Ok(token);
    }
}
