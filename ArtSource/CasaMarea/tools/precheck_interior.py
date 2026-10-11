"""Precomprobación SIN Blender de build_interior_cocina.py (triángulos, dimensiones y pivotes por simulación).

    python3 ArtSource/CasaMarea/tools/precheck_interior.py

No sustituye a stats_interior.json, que sale de Blender.
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
spec = importlib.util.spec_from_file_location("interior", os.path.join(HERE, "..", "build_interior_cocina.py"))
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

groups, alturas = mod.build_all()
for name, objs in groups.items():
    if name == "SM_Hamburguesa_Piezas":
        total = 0
        for o in objs:
            s = mod.stat(o)
            total += s["triangulos"]
            print("   %-24s tris=%3d dims=%s baseZ=%s" % (o.name, s["triangulos"], s["dimensiones_XxYxZ_m"], s["pivote"]["base_z_m"]))
        print("%-24s tris=%4d tope=%s (suma de piezas)  alturas=%s" % (name, total, mod.PIECES_BUDGET, alturas))
    else:
        s = mod.stat(objs[0], mod.BUDGET[name])
        print("%-24s tris=%4d tope=%s dims=%s min=%s max=%s centroXY=%s baseZ=%s" % (
            name, s["triangulos"], s["presupuesto_triangulos"], s["dimensiones_XxYxZ_m"], s["min"], s["max"],
            s["pivote"]["desfase_centro_XY_m"], s["pivote"]["base_z_m"]))
