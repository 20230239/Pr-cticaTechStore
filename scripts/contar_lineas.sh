#!/usr/bin/env bash
cd "$(dirname "$0")/.."
if command -v cloc >/dev/null; then
  for p in TechStore.AdoNet TechStore.EFCore; do echo "== $p =="; cloc --exclude-dir=bin,obj,Migrations --include-lang=C# "$p" --quiet; done
else
  for p in TechStore.AdoNet TechStore.EFCore; do
    n=$(find "$p" -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' -not -path '*/Migrations/*' -print0 \
        | xargs -0 cat | grep -vE '^\s*(//.*)?$' | wc -l)
    echo "$p: $n líneas de código (sin vacías ni comentarios //)"
  done
fi
