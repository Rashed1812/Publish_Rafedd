using BLL.ServiceAbstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace BLL.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Rafedd System";
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || 
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("Email settings are not properly configured");
                    return false;
                }

                // Get base URL for reset link
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://rafeed.vercel.app";
                var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "إعادة تعيين كلمة المرور - Rafedd System",
                    Body = GeneratePasswordResetEmailBody(userName, resetLink, resetToken),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                // Configure SMTP client
                using var smtpClient = new SmtpClient(smtpServer, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Password reset email sent successfully to: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to: {Email}", email);
                return false;
            }
        }

        private string GeneratePasswordResetEmailBody(string userName, string resetLink, string resetToken)
        {
            return $@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""ar"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>إعادة تعيين كلمة المرور</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }}
        .container {{
            background-color: #ffffff;
            border-radius: 10px;
            padding: 30px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo {{
            font-size: 28px;
            font-weight: bold;
            color: #2c3e50;
            margin-bottom: 10px;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background-color: #3498db;
            color: #ffffff;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .button:hover {{
            background-color: #2980b9;
        }}
        .token-box {{
            background-color: #ecf0f1;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            word-break: break-all;
            font-family: monospace;
            font-size: 12px;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            text-align: center;
            font-size: 12px;
            color: #7f8c8d;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 5px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">Rafedd System</div>
        </div>
        
        <div class=""content"">
            <h2>مرحباً {userName},</h2>
            
            <p>لقد تلقينا طلباً لإعادة تعيين كلمة المرور لحسابك في نظام Rafedd.</p>
            
            <p>إذا كنت أنت من طلب إعادة تعيين كلمة المرور، يرجى النقر على الزر أدناه:</p>
            
            <div style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">إعادة تعيين كلمة المرور</a>
            </div>
            
            <p>أو يمكنك نسخ الرابط التالي ولصقه في المتصفح:</p>
            <div class=""token-box"">{resetLink}</div>
            
            <div class=""warning"">
                <strong>ملاحظة مهمة:</strong>
                <ul>
                    <li>هذا الرابط صالح لمدة 24 ساعة فقط</li>
                    <li>إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذا البريد الإلكتروني</li>
                    <li>لأسباب أمنية، لا تشارك هذا الرابط مع أي شخص آخر</li>
                </ul>
            </div>
            
            <p>إذا لم يعمل الرابط، يمكنك استخدام رمز التحقق التالي:</p>
            <div class=""token-box"">{resetToken}</div>
        </div>
        
        <div class=""footer"">
            <p>هذا بريد إلكتروني تلقائي، يرجى عدم الرد عليه.</p>
            <p>&copy; {DateTime.Now.Year} Rafedd System. جميع الحقوق محفوظة.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

