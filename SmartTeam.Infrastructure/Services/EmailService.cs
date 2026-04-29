using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using SmartTeam.Application.Services;

namespace SmartTeam.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName, CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_emailSettings.BaseUrl}/reset-password?token={resetToken}";
        
        var subject = "Avto027 - Şifrəni Sıfırlama";
        var body = GeneratePasswordResetEmailBody(userName, resetUrl);

        return await SendEmailAsync(email, subject, body, true, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
            client.EnableSsl = _emailSettings.EnableSsl;
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

            using var message = new MailMessage();
            message.From = new MailAddress(_emailSettings.Username, _emailSettings.DisplayName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string GeneratePasswordResetEmailBody(string userName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Avto027 - Şifrə Sıfırlama</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Avto027</h1>
            <h2>Şifrə Sıfırlama Tələbi</h2>
        </div>
        <div class='content'>
            <p>Salam {userName},</p>
            
            <p>Avto027 hesabınız üçün şifrə sıfırlama tələbi aldıq. Əgər bu tələbi siz etməmisinizsə, bu e-poçtu görməməzlikdən gəlin.</p>
            
            <p>Şifrənizi sıfırlamaq üçün aşağıdakı düyməyə klikləyin:</p>
            
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Şifrəni Sıfırla</a>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Təhlükəsizlik məlumatı:</strong>
                <ul>
                    <li>Bu link 1 saat ərzində etibarlıdır</li>
                    <li>Link yalnız bir dəfə istifadə oluna bilər</li>
                    <li>Əgər link işləmirsə, yeni tələb göndərin</li>
                </ul>
            </div>
            
            <p>Əgər düymə işləmirsə, aşağıdakı linki kopyalayıb brauzerinizə yapışdırın:</p>
            <p style='word-break: break-all; background: #e9ecef; padding: 10px; border-radius: 5px;'>{resetUrl}</p>
        </div>
        <div class='footer'>
            <p>Bu e-poçt Avto027 sistemi tərəfindən avtomatik göndərilmişdir.</p>
            <p>© 2026 Avto027. Bütün hüquqlar qorunur.</p>
        </div>
    </div>
</body>
</html>";
    }
}

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Avto027";
    public bool EnableSsl { get; set; } = true;
    public string BaseUrl { get; set; } = "https://yourdomain.com";
}
