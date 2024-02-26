#!/usr/bin/env bash

set -e
dotnet publish -r win-x64 -c Release \
     -p:PublishReadyToRun=true \
     -p:PublishSingleFile=true \
     -p:IncludeNativeLibrariesForSelfExtract=true \
     -p:PublishTrimmed=true \
     --self-contained true


# ./bin/Debug/net6.0/linux-x64/publish/tm-embed-items
_DIR=$PWD
cp -v $_DIR/bin/Release/net6.0/win-x64/publish/find-remove-signs-skins.exe ./
7z a find-remove-signs-skins.zip find-remove-signs-skins.exe
