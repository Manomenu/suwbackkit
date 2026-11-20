using System.Reflection;
using Suwbackkit.Postgres;
using Suwbackkit.Server;
using Suwgate.Architecture.Build;
using Suwgate.Architecture.Code;
using Suwgate.Architecture.Design;
using Suwgate.Architecture.Sources;
using Suwgate.Architecture.Verification;
using Suwgate.Database.Migrations;

namespace Suwbackkit.Gate;

public sealed class ArchitectureTests
{
    private static readonly RepositoryFiles Repository = RepositoryFiles.Find();
    private static readonly Assembly[] Packages = [typeof(ServerKit).Assembly, typeof(Migrator).Assembly];
    private static readonly Assembly[] Assemblies = [.. Packages, typeof(ArchitectureTests).Assembly];

    [Fact]
    public void Build_settings() => Guards.Verify(new BuildSettings(Repository));

    [Fact]
    public void Project_references() =>
        Guards.Verify(new ProjectReferences(Repository, "Suwbackkit.slnx", LayersFile.Read(Repository, "layers.json")));

    [Fact]
    public void No_escape_hatches() => Guards.Verify(new NoEscapeHatches(Repository.CSharpSources()));

    [Fact]
    public void One_type_per_file() => Guards.Verify(new OneTypePerFile(Repository.CSharpSources()));

    [Fact]
    public void Interfaces_have_two_implementations() => Guards.Verify(InterfacesHaveTwoImplementations.Of(Assemblies));

    [Fact]
    public void Nothing_obsolete() => Guards.Verify(new NothingObsolete(Assemblies));

    [Fact]
    public void No_unused_types() => Guards.Verify(new NoUnusedTypes(Packages, []));

    [Fact]
    public void Forbidden_type_names() => Guards.Verify(ForbiddenTypeNames.Of(Assemblies));

    [Fact]
    public void Unique_type_names() => Guards.Verify(UniqueTypeNames.Of(Assemblies));

    [Fact]
    public void Migration_script_names() =>
        Guards.Verify(new MigrationScriptNames(Path.Combine(Repository.Root, "tests/Suwbackkit.Postgres.Tests/Database/Migrations/up")));
}
