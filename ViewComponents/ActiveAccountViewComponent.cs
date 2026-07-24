using Microsoft.AspNetCore.Mvc;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.ViewModels;
using System.Security.Claims;

namespace OPA_Pay.ViewComponents
{
    public class ActiveAccountViewComponent : ViewComponent
    {
        private readonly IAccountRepository _accounts;

        public ActiveAccountViewComponent(IAccountRepository accounts)
        {
            _accounts = accounts;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Content(string.Empty);

            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accountList = (await _accounts.GetByUserIdAsync(userId)).ToList();

            var activeId = HttpContext.Session.GetInt32("ActiveAccountId");
            if (!activeId.HasValue && accountList.Count > 0)
            {
                activeId = accountList[0].Id;
                HttpContext.Session.SetInt32("ActiveAccountId", activeId.Value);
            }

            var model = new ActiveAccountSwitcherViewModel
            {
                Accounts = accountList,
                ActiveAccountId = activeId
            };

            return View(model);
        }
    }
}
