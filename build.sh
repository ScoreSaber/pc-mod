#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
default_target_version="1.42.0"
target_version="$default_target_version"
target_version_set=false
deploy=false

usage() {
  echo "usage: ./build.sh [--target-version VERSION] [--ssh-deploy]" >&2
  echo "       ./build.sh [-v VERSION] [--ssh-deploy]" >&2
  exit 1
}

while (( $# )); do
  case "$1" in
    --target-version|--version|-v)
      (( $# >= 2 )) || usage
      target_version="$2"
      target_version_set=true
      shift 2
      ;;
    --ssh-deploy)
      deploy=true
      shift
      ;;
    1.29.0|1.37.1|1.38.0|1.40.0|1.42.0)
      target_version="$1"
      target_version_set=true
      shift
      ;;
    *)
      usage
      ;;
  esac
done

case "$target_version" in
  1.29.0|1.37.1|1.38.0|1.40.0|1.42.0) ;;
  *)
    echo "Unsupported target version '$target_version'." >&2
    usage
    ;;
esac

refs_root="$root/refs"
[[ -d "$refs_root" ]] || refs_root="$root/Refs"
refs="$refs_root/$target_version"
if [[ ! -d "$refs/Beat Saber_Data/Managed" && "$target_version" == "$default_target_version" && -d "$refs_root/Beat Saber_Data/Managed" ]]; then
  refs="$refs_root"
fi
build_platform="Any CPU"
output_dll="$root/src/bin/Release/ScoreSaber.dll"
if $target_version_set; then
  build_platform="BS$target_version"
  output_dll="$root/src/bin/$target_version/Release/ScoreSaber.dll"
fi

[[ -d "$refs/Beat Saber_Data/Managed" && -d "$refs/Plugins" && -f "$refs/Plugins/LeaderboardCore.dll" ]] || {
  echo "Refs for $target_version are incomplete at $refs; expected Beat Saber_Data/Managed and Plugins/LeaderboardCore.dll." >&2
  exit 1
}

dotnet build "$root/ScoreSaber.sln" -c Release \
  -p:Platform="$build_platform" \
  -p:ScoreSaberTargetVersion="$target_version" \
  -p:ScoreSaberUseLocalRefs=true \
  -p:LocalRefsDir="$refs" \
  -p:GameReferences="$refs" \
  -p:DisableCopyToGame=True

$deploy || exit 0

prop() { sed -nE "s:.*<$1[^>]*>(.*)</$1>.*:\\1:p" "$root/Directory.Build.local.props" 2>/dev/null | tail -n 1; }
version_prop_suffix="${target_version//./_}"

target="$(prop "ScoreSaberSshTarget$version_prop_suffix")"
target="${target:-$(prop ScoreSaberSshTarget)}"
game="$(prop "ScoreSaberSshBeatSaberDir$version_prop_suffix")"
game="${game:-$(prop ScoreSaberSshBeatSaberDir)}"
[[ -n "$target" && -n "$game" ]] || {
  echo "Directory.Build.local.props must set ScoreSaberSshTarget and ScoreSaberSshBeatSaberDir, or version-specific variants for $target_version." >&2
  exit 1
}

remote_dir="${game}\\Plugins"
remote_dll="${remote_dir}\\ScoreSaber.dll"
mkdir_command="\$ProgressPreference = 'SilentlyContinue'; New-Item -ItemType Directory -Force -Path '$remote_dir' | Out-Null"

ssh "$target" powershell -NoProfile -NonInteractive -EncodedCommand "$(printf "%s" "$mkdir_command" | iconv -f UTF-8 -t UTF-16LE | base64)"
[[ -f "$output_dll" ]] || {
  echo "Build output missing: $output_dll" >&2
  exit 1
}
scp "$output_dll" "$target:${remote_dll//\\//}"
