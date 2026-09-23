#!/bin/bash
# Compile-checks Assets/_Project (runtime, editor, tests) against Unity reference assemblies,
# then runs the pure-logic EditMode tests on .NET. Not a substitute for Unity's own compiler:
# the references are Unity 2021.3 and Collections/Burst/Input System are signature stubs.
set -uo pipefail
cd "$(dirname "$0")"
[ -d .cache/modules ] || ./setup.sh
echo "== compile"
dotnet build compile/Verify.csproj -nologo -v q 2>&1 | grep -E "(error|warning) CS" | grep "Assets/_Project" | sed 's|.*Assets/_Project|Assets/_Project|; s| \[.*||' | sort -u
dotnet build compile/Verify.csproj -nologo -v q 2>&1 | grep -qE "Build succeeded" && echo "compile OK" || { echo "compile FAILED"; exit 1; }
echo "== tests"
dotnet build tests/TestRun.csproj -nologo -v q 2>&1 | grep -E "error CS" | sort -u
dotnet tests/bin/Debug/net8.0/TestRun.dll --noresult 2>&1 | grep -E "Test Count|^[0-9]+\) |Expected|But was"
