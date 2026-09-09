using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Application.DTOs.Common;
using RegistoFinanceiro.Application.DTOs.Users;
using RegistoFinanceiro.Domain.Entities;
using RegistoFinanceiro.Infrastructure.Persistence;

namespace RegistoFinanceiro.Api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RegistoFinanceiroDbContext _dbContext;
        public UsersController(UserManager<ApplicationUser> userManager, RegistoFinanceiroDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<UserResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            var query = _userManager.Users.AsNoTracking().OrderBy(u => u.Email);

            var totalCount = await query.CountAsync(ct);
            var pagedUsers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = new List<UserResponse>(pagedUsers.Count);
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserResponse(
                    user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "Sem role", user.Active));
            }

            return Ok(new PagedResult<UserResponse>(items, page, pageSize, totalCount));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetById(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new UserResponse(
                user.Id, user.Email!, user.FullName, roles.FirstOrDefault() ?? "Sem role", user.Active));
        }

        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
        {
            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
                return Conflict(new ProblemDetails { Title = "Já existe um utilizador com este email." });

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true, // criado diretamente por um Admin, sem fluxo de confirmação
                FullName = request.FullName,
                Active = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["Password"] = createResult.Errors.Select(e => e.Description).ToArray()
                    }));
            }

            await _userManager.AddToRoleAsync(user, request.Role);

            var response = new UserResponse(user.Id, user.Email!, user.FullName, request.Role, user.Active);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
        }

        [HttpPut("{id:guid}/role")]
        public async Task<IActionResult> UpdateRole(Guid id, UpdateUserRoleRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, request.Role);

            return NoContent();
        }

        [HttpPut("{id:guid}/active")]
        public async Task<IActionResult> SetActive(Guid id, SetUserActiveRequest request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound();

            user.Active = request.Active;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["Active"] = updateResult.Errors.Select(e => e.Description).ToArray()
                    }));
            }
            if (!request.Active)
            {
                await RevokeAllActiveRefreshTokensAsync(user.Id, ct);
            }

            return NoContent();
        }
        private async Task RevokeAllActiveRefreshTokensAsync(Guid userId, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var activeTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
            }

            if (activeTokens.Count > 0)
            {
                await _dbContext.SaveChangesAsync(ct);
            }
        }
    }
}