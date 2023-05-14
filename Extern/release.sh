#!/usr/bin/env sh

# shellcheck disable=SC2164
# shellcheck disable=SC2039
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

./Rust/system_runtime/build.sh

cd ../Packages/com.dman.l-system

git update-index --no-assume-unchanged External/Lib/system_runtime_rustlib.dll
git add -f External/Lib/system_runtime_rustlib.dll
git commit -m "Release commit. updated system_runtime_rustlib.dll"
git update-index --assume-unchanged External/Lib/system_runtime_rustlib.dll