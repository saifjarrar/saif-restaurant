using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.ComponentModel.DataAnnotations;

namespace SaifRestaurantApi.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController(
    IConfiguration configuration,
    ILogger<ContactController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] ContactRequest request,
        CancellationToken cancellationToken)
    {
        var email = configuration["Contact:SmtpEmail"];
        var password = configuration["Contact:SmtpPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogError("Contact email settings are missing.");
            return StatusCode(503, new { message = "Contact email is unavailable." });
        }
        var replyTo = request.Email.Trim();
        if (!MailboxAddress.TryParse(replyTo, out var visitor) ||
            !string.Equals(visitor.Address, replyTo, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        var message = new MimeMessage();
        var mailbox = MailboxAddress.Parse(email);
        message.From.Add(mailbox);
        message.To.Add(mailbox);
        message.ReplyTo.Add(visitor);
        message.Subject = "New message from Saif Restaurant";
        message.Body = new TextPart("plain")
        {
            Text = $"""
        Name: {request.Name}
        Email: {visitor.Address}
        Phone: {request.Phone}
        Topic: {request.Topic}

        {request.Message}
        """
        };

        using var smtp = new SmtpClient();

        try
        {
            await smtp.ConnectAsync(
                "smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect, cancellationToken);
            await smtp.AuthenticateAsync(email, password, cancellationToken);
            await smtp.SendAsync(message, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException
            or MailKit.Security.AuthenticationException or SslHandshakeException
            or IOException or System.Net.Sockets.SocketException)
        {
            logger.LogError(ex, "Could not send contact message.");
            return StatusCode(502, new { message = "Could not send your message. Try again later." });
        }
    }
}


public sealed class ContactRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = "";

    [StringLength(30)]
    public string? Phone { get; init; }

    [Required, StringLength(100)]
    public string Topic { get; init; } = "";

    [Required, StringLength(3000)]
    public string Message { get; init; } = "";
}