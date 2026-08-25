using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class AccountController : Controller
{
    private readonly AuthApiService
        _authApiService;

    public AccountController(
        AuthApiService authApiService)
    {
        _authApiService =
            authApiService;
    }

    // ==================================================
    // LOGIN GET
    // ==================================================

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session
            .GetString("JWToken") != null)
        {
            return RedirectToDashboard();
        }

        return View(
            new LoginRequest());
    }

    // ==================================================
    // LOGIN POST
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        Login(LoginRequest model)
    {
        if (string.IsNullOrWhiteSpace(
            model.Email))
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(
            model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _authApiService
                .LoginAsync(model);

        if (!result.Success ||
            result.Response == null)
        {
            ViewBag.Error =
                result.Message;

            return View(model);
        }

        var response =
            result.Response;

        HttpContext.Session.SetString(
            "JWToken",
            response.Token);

        HttpContext.Session.SetString(
            "UserName",
            response.User.FullName);

        HttpContext.Session.SetString(
            "UserEmail",
            response.User.Email);

        HttpContext.Session.SetString(
            "UserId",
            response.User.Id);

        var role =
            response.User.Roles
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(
            role))
        {
            HttpContext.Session.SetString(
                "UserRole",
                role);
        }

        return RedirectToDashboard();
    }

    // ==================================================
    // REGISTER GET
    // ==================================================

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session
            .GetString("JWToken") != null)
        {
            return RedirectToDashboard();
        }

        return View(
            new RegisterRequest());
    }

    // ==================================================
    // REGISTER POST
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        Register(RegisterRequest model)
    {
        if (string.IsNullOrWhiteSpace(
            model.FullName))
        {
            ModelState.AddModelError(
                nameof(model.FullName),
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(
            model.Email))
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(
            model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password is required.");
        }

        if (model.Role != "Student" &&
            model.Role != "Recruiter")
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Select Student or Recruiter.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _authApiService
                .RegisterAsync(model);

        if (!result.Success)
        {
            ViewBag.Error =
                result.Message;

            return View(model);
        }

        TempData["Success"] =
            "Registration successful. Please log in.";

        return RedirectToAction(
            nameof(Login));
    }

    // ==================================================
    // LOGOUT
    // ==================================================

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction(
            nameof(Login));
    }

    // ==================================================
    // ROLE REDIRECT
    // ==================================================

    private IActionResult RedirectToDashboard()
    {
        var role =
            HttpContext.Session
                .GetString("UserRole");

        return role switch
        {
            "Student" =>
                RedirectToAction(
                    "Index",
                    "StudentDashboard"),

            "Recruiter" =>
                RedirectToAction(
                    "Index",
                    "RecruiterDashboard"),

            "PlacementOfficer" =>
                RedirectToAction(
                    "Index",
                    "PlacementDashboard"),

            "Admin" =>
                RedirectToAction(
                    "Index",
                    "AdminDashboard"),

            _ =>
                RedirectToAction(
                    "Login",
                    "Account")
        };
    }
}