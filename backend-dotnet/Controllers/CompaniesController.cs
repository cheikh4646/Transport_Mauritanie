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
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/companies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies.Include(c => c.Manager).Where(c => c.IsActive).ToListAsync();
        }

        // GET: api/companies/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompany(int id)
        {
            var company = await _context.Companies.Include(c => c.Manager).FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound("Compagnie non trouvée.");
            }

            return company;
        }

        // POST: api/companies (Admin uniquement)
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<ActionResult<Company>> PostCompany(Company company)
        {
            if (await _context.Companies.AnyAsync(c => c.Name == company.Name))
            {
                return BadRequest("Une compagnie avec ce nom existe déjà.");
            }

            company.CreatedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
        }

        // PUT: api/companies/5 (Admin ou Manager de la compagnie)
        [Authorize(Roles = "ADMIN,COMPANY")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCompany(int id, Company updatedCompany)
        {
            if (id != updatedCompany.Id)
            {
                return BadRequest("ID incohérent.");
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound("Compagnie non trouvée.");
            }

            // Si c'est un manager de compagnie, vérifier qu'il est bien le manager de CETTE compagnie
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (userRole == "COMPANY" && company.ManagerId != userId)
            {
                return StatusCode(403, "Vous n'êtes pas autorisé à modifier cette compagnie.");
            }

            company.Name = updatedCompany.Name;
            company.Phone = updatedCompany.Phone;
            company.Email = updatedCompany.Email;
            company.Address = updatedCompany.Address;
            company.IsActive = updatedCompany.IsActive;
            company.UpdatedAt = DateTime.UtcNow;

            if (userRole == "ADMIN" && updatedCompany.ManagerId != null)
            {
                company.ManagerId = updatedCompany.ManagerId;
            }

            _context.Entry(company).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Companies.AnyAsync(c => c.Id == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/companies/5 (Admin uniquement)
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound("Compagnie non trouvée.");
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
