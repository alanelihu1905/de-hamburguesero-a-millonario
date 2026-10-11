"""Precomprobación SIN Blender de build_clientes_vecinas.py (triángulos, dimensiones y pivotes por simulación).

    python3 ArtSource/CasaMarea/tools/precheck_clientes.py

No sustituye a stats_clientes_vecinas.json, que sale de Blender.
"""
import importlib.util
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.dont_write_bytecode = True
import fakebpy  # noqa: E402

sys.modules["bpy"] = fakebpy
sys.path.insert(0, os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("clientes", os.path.join(HERE, "..", "build_clientes_vecinas.py"))
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

for name, objs in mod.build_all().items():
    s = mod.stat(objs[0], mod.BUDGET[name])
    print("%-22s tris=%4d tope=%s dims=%s min=%s max=%s centroXY=%s baseZ=%s" % (
        name, s["triangulos"], s["presupuesto_triangulos"], s["dimensiones_XxYxZ_m"], s["min"], s["max"],
        s["pivote"]["desfase_centro_XY_m"], s["pivote"]["base_z_m"]))
    print("    materiales:", s["materiales"])
