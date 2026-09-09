using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Application.DTOs.Common;
using RegistoFinanceiro.Application.DTOs.FinancialRecords;
using RegistoFinanceiro.Application.Services;
using RegistoFinanceiro.Domain.Entities;
using RegistoFinanceiro.Domain.Services;
using RegistoFinanceiro.Infrastructure.Persistence;

namespace RegistoFinanceiro.Api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class FinancialRecordsController : ControllerBase
    {
        private static readonly string[] ValidTypes = ["income", "expense"];

        private readonly RegistoFinanceiroDbContext _dbContext;
        private readonly IReferenceGenerator _referenceGenerator;
        private readonly IAuditLogger _auditLogger;

        public FinancialRecordsController(RegistoFinanceiroDbContext dbContext, IReferenceGenerator referenceGenerator,
            IAuditLogger auditLogger)
        {
            _dbContext = dbContext;
            _referenceGenerator = referenceGenerator;
            _auditLogger = auditLogger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<FinancialRecordResponse>>> GetAll(
            [FromQuery] FinancialRecordFilter filter,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            var query = _dbContext.FinancialRecords
                .AsNoTracking()
                .Include(r => r.PaymentMethod)
                .Include(r => r.Category)
                .Include(r => r.Vehicle)
                .Include(r => r.CreatedBy)
                .AsQueryable();

            // Regra de visibilidade: Operador só vê os próprios; nunca confiar num parâmetro do cliente para isto
            if (!User.IsInRole("Admin") && !User.IsInRole("Leitor"))
            {
                var currentUserId = GetCurrentUserId();
                query = query.Where(r => r.CreatedById == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(r => r.Type == filter.Type);
            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(r => r.Status == filter.Status);
            if (filter.CategoryId.HasValue)
                query = query.Where(r => r.CategoryId == filter.CategoryId);
            if (filter.VehicleId.HasValue)
                query = query.Where(r => r.VehicleId == filter.VehicleId);
            if (filter.CreatedById.HasValue && (User.IsInRole("Admin") || User.IsInRole("Leitor")))
                query = query.Where(r => r.CreatedById == filter.CreatedById); // só Admin/Leitor podem filtrar por outro utilizador
            if (filter.FromDate.HasValue)
                query = query.Where(r => r.MovementDate >= filter.FromDate);
            if (filter.ToDate.HasValue)
                query = query.Where(r => r.MovementDate <= filter.ToDate);

            query = query.OrderByDescending(r => r.MovementDate);

            var totalCount = await query.CountAsync(ct);
            var records = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var items = records.Select(ToResponse).ToList();

            return Ok(new PagedResult<FinancialRecordResponse>(items, page, pageSize, totalCount));
        }

         [HttpGet("{id:guid}")]
        public async Task<ActionResult<FinancialRecordResponse>> GetById(Guid id, CancellationToken ct)
        {
            var record = await LoadVisibleRecordAsync(id, ct);
            if (record is null)
                return NotFound();

            return Ok(ToResponse(record));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Operador")]
        public async Task<ActionResult<FinancialRecordResponse>> Create(
            CreateFinancialRecordRequest request, CancellationToken ct)
        {
            if (!ValidTypes.Contains(request.Type))
                return ValidationProblem(BuildFieldError("Type", "Type deve ser 'income' ou 'expense'."));

            var categoryExists = await _dbContext.MovementCategories.AnyAsync(c => c.Id == request.CategoryId, ct);
            if (!categoryExists)
                return ValidationProblem(BuildFieldError("CategoryId", "Categoria não encontrada."));

            var paymentMethodExists = await _dbContext.PaymentMethods.AnyAsync(p => p.Id == request.PaymentMethodId, ct);
            if (!paymentMethodExists)
                return ValidationProblem(BuildFieldError("PaymentMethodId", "Forma de pagamento não encontrada."));

            var currentUserId = GetCurrentUserId();
            var reference = await _referenceGenerator.GenerateUniqueReferenceAsync(ct);

            var record = new FinancialRecord
            {
                Id = Guid.NewGuid(),
                Reference = reference,
                Type = request.Type,
                Amount = request.Amount,
                PaymentMethodId = request.PaymentMethodId,
                Reason = request.Reason,
                Description = request.Description,
                MovementDate = request.MovementDate,
                CategoryId = request.CategoryId,
                VehicleId = request.VehicleId,
                CreatedById = currentUserId,
                Status = "draft",
                InternalNotes = request.InternalNotes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.FinancialRecords.Add(record);
            await _dbContext.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(record.Id, "created", currentUserId, beforeData: null, afterData: record, ct);

            var loaded = await LoadVisibleRecordAsync(record.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = record.Id }, ToResponse(loaded!));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Operador")]
        public async Task<IActionResult> Update(Guid id, UpdateFinancialRecordRequest request, CancellationToken ct)
        {
            var record = await LoadOwnedOrAdminRecordAsync(id, ct);
            if (record is null)
                return NotFound();

            if (!FinancialRecordStateMachine.CanEdit(record.Status))
                return Conflict(new ProblemDetails { Title = "Não é possível editar um registo aprovado ou anulado." });

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                // operador só pode alterar Description — comparar tudo o resto contra os valores atuais
                var onlyDescriptionChanged = FinancialRecordEditPolicy.OnlyDescriptionChanged(record, request.Type, request.Amount, request.PaymentMethodId,
                request.Reason, request.MovementDate, request.CategoryId, request.VehicleId, request.InternalNotes);

                if (!onlyDescriptionChanged)
                    return Forbid();
            }

            var before = ToResponse(record);

            record.Type = request.Type;
            record.Amount = request.Amount;
            record.PaymentMethodId = request.PaymentMethodId;
            record.Reason = request.Reason;
            record.Description = request.Description;
            record.MovementDate = request.MovementDate;
            record.CategoryId = request.CategoryId;
            record.VehicleId = request.VehicleId;
            record.InternalNotes = request.InternalNotes;
            record.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(record.Id, "updated", GetCurrentUserId(), before, ToResponse(record), ct);

            return NoContent();
        }

        [HttpPost("{id:guid}/submit")]
        [Authorize(Roles = "Admin,Operador")]
        public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        {
            var record = await LoadOwnedOrAdminRecordAsync(id, ct);
            if (record is null)
                return NotFound();

            if (!FinancialRecordStateMachine.CanSubmit(record.Status))
                return Conflict(new ProblemDetails { Title = $"Só é possível submeter registos em 'draft' (estado atual: {record.Status})." });

            record.Status = "submitted";
            record.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(record.Id, "submitted", GetCurrentUserId(), null, null, ct);

            return NoContent();
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            var record = await _dbContext.FinancialRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (record is null)
                return NotFound();

            if (!FinancialRecordStateMachine.CanApprove(record.Status))
                return Conflict(new ProblemDetails { Title = $"Só é possível aprovar registos em 'submitted' (estado atual: {record.Status})." });

            var currentUserId = GetCurrentUserId();
            record.Status = "approved";
            record.ApprovedById = currentUserId;
            record.ApprovedAt = DateTimeOffset.UtcNow;
            record.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(record.Id, "approved", currentUserId, null, null, ct);

            return NoContent();
        }

        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            var record = await _dbContext.FinancialRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (record is null)
                return NotFound();

            if (!FinancialRecordStateMachine.CanCancel(record.Status))
                return Conflict(new ProblemDetails { Title = $"Só é possível anular registos que não estão 'cancelled' (estado atual: {record.Status})." });

            record.Status = "cancelled";
            record.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(record.Id, "cancelled", GetCurrentUserId(), null, null, ct);

            return NoContent();
        }

        [HttpPost("{id:guid}/adjustment")]
        [Authorize(Roles = "Admin,Operador")]
        public async Task<ActionResult<FinancialRecordResponse>> CreateAdjustment(
            Guid id, CreateFinancialRecordRequest request, CancellationToken ct)
        {
            var original = await LoadVisibleRecordAsync(id, ct);
            if (original is null)
                return NotFound();

            if (!ValidTypes.Contains(request.Type))
                return ValidationProblem(BuildFieldError("Type", "Type deve ser 'income' ou 'expense'."));

            var currentUserId = GetCurrentUserId();
            var reference = await _referenceGenerator.GenerateUniqueReferenceAsync(ct);

            var adjustment = new FinancialRecord
            {
                Id = Guid.NewGuid(),
                Reference = reference,
                Type = request.Type,
                Amount = request.Amount,
                PaymentMethodId = request.PaymentMethodId,
                Reason = request.Reason,
                Description = request.Description,
                MovementDate = request.MovementDate,
                CategoryId = request.CategoryId,
                VehicleId = request.VehicleId,
                CreatedById = currentUserId,
                Status = "draft",
                ParentRecordId = original.Id,
                InternalNotes = request.InternalNotes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.FinancialRecords.Add(adjustment);
            await _dbContext.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(adjustment.Id, "created", currentUserId, null, adjustment, ct);

            var loaded = await LoadVisibleRecordAsync(adjustment.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = adjustment.Id }, ToResponse(loaded!));
        }

        private async Task<FinancialRecord?> LoadVisibleRecordAsync(Guid id, CancellationToken ct)
        {
            var record = await _dbContext.FinancialRecords
                .Include(r => r.PaymentMethod)
                .Include(r => r.Category)
                .Include(r => r.Vehicle)
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (record is null)
                return null;

            if (User.IsInRole("Admin") || User.IsInRole("Leitor"))
                return record;

            return record.CreatedById == GetCurrentUserId() ? record : null;
        }

        private async Task<FinancialRecord?> LoadOwnedOrAdminRecordAsync(Guid id, CancellationToken ct)
        {
            var record = await _dbContext.FinancialRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (record is null)
                return null;

            if (User.IsInRole("Admin"))
                return record;

            return record.CreatedById == GetCurrentUserId() ? record : null;
        }

        private Guid GetCurrentUserId()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private static ValidationProblemDetails BuildFieldError(string field, string message) =>
            new(new Dictionary<string, string[]> { [field] = [message] });

        private static FinancialRecordResponse ToResponse(FinancialRecord r) => new(
            r.Id, r.Reference, r.Type, r.Amount,
            r.PaymentMethodId, r.PaymentMethod.Name,
            r.Reason, r.Description, r.MovementDate,
            r.CategoryId, r.Category.Name,
            r.VehicleId, r.Vehicle?.Plate,
            r.CreatedById, r.CreatedBy.FullName,
            r.ApprovedById, r.ApprovedAt,
            r.Status, r.ParentRecordId, r.InternalNotes,
            r.CreatedAt, r.UpdatedAt);

    }
}