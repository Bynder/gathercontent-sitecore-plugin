dotnet nuget add source -n Sitecore https://nuget.sitecore.com/resources/v3/index.json
dotnet tool restore
dotnet sitecore login --authority https://bynderidentityserver.dev.local --cm https://byndercm.dev.local --allow-write true
dotnet sitecore ser push