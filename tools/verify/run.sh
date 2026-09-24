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
echo "== modules"
# The compile check uses one monolithic UnityEngine.dll, so it cannot see built-in modules that
# Packages/manifest.json leaves out. Map the usual module-only APIs to their packages.
missing=0
while IFS='|' read -r pattern module; do
  if grep -rqE "$pattern" ../../Assets/_Project --include=*.cs && ! grep -q "\"$module\"" ../../Packages/manifest.json; then
    echo "missing module $module (code uses: $pattern)"; missing=1
  fi
done <<'MAP'
\bScreenCapture\.|com.unity.modules.screencapture
\bAudioSource\b|com.unity.modules.audio
\bParticleSystem\b|com.unity.modules.particlesystem
\bPhysics2D\.|com.unity.modules.physics2d
\bJsonUtility\.|com.unity.modules.jsonserialize
\bUnityWebRequest\b|com.unity.modules.unitywebrequest
\bVideoPlayer\b|com.unity.modules.video
\bTilemap\b|com.unity.modules.tilemap
\bNavMesh\b|com.unity.modules.ai
\bImageConversion\.|com.unity.modules.imageconversion
\bEncodeToPNG\b|com.unity.modules.imageconversion
MAP
[ $missing -eq 0 ] && echo "modules OK" || exit 1
echo "== tests"
dotnet build tests/TestRun.csproj -nologo -v q 2>&1 | grep -E "error CS" | sort -u
dotnet tests/bin/Debug/net8.0/TestRun.dll --noresult 2>&1 | grep -E "Test Count|^[0-9]+\) |Expected|But was"
echo "== localization"
python3 loc_extract.py > /tmp/pf_loc_strings.txt && dotnet tests/bin/Debug/net8.0/TestRun.dll --loc-check /tmp/pf_loc_strings.txt
