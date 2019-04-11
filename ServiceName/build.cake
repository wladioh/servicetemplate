#addin nuget:?package=Cake.Coverlet
#tool "nuget:?package=ReportGenerator"

var target = Argument("target", "Coverage");
Task("Build").
    Does(()=>{
        DotNetCoreBuild("./Src/Service.Api");
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testSettings = new DotNetCoreTestSettings {};

    var coverletSettings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat = CoverletOutputFormat.opencover,
        CoverletOutputDirectory = Directory(@".\Tests\coverage-results\"),
        CoverletOutputName = $"results-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss-FFF}",
        Exclude = new List<string>  {"[xunit.*]*"}
    };
    DotNetCoreTest("./Tests/Service.Api.Tests", testSettings, coverletSettings);
});
 
Task("Coverage")
    .IsDependentOn("Test")
    .Does(()=>{
        ReportGenerator(@".\Tests\coverage-results\*.opencover.xml", 
            Directory(@".\Tests\coverage-results\"),
            new ReportGeneratorSettings(){
                ReportTypes = new[] { ReportGeneratorReportType.HtmlInline }
            });
    });

RunTarget(target);