// LicensingSystem.Server/Services/EmailService.cs
// Email service for license transfer verifications and notifications
// Created: January 2026

using System.Net;
using System.Net.Mail;
using LicensingSystem.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace LicensingSystem.Server.Services;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a verification code for license transfer
    /// </summary>
    Task<bool> SendTransferVerificationAsync(string email, string licenseId, string verificationCode);

    /// <summary>
    /// Sends a notification that a license was transferred
    /// </summary>
    Task<bool> SendTransferConfirmationAsync(string email, string licenseId, string newMachineName);

    /// <summary>
    /// Sends a license activation confirmation
    /// </summary>
    Task<bool> SendActivationConfirmationAsync(string email, string licenseId, string machineName);

    /// <summary>
    /// Sends a license expiration warning
    /// </summary>
    Task<bool> SendExpirationWarningAsync(string email, string licenseId, DateTime expiresAt, int daysRemaining);

    /// <summary>
    /// Sends a license revocation notification
    /// </summary>
    Task<bool> SendRevocationNotificationAsync(string email, string licenseId, string reason);

    /// <summary>
    /// Tests the email configuration
    /// </summary>
    Task<(bool Success, string Message)> TestConfigurationAsync();
}

/// <summary>
/// Email service implementation using SMTP
/// Supports multiple providers: SMTP, SendGrid, AWS SES
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IAuditService _auditService;

    public EmailService(
        EmailSettings settings,
        ILogger<EmailService> logger,
        IAuditService auditService)
    {
        _settings = settings;
        _logger = logger;
        _auditService = auditService;
    }

    /// <inheritdoc/>
    public async Task<bool> SendTransferVerificationAsync(string email, string licenseId, string verificationCode)
    {
        var subject = $"FathomOS License Transfer Verification - Code: {verificationCode}";
        var body = GetTransferVerificationTemplate(verificationCode, licenseId);

        var success = await SendEmailAsync(email, subject, body, isHtml: true);

        await _auditService.LogAsync(
            "EMAIL_SENT",
            "TransferVerification",
            licenseId,
            null,
            email,
            null,
            success ? "Transfer verification email sent" : "Transfer verification email failed",
            success);

        return success;
    }

    /// <inheritdoc/>
    public async Task<bool> SendTransferConfirmationAsync(string email, string licenseId, string newMachineName)
    {
        var subject = "FathomOS License Transfer Complete";
        var body = GetTransferConfirmationTemplate(licenseId, newMachineName);

        var success = await SendEmailAsync(email, subject, body, isHtml: true);

        await _auditService.LogAsync(
            "EMAIL_SENT",
            "TransferConfirmation",
            licenseId,
            null,
            email,
            null,
            success ? "Transfer confirmation email sent" : "Transfer confirmation email failed",
            success);

        return success;
    }

    /// <inheritdoc/>
    public async Task<bool> SendActivationConfirmationAsync(string email, string licenseId, string machineName)
    {
        var subject = "FathomOS License Activated Successfully";
        var body = GetActivationConfirmationTemplate(licenseId, machineName);

        var success = await SendEmailAsync(email, subject, body, isHtml: true);

        await _auditService.LogAsync(
            "EMAIL_SENT",
            "ActivationConfirmation",
            licenseId,
            null,
            email,
            null,
            success ? "Activation confirmation email sent" : "Activation confirmation email failed",
            success);

        return success;
    }

    /// <inheritdoc/>
    public async Task<bool> SendExpirationWarningAsync(string email, string licenseId, DateTime expiresAt, int daysRemaining)
    {
        var subject = $"FathomOS License Expiring in {daysRemaining} Days";
        var body = GetExpirationWarningTemplate(licenseId, expiresAt, daysRemaining);

        var success = await SendEmailAsync(email, subject, body, isHtml: true);

        await _auditService.LogAsync(
            "EMAIL_SENT",
            "ExpirationWarning",
            licenseId,
            null,
            email,
            null,
            success ? $"Expiration warning email sent ({daysRemaining} days)" : "Expiration warning email failed",
            success);

        return success;
    }

    /// <inheritdoc/>
    public async Task<bool> SendRevocationNotificationAsync(string email, string licenseId, string reason)
    {
        var subject = "FathomOS License Revoked";
        var body = GetRevocationNotificationTemplate(licenseId, reason);

        var success = await SendEmailAsync(email, subject, body, isHtml: true);

        await _auditService.LogAsync(
            "EMAIL_SENT",
            "RevocationNotification",
            licenseId,
            null,
            email,
            null,
            success ? "Revocation notification email sent" : "Revocation notification email failed",
            success);

        return success;
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message)> TestConfigurationAsync()
    {
        if (!_settings.Enabled)
        {
            return (false, "Email service is disabled in configuration");
        }

        if (string.IsNullOrEmpty(_settings.SmtpServer))
        {
            return (false, "SMTP server not configured");
        }

        if (string.IsNullOrEmpty(_settings.FromEmail))
        {
            return (false, "From email address not configured");
        }

        try
        {
            using var client = CreateSmtpClient();
            // Just test the connection
            await Task.Run(() => client.Send(new MailMessage(
                _settings.FromEmail,
                _settings.FromEmail,
                "FathomOS Email Test",
                "This is a test email from FathomOS License Server.")));

            return (true, "Email configuration test successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration test failed");
            return (false, $"Email configuration test failed: {ex.Message}");
        }
    }

    private async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Email service is disabled. Would have sent email to: {To}, Subject: {Subject}", to, subject);
            return false;
        }

        if (string.IsNullOrEmpty(_settings.SmtpServer))
        {
            _logger.LogError("SMTP server not configured");
            return false;
        }

        try
        {
            using var client = CreateSmtpClient();

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName ?? "FathomOS License Server"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);

            if (!string.IsNullOrEmpty(_settings.ReplyToEmail))
            {
                message.ReplyToList.Add(_settings.ReplyToEmail);
            }

            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to: {To}, Subject: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {To}, Subject: {Subject}", to, subject);
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrEmpty(_settings.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
        }

        return client;
    }

    #region Email Templates

    private string GetTransferVerificationTemplate(string verificationCode, string licenseId)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #1a5276; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .code {{ font-size: 32px; font-weight: bold; color: #1a5276; text-align: center; padding: 20px; background: #fff; border: 2px dashed #1a5276; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .warning {{ color: #c0392b; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>FathomOS License Transfer</h1>
        </div>
        <div class='content'>
            <p>You have requested to transfer your FathomOS license to a new device.</p>
            <p>Your verification code is:</p>
            <div class='code'>{verificationCode}</div>
            <p>Enter this code in the application to complete the transfer.</p>
            <p><strong>License ID:</strong> {licenseId}</p>
            <p class='warning'><strong>Important:</strong> This code expires in 15 minutes. If you did not request this transfer, please contact support immediately.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by the FathomOS License Server.</p>
            <p>&copy; {DateTime.UtcNow.Year} FathomOS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetTransferConfirmationTemplate(string licenseId, string newMachineName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .success {{ color: #27ae60; font-weight: bold; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>License Transfer Complete</h1>
        </div>
        <div class='content'>
            <p class='success'>Your FathomOS license has been successfully transferred!</p>
            <p><strong>License ID:</strong> {licenseId}</p>
            <p><strong>New Device:</strong> {newMachineName}</p>
            <p><strong>Transfer Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
            <p>Your license is now active on the new device. The previous device has been deactivated.</p>
            <p>If you did not authorize this transfer, please contact support immediately.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by the FathomOS License Server.</p>
            <p>&copy; {DateTime.UtcNow.Year} FathomOS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetActivationConfirmationTemplate(string licenseId, string machineName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #2980b9; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .success {{ color: #27ae60; font-weight: bold; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>FathomOS License Activated</h1>
        </div>
        <div class='content'>
            <p class='success'>Your FathomOS license has been activated successfully!</p>
            <p><strong>License ID:</strong> {licenseId}</p>
            <p><strong>Device:</strong> {machineName}</p>
            <p><strong>Activation Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
            <p>Thank you for using FathomOS. You can now use all licensed features.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by the FathomOS License Server.</p>
            <p>&copy; {DateTime.UtcNow.Year} FathomOS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetExpirationWarningTemplate(string licenseId, DateTime expiresAt, int daysRemaining)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f39c12; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .warning {{ color: #e67e22; font-weight: bold; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>License Expiring Soon</h1>
        </div>
        <div class='content'>
            <p class='warning'>Your FathomOS license will expire in {daysRemaining} days!</p>
            <p><strong>License ID:</strong> {licenseId}</p>
            <p><strong>Expiration Date:</strong> {expiresAt:yyyy-MM-dd}</p>
            <p>To continue using FathomOS without interruption, please renew your license before the expiration date.</p>
            <p>Contact your license administrator or visit our website to renew.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by the FathomOS License Server.</p>
            <p>&copy; {DateTime.UtcNow.Year} FathomOS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetRevocationNotificationTemplate(string licenseId, string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #c0392b; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .error {{ color: #c0392b; font-weight: bold; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>License Revoked</h1>
        </div>
        <div class='content'>
            <p class='error'>Your FathomOS license has been revoked.</p>
            <p><strong>License ID:</strong> {licenseId}</p>
            <p><strong>Reason:</strong> {reason}</p>
            <p><strong>Revocation Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
            <p>If you believe this is an error, please contact support immediately.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by the FathomOS License Server.</p>
            <p>&copy; {DateTime.UtcNow.Year} FathomOS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    #endregion
}

/// <summary>
/// Email service settings - bind to appsettings.json "Email" section
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Whether email sending is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SMTP server hostname
    /// </summary>
    public string SmtpServer { get; set; } = "";

    /// <summary>
    /// SMTP server port (default: 587 for TLS, 465 for SSL, 25 for plain)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// SMTP username for authentication
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password for authentication
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = "";

    /// <summary>
    /// From display name
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Reply-to email address
    /// </summary>
    public string? ReplyToEmail { get; set; }
}
