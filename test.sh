rm -rf ServiceTeste/
rm Wladi.ServiceTemplate.CSharp.0.1.0.nupkg
dotnet new -u Wladi.ServiceTemplate.CSharp
nuget pack Wladi.ServiceTemplate.CSharp.nuspec -NoDefaultExcludes
dotnet new -i Wladi.ServiceTemplate.CSharp.0.1.0.nupkg 
dotnet new service -o ServiceTeste -n ServiceTeste
dotnet new -u Wladi.ServiceTemplate.CSharp