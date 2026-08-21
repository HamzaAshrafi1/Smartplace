using SmartPlace.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// MVC
// --------------------------------------------------

builder.Services.AddControllersWithViews();

// --------------------------------------------------
// SESSION
// --------------------------------------------------

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromHours(2);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Needed by StudentApiService
builder.Services.AddHttpContextAccessor();

// --------------------------------------------------
// HTTP CLIENT FOR SMARTPLACE API
// --------------------------------------------------

builder.Services.AddHttpClient(
    "SmartPlaceAPI",
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration[
                "ApiSettings:BaseUrl"]
            ?? "https://localhost:7242/");
    });

// --------------------------------------------------
// FRONTEND SERVICES
// --------------------------------------------------

builder.Services.AddScoped<AuthApiService>();

builder.Services.AddScoped<StudentApiService>();

// --------------------------------------------------
// BUILD APP
// --------------------------------------------------

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
// DEFAULT ROUTE
// --------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}");

app.Run();