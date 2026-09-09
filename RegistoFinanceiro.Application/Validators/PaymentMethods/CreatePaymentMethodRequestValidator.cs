using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using RegistoFinanceiro.Application.DTOs.PaymentMethods;
namespace RegistoFinanceiro.Application.Validators.PaymentMethods
{
    public class CreatePaymentMethodRequestValidator : AbstractValidator<CreatePaymentMethodRequest>
    {
        public CreatePaymentMethodRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .MaximumLength(50).WithMessage("Code cannot exceed 50 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
        }
    }
    public class UpdatePaymentMethodRequestValidator : AbstractValidator<UpdatePaymentMethodRequest>
    {
        public UpdatePaymentMethodRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .MaximumLength(50).WithMessage("Code cannot exceed 50 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Active)
                .NotNull().WithMessage("Active is required.");
        }
    }
}