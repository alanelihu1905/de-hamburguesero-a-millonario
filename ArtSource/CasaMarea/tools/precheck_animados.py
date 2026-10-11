"""Comprobación SIN Blender de la geometría y los pesos de los clientes animados (build_clientes_animados.py).

    python3 ArtSource/CasaMarea/tools/precheck_animados.py

Construye los tres clientes con la simulación de bpy y comprueba: triángulos dentro de presupuesto, cada vértice con un
hueso, todos los huesos del esqueleto con vértices (salvo root), grupos de cara solo en la cabeza y dimensiones.
No exporta nada ni escribe en Assets. No sustituye a stats_clientes_animados.json (sale de Blender).
"""
import importlib.util
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, ".."))
import fakebpy  # noqa: E402

sys.modules["bpy"] = fakebpy
spec = importlib.util.spec_from_file_location("animados", os.path.join(HERE, "..", "build_clientes_animados.py"))
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
rigdata = mod.rigdata

huesos = {h[0] for h in rigdata.HUESOS}
problemas = 0
for nombre, spec_c in mod.CLIENTES.items():
    obj = mod.construir_cliente(nombre, **spec_c)
    s = mod.stat(obj)
    grupos = {g.name: g for g in obj.vertex_groups}
    n = len(obj.data.vertices)
    por_vertice = {i: [] for i in range(n)}
    for g in obj.vertex_groups:
        for i, w in g.data.items():
            if g.name in huesos:
                por_vertice[i].append(g.name)
    sin_hueso = [i for i, hs in por_vertice.items() if not hs]
    varios = [i for i, hs in por_vertice.items() if len(hs) > 1]
    sin_vertices = sorted(h for h in huesos - {"root"} if h not in grupos or not grupos[h].data)
    cara_fuera = [g.name for g in obj.vertex_groups if g.name.startswith("cara_") and
                  any("cabeza" not in por_vertice[i] for i in g.data)]
    print("%-20s tris=%4d (tope %d) vértices=%d sin_hueso=%d con_varios_huesos=%d huesos_sin_vértices=%s cara_fuera_de_cabeza=%s" % (
        nombre, s["triangulos"], mod.PRESUPUESTO, n, len(sin_hueso), len(varios), sin_vertices, cara_fuera))
    print("    dims=%s min=%s max=%s materiales=%s" % (s["dimensiones_XxYxZ_m"], s["min"], s["max"], s["materiales"]))
    if s["triangulos"] > mod.PRESUPUESTO or sin_hueso or varios or sin_vertices or cara_fuera:
        problemas += 1
print("PROBLEMAS:", problemas)
sys.exit(1 if problemas else 0)
