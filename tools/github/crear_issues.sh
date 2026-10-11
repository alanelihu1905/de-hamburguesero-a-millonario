#!/usr/bin/env bash
# Crea etiquetas y los issues iniciales a partir de tools/github/issues.tsv.
# Requisitos: GitHub CLI (`gh`) autenticado y permiso de escritura en el repositorio.
# Uso:   tools/github/crear_issues.sh OWNER/REPO          (muestra lo que haría)
#        tools/github/crear_issues.sh OWNER/REPO --crear   (crea de verdad)
# Este script NO se ejecutó todavía: el repositorio aún no existe.
set -euo pipefail
REPO="${1:?Indica OWNER/REPO}"
MODO="${2:-}"
cd "$(dirname "$0")/../.."
run() { if [ "$MODO" = "--crear" ]; then "$@"; else echo "[simulación] $*"; fi; }

etiqueta_area() { case "$1" in A) echo "area:economia";; B) echo "area:casino-ui";; C) echo "area:arte-qa";; *) echo "area:desconocida";; esac; }
run gh label create "area:economia"  --repo "$REPO" --color 0E8A16 --description "Economía y guardado" --force
run gh label create "area:casino-ui" --repo "$REPO" --color 5319E7 --description "Casino y UI" --force
run gh label create "area:arte-qa"   --repo "$REPO" --color FBCA04 --description "Arte, mundo y QA" --force
for p in P1 P2 P3; do run gh label create "$p" --repo "$REPO" --color D93F0B --force; done
for t in S M L; do run gh label create "tam:$t" --repo "$REPO" --color C5DEF5 --force; done
run gh label create "decisión" --repo "$REPO" --color 0052CC --description "Decisión de diseño abierta" --force

tail -n +2 tools/github/issues.tsv | while IFS=$'\t' read -r titulo area prio tam cuerpo; do
  cuerpo="${cuerpo//\\n/$'\n'}"
  run gh issue create --repo "$REPO" --title "$titulo" --body "$cuerpo" \
      --label "$(etiqueta_area "$area"),$prio,tam:${tam:0:1}"
done
echo "Listo. Crea el tablero en GitHub: Projects > New project > Board, y añade los issues (ver Docs/COLABORACION.md)."
