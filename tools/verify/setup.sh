#!/bin/bash
# Downloads reference assemblies and package sources into tools/verify/.cache (gitignored).
# Needs: dotnet SDK 8, network access to api.nuget.org and github.com.
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p .cache && cd .cache
fetch() { # id version dir
  [ -d "$3" ] && return
  curl -sSL -o "$3.nupkg" "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg"
  mkdir -p "$3" && (cd "$3" && unzip -oq "../$3.nupkg")
}
fetch unityengine.modules 2021.3.33 modules   # UnityEngine.*Module reference assemblies
fetch unity3d.sdk 2021.1.14.1 sdk              # UnityEditor.dll
fetch nunit 3.13.3 nunit
fetch nunitlite 3.13.3 nunitlite
[ -d math ] || git clone -q --depth 1 -b 1.3.2 https://github.com/needle-mirror/com.unity.mathematics math
[ -d ugui ] || git clone -q --depth 1 -b "1.0.0/Unity-2021.1.17f1" https://github.com/needle-mirror/com.unity.ugui ugui
echo "verify cache ready"
