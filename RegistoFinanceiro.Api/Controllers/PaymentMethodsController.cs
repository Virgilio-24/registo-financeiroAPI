using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RegistoFinanceiro.Infrastructure.Persistence;
using RegistoFinanceiro.Application.DTOs.PaymentMethods;
using RegistoFinanceiro.Application.DTOs.Common;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Domain.Entities;

namespace RegistoFinanceiro.Api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PaymentMethodsController : ControllerBase
    {
        private readonly RegistoFinanceiroDbContext _dbContext;
        public PaymentMethodsController(RegistoFinanceiroDbContext dbContext) => _dbContext = dbContext;

        [HttpGet]
        public async Task<ActionResult<PagedResult<PaymentMethodResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            var query = _dbContext.PaymentMethods.AsNoTracking().OrderBy(p => p.Name);
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentMethodResponse (p.Id, p.Code, p.Name, p.Active))
                .ToListAsync(ct);

            return Ok(new PagedResult<PaymentMethodResponse>(items, totalCount, page, pageSize));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PaymentMethodResponse>> GetById(Guid id, CancellationToken ct = default)
        {
            var paymentMethod = await _dbContext.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (paymentMethod == null)
            {
                return NotFound();
            }

            return Ok(new PaymentMethodResponse(paymentMethod.Id, paymentMethod.Code, paymentMethod.Name, paymentMethod.Active));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentMethodResponse>> Create(CreatePaymentMethodRequest request, CancellationToken ct = default)
        {
            var existingPaymentMethod = await _dbContext.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == request.Code && p.Name == request.Name, ct);

            if (existingPaymentMethod != null)
                return Conflict("A payment method with the same code and name already exists.");
                
            var paymentMethod = new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name
            };

            _dbContext.PaymentMethods.Add(paymentMethod);
            await _dbContext.SaveChangesAsync(ct);

            var response = new PaymentMethodResponse(paymentMethod.Id, paymentMethod.Code, paymentMethod.Name, paymentMethod.Active);

            return CreatedAtAction(nameof(GetById), new { id = paymentMethod.Id }, response);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Update(Guid id, UpdatePaymentMethodRequest request, CancellationToken ct = default)
        {
            var paymentMethod = await _dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (paymentMethod == null)
                return NotFound();

            var existingPaymentMethod = await _dbContext.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == request.Code && p.Name == request.Name && p.Id != id, ct);

            if (existingPaymentMethod != null)
                return Conflict("A payment method with the same code and name already exists.");

            paymentMethod.Code = request.Code;
            paymentMethod.Name = request.Name;

            await _dbContext.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}