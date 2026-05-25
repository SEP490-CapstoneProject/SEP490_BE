using Auth.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Auth.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "RecruitmentPlatform";
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Đặt lại mật khẩu - Mã OTP của bạn";
        var body = BuildPasswordResetEmailBody(resetToken);

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }

    private static string BuildPasswordResetEmailBody(string otp)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Đặt lại mật khẩu</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f4f7fb;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f7fb;padding:40px 0;">
                    <tr>
                        <td align="center">
                            <table width="580" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                                <!-- Header -->
                                <tr>
                                    <td style="background:linear-gradient(135deg,#6366f1,#8b5cf6);padding:36px 40px;text-align:center;">
                                        <h1 style="color:#ffffff;margin:0;font-size:26px;font-weight:700;letter-spacing:-0.5px;">🔐 Đặt Lại Mật Khẩu</h1>
                                        <p style="color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;">RecruitmentPlatform</p>
                                    </td>
                                </tr>
                                <!-- Body -->
                                <tr>
                                    <td style="padding:40px 40px 32px;">
                                        <p style="color:#374151;font-size:16px;line-height:1.6;margin:0 0 20px;">Xin chào,</p>
                                        <p style="color:#374151;font-size:16px;line-height:1.6;margin:0 0 28px;">
                                            Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Sử dụng mã OTP bên dưới để xác thực:
                                        </p>
                                        <!-- OTP Box -->
                                        <div style="text-align:center;margin:0 0 28px;">
                                            <div style="display:inline-block;background:linear-gradient(135deg,#f0f4ff,#e8edff);border:2px solid #c7d2fe;border-radius:12px;padding:24px 48px;">
                                                <span style="font-size:40px;font-weight:800;letter-spacing:10px;color:#4f46e5;font-family:'Courier New',monospace;">{otp}</span>
                                            </div>
                                        </div>
                                        <!-- Warning -->
                                        <div style="background:#fff7ed;border-left:4px solid #f97316;border-radius:8px;padding:16px 20px;margin:0 0 28px;">
                                            <p style="color:#92400e;font-size:14px;margin:0;line-height:1.5;">
                                                ⏱️ <strong>Mã OTP này có hiệu lực trong 15 phút.</strong><br>
                                                Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                                            </p>
                                        </div>
                                        <p style="color:#6b7280;font-size:14px;line-height:1.6;margin:0;">
                                            Vì lý do bảo mật, không chia sẻ mã OTP này với bất kỳ ai.
                                        </p>
                                    </td>
                                </tr>
                                <!-- Footer -->
                                <tr>
                                    <td style="background:#f9fafb;border-top:1px solid #e5e7eb;padding:24px 40px;text-align:center;">
                                        <p style="color:#9ca3af;font-size:12px;margin:0;">
                                            © 2025 RecruitmentPlatform. Tất cả quyền được bảo lưu.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
