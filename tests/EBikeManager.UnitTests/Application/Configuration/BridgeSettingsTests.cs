using EBikeManager.Application.Configuration;

namespace EBikeManager.UnitTests.Application.Configuration;

public class BridgeSettingsTests
{
    [Theory]
    [InlineData("192.168.1.50", "192.168.1.50", 6053)]
    [InlineData("192.168.1.50:6060", "192.168.1.50", 6060)]
    [InlineData(" ebike-bridge-1a2b3c.local ", "ebike-bridge-1a2b3c.local", 6053)]
    [InlineData("bridge:1", "bridge", 1)]
    public void AnAddressCanCarryItsOwnPort(string address, string host, int port)
    {
        Assert.True(BridgeSettings.TryParseAddress(address, out var parsedHost, out var parsedPort));
        Assert.Equal((host, port), (parsedHost, parsedPort));
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://192.168.1.50")]
    [InlineData("192.168.1.50:0")]
    [InlineData("192.168.1.50:70000")]
    [InlineData("bridge:port")]
    [InlineData("two words")]
    public void OtherAddressesAreRefused(string address)
    {
        Assert.False(BridgeSettings.IsValidAddress(address));
    }

    [Fact]
    public void OnlyA32ByteBase64KeyIsAnEncryptionKey()
    {
        var key = Convert.ToBase64String(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());

        Assert.True(BridgeSettings.TryReadKey($" {key} ", out var parsed));
        Assert.Equal(32, parsed.Length);
        Assert.False(BridgeSettings.TryReadKey(Convert.ToBase64String(new byte[16]), out _));
        Assert.False(BridgeSettings.TryReadKey("not base64!", out _));
        Assert.False(BridgeSettings.TryReadKey(null, out _));
    }
}
