using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace SaifRestaurantApi.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<ContactController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] ContactRequest request,
        CancellationToken cancellationToken)
    {
        var replyTo = request.Email.Trim();
        if (!MailboxAddress.TryParse(replyTo, out var visitor) ||
            !string.Equals(visitor.Address, replyTo, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        const string subject = "New message from Saif Restaurant";
        var body = $"""
        Name: {request.Name}
        Email: {visitor.Address}
        Phone: {request.Phone}
        Topic: {request.Topic}

        {request.Message}
        """;

        if (string.Equals(configuration["Contact:Provider"], "Brevo", StringComparison.OrdinalIgnoreCase))
            return await SendWithBrevo(visitor.Address, subject, body, cancellationToken);

        var email = configuration["Contact:SmtpEmail"];
        var password = configuration["Contact:SmtpPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogError("Contact SMTP settings are missing.");
            return StatusCode(503, new { message = "Contact email is unavailable." });
        }

        var message = new MimeMessage();
        var mailbox = MailboxAddress.Parse(email);
        message.From.Add(mailbox);
        message.To.Add(mailbox);
        message.ReplyTo.Add(visitor);
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

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
    private async Task<IActionResult> SendWithBrevo(
        string replyTo, string subject, string body, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Contact:BrevoApiKey"];
        var sender = configuration["Contact:SenderEmail"];
        var recipient = configuration["Contact:RecipientEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sender) ||
            string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogError("Contact Brevo settings are missing.");
            return StatusCode(503, new { message = "Contact email is unavailable." });
        }

        using var client = httpClientFactory.CreateClient("ContactEmail");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { email = sender, name = "Saif Restaurant" },
            to = new[] { new { email = recipient } },
            replyTo = new { email = replyTo },
            subject,
            textContent = body
        });

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return NoContent();

            logger.LogWarning("Contact email provider returned HTTP {StatusCode}.", (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Contact email provider could not be reached.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Contact email provider timed out.");
        }

        return StatusCode(502, new { message = "Could not send your message. Try again later." });
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
