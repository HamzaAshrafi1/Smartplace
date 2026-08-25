using Microsoft.AspNetCore.Identity;

namespace SmartPlace.API.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public Student? Student { get; set; }

    // Recruiter accounts can own one company.
    public Company? Company { get; set; }
}