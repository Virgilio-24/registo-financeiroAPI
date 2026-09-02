using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RegistoFinanceiro.Domain.Entities;

namespace RegistoFinanceiro.Application.Services
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        (string RawToken, RefreshTokens Entity) GenerateRefreshToken(Guid userId);
        string HashToken(string rawToken);
    }
}