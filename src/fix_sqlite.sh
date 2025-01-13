#!/usr/bin/bash

echo "Target arch: $TARGETARCH"
if [ "$TARGETARCH" = "amd64" ]; then
    echo "Replacing x64 /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6"
    cp /app/runtimes/linux-x64/native/libe_sqlite3.so /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6
elif [ "$TARGETARCH" = "arm64" ]; then
    echo "Replacing arm64 /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6"
    cp /app/runtimes/linux-arm64/native/libe_sqlite3.so /lib/x86_64-linux-gnu/libsqlite3.so.0.8.6
else
    echo "unhandled arch $TARGETARCH"
fi
