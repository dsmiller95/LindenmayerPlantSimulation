#!/usr/bin/env sh

# shellcheck disable=SC2164
# shellcheck disable=SC2039
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

cargo expand interop_extern > expanded.rs.tmp

cargo build --release

LIB_PATH_DEST=../../../Packages/com.dman.l-system/External/RustSubsystem

cp target/dotnet/* ../../../Packages/com.dman.l-system/External

mkdir -p $LIB_PATH_DEST
rm -f $LIB_PATH_DEST/*.dll
cp target/release/system_runtime_rustlib.* $LIB_PATH_DEST

