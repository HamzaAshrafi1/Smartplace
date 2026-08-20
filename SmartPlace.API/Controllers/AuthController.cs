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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // POST: api/Auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto model)
    {
        var userExists =
            await _userManager.FindByEmailAsync(model.Email);

        if (userExists != null)
        {
            return BadRequest(new
            {
                message = "User already exists."
            });
        }

        var validRoles = new[]
        {
            "Admin",
            "Student",
            "Recruiter",
            "PlacementOfficer"
        };

        if (!validRoles.Contains(model.Role))
        {
            return BadRequest(new
            {
                message = "Invalid role."
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
            return BadRequest(result.Errors);
        }

        await _userManager.AddToRoleAsync(
            user,
            model.Role);

        return Ok(new
        {
            message = "Registration successful."
        });
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto model)
    {
        var user =
            await _userManager.FindByEmailAsync(
                model.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
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
                message = "Invalid email or password."
            });
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email!)
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer:
                    _configuration["Jwt:Issuer"],

                audience:
                    _configuration["Jwt:Audience"],

                claims: claims,

                expires:
                    DateTime.Now.AddHours(2),

                signingCredentials:
                    credentials);

        return Ok(new
        {
            token =
                new JwtSecurityTokenHandler()
                    .WriteToken(token),

            roles
        });
    }
}