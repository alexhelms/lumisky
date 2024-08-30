# Migrations

Start in the ``OdinEye.Core` directory.

To create a new migration:

```
dotnet tool run dotnet-ef migrations add ANewMigrationName --project OdinEye.Core.csproj --startup-project ..\OdinEye\OdinEye.csproj -o .\Data\Migrations\
```

To remove the most recent migration:

```
dotnet tool run dotnet-ef migrations remove --project OdinEye.Core.csproj --startup-project ..\OdinEye\OdinEye.csproj
```