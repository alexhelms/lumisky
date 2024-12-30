# LumiSky Dev Notes

## Build and Run the Container Locally

```
docker compose -f dev.docker-compose.yml up --build
```

## Build the container to be pushed

```
docker build -t registry.local.sdso.space/lumisky:latest -f src/LumiSky/Dockerfile .
docker push registry.local.sdso.space/lumisky:latest
```

## Compiling cfitsio for Linux

Run these commands both on an arm64 and x64 device and grab the dynamic libraries.

`cfitsio` must be compiled with `--enable-reentrant`. Calling `fits_is_reentrant()` must return a `1`.

```
wget https://heasarc.gsfc.nasa.gov/FTP/software/fitsio/c/cfitsio-4.4.1.tar.gz
tar zxvf cfitsio-4.4.1.tar.gz
cd cfitsio-4.4.1
./configure --enable-reentrant
make -j4
make utils -j4
./testprog
```

## Compiling emgucv for Linux

The opencv binary in the debian arm64 nuget package for emgucv works as-is.
Unfortunately there is not a debian version for x64 so below are steps to compile our own.
This was done in Debian 12 in WSL.

```
git clone --recurse-submodules --shallow-submodules --branch 4.9.0 https://github.com/emgucv/emgucv.git
cd emgucv/platforms/debian/bookworm/
./apt_install_dependency
# edit cmake_configure and change `--parallel 1` to `--parallel $(nproc)`
./cmake_configure
```