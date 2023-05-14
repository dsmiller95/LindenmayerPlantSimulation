#!/usr/bin/env sh

# shellcheck disable=SC2164
# shellcheck disable=SC2039
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

./Rust/system_runtime/build.sh

cd ../Packages/com.dman.l-system

LIB_PATH=External/RustSubsystem
LIB_DLL_PATH=$LIB_PATH/system_runtime_rustlib.dll

git update-index --no-assume-unchanged $LIB_PATH.meta
git add -f $LIB_PATH.meta
git update-index --no-assume-unchanged $LIB_DLL_PATH
git add -f $LIB_DLL_PATH
git update-index --no-assume-unchanged $LIB_DLL_PATH.meta
git add -f $LIB_DLL_PATH.meta

git commit -m "Release commit. updated system_runtime_rustlib.dll"

git update-index --assume-unchanged $LIB_PATH.meta
git update-index --assume-unchanged $LIB_DLL_PATH
git update-index --assume-unchanged $LIB_DLL_PATH.meta