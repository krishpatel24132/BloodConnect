using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BloodConnect.Models;
using Microsoft.AspNetCore.Mvc;

namespace BloodConnect.Controllers;

public class AccountController : Controller
{
    private const int MaxFailedAttempts = 5;
    private const int LockMinutes = 1;

    private readonly IConfiguration _config;

    public AccountController(IConfiguration config) => _config = config;

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model, string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        // State management: after 5 wrong attempts the session is locked for 1 minute
        var lockText = HttpContext.Session.GetString("LockUntil");
        if (DateTime.TryParse(lockText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lockUntil)
            && lockUntil > DateTime.UtcNow)
        {
            var seconds = (int)Math.Ceiling((lockUntil - DateTime.UtcNow).TotalSeconds);
            ModelState.AddModelError(string.Empty, $"Too many failed attempts. Try again in {seconds} seconds.");
            return View(model);
        }

        var username = _config["AdminCredentials:Username"];
        var password = _config["AdminCredentials:Password"];

        if (SameText(model.Username, username) && SameText(model.Password, password))
        {
            HttpContext.Session.Remove("FailedLogins");
            HttpContext.Session.Remove("LockUntil");
            HttpContext.Session.SetString("Admin", model.Username);
            TempData["Success"] = $"Welcome back, {model.Username}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        var failed = (HttpContext.Session.GetInt32("FailedLogins") ?? 0) + 1;
        if (failed >= MaxFailedAttempts)
        {
            HttpContext.Session.SetString("LockUntil",
                DateTime.UtcNow.AddMinutes(LockMinutes).ToString("O", CultureInfo.InvariantCulture));
            HttpContext.Session.SetInt32("FailedLogins", 0);
            ModelState.AddModelError(string.Empty, $"Too many failed attempts. Locked for {LockMinutes} minute.");
        }
        else
        {
            HttpContext.Session.SetInt32("FailedLogins", failed);
            ModelState.AddModelError(string.Empty,
                $"Invalid username or password. {MaxFailedAttempts - failed} attempt(s) left.");
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    // Constant-time comparison so response time does not leak how much of the password matched
    private static bool SameText(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a ?? string.Empty),
            Encoding.UTF8.GetBytes(b ?? string.Empty));
}
