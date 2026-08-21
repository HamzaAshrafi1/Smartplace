using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class AccountController : Controller
{
    private readonly AuthApiService _authApiService;

    public AccountController(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    // --------------------------------------------------
    // LOGIN PAGE
    // GET: /Account/Login
    // --------------------------------------------------

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // --------------------------------------------------
    // LOGIN SUBMIT
    // POST: /Account/Login
    // --------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> Login(
        LoginRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _authApiService.LoginAsync(model);

        if (result == null)
        {
            ViewBag.Error =
                "Invalid email or password.";

            return View(model);
        }

        // Store JWT in session
        HttpContext.Session.SetString(
            "JWToken",
            result.Token);

        HttpContext.Session.SetString(
            "UserName",
            result.User.FullName);

        HttpContext.Session.SetString(
            "UserEmail",
            result.User.Email);

        if (result.User.Roles.Count > 0)
        {
            HttpContext.Session.SetString(
                "UserRole",
                result.User.Roles[0]);
        }

        // Role-based redirect
        var role =
            result.User.Roles.FirstOrDefault();

        if (role == "Student")
        {
            return RedirectToAction(
                "Index",
                "StudentDashboard");
        }

        if (role == "Recruiter")
        {
            return RedirectToAction(
                "Index",
                "RecruiterDashboard");
        }

        if (role == "PlacementOfficer")
        {
            return RedirectToAction(
                "Index",
                "PlacementDashboard");
        }

        if (role == "Admin")
        {
            return RedirectToAction(
                "Index",
                "AdminDashboard");
        }

        return RedirectToAction(
            "Index",
            "Home");
    }

    // --------------------------------------------------
    // REGISTER PAGE
    // GET: /Account/Register
    // --------------------------------------------------

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // --------------------------------------------------
    // REGISTER SUBMIT
    // POST: /Account/Register
    // --------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success =
            await _authApiService.RegisterAsync(model);

        if (!success)
        {
            ViewBag.Error =
                "Registration failed. Please check the details and try again.";

            return View(model);
        }

        TempData["Success"] =
            "Registration successful. Please login.";

        return RedirectToAction(
            "Login");
    }

    // --------------------------------------------------
    // LOGOUT
    // --------------------------------------------------

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction(
            "Login");
    }
}