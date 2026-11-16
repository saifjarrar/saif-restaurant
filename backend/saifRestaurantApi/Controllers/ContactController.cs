using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Net.Mail;

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
        if (!MailAddress.TryCreate(replyTo, out var visitor) ||
            !string.Equals(visitor.Address, replyTo, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        var accessKey = configuration["Contact:SilentFormsAccessKey"]?.Trim();
        if (string.IsNullOrEmpty(accessKey))
        {
            logger.LogWarning("The contact SilentForms access key is missing.");
            return StatusCode(503, new { message = "Contact email is unavailable." });
        }

        using var client = httpClientFactory.CreateClient("ContactEmail");
        using var submission = new HttpRequestMessage(HttpMethod.Post, "https://silentforms.com/api/submit");
        submission.Headers.Accept.ParseAdd("application/json");
        submission.Content = JsonContent.Create(new
        {
            accessKey,
            subject = "New message from Saif Restaurant",
            name = request.Name.Trim(),
            email = visitor.Address,
            phone = request.Phone?.Trim() ?? "",
            topic = request.Topic.Trim(),
            message = request.Message.Trim()
        });

        try
        {
            using var response = await client.SendAsync(submission, cancellationToken);
            if (response.IsSuccessStatusCode)
                return NoContent();

            logger.LogWarning("SilentForms did not accept the contact submission (HTTP {StatusCode}).",
                (int)response.StatusCode);
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
