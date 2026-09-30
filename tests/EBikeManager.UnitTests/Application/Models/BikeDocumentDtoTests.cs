using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.UnitTests.Application.Models;

public class BikeDocumentDtoTests
{
    private static readonly DateTime _saved = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("BIKE_IMAGE", BikeDocumentKind.BikePhoto)]
    [InlineData("BIKE_INVOICE", BikeDocumentKind.BikeInvoice)]
    [InlineData("LOCK_INVOICE", BikeDocumentKind.LockInvoice)]
    [InlineData("INSURANCE", BikeDocumentKind.Other)]
    public void BoschFileTypesBecomeDocumentKinds(string fileType, BikeDocumentKind expected)
    {
        var document = BikeDocumentDto.From(new BikeDocumentInfo("bike-1", "file-1", fileType, "application/pdf", null, null, _saved));

        Assert.Equal(expected, document.Kind);
    }

    [Theory]
    [InlineData("image/png", true)]
    [InlineData("image/jpeg", true)]
    [InlineData("application/pdf", false)]
    public void OnlyImagesArePreviewed(string contentType, bool expected)
    {
        Assert.Equal(expected, new BikeDocumentDto("file-1", BikeDocumentKind.Other, contentType, null, _saved).IsImage);
    }
}
