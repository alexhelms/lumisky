# OdinEye

Astronomy allsky web server.

## Build and Run the Container Locally

```
docker compose -f dev.docker-compose.yml up --build
```

## Build the container to be pushed

```
docker build -t registry.local.sdso.space/odineye:latest -f OdinEye/Dockerfile .
docker push registry.local.sdso.space/odineye:latest
```