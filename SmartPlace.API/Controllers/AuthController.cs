using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IConfiguration
        _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager =
            userManager;

        _configuration =
            configuration;
    }

    // ==================================================
    // REGISTER
    // POST: api/Auth/register
    //
    // Public registration is intentionally restricted
    // to Student and Recruiter.
    //
    // Admin and Placement Officer are system-managed
    // accounts and cannot be self-registered.
    // ==================================================

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto model)
    {
        if (string.IsNullOrWhiteSpace(
            model.FullName))
        {
            return BadRequest(new
            {
                message =
                    "Full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            model.Email))
        {
            return BadRequest(new
            {
                message =
                    "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            model.Password))
        {
            return BadRequest(new
            {
                message =
                    "Password is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            model.Role))
        {
            return BadRequest(new
            {
                message =
                    "Role is required."
            });
        }

        // --------------------------------------------------
        // SECURITY:
        // Never allow public registration of privileged
        // system roles.
        // --------------------------------------------------

        string[] allowedPublicRoles =
        {
            "Student",
            "Recruiter"
        };

        var validRole =
            allowedPublicRoles
                .FirstOrDefault(role =>
                    string.Equals(
                        role,
                        model.Role.Trim(),
                        StringComparison
                            .OrdinalIgnoreCase));

        if (validRole == null)
        {
            return BadRequest(new
            {
                message =
                    "Public registration is available only for Student and Recruiter accounts."
            });
        }

        var normalizedEmail =
            model.Email
                .Trim()
                .ToLowerInvariant();

        var existingUser =
            await _userManager
                .FindByEmailAsync(
                    normalizedEmail);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                message =
                    "An account with this email already exists."
            });
        }

        var user =
            new ApplicationUser
            {
                FullName =
                    model.FullName.Trim(),

                UserName =
                    normalizedEmail,

                Email =
                    normalizedEmail,

                EmailConfirmed =
                    true
            };

        var result =
            await _userManager
                .CreateAsync(
                    user,
                    model.Password);

        if (!result.Succeeded)
        {
            var errors =
                result.Errors
                    .Select(e =>
                        e.Description)
                    .ToList();

            return BadRequest(new
            {
                message =
                    errors.FirstOrDefault()
                    ?? "Registration failed.",

                errors
            });
        }

        var roleResult =
            await _userManager
                .AddToRoleAsync(
                    user,
                    validRole);

        if (!roleResult.Succeeded)
        {
            // Avoid leaving an account without
            // its intended role.
            await _userManager
                .DeleteAsync(user);

            var errors =
                roleResult.Errors
                    .Select(e =>
                        e.Description)
                    .ToList();

            return BadRequest(new
            {
                message =
                    "Unable to assign account role.",

                errors
            });
        }

        return Ok(new
        {
            message =
                "Registration successful."
        });
    }

    // ==================================================
    // LOGIN
    // POST: api/Auth/login
    // ==================================================

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto model)
    {
        if (string.IsNullOrWhiteSpace(
            model.Email) ||
            string.IsNullOrWhiteSpace(
                model.Password))
        {
            return Unauthorized(new
            {
                message =
                    "Invalid email or password."
            });
        }

        var normalizedEmail =
            model.Email
                .Trim()
                .ToLowerInvariant();

        var user =
            await _userManager
                .FindByEmailAsync(
                    normalizedEmail);

        if (user == null)
        {
            // Do not reveal whether the email exists.
            return Unauthorized(new
            {
                message =
                    "Invalid email or password."
            });
        }

        // --------------------------------------------------
        // LOCKOUT CHECK
        // --------------------------------------------------

        if (await _userManager
                .IsLockedOutAsync(user))
        {
            var lockoutEnd =
                await _userManager
                    .GetLockoutEndDateAsync(
                        user);

            return Unauthorized(new
            {
                message =
                    "Account temporarily locked due to multiple failed login attempts. Please try again later.",

                lockoutEnd
            });
        }

        var validPassword =
            await _userManager
                .CheckPasswordAsync(
                    user,
                    model.Password);

        if (!validPassword)
        {
            await _userManager
                .AccessFailedAsync(user);

            if (await _userManager
                    .IsLockedOutAsync(user))
            {
                return Unauthorized(new
                {
                    message =
                        "Account temporarily locked due to multiple failed login attempts. Please try again later."
                });
            }

            return Unauthorized(new
            {
                message =
                    "Invalid email or password."
            });
        }

        // Successful login resets failures.
        await _userManager
            .ResetAccessFailedCountAsync(
                user);

        var roles =
            await _userManager
                .GetRolesAsync(user);

        // --------------------------------------------------
        // JWT CLAIMS
        //
        // NameIdentifier is CRITICAL because Student and
        // Recruiter ownership checks use it.
        // --------------------------------------------------

        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    user.Id),

                new(
                    ClaimTypes.Name,
                    user.FullName),

                new(
                    ClaimTypes.Email,
                    user.Email ?? string.Empty),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
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

        if (string.IsNullOrWhiteSpace(
            jwtKey))
        {
            throw new InvalidOperationException(
                "JWT key is missing from configuration.");
        }

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms
                    .HmacSha256);

        var expiresAt =
            DateTime.UtcNow
                .AddHours(2);

        var token =
            new JwtSecurityToken(
                issuer:
                    _configuration[
                        "Jwt:Issuer"],

                audience:
                    _configuration[
                        "Jwt:Audience"],

                claims:
                    claims,

                expires:
                    expiresAt,

                signingCredentials:
                    credentials);

        var writtenToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return Ok(new
        {
            token =
                writtenToken,

            expiresAt,

            user = new
            {
                user.Id,

                user.FullName,

                email =
                    user.Email,

                roles
            }
        });
    }
}