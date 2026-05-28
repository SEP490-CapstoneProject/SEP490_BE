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
    public string SenderName { get; set; } = "SkillSnap";
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
                <title>Đặt lại mật khẩu - SkillSnap</title>
            </head>
            <body style="margin:0;padding:0;background-color:#eef2f7;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#eef2f7;padding:40px 16px;">
                    <tr>
                        <td align="center">
                            <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">

                                <!-- HEADER / LOGO -->
                                <tr>
                                    <td style="background:linear-gradient(135deg,#0d2d6b 0%,#0a4fa8 50%,#0891b2 100%);border-radius:16px 16px 0 0;padding:36px 40px 32px;text-align:center;">
                                        <h1 style="color:#ffffff;margin:0;font-size:28px;font-weight:800;letter-spacing:1px;">SkillSnap</h1>
                                    </td>
                                </tr>

                                <!-- BODY -->
                                <tr>
                                    <td style="background:#ffffff;padding:40px 40px 32px;">
                                        <p style="color:#1e3a5f;font-size:16px;font-weight:600;margin:0 0 8px;">Xin chào! 👋</p>
                                        <p style="color:#4b5563;font-size:15px;line-height:1.7;margin:0 0 28px;">
                                            Chúng tôi nhận được yêu cầu <strong>đặt lại mật khẩu</strong> cho tài khoản SkillSnap của bạn.<br>
                                            Sử dụng mã OTP dưới đây để tiếp tục:
                                        </p>

                                        <!-- OTP Box -->
                                        <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:28px;">
                                            <tr>
                                                <td align="center">
                                                    <div style="display:inline-block;background:linear-gradient(135deg,#e0f7ff,#e8f4ff);border:2px solid #29b6f6;border-radius:16px;padding:28px 52px;text-align:center;">
                                                        <p style="color:#0a4fa8;font-size:12px;font-weight:700;letter-spacing:3px;margin:0 0 10px;text-transform:uppercase;">Mã xác thực OTP</p>
                                                        <span style="font-size:44px;font-weight:900;letter-spacing:12px;color:#0d2d6b;font-family:'Courier New',Courier,monospace;display:block;">{otp}</span>
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Warning -->
                                        <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px;">
                                            <tr>
                                                <td style="background:#fff8e1;border-left:4px solid #f59e0b;border-radius:8px;padding:14px 18px;">
                                                    <p style="color:#92400e;font-size:14px;margin:0;line-height:1.6;">
                                                        <strong>Mã OTP có hiệu lực trong 15 phút.</strong><br>
                                                        Nếu bạn không yêu cầu, hãy bỏ qua email này — tài khoản vẫn an toàn.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="color:#9ca3af;font-size:13px;line-height:1.6;margin:0;">
                                            Vì lý do bảo mật, <strong>không chia sẻ mã OTP</strong> này với bất kỳ ai, kể cả nhân viên SkillSnap.
                                        </p>
                                    </td>
                                </tr>

                                <!-- DIVIDER -->
                                <tr>
                                    <td style="background:#ffffff;padding:0 40px;">
                                        <div style="height:1px;background:linear-gradient(90deg,transparent,#29b6f6,#26c6da,transparent);"></div>
                                    </td>
                                </tr>

                                <!-- FOOTER -->
                                <tr>
                                    <td style="background:#ffffff;border-radius:0 0 16px 16px;padding:20px 40px 28px;text-align:center;">
                                        <p style="color:#6b7280;font-size:12px;margin:0 0 6px;">
                                            Email này được gửi tự động từ hệ thống <strong style="color:#0a4fa8;">SkillSnap</strong>.
                                        </p>
                                        <p style="color:#9ca3af;font-size:11px;margin:0;">
                                            © 2025 SkillSnap. Tất cả quyền được bảo lưu.
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
