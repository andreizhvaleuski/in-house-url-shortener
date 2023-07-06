using IHUS.Database.Repositories;
using IHUS.Domain.Constants;
using IHUS.Domain.Services.Generation.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace IHUS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlShortenerController : ControllerBase
{
    private readonly IShortenedUrlGenerator _shortenedUrlGenerator;
    private readonly ILogger<UrlShortenerController> _logger;

    public UrlShortenerController(
        IShortenedUrlGenerator shortenedUrlGenerator,
        ILogger<UrlShortenerController> logger)
    {
        _shortenedUrlGenerator = shortenedUrlGenerator
            ?? throw new ArgumentNullException(nameof(shortenedUrlGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("{shortUrlKey:required:length(6)}", Name = "GetActualUrl")]
    public async Task<IActionResult> Get([FromRoute] string shortUrlKey)
    {
        try
        {
            var shortenedUrl = await _shortenedUrlGenerator.GetAsync(shortUrlKey);

            return Ok(new GetActualUrlSuccessResponse(shortenedUrl.ActualUrl));
        }
        catch (ShortenedUrlNotFoundException)
        {
            return NotFound(new ErrorResponse($"The '{shortUrlKey}' short URL Not found."));
        }
    }

    [HttpPost(Name = "CreateShortUrl")]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest request)
    {
        try
        {
            var shortenedUrl = request.ShortUrl is null
                ? await _shortenedUrlGenerator.GenerateAsync(request.ActualUrl)
                : await _shortenedUrlGenerator.GenerateAsync(
                    request.ShortUrl,
                    request.ActualUrl);

            return Ok(new CreateShortUrlSuccessResponse(shortenedUrl.UrlKey));
        }
        catch (DuplicateShortUrlKeyException)
        {
            return BadRequest(new ErrorResponse("Can't create shortened URL because URL with the same short URL key already exists."));
        }
        catch (CantCreateShortenedUrlException ex)
        {
            _logger.LogInformation(ex, "Short URL can't be created");
            return BadRequest(new ErrorResponse("Can't create shortened URL. Please try again."));
        }
    }

    public record class GetActualUrlSuccessResponse(string ActualUrl);

    public record class CreateShortUrlRequest(
        [Required, MaxLength(Limits.ActualUrlMaxLength)]
        string ActualUrl,
        [StringLength(Limits.ShortUrlKeyLength, MinimumLength = Limits.ShortUrlKeyLength)]
        string? ShortUrl);

    public record class CreateShortUrlSuccessResponse(string ShortUrl);

    public record class ErrorResponse(string Message);
}
