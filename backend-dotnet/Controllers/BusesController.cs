using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendDotnet.Data;
using BackendDotnet.Models;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/buses")]
    [Authorize]
    public class BusesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusesController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetUserCompanyIdAsync()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole == "ADMIN") return null;

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == userId);
            return company?.Id;
        }

        // GET: api/buses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bus>>> GetBuses()
        {
            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue)
            {
                return await _context.Buses
                    .Include(b => b.Company)
                    .Where(b => b.CompanyId == companyId.Value)
                    .ToListAsync();
            }

            return await _context.Buses
                .Include(b => b.Company)
                .ToListAsync();
        }

        // GET: api/buses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Bus>> GetBus(int id)
        {
            var bus = await _context.Buses.Include(b => b.Company).FirstOrDefaultAsync(b => b.Id == id);
            if (bus == null) return NotFound("Bus non trouvé.");

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && bus.CompanyId != companyId.Value)
            {
                return Forbid("Vous n'êtes pas autorisé à accéder aux informations de ce bus.");
            }

            return bus;
        }

        // POST: api/buses
        [HttpPost]
        [Authorize(Roles = "ADMIN,COMPANY")]
        public async Task<ActionResult<Bus>> PostBus(Bus bus)
        {
            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue)
            {
                bus.CompanyId = companyId.Value; // Forcer l'ID de la compagnie du gérant connecté
            }
            else if (bus.CompanyId == 0)
            {
                return BadRequest("L'identifiant de la compagnie (company_id) est requis pour les administrateurs.");
            }

            bus.CreatedAt = DateTime.UtcNow;
            bus.UpdatedAt = DateTime.UtcNow;

            _context.Buses.Add(bus);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBus), new { id = bus.Id }, bus);
        }

        // PUT: api/buses/5
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,COMPANY")]
        public async Task<IActionResult> PutBus(int id, Bus updatedBus)
        {
            if (id != updatedBus.Id) return BadRequest("ID incohérent.");

            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound("Bus non trouvé.");

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && bus.CompanyId != companyId.Value)
            {
                return Forbid("Vous n'êtes pas autorisé à modifier ce bus.");
            }

            bus.BusNumber = updatedBus.BusNumber;
            bus.Capacity = updatedBus.Capacity;
            bus.UpdatedAt = DateTime.UtcNow;

            if (!companyId.HasValue && updatedBus.CompanyId != 0)
            {
                bus.CompanyId = updatedBus.CompanyId;
            }

            _context.Entry(bus).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/buses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,COMPANY")]
        public async Task<IActionResult> DeleteBus(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound("Bus non trouvé.");

            var companyId = await GetUserCompanyIdAsync();
            if (companyId.HasValue && bus.CompanyId != companyId.Value)
            {
                return Forbid("Vous n'êtes pas autorisé à supprimer ce bus.");
            }

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
