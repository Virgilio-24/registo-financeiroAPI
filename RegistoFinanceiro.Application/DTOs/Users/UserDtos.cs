using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.DTOs.Users
{
    public record UserResponse(Guid Id, string Email, string FullName, string Role, bool Active);

    public record CreateUserRequest(string Email, string Password, string FullName, string Role);

    public record UpdateUserRoleRequest(string Role);

    public record SetUserActiveRequest(bool Active);
}