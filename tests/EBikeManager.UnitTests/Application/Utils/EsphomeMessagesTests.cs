using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class EsphomeMessagesTests
{
    [Fact]
    public void TheHelloRequestIsEncodedLikeProtobufDoesIt()
    {
        Assert.Equal(Convert.FromHexString("0a03656d621001180a"), EsphomeMessages.HelloRequest("emb"));
    }

    [Fact]
    public void DeviceInfoIsReadAndUnknownFieldsAreSkipped()
    {
        var payload = new ProtoWriter()
            .Bool(1, false)
            .String(2, "ebike-bridge-1a2b3c")
            .String(3, "AA:BB:CC:1A:2B:3C")
            .String(4, "2025.11.2")
            .String(5, "Nov 20 2025, 10:00:00")
            .UInt32(10, 80)
            .String(8, "xunil99.bosch_ebike_ldi_dual")
            .String(13, "eBike Dual Bridge")
            .Fixed32(99, 7)
            .ToArray();

        var device = EsphomeMessages.DeviceInfo(payload);

        Assert.Equal("ebike-bridge-1a2b3c", device.Name);
        Assert.Equal("eBike Dual Bridge", device.FriendlyName);
        Assert.Equal("AA:BB:CC:1A:2B:3C", device.MacAddress);
        Assert.Equal("2025.11.2", device.EsphomeVersion);
        Assert.Equal("xunil99.bosch_ebike_ldi_dual", device.ProjectName);
    }

    [Fact]
    public void SensorStatesCarryTheirValueOrNothingWhenMissing()
    {
        var battery = EsphomeMessages.SensorState(new ProtoWriter().Fixed32(1, 42).Float(2, 50f).ToArray());
        var missing = EsphomeMessages.SensorState(new ProtoWriter().Fixed32(1, 42).Float(2, 50f).Bool(3, true).ToArray());
        var unknown = EsphomeMessages.SensorState(new ProtoWriter().Fixed32(1, 42).Float(2, float.NaN).ToArray());
        var locked = EsphomeMessages.BinarySensorState(new ProtoWriter().Fixed32(1, 43).Bool(2, true).ToArray());

        Assert.Equal((42u, 50f), (battery.Key, battery.Number));
        Assert.Null(missing.Number);
        Assert.Null(unknown.Number);
        Assert.Equal((43u, true), (locked.Key, locked.Flag));
    }

    [Fact]
    public void ATruncatedMessageIsRefused()
    {
        var payload = new ProtoWriter().String(3, "eBike Battery SoC (Live)").ToArray()[..5];

        Assert.Throws<InvalidDataException>(() => EsphomeMessages.Entity(payload));
    }
}
