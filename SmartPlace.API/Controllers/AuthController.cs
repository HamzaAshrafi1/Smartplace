using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // --------------------------------------------------
    // REGISTER
    // Public registration is allowed only for:
    // Student and Recruiter
    // POST: api/Auth/register
    // --------------------------------------------------

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            return BadRequest(new
            {
                message = "Full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new
            {
                message = "Password is required."
            });
        }

        if (string.IsNullOrWhiteSpace(model.Role))
        {
            return BadRequest(new
            {
                message = "Role is required."
            });
        }

        model.FullName = model.FullName.Trim();

        model.Email =
            model.Email.Trim().ToLowerInvariant();

        // Only these roles can self-register.
        string[] publicRoles =
        {
            "Student",
            "Recruiter"
        };

        var normalizedRole =
            publicRoles.FirstOrDefault(role =>
                string.Equals(
                    role,
                    model.Role.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (normalizedRole == null)
        {
            return BadRequest(new
            {
                message =
                    "Public registration is allowed only for Student or Recruiter."
            });
        }

        var userExists =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (userExists != null)
        {
            return BadRequest(new
            {
                message =
                    "A user with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            UserName = model.Email,
            Email = model.Email
        };

        var result =
            await _userManager.CreateAsync(
                user,
                model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = result.Errors.Select(e =>
                    e.Description)
            });
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                normalizedRole);

        if (!roleResult.Succeeded)
        {
            // Remove user if role assignment fails
            await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                message =
                    "Registration failed while assigning user role.",
                errors = roleResult.Errors.Select(e =>
                    e.Description)
            });
        }

        return Ok(new
        {
            message = "Registration successful.",
            role = normalizedRole
        });
    }

    // --------------------------------------------------
    // LOGIN
    // POST: api/Auth/login
    // --------------------------------------------------

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new
            {
                message =
                    "Email and password are required."
            });
        }

        var email =
            model.Email.Trim().ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(
                email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message =
                    "Invalid email or password."
            });
        }

        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                model.Password);

        if (!validPassword)
        {
            return Unauthorized(new
            {
                message =
                    "Invalid email or password."
            });
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var jwtKey =
            _configuration["Jwt:Key"];

        var jwtIssuer =
            _configuration["Jwt:Issuer"];

        var jwtAudience =
            _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) ||
            string.IsNullOrWhiteSpace(jwtIssuer) ||
            string.IsNullOrWhiteSpace(jwtAudience))
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "JWT configuration is incomplete."
                });
        }

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return Ok(new
        {
            token = tokenString,

            expiresAt =
                token.ValidTo,

            user = new
            {
                id = user.Id,
                user.FullName,
                user.Email,
                roles
            }
        });
    }
}