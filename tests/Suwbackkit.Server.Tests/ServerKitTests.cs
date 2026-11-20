namespace Suwbackkit.Server.Tests;

public sealed class ServerKitTests
{
    [Fact]
    public void The_kit_tells_its_package_version() => Assert.Matches(@"^\d+\.\d+\.\d+", ServerKit.Version);
}
