using System.Text.Json;
using EBikeManager.Application.Models;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class BoschJsonTests
{
    [Fact]
    public void BikesAreNamedAfterBrandAndDriveUnitLikeTheHomeAssistantIntegration()
    {
        using var json = JsonDocument.Parse("""
            {"data":[
              {"id":"bike-1","attributes":{"brandName":"Cube","driveUnit":{"productName":"Performance Line CX"}}},
              {"id":"bike-2","attributes":{"brandName":"Riese & Müller","frameNumber":"WAB123456789"}},
              {"id":"bike-3","attributes":{}},
              {"attributes":{"brandName":"No id"}}
            ]}
            """);

        Assert.Equal(
            [
                new BoschBikeInfo("bike-1", "Cube (Performance Line CX)"),
                new BoschBikeInfo("bike-2", "Riese & Müller (…6789)"),
                new BoschBikeInfo("bike-3", "eBike")
            ],
            BoschJson.ParseBikes(json.RootElement));
    }

    [Fact]
    public void ActivitiesAreReadWithTheirSummaryAndPageCount()
    {
        using var json = JsonDocument.Parse("""
            {"data":[
              {"id":"a1","attributes":{
                "title":"Morning ride","bikeId":"bike-1","startTime":1759219200,"endTime":1759222800,
                "timeZoneOfActivity":"Europe/London","distance":20512,"durationWithoutStops":3420,
                "caloriesBurnt":41.5,"elevationGain":130,"averageSpeed":21.6,"riderEnergyShare":58,
                "averageRiderPower":104.2,"assistModeUsage":[{"mode":"tour"}]}},
              {"id":"a2","attributes":{"bikeId":"bike-1","startTime":1759219200000}},
              {"id":"a3","attributes":{"bikeId":"bike-1"}}
            ],
            "meta":{"pages":4,"total":97}}
            """);

        var page = BoschJson.ParseActivityPage(json.RootElement);

        var start = DateTime.UnixEpoch.AddSeconds(1759219200);
        Assert.Equal(4, page.TotalPages);
        Assert.Equal(["a1", "a2"], page.Activities.Select(activity => activity.Id));
        Assert.Equal(
            new BoschActivity("a1", "bike-1", "Morning ride", start, start.AddHours(1), "Europe/London", 20512, 3420, 41.5, 130, 21.6, 58, 104.2, ""),
            page.Activities[0] with { AttributesJson = "" });
        Assert.Contains("assistModeUsage", page.Activities[0].AttributesJson, StringComparison.Ordinal);
        Assert.Equal(start, page.Activities[1].StartTime);
        Assert.Null(page.Activities[1].EndTime);
    }

    [Fact]
    public void AssistModeNamesAndColoursComeFromRideSummaries()
    {
        var names = BoschJson.AssistModeNames(["not json", EBikeManager.TestSupport.BoschSamples.RideSummary("Ride")]);

        Assert.Equal(new AssistModeName("TURBO", "#E20015"), names["A100M40010"]);
        Assert.Equal(new AssistModeName("OFF", "#000000"), names["0"]);
        Assert.Equal(5, names.Count);
    }

    [Fact]
    public void ProfilesArriveWrappedOrFlatAndPassesAreMatchedByBike()
    {
        using var wrapped = JsonDocument.Parse("""{"data":{"attributes":{"brandName":"Cube"}}}""");
        using var flat = JsonDocument.Parse("""{"brandName":"Cube"}""");
        using var passes = JsonDocument.Parse("""{"bikePasses":[{"bikeId":"a","frameNumber":"1"},{"bikeId":"b","frameNumber":"2"}]}""");
        using var locations = JsonDocument.Parse("""{"locations":[{"latitude":1},{"latitude":2}]}""");

        Assert.Equal("""{"brandName":"Cube"}""", BoschJson.UnwrapProfile(wrapped.RootElement));
        Assert.Equal("""{"brandName":"Cube"}""", BoschJson.UnwrapProfile(flat.RootElement));
        Assert.Contains("\"2\"", BoschJson.PassFor(passes.RootElement, "b"), StringComparison.Ordinal);
        Assert.Null(BoschJson.PassFor(passes.RootElement, "c"));
        Assert.Equal("""{"latitude":1}""", BoschJson.LatestLocation(locations.RootElement));
    }

    [Theory]
    [InlineData("""{"mediaAssets":{"bike_picture_url":"https://cdn.example.test/a.png"}}""", "https://cdn.example.test/a.png")]
    [InlineData("""{"mediaAssets":{"bikePictureUrl":"https://cdn.example.test/b.jpg"}}""", "https://cdn.example.test/b.jpg")]
    [InlineData("""{"mediaAssets":{"bikePictureUrl":"http://cdn.example.test/c.png"}}""", null)]
    [InlineData("""{"mediaAssets":{"bikePictureUrl":"data:image/svg+xml;base64,PHN2Zy8+"}}""", null)]
    [InlineData("""{"mediaAssets":{"bikePictureUrl":"pictures/d.png"}}""", null)]
    [InlineData("""{"brandName":"Cube"}""", null)]
    public void OnlyAnHttpsPictureAddressIsUsed(string profile, string? expected)
    {
        using var json = JsonDocument.Parse(profile);

        Assert.Equal(expected, BoschJson.PictureUrl(json.RootElement)?.AbsoluteUri);
    }

    [Fact]
    public void AnEmptyResponseHasNothingInIt()
    {
        using var json = JsonDocument.Parse("{}");

        Assert.Empty(BoschJson.ParseBikes(json.RootElement));
        Assert.Equal(0, BoschJson.ParseActivityPage(json.RootElement).TotalPages);
    }
}
