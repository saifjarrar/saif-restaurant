using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly IConfiguration _configuration;

        private readonly ApiResponse _response;


        public AuthController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)

        {

            _userManager = userManager;

            _roleManager = roleManager;

            this._configuration = configuration;

            _response = new ApiResponse();

        }


        [HttpPost("register")]


        public async Task<IActionResult> Register([FromBody] RegisterDto model)

        {

            if (await _userManager.FindByEmailAsync(model.Email) != null ||
                await _userManager.FindByNameAsync(model.Email) != null)
            {
                return AccountAlreadyExists();
            }

            var user = new AppUser { UserName = model.Email, Email = model.Email };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, model.password);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException sqlException &&
                sqlException.Number == 2601 &&
                sqlException.Message.Contains("'UserNameIndex'", StringComparison.Ordinal))
            {
                return AccountAlreadyExists();
            }

            if (!result.Succeeded)

            {

                _response.Status = HttpStatusCode.BadRequest;

                _response.IsSuccess = false;

                _response.Errors.AddRange(

                    result.Errors.Select(e => e.Description)

                );


                return BadRequest(_response);



            }

            // make sure both roles exist (before, it checked Admin but created Customer)
            foreach (var roleName in new[] { SD.Role_Admin, SD.Role_Customer })
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }



            var roleResult = await _userManager.AddToRoleAsync(

                user,

                SD.Role_Customer

            );

            _response.Status = HttpStatusCode.OK;

            _response.IsSuccess = true;

            _response.Result = new
            {

                message = "Account created successfully"
            };


            return Ok(_response);

        }

        private IActionResult AccountAlreadyExists()
        {
            _response.Status = HttpStatusCode.BadRequest;
            _response.IsSuccess = false;
            _response.Errors.Add("An account with this email already exists. Please log in.");
            return BadRequest(_response);
        }

        [HttpPost("login")]

        public async Task<IActionResult> Login(

            [FromBody] LoginRequestDto model)

        {

            var response = new ApiResponse();


            var user = await _userManager.FindByEmailAsync(model.Email);


            if (user == null ||

                !await _userManager.CheckPasswordAsync(

                    user,

                    model.password))

            {

                response.Status = HttpStatusCode.Unauthorized;

                response.IsSuccess = false;

                response.Errors.Add("Invalid email or password");


                return Unauthorized(response);

            }


            var roles = await _userManager.GetRolesAsync(user);


            var claims = new List<Claim>

        {

            new Claim(ClaimTypes.NameIdentifier, user.Id),

            new Claim(

                ClaimTypes.Name,

                user.UserName ?? user.Email ?? ""            ),

            new Claim(ClaimTypes.Email, user.Email ?? "")

        };


            foreach (var role in roles)

            {

                claims.Add(new Claim(ClaimTypes.Role, role));

            }


            var key = _configuration["Jwt:Key"]!;

            var issuer = _configuration["Jwt:Issuer"]!;

            var audience = _configuration["Jwt:Audience"]!;


            var expiryMinutes = int.Parse(

                _configuration["Jwt:ExpiryMinutes"] ?? "60");


            var signingKey = new SymmetricSecurityKey(

                Encoding.UTF8.GetBytes(key)

            );


            var credentials = new SigningCredentials(

                signingKey,

                SecurityAlgorithms.HmacSha256

            );


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

                Token = new JwtSecurityTokenHandler()

                    .WriteToken(token),

                // if the user has the Admin role, always return Admin
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
