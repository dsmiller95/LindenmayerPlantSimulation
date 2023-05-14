#!/usr/bin/env sh

cargo build

LIB_PATH_DEST=../../../Packages/com.dman.l-system/External/Lib

cp target/dotnet/* ../../../Packages/com.dman.l-system/External

mkdir -p $LIB_PATH_DEST
rm -f $LIB_PATH_DEST/*.dll
cp target/debug/system_runtime_rustlib.* $LIB_PATH_DEST

