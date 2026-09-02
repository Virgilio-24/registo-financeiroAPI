using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RegistoFinanceiro.Application.Configuration;
using RegistoFinanceiro.Domain.Entities;

namespace RegistoFinanceiro.Infrastructure.Seed
{
    public class DatabaseSeederHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SeedOptions _seedOptions;
        private readonly ILogger<DatabaseSeederHostedService> _logger;

        private static readonly string[] Roles = ["Admin", "Operador", "Leitor"];

        public DatabaseSeederHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<SeedOptions> seedOptions,
            ILogger<DatabaseSeederHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _seedOptions = seedOptions.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            foreach (var roleName in Roles)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                    continue;

                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    _logger.LogError(
                        "Falha ao criar a role {Role}: {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            if (string.IsNullOrWhiteSpace(_seedOptions.AdminEmail) ||
                string.IsNullOrWhiteSpace(_seedOptions.AdminPassword))
            {
                _logger.LogWarning("Seed:AdminEmail/AdminPassword não configurados — a saltar seed do admin.");
                return;
            }

            var existingAdmin = await userManager.FindByEmailAsync(_seedOptions.AdminEmail);
            if (existingAdmin is not null)
                return; // já existe, não faz nada — é isto que torna o seed idempotente

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = _seedOptions.AdminEmail,
                Email = _seedOptions.AdminEmail,
                EmailConfirmed = true,
                FullName = _seedOptions.AdminFullName,
                Active = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var createResult = await userManager.CreateAsync(admin, _seedOptions.AdminPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Falha ao criar o utilizador admin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, "Admin");
            _logger.LogInformation("Utilizador admin inicial criado: {Email}", admin.Email);
        }
    }
}