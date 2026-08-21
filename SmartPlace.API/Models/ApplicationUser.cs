using Microsoft.AspNetCore.Identity;

namespace SmartPlace.API.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // One-to-One relationship with Student profile.
    // This will be null for Recruiter/Admin/PlacementOfficer
    // and for Student users who have not completed
    // their profile yet.
    public Student? Student { get; set; }
}