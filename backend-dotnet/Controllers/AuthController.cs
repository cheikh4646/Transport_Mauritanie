using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendDotnet.Data;
using BackendDotnet.DTOs;
using BackendDotnet.Helpers;
using BackendDotnet.Models;
using BackendDotnet.Services;

namespace BackendDotnet.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public Task<User?> GetCurrentUserAsync() =>
            _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
            {
                return BadRequest("Cet e-mail est déjà utilisé.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email.ToLower(),
                Password = PasswordHasher.HashPassword(dto.Password),
                Role = dto.Role.ToUpper() is "ADMIN" or "COMPANY" or "TRAVELER" ? dto.Role.ToUpper() : "TRAVELER",
                CreatedAt = System.DateTime.UtcNow,
                UpdatedAt = System.DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);

            return new AuthResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Token = token
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

            if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.Password))
            {
                return Unauthorized("E-mail ou mot de passe incorrect.");
            }

            var token = _tokenService.CreateToken(user);

            return new AuthResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Token = token
            };
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<User>> GetMe()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userIdClaim));
            if (user == null) return NotFound("Utilisateur non trouvé.");

            return user;
        }
    }
}
