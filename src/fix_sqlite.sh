#!/usr/bin/bash

# The shared library is not in the runtimes directory when published with "-a amd64" / "-a amd64"

echo "Target arch: $1"
if [ "$1" = "amd64" ]; then
    echo "Replacing x64 /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6"
    rm /app/libe_sqlite3.so
    ln -s /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6 /app/libe_sqlite3.so
elif [ "$1" = "arm64" ]; then
    echo "Replacing arm64 /lib/aarch64-linux-gnu/libsqlite3.so.0.8.6"
    rm /app/libe_sqlite3.so
    ln -s /lib/aarch64-linux-gnu/libsqlite3.so.0.8.6 /app/libe_sqlite3.so
else
    echo "unhandled arch $1"
fi
