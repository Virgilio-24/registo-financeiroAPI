using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using RegistoFinanceiro.Application.DTOs.Users;

namespace RegistoFinanceiro.Application.Validators.Users
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        private static readonly string[] ValidRoles = ["Admin", "Operador", "Leitor"];

        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Role).Must(r => ValidRoles.Contains(r))
                .WithMessage($"Role deve ser uma de: {string.Join(", ", ValidRoles)}.");
        }
    }

    public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
    {
        private static readonly string[] ValidRoles = ["Admin", "Operador", "Leitor"];

        public UpdateUserRoleRequestValidator()
        {
            RuleFor(x => x.Role).Must(r => ValidRoles.Contains(r))
                .WithMessage($"Role deve ser uma de: {string.Join(", ", ValidRoles)}.");
        }
    }
}