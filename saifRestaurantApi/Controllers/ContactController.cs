using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        if (!MailAddress.TryCreate(replyTo, out var visitor) ||
            !string.Equals(visitor.Address, replyTo, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        using var message = new MailMessage(email, email)
        {
            Subject = "New message from Saif Restaurant",
            Body = $"""
                Name: {request.Name}
                Email: {visitor.Address}
                Phone: {request.Phone}
                Topic: {request.Topic}

                {request.Message}
                """
        };
        message.ReplyToList.Add(visitor);

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(email, password)
        };

        try
        {
            await smtp.SendMailAsync(message, cancellationToken);
            return NoContent();
        }
        catch (SmtpException ex)
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