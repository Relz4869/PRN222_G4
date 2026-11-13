using FEMS.Services.Interfaces;
using FEMS.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FEMS.Controllers
{
    public class AccountController : Controller
    {
        private const string ResetAuthCookieName = "FEMS_RESET_AUTH";
        private readonly IAccountService _accountService;
        private readonly IHouseholdService _householdService;
        private readonly IPasswordResetService _passwordResetService;

        public AccountController(
            IAccountService accountService,
            IHouseholdService householdService,
            IPasswordResetService passwordResetService)
        {
            _accountService = accountService;
            _householdService = householdService;
            _passwordResetService = passwordResetService;
        }

        // =========================================
        // LOGIN
        // =========================================

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string? uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(uidStr, out int currentUserId))
                {
                    var activeMember = await _householdService.GetActiveMembershipAsync(currentUserId);
                    if (activeMember == null)
                    {
                        return RedirectToAction("Setup", "Household");
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Authenticate user
            var user = await _accountService.AuthenticateAsync(
                model.Email,
                model.Password
            );

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password."
                );

                return View(model);
            }

            // Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            // Create authentication cookie
            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties
            );

            var activeMembership = await _householdService.GetActiveMembershipAsync(user.UserId);
            if (activeMembership == null)
            {
                return RedirectToAction("Setup", "Household");
            }

            return RedirectToAction("Index", "Home");
        }

        // =========================================
        // REGISTER
        // =========================================

        // GET: /Account/Register
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

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool registered = await _accountService.RegisterAsync(
                model.FullName,
                model.Email,
                model.Password,
                model.Phone
            );

            if (!registered)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered."
                );

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Registration successful. Please log in.";

            return RedirectToAction(nameof(Login));
        }


        // =========================================
        // CHANGE PASSWORD
        // =========================================

        // GET: /Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId) || userId <= 0)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _accountService.ChangePasswordAsync(
                userId,
                model.CurrentPassword,
                model.NewPassword
            );

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(ChangePassword));
        }

        // =========================================
        // LOGOUT
        // =========================================

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login", "Account");
        }

        // =========================================
        // FORGOT PASSWORD
        // =========================================

        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _passwordResetService.RequestPasswordResetOtpAsync(model.Email, clientIp);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Lưu email tạm vào TempData để chuyển sang bước nhập OTP
            TempData["ResetEmail"] = model.Email.Trim();
            TempData["InfoMessage"] = result.Message;

            return RedirectToAction(nameof(VerifyOtp));
        }

        // =========================================
        // VERIFY OTP
        // =========================================

        // GET: /Account/VerifyOtp
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyOtp()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            string? email = TempData["ResetEmail"]?.ToString();
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Giữ lại email trong TempData cho các request tiếp theo
            TempData.Keep("ResetEmail");

            return View(new VerifyOtpViewModel { Email = email });
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData.Keep("ResetEmail");
                return View(model);
            }

            var result = await _passwordResetService.VerifyOtpAsync(model.Email, model.Otp);

            if (!result.Success || string.IsNullOrEmpty(result.ResetAuthToken))
            {
                ModelState.AddModelError(string.Empty, result.Message);
                TempData.Keep("ResetEmail");
                return View(model);
            }

            // Cấp Reset Authorization Token qua HttpOnly secure cookie
            Response.Cookies.Append(ResetAuthCookieName, result.ResetAuthToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/Account",
                Expires = DateTimeOffset.UtcNow.AddMinutes(10)
            });

            return RedirectToAction(nameof(ResetPassword));
        }

        // POST: /Account/ResendOtp (AJAX or Form POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email không hợp lệ.", cooldown = 0 });
            }

            string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _passwordResetService.ResendOtpAsync(email, clientIp);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                cooldown = result.CooldownSeconds
            });
        }

        // =========================================
        // RESET PASSWORD
        // =========================================

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            string? token = Request.Cookies[ResetAuthCookieName];
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Phiên đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var auth = await _passwordResetService.ValidateResetAuthorizationAsync(token);
            if (!auth.Valid)
            {
                Response.Cookies.Delete(ResetAuthCookieName, new CookieOptions { Path = "/Account" });
                TempData["ErrorMessage"] = "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu mã xác minh mới.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel());
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            string? token = Request.Cookies[ResetAuthCookieName];
            if (string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(string.Empty, "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu mã mới.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _passwordResetService.ResetPasswordAsync(token, model.NewPassword);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Xóa cookie Reset Authorization sau khi đã đổi mật khẩu thành công
            Response.Cookies.Delete(ResetAuthCookieName, new CookieOptions { Path = "/Account" });

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }
    }
}
