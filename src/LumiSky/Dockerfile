FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update -y \
    && apt-get upgrade -y \
    && apt-get install -y \
    nano \
    htop \
    wget \
    curl \
    gdb \
    netcat-traditional \
    python3 \
    python3-pip \
    python3-pil \
    ffmpeg \
    libgeotiff5 \
    libgtk-3-0 \
    vtk9

# app data
RUN mkdir /lumisky \
    && chmod 755 /lumisky \
    && chown app:app /lumisky

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet build "./src/LumiSky/LumiSky.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./src/LumiSky/LumiSky.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG TARGETARCH
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .

# Replace the global libsqlite.
# It is a different version than what is required by entity framework
# and both are loaded in the process but are incompatible and causes
# segfaults on *some* queries.
COPY ./src/fix_sqlite.sh .
RUN chmod +x fix_sqlite.sh && ./fix_sqlite.sh

USER app
ENTRYPOINT ["dotnet", "LumiSky.dll"]
