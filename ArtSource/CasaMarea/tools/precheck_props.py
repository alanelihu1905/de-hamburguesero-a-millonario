"""Precomprobación SIN Blender de build_props_barrio.py: triángulos, dimensiones y pivotes por simulación de geometría.

    python3 ArtSource/CasaMarea/tools/precheck_props.py

Sirve para detectar errores de lógica y desvíos de medidas antes de ejecutar Blender. No sustituye a stats_props.json,
que sale de Blender. Puede diferir en detalles (p. ej. normales, UV, materiales por cara).
"""
import importlib.util
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import fakebpy  # noqa: E402

sys.modules["bpy"] = fakebpy
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("props", os.path.join(HERE, "..", "build_props_barrio.py"))
props = importlib.util.module_from_spec(spec)
spec.loader.exec_module(props)

results = {}
for fn in (props.build_banco, props.build_maceta, props.build_farol, props.build_cubeta):
    o = fn()
    results[o.name] = props.stat(o, props.BUDGET[o.name])
cartel, texto = props.build_cartel()
results[cartel.name] = props.stat(cartel, props.BUDGET[cartel.name])
results[texto.name] = props.stat(texto)
for name, s in results.items():
    print("%-20s tris=%4d tope=%s dims=%s centroXY=%s baseZ=%s" % (
        name, s["triangulos"], s.get("presupuesto_triangulos"), s["dimensiones_XxYxZ_m"],
        s["pivote"]["desfase_centro_XY_m"], s["pivote"]["base_z_m"]))
print("materiales:", sorted({m for k, s in results.items() for m in s["materiales"]}))
