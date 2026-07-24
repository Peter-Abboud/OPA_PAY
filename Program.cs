using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Configuration;
using OPA_Pay.Data;
using OPA_Pay.Data.Seeders;
using OPA_Pay.Models;
using OPA_Pay.Repositories;
using OPA_Pay.Repositories.Implementations;
using OPA_Pay.Repositories.Interfaces;
using OPA_Pay.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// =========================
// STRIPE
// =========================
var stripeSettings = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSettings["SecretKey"];

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// =========================
// DATABASE
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================
// IDENTITY
// =========================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// =========================
// COOKIE CONFIG
// =========================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.LogoutPath = "/Auth/Logout";

    options.Cookie.Name = "OPA_Pay_Cookie";
    options.ExpireTimeSpan = TimeSpan.FromHours(5);

    options.SlidingExpiration = true;
});

// =========================
// MVC + RAZOR
// =========================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// =========================
// SESSION
// =========================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =========================
// REPOSITORIES
// =========================
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();
builder.Services.AddScoped<ITransferRepository, TransferRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAgentProfileRepository, AgentProfileRepository>();
builder.Services.AddScoped<ICommissionRepository, CommissionRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<ITransferService, AppTransferService>();
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.AddScoped<IRecipientLookupService, RecipientLookupService>();
builder.Services.AddScoped<IWalletSetupService, WalletSetupService>();
builder.Services.AddScoped<IFundRequestService, FundRequestService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// =========================
// ERROR HANDLING
// =========================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// =========================
// MIDDLEWARE
// =========================
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =========================
// ROUTES
// =========================
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// =========================
// SEEDING
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var context = services.GetRequiredService<ApplicationDbContext>();

    await DbSeeder.SeedRolesAndAdminAsync(roleManager, userManager);
    await DbSeeder.SeedDemoAgentsAsync(context, userManager);
}

app.Run();