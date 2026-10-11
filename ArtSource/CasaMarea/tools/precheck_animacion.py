"""Comprobación SIN Blender del esqueleto y las animaciones de los clientes (cinemática directa en Python puro).

    python3 ArtSource/CasaMarea/tools/precheck_animacion.py

Comprueba: huesos y jerarquía, bucles cerrados, límites articulares (rodillas y codos solo en su sentido), contacto de la
suela con el suelo en cada llave, y estima la velocidad de referencia del ciclo de caminar.
No sustituye a stats_clientes_animados.json, que sale de Blender.
"""
import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import rigdata  # noqa: E402

print("huesos (%d):" % len(rigdata.HUESOS))
for n, padre, cab, col in rigdata.HUESOS:
    print("  %-16s padre=%-14s cabeza=(%.2f, %.2f, %.2f) cola=(%.2f, %.2f, %.2f)" % ((n, padre or "-") + cab + col))
problemas = 0
for nombre, fn in rigdata.CLIPS.items():
    clip = fn()
    inf = rigdata.verificar_clip(clip)
    dur = clip["frames"] / float(rigdata.FPS)
    print("\n== %s: %d fotogramas a %d fps (%.2f s), bucle=%s, llaves=%s" % (
        nombre, clip["frames"], rigdata.FPS, dur, clip["bucle"], sorted(clip["llaves"])))
    print("   primera == última:", inf["primera_igual_a_ultima"], "| huesos animados:", len(inf["huesos_animados"]))
    print("   suela mínima en z por llave (0 = apoyada):", inf["suela_min_z_por_llave"])
    pelvis = {f: round(p["loc"].get("pelvis", (0, 0, 0))[2], 3) for f, p in clip["llaves"].items()}
    print("   desplazamiento vertical de la pelvis por llave:", pelvis)
    for e in inf["errores"]:
        print("   ERROR:", e)
        problemas += 1
if True:
    print("\nciclo de caminar:", rigdata.estadisticas_paso())
print("\nPROBLEMAS:", problemas)
sys.exit(1 if problemas else 0)
