#!/bin/zsh
# Waits for a TikTok build queued by trigger_build.cs (or pack_only.cs) and prints one verdict line:
#   SUCCESS / COMMIT_REFUSED / FAILED <first error line> / NOT_STARTED / TIMEOUT
# Run it in the background from the project root: watch_build.sh <OutputDir> [timeout_minutes]
# Errors already in the console are counted first, so an earlier build's failure never reads as this one's.
# A busy Editor answers console calls with a timeout; those polls are skipped, never read as an empty console.
out="$1"; limit=$(( ${2:-40} * 60 )); start=$(date +%s)
[ -d "$out" ] || { echo "usage: watch_build.sh <OutputDir> [timeout_minutes]"; exit 2; }

errorPattern='Build failed[^"\\]*\|failed with output[^"\\]*\|Refusing to continue committing[^"\\]*\|Fatal error[^"\\]*\|error CS[^"\\]*'
# The build logs "[TTSDK] Strip Engine Code" as it starts; pack_only.cs logs "[PackOnly]" when it ends.
startPattern='Strip Engine Code\|\[PackOnly\]'
console() { local r; r=$(unity command console "$@" 2>/dev/null); echo "$r" | grep -q '"entries"' && echo "$r"; }
elapsed() { echo $(( $(date +%s) - start )); }

until errors=$(console --level error); do
  [ $(elapsed) -ge $limit ] && { echo "TIMEOUT: the Editor never answered"; exit 4; }
  sleep 20
done
base=$(echo "$errors" | grep -o "$errorPattern" | grep -c '')
startBase=$(console | grep -o "$startPattern" | grep -c '')
started=0

while [ $(elapsed) -lt $limit ]; do
  sleep 20
  wasm=$(ls "$out"/tt-minigame/wasmcode/*.br 2>/dev/null | head -1)
  if [ -n "$wasm" ] && [ $(stat -f %m "$wasm") -gt $start ]; then
    echo "SUCCESS after $(elapsed)s"; exit 0
  fi
  if all=$(console); then
    [ "$(echo "$all" | grep -o "$startPattern" | grep -c '')" -ne "$startBase" ] && started=1
  else
    started=1                                           # the Editor is busy, which a running build explains
    continue
  fi
  errors=$(console --level error) || continue
  found=$(echo "$errors" | grep -o "$errorPattern")
  count=$(echo -n "$found" | grep -c '')
  [ "$count" -lt "$base" ] && base=0                    # "Clear on Build" emptied the console
  new=$(echo "$found" | tail -n +$(( base + 1 )))
  if [ -n "$new" ]; then
    if echo "$new" | grep -q 'Refusing to continue committing'; then
      echo "COMMIT_REFUSED after $(elapsed)s: the WebGL build may be fine, run pack_only.cs"
    else
      echo "FAILED after $(elapsed)s: $(echo "$new" | head -1)"
    fi
    exit 1
  fi
  if [ $started -eq 0 ] && [ $(elapsed) -ge 60 ]; then
    echo "NOT_STARTED: click the Unity window once so delayCall fires (do not press Build)"; exit 3
  fi
done
echo "TIMEOUT after ${limit}s"; exit 4
