#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
refs="$root/Refs"
deploy="${1:-}"

if (( $# > 1 )) || [[ "$deploy" != "" && "$deploy" != "--ssh-deploy" ]]; then
  echo "usage: ./build.sh [--ssh-deploy]" >&2
  exit 1
fi

[[ -d "$refs/Beat Saber_Data/Managed" && -d "$refs/Plugins" && -f "$refs/Plugins/LeaderboardCore.dll" ]] || {
  echo "Refs are incomplete; expected Beat Saber_Data/Managed and Plugins/LeaderboardCore.dll." >&2
  exit 1
}

dotnet build "$root/ScoreSaber.sln" -c Release \
  -p:BeatSaberDir="$refs" \
  -p:DisableCopyToGame=True

[[ "$deploy" == "--ssh-deploy" ]] || exit 0

prop() { sed -nE "s:.*<$1[^>]*>(.*)</$1>.*:\\1:p" "$root/Directory.Build.local.props" 2>/dev/null | tail -n 1; }

target="$(prop ScoreSaberSshTarget)"
game="$(prop ScoreSaberSshBeatSaberDir)"
[[ -n "$target" && -n "$game" ]] || {
  echo "Directory.Build.local.props must set ScoreSaberSshTarget and ScoreSaberSshBeatSaberDir." >&2
  exit 1
}

remote_dir="${game}\\Plugins"
remote_dll="${remote_dir}\\ScoreSaber.dll"
mkdir_command="\$ProgressPreference = 'SilentlyContinue'; New-Item -ItemType Directory -Force -Path '$remote_dir' | Out-Null"

ssh "$target" powershell -NoProfile -NonInteractive -EncodedCommand "$(printf "%s" "$mkdir_command" | iconv -f UTF-8 -t UTF-16LE | base64)"
scp "$root/src/bin/Release/ScoreSaber.dll" "$target:${remote_dll//\\//}"
