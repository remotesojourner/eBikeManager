using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EBikeManager.Web.Controllers;

[Route("bikes")]
public sealed class BikesController : ControllerBase
{
    private readonly BikeService _bikes;

    public BikesController(BikeService bikes)
    {
        _bikes = bikes;
    }

    [HttpGet("{id}/picture")]
    public async Task<IActionResult> PictureAsync(string id, CancellationToken cancellationToken) =>
        Serve(await _bikes.GetPictureAsync(id, cancellationToken));

    [HttpGet("{id}/documents/{fileId}")]
    public async Task<IActionResult> DocumentAsync(string id, string fileId, CancellationToken cancellationToken) =>
        Serve(await _bikes.GetDocumentAsync(id, fileId, cancellationToken));

    private IActionResult Serve(OperationResult<MediaFile> result)
    {
        if (result.Outcome == OperationOutcome.Denied) return Unauthorized();
        if (!result.Succeeded || result.Value is not { } media) return NotFound();

        Response.Headers.CacheControl = BikeMediaEndpoint.CacheControl;
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(media.Content, media.ContentType);
    }
}
