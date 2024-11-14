# Migrations

Start in the ``LumiSky.Core` directory.

To create a new migration:

```
dotnet tool run dotnet-ef migrations add ANewMigrationName --project LumiSky.Core.csproj --startup-project ..\LumiSky\LumiSky.csproj -o .\Data\Migrations\
```

To remove the most recent migration:

```
dotnet tool run dotnet-ef migrations remove --project LumiSky.Core.csproj --startup-project ..\LumiSky\LumiSky.csproj
```