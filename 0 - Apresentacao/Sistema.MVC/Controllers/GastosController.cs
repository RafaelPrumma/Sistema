using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;

namespace Sistema.MVC.Controllers;

// Submódulo Gastos (G1) — "Visão geral". Sem permissão própria "Gastos" ainda: reusa "Financas"
// (mesma decisão registrada na spec/brief). O Index é À PROVA DE FALHA: a materialização lazy roda
// dentro do serviço (try-catch que degrada para vazio) e o controller nunca propaga exceção — o
// menu "Gastos › Visão geral" não pode cair em erro enquanto as fases de UI (G3+) não chegam.
[AuthorizePermission("Financas", Permissao.Visualizar)]
public class GastosController(IGastosService service) : Controller
{
    private readonly IGastosService _service = service;

    [HttpGet("/Gastos")]
    [HttpGet("/Gastos/Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // ObterVisaoGeralAsync já é à prova de falha (degrada para DTO indisponível). O try/catch aqui
        // é o cinto-e-suspensório: nem um erro de infra (ex.: banco fora) derruba a tela.
        GastosVisaoGeralDto visao;
        try
        {
            visao = await _service.ObterVisaoGeralAsync(cancellationToken);
        }
        catch
        {
            var hoje = DateTime.Today;
            visao = new GastosVisaoGeralDto { Ano = hoje.Year, Mes = hoje.Month, Disponivel = false };
        }

        return View(visao);
    }
}
