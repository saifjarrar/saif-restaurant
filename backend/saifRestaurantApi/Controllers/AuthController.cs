using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.IdentityModel.Tokens;
using SaifRestaurantApi.Dto;
using SaifRestaurantApi.Models;
using SaifRestaurantApi.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SaifRestaurantApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<AppUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // UserName is set to Email, so one lookup is sufficient.
            if (await _userManager.FindByEmailAsync(model.Email) is not null)
            {
                return AccountAlreadyExists();
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, model.password);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // Handles two simultaneous registrations for the same email.
                return AccountAlreadyExists();
            }

            if (!result.Succeeded)
            {
                var response = new ApiResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    IsSuccess = false
                };
                response.Errors.AddRange(result.Errors.Select(error => error.Description));
                return BadRequest(response);
            }

            // Roles are created once during application startup, not on every registration.
            var roleResult = await _userManager.AddToRoleAsync(user, SD.Role_Customer);
            if (!roleResult.Succeeded)
            {
                var response = new ApiResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    IsSuccess = false
                };
                response.Errors.AddRange(roleResult.Errors.Select(error => error.Description));
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            var successResponse = new ApiResponse
            {
                Status = HttpStatusCode.OK,
                IsSuccess = true,
                Result = new { message = "Account created successfully" }
            };

            return Ok(successResponse);
        }

        private IActionResult AccountAlreadyExists()
        {
            var response = new ApiResponse
            {
                Status = HttpStatusCode.BadRequest,
                IsSuccess = false
            };
            response.Errors.Add("An account with this email already exists. Please log in.");
            return BadRequest(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            var response = new ApiResponse();
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null || !await _userManager.CheckPasswordAsync(user, model.password))
            {
                response.Status = HttpStatusCode.Unauthorized;
                response.IsSuccess = false;
                response.Errors.Add("Invalid email or password");
                return Unauthorized(response);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
                new(ClaimTypes.Email, user.Email ?? "")
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is missing.");
            var issuer = _configuration["Jwt:Issuer"]!;
            var audience = _configuration["Jwt:Audience"]!;
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var loginResponse = new LoginResponseDto
            {
                Email = user.Email ?? "",
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = roles.Contains(SD.Role_Admin)
                    ? SD.Role_Admin
                    : roles.FirstOrDefault() ?? "",
                ExpiersAt = expiresAt
            };

            response.Status = HttpStatusCode.OK;
            response.IsSuccess = true;
            response.Result = loginResponse;

            return Ok(response);
        }
    }
}