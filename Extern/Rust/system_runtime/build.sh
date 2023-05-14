#!/usr/bin/env sh

cargo build

rm -f ../../../Packages/com.dman.l-system/External/*.dll

cp ../../dotnet/* ../../../Packages/com.dman.l-system/External
cp target/debug/system_runtime_rustlib.* ../../../Packages/com.dman.l-system/External

