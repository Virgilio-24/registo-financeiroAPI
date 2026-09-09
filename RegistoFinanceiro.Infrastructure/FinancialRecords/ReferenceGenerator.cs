using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RegistoFinanceiro.Application.Services;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Infrastructure.Persistence;
using RegistoFinanceiro.Domain.Services;

namespace RegistoFinanceiro.Infrastructure.FinancialRecords
{
    public class ReferenceGenerator : IReferenceGenerator
    {
        private const int MaxAttempts = 10;
        private readonly RegistoFinanceiroDbContext _dbContext;

        public ReferenceGenerator(RegistoFinanceiroDbContext dbContext) => _dbContext = dbContext;

        public async Task<string> GenerateUniqueReferenceAsync(CancellationToken ct)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var candidate = Build();
                var exists = await _dbContext.FinancialRecords.AnyAsync(r => r.Reference == candidate, ct);
                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                $"Não foi possível gerar uma referência única após {MaxAttempts} tentativas.");
        }

        private static string Build() => ReferenceCodeGenerator.NewCandidate();
    }
}