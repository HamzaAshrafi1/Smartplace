using Microsoft.AspNetCore.Identity;

namespace SmartPlace.API.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}