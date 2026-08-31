using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Spinoza.Services;

public sealed class EmailDeliveryService(
    IConfiguration configuration,
    ILogger<EmailDeliveryService> logger)
{
    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var host = configuration["SES_SMTP_HOST"]
            ?? throw new InvalidOperationException(
                "SES_SMTP_HOST is not configured.");

        var port = configuration.GetValue<int?>("SES_SMTP_PORT") ?? 587;

        var username = configuration["SES_SMTP_USERNAME"]
            ?? throw new InvalidOperationException(
                "SES_SMTP_USERNAME is not configured.");

        var password = configuration["SES_SMTP_PASSWORD"]
            ?? throw new InvalidOperationException(
                "SES_SMTP_PASSWORD is not configured.");

        var fromAddress = configuration["SES_FROM_ADDRESS"]
            ?? throw new InvalidOperationException(
                "SES_FROM_ADDRESS is not configured.");

        var fromName = configuration["SES_FROM_NAME"] ?? "Spinoza";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                fromName,
                fromAddress));

        message.To.Add(
            MailboxAddress.Parse(recipient));

        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();

        logger.LogInformation(
            "Connecting to SES SMTP {Host}:{Port}",
            host,
            port);

        await client.ConnectAsync(
            host,
            port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        await client.AuthenticateAsync(
            username,
            password,
            cancellationToken);

        await client.SendAsync(
            message,
            cancellationToken);

        await client.DisconnectAsync(
            true,
            cancellationToken);

        logger.LogInformation(
            "Email sent successfully to {Recipient}",
            recipient);
    }
}