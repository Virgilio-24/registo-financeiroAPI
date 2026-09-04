using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Application.DTOs.Auth;
using RegistoFinanceiro.Application.Services;
using RegistoFinanceiro.Domain.Entities;
using RegistoFinanceiro.Infrastructure.Persistence;

namespace RegistoFinanceiro.Api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController  : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailSender _emailSender;
        private readonly RegistoFinanceiroDbContext _dbContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IJwtTokenService jwtTokenService,
            IEmailSender emailSender, RegistoFinanceiroDbContext dbContext, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _emailSender = emailSender;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            // mensagem genérica sempre que algo falha — nunca revelar se foi o email ou a password
            if (user is null || !user.Active)
                return Unauthorized(new ProblemDetails { Title = "Credenciais inválidas." });

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                    return Unauthorized(new ProblemDetails { Title = "Conta temporariamente bloqueada. Tenta mais tarde." });

                return Unauthorized(new ProblemDetails { Title = "Credenciais inválidas." });
            }

            var (accessToken, refreshToken) = await IssueTokensAsync(user, ct);
            return Ok(new LoginResponse(accessToken, refreshToken));
        }
        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshResponse>> Refresh(RefreshRequest request, CancellationToken ct)
        {
            var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

            var existing = await _dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTimeOffset.UtcNow)
                return Unauthorized(new ProblemDetails { Title = "Refresh token inválido ou expirado." });

            if (!existing.User.Active)
                return Unauthorized(new ProblemDetails { Title = "Utilizador inativo." });

            // rotação: revoga o antigo, emite um par novo
            existing.RevokedAt = DateTimeOffset.UtcNow;

            var (accessToken, refreshToken) = await IssueTokensAsync(existing.User, ct);
            return Ok(new RefreshResponse(accessToken, refreshToken));
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
        {
            var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

            var existing = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (existing is not null && existing.RevokedAt is null)
            {
                existing.RevokedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
            }

            // idempotente: mesmo que o token já não exista/esteja revogado, devolve 204
            return NoContent();
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            // responde sempre 200, exista ou não o email — evita confirmar a um atacante que emails existem
            if (user is not null && user.Active)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _emailSender.SendPasswordResetAsync(user.Email!, token, ct);
            }

            return Ok();
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return BadRequest(new ProblemDetails { Title = "Pedido inválido." });

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["Token"] = result.Errors.Select(e => e.Description).ToArray()
                    }));
            }

            return Ok();
        }

        private async Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(
        ApplicationUser user, CancellationToken ct)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
            var (rawRefreshToken, refreshTokenEntity) = _jwtTokenService.GenerateRefreshToken(user.Id);

            _dbContext.RefreshTokens.Add(refreshTokenEntity);
            await _dbContext.SaveChangesAsync(ct);

            return (accessToken, rawRefreshToken);
        }
    }
}