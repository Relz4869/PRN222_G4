using FEMS.Models.Settings;
using FEMS.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FEMS.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<SmtpSettings> smtpOptions,
            ILogger<EmailService> logger)
        {
            _smtpSettings = smtpOptions.Value;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetOtpAsync(string recipientEmail, string recipientName, string otp)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(otp))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_smtpSettings.SenderEmail) || string.IsNullOrWhiteSpace(_smtpSettings.Password))
            {
                _logger.LogWarning("[EMAIL_SERVICE] Chưa cấu hình SenderEmail hoặc App Password trong SmtpSettings.");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(recipientName) ? "Quý khách" : recipientName, recipientEmail));
                message.Subject = "[FEMS] Mã xác minh đặt lại mật khẩu";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mã xác minh đặt lại mật khẩu</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #FAF8F3;
            color: #35483A;
            margin: 0;
            padding: 24px;
        }}
        .email-card {{
            max-width: 540px;
            margin: 0 auto;
            background-color: #FFFFFF;
            border: 1px solid #D5E6D8;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 4px 16px rgba(0,0,0,0.06);
        }}
        .email-header {{
            background: linear-gradient(135deg, #2D6A4F 0%, #1B4332 100%);
            color: #FFFFFF;
            padding: 28px 24px;
            text-align: center;
        }}
        .brand-logo {{
            display: inline-block;
            background-color: #D9A441;
            color: #FFFFFF;
            font-size: 20px;
            font-weight: 800;
            width: 44px;
            height: 44px;
            line-height: 44px;
            border-radius: 10px;
            margin-bottom: 10px;
        }}
        .email-title {{
            margin: 0;
            font-size: 20px;
            font-weight: 700;
            letter-spacing: 0.5px;
        }}
        .email-body {{
            padding: 32px 28px;
            line-height: 1.6;
        }}
        .otp-container {{
            margin: 28px 0;
            text-align: center;
        }}
        .otp-box {{
            display: inline-block;
            background-color: #F0F7F1;
            border: 2px dashed #86B89A;
            color: #1B4332;
            font-size: 32px;
            font-weight: 800;
            letter-spacing: 6px;
            padding: 14px 28px;
            border-radius: 12px;
            font-family: 'Courier New', Courier, monospace;
        }}
        .warning-box {{
            background-color: #FFFBEB;
            border-left: 4px solid #D9A441;
            padding: 12px 16px;
            border-radius: 6px;
            font-size: 13px;
            color: #92400E;
            margin-top: 24px;
        }}
        .email-footer {{
            background-color: #FAF8F3;
            border-top: 1px solid #E8DFD2;
            padding: 18px 24px;
            text-align: center;
            font-size: 12px;
            color: #8C827A;
        }}
    </style>
</head>
<body>
    <div class=""email-card"">
        <div class=""email-header"">
            <div class=""brand-logo"">F</div>
            <h1 class=""email-title"">FEMS - Quản Lý Chi Tiêu Gia Đình</h1>
        </div>
        <div class=""email-body"">
            <p>Xin chào <strong>{recipientName}</strong>,</p>
            <p>Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với email này. Vui lòng sử dụng mã xác minh dưới đây để tiếp tục:</p>
            
            <div class=""otp-container"">
                <div class=""otp-box"">{otp}</div>
                <div style=""margin-top: 10px; font-size: 13px; color: #5A7361;"">
                    Mã xác minh có hiệu lực trong vòng <strong>5 phút</strong>.
                </div>
            </div>

            <div class=""warning-box"">
                <strong>Lưu ý quan trọng:</strong> Tuyệt đối không chia sẻ mã này với bất kỳ ai, kể cả nhân viên hỗ trợ hệ thống.
            </div>

            <p style=""margin-top: 24px; font-size: 13px; color: #5A7361;"">
                Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email. Mật khẩu hiện tại của bạn vẫn an toàn và không thay đổi.
            </p>
        </div>
        <div class=""email-footer"">
            &copy; 2026 FEMS - Family Expense Management System. Email này được gửi tự động, vui lòng không phản hồi.
        </div>
    </div>
</body>
</html>",
                    TextBody = $@"Xin chào {recipientName},

Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản FEMS.
Mã xác minh OTP của bạn là: {otp}
Mã có hiệu lực trong vòng 5 phút.

Tuyệt đối không chia sẻ mã này với bất kỳ ai.
Nếu bạn không yêu cầu, vui lòng bỏ qua email này."
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                // Kết nối tới Gmail SMTP với STARTTLS
                await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.SenderEmail, _smtpSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("[EMAIL_SERVICE] Đã gửi mã OTP thành công tới địa chỉ {Recipient}", MaskEmail(recipientEmail));
                return true;
            }
            catch (Exception ex)
            {
                // Tuyệt đối không log thông tin nhạy cảm
                _logger.LogError("[EMAIL_SERVICE] Lỗi khi gửi email qua SMTP: {Message}", ex.Message);
                return false;
            }
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            int atIndex = email.IndexOf('@');
            if (atIndex <= 2) return "***" + email[atIndex..];
            return email[..2] + new string('*', atIndex - 2) + email[atIndex..];
        }
    }
}

