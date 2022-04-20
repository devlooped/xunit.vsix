using NuGet.Versioning;
using Xunit.Abstractions;

namespace Xunit;

public record class VsVersionsTests(ITestOutputHelper Output)
{
    [Fact]
    public void GetAllVersions()
    {
        var versions = new VsVersions(
            currentVersion: "17.1",
            latestVersion: "17.2",
            installedVersions: new[] { "17.2", "17.1", "16.10", "16.5", "16.0" });

        var final = versions.GetFinalVersions(new[] { VisualStudioVersion.All }, null, null);

        Assert.Equal(versions.InstalledVersions, final);
    }

    [Fact]
    public void GetCurrentVersion()
    {
        var versions = new VsVersions(
            currentVersion: "17.1",
            latestVersion: "17.2",
            installedVersions: new[] { "17.2", "17.1", "16.10", "16.5", "16.0" });

        var final = versions.GetFinalVersions(new[] { VisualStudioVersion.Current }, null, null);

        Assert.Equal(new[] { versions.CurrentVersion }, final);
    }

    [Fact]
    public void GetLatestVersion()
    {
        var versions = new VsVersions(
            currentVersion: "17.1",
            latestVersion: "17.2",
            installedVersions: new[] { "17.2", "17.1", "16.10", "16.5", "16.0" });

        var final = versions.GetFinalVersions(new[] { VisualStudioVersion.Latest }, null, null);

        Assert.Equal(new[] { versions.LatestVersion }, final);
    }

    [Fact]
    public void GetRange()
    {
        var versions = new VsVersions(
            currentVersion: "17.1",
            latestVersion: "17.2",
            installedVersions: new[] { "16.0", "16.5", "16.10", "17.1", "17.2" });

        var final = versions.GetFinalVersions(new[] { VisualStudioVersion.All }, NuGetVersion.Parse("16.5"), NuGetVersion.Parse("17.1"));

        Assert.Equal(new[] { "16.5", "16.10", "17.1" }, final);
    }

    [Fact]
    public void GetWildcardRange()
    {
        var versions = new VsVersions(
            currentVersion: "17.1",
            latestVersion: "17.2",
            installedVersions: new[] { "16.0", "16.5", "16.10", "17.1", "17.2" });

        var final = versions.GetFinalVersions(new[] { "17.*" });

        Assert.Equal(new[] { "17.1", "17.2" }, final);
    }

    [Fact]
    public void GetWildcardRangeWithMaximum()
    {
        var versions = new VsVersions(
            currentVersion: "17.1",
            latestVersion: "17.2",
            installedVersions: new[] { "16.0", "16.5", "16.10", "17.1", "17.2", "18.0" });

        var final = versions.GetFinalVersions(new[] { "17.*" }, maximumVersion: NuGetVersion.Parse("17.9"));

        Assert.Equal(new[] { "17.1", "17.2" }, final);
    }

    [Fact]
    public void SemVer()
    {
        var wildcard = VersionRange.Parse("[17.*, 17.9]");

        Assert.True(wildcard.Satisfies(NuGetVersion.Parse("17.2")), $"17.2 does not satisfy {wildcard.ToNormalizedString()}");
        Assert.True(wildcard.Satisfies(NuGetVersion.Parse("17.1")), $"17.1 does not satisfy {wildcard.ToNormalizedString()}");
        Assert.True(wildcard.Satisfies(NuGetVersion.Parse("17.0")), $"17.0 does not satisfy {wildcard.ToNormalizedString()}");
        Assert.False(wildcard.Satisfies(NuGetVersion.Parse("16.9")), $"16.9 should not satisfy {wildcard.ToNormalizedString()}");
        Assert.False(wildcard.Satisfies(NuGetVersion.Parse("18.0")), $"18.0 should not satisfy {wildcard.ToNormalizedString()}");
    }
}
