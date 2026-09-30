using EBikeManager.Application.Configuration;
using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class MapSourcesTests
{
    [Fact]
    public void OpenStreetMapIsDrawnFromItsTilesWithCredit()
    {
        var source = MapSources.For(MapSettings.From(MapSettings.OpenStreetMapKey, null, null), dark: true);

        Assert.Equal(MapSettings.OpenStreetMapTiles, source.Tiles);
        Assert.Null(source.StyleUrl);
        Assert.Contains(MapSettings.OpenStreetMapCopyright, source.Attribution, StringComparison.Ordinal);
        Assert.Contains("OpenStreetMap contributors", source.Attribution, StringComparison.Ordinal);
        Assert.True(source.Dark);
    }

    [Fact]
    public void StyledMapsUseTheirStyleForTheTheme()
    {
        var map = MapSettings.From(MapSettings.OpenFreeMapKey, null, null);

        Assert.Equal(new MapSource(null, MapSettings.OpenFreeMapStyle, null, false), MapSources.For(map, dark: false));
        Assert.Equal(new MapSource(null, MapSettings.OpenFreeMapDarkStyle, null, true), MapSources.For(map, dark: true));
    }
}
