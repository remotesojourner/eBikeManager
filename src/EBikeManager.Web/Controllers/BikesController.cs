using EBikeManager.Application.Enums;
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
    public async Task<IActionResult> PictureAsync(string id, CancellationToken cancellationToken)
    {
        var picture = await _bikes.GetPictureAsync(id, cancellationToken);
        if (picture.Outcome == OperationOutcome.Denied) return Unauthorized();
        if (!picture.Succeeded || picture.Value is not { } image) return NotFound();

        Response.Headers.CacheControl = BikePictureEndpoint.CacheControl;
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Content, image.ContentType);
    }
}
