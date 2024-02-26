#!/usr/bin/env bash

set -e

./build.sh
_DIR=$PWD
$_DIR/bin/Debug/net6.0/linux-x64/publish/find-remove-signs-skins "$@" || true
