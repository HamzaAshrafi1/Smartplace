using SmartPlace.Web.Services;

var builder =
    WebApplication.CreateBuilder(args);

// --------------------------------------------------
// MVC
// --------------------------------------------------

builder.Services
    .AddControllersWithViews();

// --------------------------------------------------
// SESSION
// --------------------------------------------------

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromHours(2);

    options.Cookie.HttpOnly =
        true;

    options.Cookie.IsEssential =
        true;

    options.Cookie.SameSite =
        SameSiteMode.Lax;
});

builder.Services
    .AddHttpContextAccessor();

// --------------------------------------------------
// SMARTPLACE API CLIENT
// --------------------------------------------------

builder.Services.AddHttpClient(
    "SmartPlaceAPI",
    client =>
    {
        client.BaseAddress =
            new Uri(
                builder.Configuration[
                    "ApiSettings:BaseUrl"]
                ?? "https://localhost:7242/");
    });

// --------------------------------------------------
// FRONTEND SERVICES
// --------------------------------------------------

builder.Services
    .AddScoped<AuthApiService>();

builder.Services
    .AddScoped<StudentApiService>();

builder.Services
    .AddScoped<ManagementApiService>();

builder.Services
    .AddScoped<UserManagementApiService>();

// --------------------------------------------------
// BUILD
// --------------------------------------------------

var app =
    builder.Build();

if (!app.Environment
    .IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

// --------------------------------------------------
// MIDDLEWARE
// --------------------------------------------------

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// --------------------------------------------------
// ROUTING
// --------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}");

app.Run();