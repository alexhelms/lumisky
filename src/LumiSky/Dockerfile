FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
USER app
WORKDIR /app
EXPOSE 8080

USER root
RUN apt-get update -y \
    && apt-get upgrade -y \
    && apt-get install -y \
    htop \
    wget \
    curl \
    python3 \
    python3-pip \
    python3-pil \
    ffmpeg \
    libgeotiff5 \
    libgtk-3-0 \
    vtk9
RUN mkdir /lumisky \
    && chmod 755 /lumisky \
    && chown app:app /lumisky
USER app

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Directory.Packages.props", "."]
COPY ["src/Directory.Build.props", "."]
COPY ["src/LumiSky/LumiSky.csproj", "LumiSky/"]
COPY ["src/LumiSky.Core/LumiSky.Core.csproj", "LumiSky.Core/"]
RUN dotnet restore "./LumiSky/LumiSky.csproj"
COPY ./src .
WORKDIR "/src/LumiSky"
RUN dotnet build "./LumiSky.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LumiSky.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LumiSky.dll"]
