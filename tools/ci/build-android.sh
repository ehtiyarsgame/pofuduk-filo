#!/usr/bin/env bash
# Runs INSIDE the unityci/editor Android image (see .github/workflows/android.yml).
# Activates a Unity Personal seat with account credentials, builds the APK, and always
# returns the seat — a leaked Personal seat blocks every later build on the account.
# Commands follow game-ci/cli's ubuntu personal-license steps (measured on 6000.x).
set -uo pipefail

PROJECT="${PROJECT_PATH:-/project}"
APK="$PROJECT/build/Android/PofudukFilo.apk"
CLIENT=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client

if [ -z "${UNITY_EMAIL:-}" ] || [ -z "${UNITY_PASSWORD:-}" ]; then
  echo "::error::UNITY_EMAIL / UNITY_PASSWORD secrets are missing."
  exit 1
fi

return_seat() {
  echo "Returning the Personal license seat…"
  for attempt in 1 2 3; do
    if "$CLIENT" --return-ulf 2>&1 | tee /tmp/return.log | grep -qE "Successfully returned|License has been returned"; then
      echo "Seat returned."
      return 0
    fi
    sleep 5
  done
  echo "::warning::Could not confirm the seat was returned. If the next build says 'No seat available', remove the old activation at https://id.unity.com."
}

echo "Activating Unity Personal license…"
ACTIVATED=0
for attempt in 1 2 3; do
  # The password is an argument (the client has no stdin option); it is never echoed.
  "$CLIENT" --activate-all --include-personal --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" > /tmp/activate.log 2>&1
  code=$?
  sed -E 's/(password|Password)[^ ]*/\1 ***/g' /tmp/activate.log
  if [ $code -eq 0 ] && ! grep -qiE "No seat available|No license activation found" /tmp/activate.log; then
    ACTIVATED=1
    break
  fi
  echo "Activation attempt $attempt failed (exit $code); retrying…"
  sleep $((attempt * 10))
done

if [ $ACTIVATED -ne 1 ]; then
  echo "::error::Unity Personal activation failed. Check the e-mail/password secrets; accounts with two-factor authentication cannot be activated this way."
  exit 1
fi
trap return_seat EXIT

mkdir -p "$(dirname "$APK")"
echo "Building Android APK…"
unity-editor \
  -batchmode -nographics \
  -logFile - \
  -projectPath "$PROJECT" \
  -buildTarget Android \
  -executeMethod PofudukFilo.EditorTools.CiBuild.BuildAndroid \
  -customBuildPath "$APK"
BUILD_EXIT=$?

if [ $BUILD_EXIT -ne 0 ] || [ ! -f "$APK" ]; then
  echo "::error::Unity build failed (exit $BUILD_EXIT)."
  exit 1
fi
ls -lh "$APK"
