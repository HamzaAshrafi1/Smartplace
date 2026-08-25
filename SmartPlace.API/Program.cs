using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartPlace.API.Data;
using SmartPlace.API.Models;
using SmartPlace.API.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// CONTROLLERS + JSON CYCLE HANDLING
// --------------------------------------------------

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

// --------------------------------------------------
// SWAGGER + JWT AUTHORIZATION
// --------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer",
                document)] = []
        });
});

// --------------------------------------------------
// DATABASE
// --------------------------------------------------

builder.Services.AddDbContext<SmartPlaceDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString("DefaultConnection")));

// --------------------------------------------------
// IDENTITY
// --------------------------------------------------

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(
        options =>
        {
            // Password policy
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;

            // NEW:
            // Password must contain at least one
            // non-alphanumeric / special character.
            options.Password.RequireNonAlphanumeric = true;

            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // User rules
            options.User.RequireUniqueEmail = true;

            // Lockout protection
            options.Lockout.AllowedForNewUsers = true;

            options.Lockout.MaxFailedAccessAttempts = 5;

            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(10);
        })
    .AddEntityFrameworkStores<SmartPlaceDbContext>()
    .AddDefaultTokenProviders();

// --------------------------------------------------
// HYBRID AI + ELIGIBILITY SERVICES
// --------------------------------------------------

builder.Services.AddScoped<SkillExtractionService>();

builder.Services.AddScoped<JobMatchingService>();

builder.Services.AddScoped<OpenAIAnalysisService>();

builder.Services.AddScoped<JobEligibilityService>();

// --------------------------------------------------
// JWT AUTHENTICATION
// --------------------------------------------------

var jwtKey =
    builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT key is missing from configuration.");
}

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,

                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

// --------------------------------------------------
// BUILD APPLICATION
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// CREATE DEFAULT ROLES + SYSTEM USERS
// --------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<
                RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<
                UserManager<ApplicationUser>>();

    string[] roles =
    {
        "Admin",
        "Student",
        "Recruiter",
        "PlacementOfficer"
    };

    // --------------------------------------------------
    // CREATE ROLES
    // --------------------------------------------------

    foreach (var role in roles)
    {
        if (!await roleManager
                .RoleExistsAsync(role))
        {
            var createRoleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            if (!createRoleResult.Succeeded)
            {
                var errors =
                    string.Join(
                        ", ",
                        createRoleResult.Errors
                            .Select(e =>
                                e.Description));

                throw new InvalidOperationException(
                    $"Unable to create role '{role}': {errors}");
            }
        }
    }

    // --------------------------------------------------
    // DEFAULT ADMIN ACCOUNT
    // --------------------------------------------------

    const string adminEmail =
        "admin@smartplace.com";

    const string adminPassword =
        "Admin@123";

    var adminUser =
        await userManager.FindByEmailAsync(
            adminEmail);

    if (adminUser == null)
    {
        adminUser =
            new ApplicationUser
            {
                FullName =
                    "SmartPlace Administrator",

                UserName =
                    adminEmail,

                Email =
                    adminEmail,

                EmailConfirmed =
                    true
            };

        var createAdminResult =
            await userManager.CreateAsync(
                adminUser,
                adminPassword);

        if (!createAdminResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    createAdminResult.Errors
                        .Select(e =>
                            e.Description));

            throw new InvalidOperationException(
                $"Unable to create default Admin account: {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(
            adminUser,
            "Admin"))
    {
        var addAdminRoleResult =
            await userManager.AddToRoleAsync(
                adminUser,
                "Admin");

        if (!addAdminRoleResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    addAdminRoleResult.Errors
                        .Select(e =>
                            e.Description));

            throw new InvalidOperationException(
                $"Unable to assign Admin role: {errors}");
        }
    }

    // --------------------------------------------------
    // DEFAULT PLACEMENT OFFICER ACCOUNT
    // --------------------------------------------------

    const string placementEmail =
        "placement@smartplace.com";

    const string placementPassword =
        "Placement@123";

    var placementUser =
        await userManager.FindByEmailAsync(
            placementEmail);

    if (placementUser == null)
    {
        placementUser =
            new ApplicationUser
            {
                FullName =
                    "Placement Officer",

                UserName =
                    placementEmail,

                Email =
                    placementEmail,

                EmailConfirmed =
                    true
            };

        var createPlacementResult =
            await userManager.CreateAsync(
                placementUser,
                placementPassword);

        if (!createPlacementResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    createPlacementResult.Errors
                        .Select(e =>
                            e.Description));

            throw new InvalidOperationException(
                $"Unable to create default Placement Officer account: {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(
            placementUser,
            "PlacementOfficer"))
    {
        var addPlacementRoleResult =
            await userManager.AddToRoleAsync(
                placementUser,
                "PlacementOfficer");

        if (!addPlacementRoleResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    addPlacementRoleResult.Errors
                        .Select(e =>
                            e.Description));

            throw new InvalidOperationException(
                $"Unable to assign PlacementOfficer role: {errors}");
        }
    }
}

// --------------------------------------------------
// HTTP PIPELINE
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();