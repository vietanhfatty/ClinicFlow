using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Controllers;

public class AccountController : Controller
{
    private readonly AuthApiService _authApiService;
    private readonly PatientApiService _patientApiService;

    public AccountController(AuthApiService authApiService, PatientApiService patientApiService)
    {
        _authApiService = authApiService;
        _patientApiService = patientApiService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        // Login goes through the WebApi (JWT issuer). WebMvc never touches the DB directly.
        var result = await _authApiService.LoginAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View();
        }

        // BFF pattern: the browser keeps its usual Cookie Authentication session for
        // [Authorize]/redirect-to-login UX, but the JWT issued by WebApi is stashed inside
        // that cookie as a claim so BearerTokenForwardingHandler can forward it as a
        // Bearer token on every outgoing call to WebApi.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, result.Username!),
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()!),
            new Claim(ClaimTypes.Role, result.RoleName!),
            new Claim("FullName", result.FullName!),
            new Claim("AccessToken", result.AccessToken!)
        };

        if (result.PatientId.HasValue)
            claims.Add(new Claim("PatientId", result.PatientId.Value.ToString()));
        if (result.DoctorId.HasValue)
            claims.Add(new Claim("DoctorId", result.DoctorId.Value.ToString()));
        if (result.StaffId.HasValue)
            claims.Add(new Claim("StaffId", result.StaffId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CreatePatientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _patientApiService.CreateAsync(request);
            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction(nameof(Login));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(request);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Registration failed: " + ex.Message);
            return View(request);
        }
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordRequest("", ""));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _authApiService.ChangePasswordAsync(request);
            TempData["SuccessMessage"] = "Password changed successfully. Please log in again.";
            return RedirectToAction(nameof(Logout));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(request);
        }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
