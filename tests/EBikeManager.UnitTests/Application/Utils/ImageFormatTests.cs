using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Utils;

public class ImageFormatTests
{
    [Theory]
    [InlineData("89504E470D0A1A0A0000000D", "image/png")]
    [InlineData("FFD8FFE000104A464946", "image/jpeg")]
    [InlineData("474946383961", "image/gif")]
    [InlineData("474946383761", "image/gif")]
    [InlineData("524946460000000057454250", "image/webp")]
    [InlineData("524946460000000057415645", null)]
    [InlineData("3C73766720786D6C6E733D", null)]
    [InlineData("89504E47", null)]
    [InlineData("", null)]
    public void OnlyRasterImagesAreRecognised(string hex, string? expected)
    {
        Assert.Equal(expected, ImageFormat.ContentTypeOf(Convert.FromHexString(hex)));
    }

    [Fact]
    public void TheSampleBikePictureIsAPng()
    {
        Assert.Equal("image/png", ImageFormat.ContentTypeOf(BoschSamples.Picture));
    }
}
