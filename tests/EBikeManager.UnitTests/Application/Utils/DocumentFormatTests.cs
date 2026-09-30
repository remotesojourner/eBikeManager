using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Utils;

public class DocumentFormatTests
{
    [Theory]
    [InlineData("255044462D312E340A", "application/pdf")]
    [InlineData("89504E470D0A1A0A0000000D", "image/png")]
    [InlineData("FFD8FFE000104A464946", "image/jpeg")]
    [InlineData("25504446", null)]
    [InlineData("3C68746D6C3E", null)]
    [InlineData("", null)]
    public void OnlyImagesAndPdfsAreRecognised(string hex, string? expected)
    {
        Assert.Equal(expected, DocumentFormat.ContentTypeOf(Convert.FromHexString(hex)));
    }

    [Fact]
    public void TheSampleInvoiceIsAPdf()
    {
        Assert.Equal(DocumentFormat.Pdf, DocumentFormat.ContentTypeOf(BoschSamples.Pdf));
    }
}
