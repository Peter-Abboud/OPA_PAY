using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using OPA_Pay.Repositories.Interfaces;

using OPA_Pay.ViewModels;

using System.Security.Claims;



namespace OPA_Pay.Controllers

{

    [Authorize]

    public class DashboardController : Controller

    {

        private readonly IAccountRepository _accounts;

        private readonly ITransferRepository _transfers;

        private readonly INotificationRepository _notifications;



        public DashboardController(

            IAccountRepository accounts,

            ITransferRepository transfers,

            INotificationRepository notifications)

        {

            _accounts = accounts;

            _transfers = transfers;

            _notifications = notifications;

        }



        public async Task<IActionResult> Index()

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var activeId = HttpContext.Session.GetInt32("ActiveAccountId");



            var accounts = (await _accounts.GetByUserIdAsync(userId)).ToList();

            var transfers = (await _transfers.GetByUserIdAsync(userId)).ToList();

            var notifications = await _notifications.GetByUserIdAsync(userId);



            var model = new DashboardViewModel

            {

                AccountCount = accounts.Count,

                TransferCount = transfers.Count,

                UnreadNotifications = notifications.Count(n => !n.IsRead),

                WalletBalances = accounts

                    .OrderBy(a => a.CurrencyId)

                    .Select(a => new WalletBalanceSummary

                    {

                        CurrencyCode = a.Currency?.Code ?? "—",

                        CurrencyName = a.Currency?.Name ?? "",

                        AccountNumber = a.AccountNumber,

                        Balance = a.Balance,

                        IsActive = activeId == a.Id

                    })

                    .ToList(),

                MonthlyTransfers = transfers

                    .GroupBy(t => t.CreatedAt.ToString("MMM yyyy"))

                    .Select(g => new MonthlyTransferStat

                    {

                        Month = g.Key,

                        Amount = g.Sum(t => t.Amount)

                    })

                    .ToList()

            };



            return View(model);

        }

    }

}


