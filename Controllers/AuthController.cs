using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using OPA_Pay.ViewModels;
using System.Security.Claims;

namespace OPA_Pay.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAgentProfileRepository _agentRepo;
        private readonly IWalletSetupService _walletSetup;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAgentProfileRepository agentRepo,
            IWalletSetupService walletSetup)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _agentRepo = agentRepo;
            _walletSetup = walletSetup;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }

            var existingClaims = await _userManager.GetClaimsAsync(user);
            if (existingClaims.Count > 0)
                await _userManager.RemoveClaimsAsync(user, existingClaims);

            await _userManager.AddClaimAsync(user, new Claim("FullName", user.FullName ?? ""));

            if (await _userManager.IsInRoleAsync(user, "Client"))
            {
                await _walletSetup.EnsureClientWalletsAsync(user.Id);
                await SetPreferredActiveWalletAsync(user);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("", "This email is already registered.");
                return View(model);
            }

            if (!new[] { "Client", "Agent" }.Contains(model.RoleType))
            {
                ModelState.AddModelError("", "Invalid role selected.");
                return View(model);
            }

            if (model.RoleType == "Client" && model.PreferredCurrencyId is < 1 or > 3)
            {
                ModelState.AddModelError(nameof(model.PreferredCurrencyId), "Select USD, EUR, or LBP.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                RoleType = model.RoleType,
                PreferredCurrencyId = model.RoleType == "Client" ? model.PreferredCurrencyId : 1
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.RoleType);

            if (model.RoleType == "Client")
            {
                await _walletSetup.EnsureClientWalletsAsync(user.Id);
                await SetPreferredActiveWalletAsync(user);
            }
            else if (model.RoleType == "Agent")
            {
                await _agentRepo.AddAsync(new Agent
                {
                    UserId = user.Id,
                    OfficeName = $"{model.FullName}'s Office",
                    City = "Beirut",
                    Latitude = 33.8938,
                    Longitude = 35.5018,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(18, 0, 0),
                    IsOpen = false,
                    IsApproved = false
                });
                await _agentRepo.SaveAsync();
            }

            await _userManager.AddClaimAsync(user, new Claim("FullName", user.FullName ?? ""));
            await _signInManager.SignInAsync(user, false);

            if (model.RoleType == "Client")
            {
                var code = model.PreferredCurrencyId switch
                {
                    2 => "EUR",
                    3 => "LBP",
                    _ => "USD"
                };
                TempData["Success"] = $"Welcome! Your USD, EUR, and LBP wallets were created at 0 balance. {code} is your default wallet.";
            }

            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Auth");
        }

        private async Task SetPreferredActiveWalletAsync(ApplicationUser user)
        {
            var accountId = await _walletSetup.GetAccountIdForCurrencyAsync(user.Id, user.PreferredCurrencyId);
            if (accountId.HasValue)
                HttpContext.Session.SetInt32("ActiveAccountId", accountId.Value);
        }
    }
}
