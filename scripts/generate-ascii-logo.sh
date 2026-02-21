#!/usr/bin/env bash
set -euo pipefail

input="KonciergeUi.Client/Resources/Images/koncierge_logo.svg"
output="KonciergeUI.Cli/Resources/logo.txt"
width="40"
height_set="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --width)
      width="$2"
      shift 2
      ;;
    --height)
      height="$2"
      height_set="true"
      shift 2
      ;;
    *)
      echo "Unknown arg: $1" >&2
      exit 1
      ;;
  esac
done

if [[ "$height_set" != "true" ]]; then
  height=$((width / 3))
fi

if [[ ! -f "$input" ]]; then
  echo "Input SVG not found: $input" >&2
  exit 1
fi

if ! command -v chafa >/dev/null 2>&1; then
  echo "Missing dependency: chafa" >&2
  echo "Install: brew install chafa" >&2
  exit 1
fi

convert_svg_to_png() {
  local svg="$1"
  local png="$2"
  if command -v magick >/dev/null 2>&1; then
    magick -background none -density 300 "$svg" "$png"
    return 0
  fi

  if command -v rsvg-convert >/dev/null 2>&1; then
    rsvg-convert -o "$png" "$svg"
    return 0
  fi

  echo "Missing dependency: ImageMagick (magick) or rsvg-convert" >&2
  echo "Install: brew install imagemagick  OR  brew install librsvg" >&2
  exit 1
}

png_tmp="$(mktemp -t koncierge-logo-XXXXXX).png"
cleanup() { rm -f "$png_tmp"; }
trap cleanup EXIT

convert_svg_to_png "$input" "$png_tmp"
chafa --size "${width}x${height}" --colors=256 --symbols=block --dither=none "$png_tmp" > "$output"

echo "Wrote ASCII logo to $output"
