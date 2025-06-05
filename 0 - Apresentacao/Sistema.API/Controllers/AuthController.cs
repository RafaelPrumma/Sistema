using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.CORE.Services;
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
        var token = await _authService.AuthenticateAsync(dto.Cpf, dto.Senha);
        if (token is null) return Unauthorized();
        return Ok(token);
    }
}
