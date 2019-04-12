#addin nuget:?package=Cake.Coverlet
#tool "nuget:?package=ReportGenerator"

var rootFolder = Directory("..");
var target = Argument("target", "Coverage");
var solution = File($@"{rootFolder}\ServiceName.sln");
var coverageDirectory = Directory($@"{rootFolder}\Tests\coverage-results\");
Task("Build").
    Does(()=>{
       // DotNetCoreClean(solution);
        DotNetCoreBuild(solution);
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{   
    var testSettings = new DotNetCoreTestSettings {
        NoBuild = true,
        Logger = $"trx;LogFileName=UnitTests.trx",
        ResultsDirectory = coverageDirectory
    };
    var coverletSettings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat = CoverletOutputFormat.opencover | CoverletOutputFormat.cobertura,
        CoverletOutputDirectory = coverageDirectory,
        CoverletOutputName = $"coverage",
        Exclude = new List<string>  {"[xunit.*]*"}
    };
    if(DirectoryExists(coverageDirectory))
        DeleteDirectory(coverageDirectory, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    DotNetCoreTest($@"{rootFolder}\Tests\Service.Api.Tests", testSettings, coverletSettings);
});
 
Task("IntegrationTest")
    .IsDependentOn("Build")
    .Does(() =>
{   
    var testSettings = new DotNetCoreTestSettings {
        NoBuild = true,
        Logger = $"trx;LogFileName=IntegrationTests.trx",
        ResultsDirectory = coverageDirectory
    };
    if(DirectoryExists(coverageDirectory))
        DeleteDirectory(coverageDirectory, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    DotNetCoreTest($@"{rootFolder}\Tests\Service.Integration.Tests", testSettings);
});

Task("Coverage")
    .IsDependentOn("Test")
    .Does(()=>{
         ReportGenerator($@"{coverageDirectory}\*.opencover.xml", 
            coverageDirectory,
            new ReportGeneratorSettings(){
                ReportTypes = new[] { ReportGeneratorReportType.HtmlInline }
            });
    });

RunTarget(target);