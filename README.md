# OdinEye

Astronomy allsky web server.

# Dev Notes

## Build and Run the Container Locally

```
docker compose -f dev.docker-compose.yml up --build
```

## Build the container to be pushed

```
docker build -t registry.local.sdso.space/odineye:latest -f OdinEye/Dockerfile .
docker push registry.local.sdso.space/odineye:latest
```

## Compiling cfitsio

`cfitsio` must be compiled with `--enable-reentrant`. Calling `fits_is_reentrant()` must return a `1`.

```
./configure --enable-reentrant
make -j4
make utils -j4
./testprog
```