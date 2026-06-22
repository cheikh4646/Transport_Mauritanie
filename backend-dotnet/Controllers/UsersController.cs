using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendDotnet.Data;
using BackendDotnet.Models;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "ADMIN")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return users;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Utilisateur non trouvé.");

            return new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.CreatedAt
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Utilisateur non trouvé.");

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Role))
                user.Role = dto.Role;

            if (!string.IsNullOrEmpty(dto.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            user.UpdatedAt = DateTime.UtcNow;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Utilisateur mis à jour avec succès." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id == 1)
                return BadRequest("Impossible de supprimer le compte administrateur principal.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Utilisateur non trouvé.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Utilisateur supprimé avec succès." });
        }
    }

    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Password { get; set; }
    }
}
