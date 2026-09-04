using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.DTOs.Auth
{
    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string AccessToken, string RefreshToken);

    public record RefreshRequest(string RefreshToken);
    public record RefreshResponse(string AccessToken, string RefreshToken);

    public record LogoutRequest(string RefreshToken);

    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NewPassword);
}