"""Validador del kit modular de Puerto Alba. Solo biblioteca estándar: no necesita Blender ni Unity.

Uso (desde la raíz del proyecto):
    python3 ArtSource/PuertoAlba/validar_kit.py

Comprueba:
  1. Cada pieza de stats_kit.json tiene un FBX real (no un puntero de Git LFS), con su .meta y GUID.
  2. Triángulos dentro del presupuesto y cantidad de piezas esperada.
  3. Medidas de Blender y de Unity (unity_import_check.txt) coinciden con los ejes intercambiados (Y de Blender = Z de Unity).
  4. Referencias: piezas preferentes de zonas_variacion.json existen; las zonas coinciden con ZoneId de CityMap.cs;
     cada zona tiene sus seis colores válidos.
  5. Materiales por zona en Assets/_Game/Models/PuertoAlba/Zonas (uno por zona y campo, con .meta).
  6. Vistas previas presentes.

Sale con código 0 si todo pasa. Lo que no puede comprobar sin Unity visual queda marcado como «NO VERIFICADO».
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba")
ZONAS_DIR = os.path.join(MODEL_DIR, "Zonas")
PREVIEW = os.path.join(HERE, "Preview")
CAMPOS = ["muro", "zocalo", "acento", "tejado", "toldo", "vidrio"]
PIEZAS_ESPERADAS = 9

fallos = []
ok = []


def check(cond, texto):
    (ok if cond else fallos).append(texto)


def leer(ruta):
    # utf-8-sig: Unity escribe el informe con marca BOM
    with open(ruta, encoding="utf-8-sig") as f:
        return f.read()


def es_fbx_real(ruta):
    with open(ruta, "rb") as f:
        cab = f.read(32)
    return cab.startswith(b"Kaydara FBX Binary")


stats = json.loads(leer(os.path.join(HERE, "stats_kit.json")))
check(len(stats) == PIEZAS_ESPERADAS, "stats_kit.json tiene %d piezas (esperadas %d)" % (len(stats), PIEZAS_ESPERADAS))

# 1–2. FBX, metas, presupuesto
guids = {}
for nombre, d in stats.items():
    fbx = os.path.join(MODEL_DIR, nombre + ".fbx")
    meta = fbx + ".meta"
    check(os.path.isfile(fbx), nombre + ": existe el FBX")
    if os.path.isfile(fbx):
        check(es_fbx_real(fbx), nombre + ": el FBX es binario real (no un puntero de Git LFS sin descargar)")
    check(os.path.isfile(meta), nombre + ": tiene .meta de Unity")
    if os.path.isfile(meta):
        m = re.search(r"^guid: ([0-9a-f]{32})$", leer(meta), re.M)
        check(m is not None, nombre + ": el .meta tiene GUID válido")
        if m:
            guids[nombre] = m.group(1)
    check(d["triangulos"] <= d["presupuesto_triangulos"],
          "%s: %d triángulos dentro de %d" % (nombre, d["triangulos"], d["presupuesto_triangulos"]))
    check(d.get("pivote_correcto") is True, nombre + ": pivote en z = 0 y centrado en X")

check(len(set(guids.values())) == len(guids), "los GUID de las piezas son distintos")

# 3. medidas Blender (X, Y, Z) frente a Unity (X, Z, Y): Unity lee Y de Blender como Z y Z de Blender como Y
ruta_unity = os.path.join(HERE, "unity_import_check.txt")
if os.path.isfile(ruta_unity):
    lineas = [l for l in leer(ruta_unity).splitlines() if l.strip()]
    vistos = {}
    for l in lineas:
        nombre = l.split(":", 1)[0].strip()
        m = re.search(r"tamaño_XYZ=([\d.]+)x([\d.]+)x([\d.]+)", l)
        t = re.search(r"tris=(\d+)", l)
        if m and t:
            vistos[nombre] = (float(m.group(1)), float(m.group(2)), float(m.group(3)), int(t.group(1)))
    for nombre, d in stats.items():
        if nombre not in vistos:
            check(False, nombre + ": falta en el informe de Unity")
            continue
        bx, by, bz = d["dimensiones_XxYxZ_m"]
        ux, uy, uz, ut = vistos[nombre]
        check(abs(bx - ux) < .02 and abs(bz - uy) < .02 and abs(by - uz) < .02,
              "%s: medidas Blender %s = Unity (%.2f, %.2f, %.2f) con ejes intercambiados" % (nombre, d["dimensiones_XxYxZ_m"], ux, uy, uz))
        check(ut == d["triangulos"], "%s: mismo número de triángulos en Blender y Unity (%d)" % (nombre, ut))
    check(os.path.isfile(ruta_unity), "informe de Unity presente")
else:
    check(False, "falta unity_import_check.txt (ejecutar PuertoAlbaKitImportCheck en Unity)")

# 4. referencias del JSON y de CityMap
zonas = json.loads(leer(os.path.join(HERE, "zonas_variacion.json")))
zonas = {k: v for k, v in zonas.items() if not k.startswith("_")}
mapa = leer(os.path.join(PROJECT, "Assets", "_Game", "Scripts", "City", "CityMap.cs"))
enum = re.search(r"enum ZoneId\s*\{([^}]*)\}", mapa).group(1)
ids_codigo = [x.strip() for x in enum.split(",") if x.strip()]
check(sorted(zonas.keys()) == sorted(ids_codigo), "las zonas del JSON coinciden con ZoneId de CityMap.cs (%d)" % len(ids_codigo))
for zid, z in zonas.items():
    for campo in CAMPOS:
        valor = z.get(campo, "")
        check(bool(re.fullmatch(r"#[0-9A-Fa-f]{6}", valor)), "%s.%s es un color hexadecimal válido" % (zid, campo))
    for pieza in z.get("piezas_preferentes", []):
        check(pieza in stats, "%s: pieza preferente %s existe en el kit" % (zid, pieza))

# 5. materiales por zona
esperados = [("Zona_%s_%s.mat" % (zid, c)) for zid in zonas for c in CAMPOS]
if os.path.isdir(ZONAS_DIR):
    for nombre in esperados:
        check(os.path.isfile(os.path.join(ZONAS_DIR, nombre)), "material " + nombre)
        check(os.path.isfile(os.path.join(ZONAS_DIR, nombre + ".meta")), "meta de " + nombre)
else:
    check(False, "falta la carpeta Models/PuertoAlba/Zonas (ejecutar PuertoAlbaKitImportCheck en Unity)")

# 6. vistas previas
for ruta in [os.path.join(PREVIEW, "Kit_residencial_tienda.png"), os.path.join(PREVIEW, "Kit_gasolinera.png"),
             os.path.join(PREVIEW, "Zonas", "hoja_zonas.png")]:
    check(os.path.isfile(ruta), "vista previa " + os.path.relpath(ruta, HERE))

print("OK (%d):" % len(ok))
for t in ok:
    print("  ✔", t)
print("FALLOS (%d):" % len(fallos))
for t in fallos:
    print("  ✘", t)
print("NO VERIFICADO sin Unity visual: aspecto en escena, colocación en la cuadrícula 21×21, colores aplicados a instancias en escena.")
sys.exit(1 if fallos else 0)
