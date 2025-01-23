FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 8080

RUN DEBIAN_FRONTEND=noninteractive apt update && apt -y --no-install-recommends install gnupg

RUN echo "deb http://archive.raspberrypi.org/debian/ bookworm main" > /etc/apt/sources.list.d/raspi.list \
  && apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 82B129927FA3303E

RUN DEBIAN_FRONTEND=noninteractive apt update && apt -y upgrade

RUN DEBIAN_FRONTEND=noninteractive apt update && apt install -y --no-install-recommends \
    rpicam-apps-lite \
    libraw-bin \
  && apt-get clean \
  && apt-get autoremove \
  && rm -rf /var/cache/apt/archives/* \
  && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG TARGETARCH
WORKDIR /src
COPY . .
RUN dotnet build "./src/LumiSky.Rpicam/LumiSky.Rpicam.csproj" -c Release -a $TARGETARCH -o /app/build

FROM build AS publish
RUN dotnet publish "./src/LumiSky.Rpicam/LumiSky.Rpicam.csproj" -c Release -a $TARGETARCH -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG TARGETARCH
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .

USER app
ENTRYPOINT ["dotnet", "LumiSky.Rpicam.dll"]