using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Application.DTOs.PaymentMethods
{
    public record PaymentMethodResponse(Guid Id, string Code, string Name, bool Active);

    public record CreatePaymentMethodRequest(string Code, string Name);

    public record UpdatePaymentMethodRequest(string Code, string Name, bool Active);
}