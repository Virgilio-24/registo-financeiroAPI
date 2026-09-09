using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistoFinanceiro.Application.DTOs.Common;
using RegistoFinanceiro.Application.DTOs.MovementCategory;
using RegistoFinanceiro.Infrastructure.Persistence;

namespace RegistoFinanceiro.Api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class MovementCategoryController : ControllerBase
    {
        private readonly RegistoFinanceiroDbContext _dbContext;
        public MovementCategoryController(RegistoFinanceiroDbContext dbContext) => _dbContext = dbContext;

        [HttpGet]
        public async Task<ActionResult<PagedResult<MovementCategoryResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            var query = _dbContext.MovementCategories.AsNoTracking().OrderBy(mc => mc.Name);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(mc => new MovementCategoryResponse(mc.Id, mc.Name, mc.Type, mc.Active))
                .ToListAsync(ct);

            return Ok(new PagedResult<MovementCategoryResponse>(items, page, pageSize, totalCount));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MovementCategoryResponse>> GetById(Guid id, CancellationToken ct)
        {
            var movementCategory = await _dbContext.MovementCategories.AsNoTracking().FirstOrDefaultAsync(mc => mc.Id == id, ct);
            if (movementCategory is null)
                return NotFound();

            return Ok(new MovementCategoryResponse(
                movementCategory.Id, movementCategory.Name, movementCategory.Type, movementCategory.Active));
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MovementCategoryResponse>> Create(CreateMovementCategoryRequest request, CancellationToken ct)
        {
            var nameExists = await _dbContext.MovementCategories.AnyAsync(mc => mc.Name == request.Name, ct);
            if (nameExists)
                return Conflict("A movement category with the same name already exists.");

            var movementCategory = new Domain.Entities.MovementCategory
            {
                Name = request.Name,
                Type = request.Type,
                Active = true
            };

            _dbContext.MovementCategories.Add(movementCategory);
            await _dbContext.SaveChangesAsync(ct);

            var response = new MovementCategoryResponse(
                movementCategory.Id, movementCategory.Name, movementCategory.Type, movementCategory.Active);

            return CreatedAtAction(nameof(GetById), new { id = movementCategory.Id }, response);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Update(Guid id, UpdateMovementCategoryRequest request, CancellationToken ct)
        {
            var movementCategory = await _dbContext.MovementCategories.FirstOrDefaultAsync(mc => mc.Id == id, ct);
            if (movementCategory is null)
                return NotFound();

            var nameExists = await _dbContext.MovementCategories.AnyAsync(mc => mc.Name == request.Name && mc.Id != id, ct);
            if (nameExists)
                return Conflict("A movement category with the same name already exists.");

            movementCategory.Name = request.Name;
            movementCategory.Type = request.Type;
            movementCategory.Active = request.Active;

            await _dbContext.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}