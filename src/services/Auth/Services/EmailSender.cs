using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace Auth.Services;

public class EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor, ILogger<EmailSender> logger)
    : IEmailSender<ApplicationUser>,
        IEmailSender
{
    private readonly ILogger<EmailSender> _logger = logger;

    private readonly AuthMessageSenderOptions _options = optionsAccessor.Value;

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        await SendEmailAsync(
            email,
            "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."
        );

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        await SendEmailAsync(
            email,
            "Reset your password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>."
        );

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        await SendEmailAsync(
            email,
            "Reset your password",
            $"Please reset your password using the following code: {resetCode}"
        );

    // IEmailSender implementation (non-generic)
    public async Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        await SendEmailInternalAsync(email, subject, htmlMessage);

    private async Task SendEmailInternalAsync(string toEmail, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(_options.SmtpHost))
            throw new InvalidOperationException("SMTP host is not configured.");

        try
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                UseDefaultCredentials = false,
            };

            // Only set credentials if username and password are provided
            if (!string.IsNullOrEmpty(_options.SmtpUsername) && !string.IsNullOrEmpty(_options.SmtpPassword))
                client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _options.FromEmail ?? "noreply@bookmarks.local",
                    _options.FromName ?? "Bookmarks"
                ),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email to {Email} queued successfully!", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}
