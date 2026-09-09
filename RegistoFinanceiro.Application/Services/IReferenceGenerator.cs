using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.Services
{
    public interface IReferenceGenerator
    {
        Task<string> GenerateUniqueReferenceAsync(CancellationToken ct);
    }
}