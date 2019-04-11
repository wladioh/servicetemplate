#addin nuget:?package=Cake.Coverlet

Task("Test")
    .IsDependentOn("Build")
    .Does<MyBuildData>((data) =>
{
    var testSettings = new DotNetCoreTestSettings {
    };

    var coverletSettings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat =  CoverletOutputFormat.opencover,
        CoverletOutputDirectory = Directory(@".\Tests\coverage-results\"),
        CoverletOutputName = $"results-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss-FFF}"
    };

    DotNetCoreTest("./Tests/Service.Api.Tests/Service.Api.Tests.csproj", testSetting, coverletSettings);
}