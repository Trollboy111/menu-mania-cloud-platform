using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace MenuManiaCloudPlatform.Controllers;

public class AccountController : Controller
{
    public IActionResult Login()
    {
        string? redirectUrl = Url.Action("GoogleResponse", "Account");

        AuthenticationProperties properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync();

        if (!result.Succeeded)
        {
            return RedirectToAction("Login");
        }

        return RedirectToAction("Index", "Menu");
    }

    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Menu")
            },
            CookieAuthenticationDefaults.AuthenticationScheme
        );
    }
}