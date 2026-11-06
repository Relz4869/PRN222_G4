using FEMS.Data;
using Microsoft.EntityFrameworkCore;
using FEMS.Repositories.Interfaces;
using FEMS.Repositories.Implementations;
using FEMS.Services.Interfaces;
using FEMS.Services.Implementations;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace FEMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // Database Connection
            builder.Services.AddDbContext<FemsDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("FemsConnection")
                )
            );
            // Repository Registration
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Service Registration
            builder.Services.AddScoped<IAccountService, AccountService>();

            // Authentication Configuration
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();
            

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}