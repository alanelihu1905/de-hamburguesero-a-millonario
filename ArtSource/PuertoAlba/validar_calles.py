"""Validador sin Unity del trazado de calles de Puerto Alba.

Uso (desde la raíz del proyecto):
    python3 ArtSource/PuertoAlba/validar_calles.py

Lee la tabla `Edificios` de Assets/Editor/PuertoAlbaCallesBuilder.cs (una sola fuente de verdad), carga las cajas reales
de cada pieza que midió Blender (stats_kit.json, stats_zonas_hitos.json, stats_casas.json) y reproduce la colocación:
giro, apoyo contra la acera por la cara más cercana a la calle (igual que `Apoyar()` en C#) y centrado del casino.

Comprueba:
  1. Ningún edificio invade la avenida (x −60…60, z −6…6) ni el cruce (x −6…6, z −40…40).
  2. Dos piezas no se solapan en planta (las cajas se cruzan menos de 1 cm).
  3. La plaza de entrada del casino (x 6…14, z −26…−14) queda libre y el casino no invade la vía.
  4. Cada pieza de la tabla tiene sus medidas en Blender.

No sustituye a Unity: no mide mallas reales ni colisiones. Sale con código 0 si todo pasa.
"""
import json
import math
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
CS = os.path.join(PROJECT, "Assets", "Editor", "PuertoAlbaCallesBuilder.cs")

AVENIDA_LIBRE = (-60.0, -6.0, 60.0, 6.0)     # x0, z0, x1, z1
CRUCE_LIBRE = (-6.0, -40.0, 6.0, 40.0)
PLAZA_CASINO = (6.0, -26.0, 14.0, -14.0)
CASINO_CENTRO = (16.0, -20.0)
CASINO_YAW = -90.0
RETRANQUEO = 6.3

stats = {}
for nombre in ("stats_kit.json", "stats_zonas_hitos.json", "stats_casas.json"):
    with open(os.path.join(HERE, nombre), encoding="utf-8") as f:
        stats.update(json.load(f))

fallos, oks = [], []


def check(cond, texto):
    (oks if cond else fallos).append(texto)


def bbox_unity(pieza, dx=0.0):
    """Caja en planta en el sistema de Unity (x = x de Blender, z = −y de Blender): (x0, z0, x1, z1)."""
    d = stats[pieza]
    (bx0, by0, _), (bx1, by1, _) = d["bbox_min_XYZ_m"], d["bbox_max_XYZ_m"]
    return (bx0 + dx, -by1, bx1 + dx, -by0)


def union(cajas):
    return (min(c[0] for c in cajas), min(c[1] for c in cajas), max(c[2] for c in cajas), max(c[3] for c in cajas))


def caja_prefab(prefab):
    """Caja en planta del prefab. PF_Gasolinera_Completa es un compuesto (canopy, dos surtidores, caseta y letrero)."""
    if prefab == "PF_Gasolinera_Completa":
        return union([
            bbox_unity("SM_Gasolinera_Marquesina"),
            bbox_unity("SM_Gasolinera_Surtidor", -2.5), bbox_unity("SM_Gasolinera_Surtidor", 2.5),
            bbox_unity("SM_Gasolinera_Caseta", 8.5), bbox_unity("SM_Gasolinera_Letrero", -8.5),
        ])
    return bbox_unity("SM_" + prefab[len("PF_"):])


def rotar(caja, yaw):
    """Gira la caja alrededor de Y (como Quaternion.Euler(0, yaw, 0) de Unity) y devuelve su caja envolvente."""
    t = math.radians(yaw)
    c, s = math.cos(t), math.sin(t)
    esquinas = [(caja[0], caja[1]), (caja[2], caja[1]), (caja[2], caja[3]), (caja[0], caja[3])]
    pts = [(x * c + z * s, -x * s + z * c) for x, z in esquinas]
    return (min(p[0] for p in pts), min(p[1] for p in pts), max(p[0] for p in pts), max(p[1] for p in pts))


def cruza(a, b, tol=0.0):
    return a[2] > b[0] + tol and a[0] < b[2] - tol and a[3] > b[1] + tol and a[1] < b[3] - tol


# 1. tabla del constructor de C#
texto = open(CS, encoding="utf-8").read()
patron = re.compile(r'new Pieza \{ Prefab = "(\w+)", Nombre = "([^"]+)", Tipo = "(\w+)", Pos = new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\), Yaw = ([-\d.]+) \}')
piezas = [(m.group(1), m.group(2), m.group(3), float(m.group(4)), float(m.group(6)), float(m.group(7))) for m in patron.finditer(texto)]
check(len(piezas) >= 20, "la tabla de C# tiene %d piezas" % len(piezas))

colocadas = []
for prefab, nombre, tipo, x, z, yaw in piezas:
    try:
        local = caja_prefab(prefab)
    except KeyError:
        check(False, "%s: sin medidas de Blender para %s" % (nombre, prefab))
        continue
    r = rotar(local, yaw)
    caja = (r[0] + x, r[1] + z, r[2] + x, r[3] + z)
    # apoyo contra la acera: igual que Apoyar() en C# (la cara más cercana a la calle queda a 6.3 m del eje)
    if tipo == "Avenida":
        dz = (RETRANQUEO - caja[1]) if z > 0 else (-RETRANQUEO - caja[3])
        caja = (caja[0], caja[1] + dz, caja[2], caja[3] + dz)
    elif tipo == "Cruce":
        dx = (RETRANQUEO - caja[0]) if x > 0 else (-RETRANQUEO - caja[2])
        caja = (caja[0] + dx, caja[1], caja[2] + dx, caja[3])
    colocadas.append((nombre, tipo, caja))

# 2. invasión de la vía y solapes
for nombre, tipo, caja in colocadas:
    check(not cruza(caja, AVENIDA_LIBRE), "%s fuera de la avenida" % nombre)
    check(not cruza(caja, CRUCE_LIBRE), "%s fuera del cruce" % nombre)

# 3. casino (hito) centrado en su punto, plaza libre y fuera de la vía
hito_local = rotar(caja_prefab("PF_Hito_Casino"), CASINO_YAW)
cx, cz = (hito_local[0] + hito_local[2]) / 2, (hito_local[1] + hito_local[3]) / 2
casino = (hito_local[0] - cx + CASINO_CENTRO[0], hito_local[1] - cz + CASINO_CENTRO[1],
          hito_local[2] - cx + CASINO_CENTRO[0], hito_local[3] - cz + CASINO_CENTRO[1])
check(not cruza(casino, AVENIDA_LIBRE) and not cruza(casino, CRUCE_LIBRE), "casino fuera de la avenida y del cruce")
for nombre, tipo, caja in colocadas:
    check(not cruza(caja, PLAZA_CASINO), "%s no ocupa la plaza de entrada del casino" % nombre)
    check(not cruza(caja, casino, 0.01), "%s no se solapa con el casino" % nombre)

# solapes entre piezas
for i in range(len(colocadas)):
    for j in range(i + 1, len(colocadas)):
        a, b = colocadas[i], colocadas[j]
        check(not cruza(a[2], b[2], 0.01), "%s y %s no se solapan" % (a[0], b[0]))

print("OK (%d):" % len(oks))
for t in oks[:6]:
    print("  ✔", t)
print("  ... y %d más" % max(0, len(oks) - 6))
print("FALLOS (%d):" % len(fallos))
for t in fallos:
    print("  ✘", t)
print("NO VERIFICADO sin Unity: mallas reales, colisiones, frente en escena.")
sys.exit(1 if fallos else 0)
