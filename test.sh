rm -rf ServiceTeste/
rm Wladi.ServiceTemplate.CSharp.0.1.0.nupkg
dotnet new -u Wladi.ServiceTemplate.CSharp
nuget pack Linx.Solution.CSharp.nuspec
dotnet new -i Wladi.ServiceTemplate.CSharp.0.1.0.nupkg
dotnet new service -o ServiceTeste -n ServiceTeste
dotnet new -u Wladi.ServiceTemplate.CSharp