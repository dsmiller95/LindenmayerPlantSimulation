#!/usr/bin/env sh

# shellcheck disable=SC2164
# shellcheck disable=SC2039
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

cargo build

LIB_PATH_DEST=../../../Packages/com.dman.l-system/External/Lib

cp target/dotnet/* ../../../Packages/com.dman.l-system/External

mkdir -p $LIB_PATH_DEST
rm -f $LIB_PATH_DEST/*.dll
cp target/debug/system_runtime_rustlib.* $LIB_PATH_DEST

