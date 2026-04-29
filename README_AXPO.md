# Local build and publish

```shell
# Replace version number with the correct one
dotnet pack -c Release -o output src/NRules/NRules.sln /p:Version=2.0.0-beta002
dotnet nuget push --source "https://pkgs.dev.azure.com/Axpo-AXSO/ETRM/_packaging/axpo-axso-etrm/nuget/v3/index.json" --api-key az output\*.nupkg
```