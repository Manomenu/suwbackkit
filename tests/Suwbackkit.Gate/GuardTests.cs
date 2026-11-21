using Suwbackkit.Postgres;
using Suwbackkit.Server;
using Suwbackkit.Testing;
using Suwgate.Architecture.Profiles;
using Suwgate.Database.Profiles;

namespace Suwbackkit.Gate;

public sealed class GuardTests
{
    [Fact]
    public void Every_guard_of_suwgate_passes() =>
        SuwgateGuards.VerifyAll(
            new GuardInputs
            {
                Production = [typeof(ServerKit).Assembly, typeof(Migrator).Assembly, typeof(TestDatabases).Assembly],
                Tests = [typeof(GuardTests).Assembly],
            },
            new DatabaseProfile()
        );
}
