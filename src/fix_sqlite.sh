#!/usr/bin/bash

echo "Target arch: $1"
if [ "$1" = "amd64" ]; then
    echo "Replacing x64 /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6"
    cp /app/runtimes/linux-x64/native/libe_sqlite3.so /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6
elif [ "$1" = "arm64" ]; then
    echo "Replacing arm64 /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6"
    cp /app/runtimes/linux-arm64/native/libe_sqlite3.so /lib/aarch64-linux-gnu/libsqlite3.so.0.8.6
else
    echo "unhandled arch $1"
fi
