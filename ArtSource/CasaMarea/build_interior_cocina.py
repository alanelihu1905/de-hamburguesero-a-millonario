"""Genera los modelos del interior de la cocina de Casa Marea (tanda 2) en Blender.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_interior_cocina.py

Modelos (documento 04, secciones 2.4 y 6): refrigerador, hamburguesa modular (siete piezas apilables y una versión
armada), tabla de corte con cuchillo, campana de extracción con ducto, repisa con frascos, lámpara colgante y un
módulo de mostrador. Reutiliza los ayudantes de build_props_barrio.py.

Salidas:
    ArtSource/CasaMarea/Interior_Cocina.blend
    Assets/_Game/Models/CasaMarea/SM_Refrigerador.fbx, SM_Hamburguesa_Piezas.fbx, SM_Hamburguesa_Completa.fbx,
        SM_Tabla_Corte.fbx, SM_Campana_Extraccion.fbx, SM_Repisa_Frascos.fbx, SM_Lampara_Colgante.fbx,
        SM_Mostrador_Modulo.fbx
    ArtSource/CasaMarea/Preview/Interior_lineup_frontal.png, Interior_lineup_tres_cuartos.png
    ArtSource/CasaMarea/stats_interior.json

Convenciones: 1 u = 1 m, Z arriba, frente hacia -Y, origen en el centro de la huella con la base en z = 0.
Excepciones de pivote (documentadas en stats_interior.json): SM_Campana_Extraccion (borde inferior de la campana, se
coloca en z = 2.1) y SM_Lampara_Colgante (punto de anclaje al techo, el modelo cuelga hacia z negativa).
"""
import math
import os
import sys
import json

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_props_barrio as P  # noqa: E402  (mismos ayudantes, materiales y utilidades de render)
from build_props_barrio import (box, cyl, cone, sphere, prism, finalize, stat, export_fbx)  # noqa: E402

bpy = P.bpy

P.MATS.update({
    "M_Pan":          ("#D99A4E", 0.60, 0.00, None, 0.0),
    "M_Carne":        ("#5A3524", 0.70, 0.00, None, 0.0),
    "M_Queso":        ("#F2B63A", 0.45, 0.00, None, 0.0),
    "M_Lechuga":      ("#6FA83F", 0.60, 0.00, None, 0.0),
    "M_Jitomate":     ("#C0261F", 0.45, 0.00, None, 0.0),
    "M_Cebolla":      ("#8E4A78", 0.50, 0.00, None, 0.0),
    "M_Madera_Clara": ("#B88556", 0.70, 0.00, None, 0.0),
    "M_Acero_Claro":  ("#8E9AA0", 0.40, 0.45, None, 0.0),
    "M_Metal_Plancha": ("#3E4A50", 0.50, 0.25, None, 0.0),
})

BUDGET = {"SM_Refrigerador": 200, "SM_Hamburguesa_Completa": 400, "SM_Tabla_Corte": 150,
          "SM_Campana_Extraccion": 150, "SM_Repisa_Frascos": 450, "SM_Lampara_Colgante": 250,
          "SM_Mostrador_Modulo": 200}
PIECES_BUDGET = 400


def build_refrigerador():
    """Refrigerador crema con manijas chile y puerta de congelador (0.80 x 0.70 x 1.75)."""
    F = -0.29                                                   # cara frontal del cuerpo
    box("cuerpo", (0.80, 0.64, 1.69), (0, F + 0.32, 0.905), "M_Crema")
    box("rodapie", (0.74, 0.58, 0.06), (0, F + 0.32, 0.03), "M_Herreria")
    box("puerta_baja", (0.76, 0.03, 1.10), (0, F - 0.015, 0.63), "M_Crema")
    box("puerta_alta", (0.76, 0.03, 0.51), (0, F - 0.015, 1.475), "M_Crema")
    box("junta", (0.78, 0.02, 0.02), (0, F - 0.02, 1.20), "M_Herreria")
    box("manija_baja", (0.03, 0.03, 0.30), (0.31, F - 0.045, 1.00), "M_Chile")
    box("manija_alta", (0.03, 0.03, 0.15), (0.31, F - 0.045, 1.42), "M_Chile")
    box("emblema", (0.14, 0.012, 0.05), (-0.18, F - 0.036, 1.60), "M_Turquesa")
    return finalize("SM_Refrigerador")


# pieza: (altura, función constructora relativa a z0)
def _piece(kind, z0):
    if kind == "pan_base":
        cyl("pan_base", 0.080, 0.025, (0, 0, z0 + 0.0125), "M_Pan", verts=12)
        return z0 + 0.025
    if kind == "carne":
        cyl("carne", 0.078, 0.030, (0, 0, z0 + 0.015), "M_Carne", verts=12)
        return z0 + 0.030
    if kind == "queso":
        box("queso", (0.115, 0.115, 0.006), (0, 0, z0 + 0.003), "M_Queso", (0, 0, math.radians(45)))
        return z0 + 0.006
    if kind == "lechuga":
        cyl("lechuga", 0.090, 0.010, (0, 0, z0 + 0.005), "M_Lechuga", verts=12)
        return z0 + 0.010
    if kind == "jitomate":
        for k, dx in enumerate((-0.03, 0.03)):
            cyl("jitomate_%d" % k, 0.045, 0.010, (dx, 0, z0 + 0.005), "M_Jitomate", verts=8)
        return z0 + 0.010
    if kind == "cebolla":
        cyl("cebolla", 0.050, 0.006, (0, 0, z0 + 0.003), "M_Cebolla", verts=8)
        return z0 + 0.006
    if kind == "pan_tapa":
        cone("tapa_1", 0.085, 0.078, 0.015, (0, 0, z0 + 0.0075), "M_Pan", verts=12)
        cone("tapa_2", 0.078, 0.055, 0.030, (0, 0, z0 + 0.030), "M_Pan", verts=12)
        cone("tapa_3", 0.055, 0.020, 0.025, (0, 0, z0 + 0.0575), "M_Pan", verts=12)
        return z0 + 0.070
    raise ValueError(kind)


STACK = ["pan_base", "carne", "queso", "lechuga", "jitomate", "cebolla", "pan_tapa"]


def build_hamburguesa_piezas():
    """Siete piezas sueltas, cada una con su pivote en el centro de su base (z = 0), para apilarlas por código."""
    objs, alturas = [], {}
    for kind in STACK:
        top = _piece(kind, 0.0)
        o = finalize("SM_Burger_" + kind)
        objs.append(o)
        alturas[kind] = round(top, 4)
    return objs, alturas


def build_hamburguesa_completa():
    """La misma hamburguesa ya armada, en una sola malla (0.18 x 0.18 x 0.157), para el estado «lista para vender»."""
    z = 0.0
    for kind in STACK:
        z = _piece(kind, z)
    return finalize("SM_Hamburguesa_Completa")


def build_tabla():
    """Tabla de corte de madera clara con un cuchillo encima (0.40 x 0.28 x ≈0.05)."""
    for sx in (-0.15, 0.15):
        for sy in (-0.09, 0.09):
            box("pie", (0.03, 0.03, 0.01), (sx, sy, 0.005), "M_Herreria")
    box("tabla", (0.40, 0.28, 0.03), (0, 0, 0.025), "M_Madera_Clara")
    ang = math.radians(20)
    box("hoja", (0.18, 0.004, 0.035), (-0.02, 0.0, 0.0415), "M_Acero_Claro", (0, 0, ang))
    box("mango", (0.08, 0.014, 0.02), (0.100, 0.034, 0.0475), "M_Caoba", (0, 0, ang))
    return finalize("SM_Tabla_Corte")


def build_campana():
    """Campana de extracción de acero con ducto al techo (1.50 x 0.90 x 0.955).
    Pivote: centro del borde inferior de la campana; se coloca a z = 2.10 (el ducto llega a 3.05)."""
    prism("campana", [(-0.45, 0.0), (0.45, 0.0), (0.18, 0.50), (-0.18, 0.50)], 1.50, (0, 0, 0), "M_Acero_Claro", "YZ")
    box("campana_borde", (1.52, 0.90, 0.04), (0, 0, 0.02), "M_Acero_Claro")
    box("campana_banda", (1.50, 0.03, 0.03), (0, -0.40, 0.10), "M_Chile")
    box("ducto", (0.30, 0.30, 0.455), (0, 0, 0.7275), "M_Metal_Plancha")
    return finalize("SM_Campana_Extraccion")


def build_repisa():
    """Dos repisas de madera con ménsulas, dos botellas de salsa y cuatro frascos (1.00 x 0.25 x 0.74).
    Pivote: centro de la huella, base en z = 0 (extremo inferior de las ménsulas bajas)."""
    zb = (0.10, 0.50)                                                         # cara inferior de cada tabla
    for z in zb:
        box("tabla", (1.0, 0.25, 0.04), (0, 0, z + 0.02), "M_Caoba")
        for sx in (-0.40, 0.40):
            box("mensula", (0.04, 0.20, 0.10), (sx, 0.0, z - 0.05), "M_Herreria")
    for k, x in enumerate((-0.30, -0.20)):                                    # botellas de salsa (tabla alta)
        cyl("botella_%d" % k, 0.030, 0.140, (x, 0.0, 0.54 + 0.07), "M_Chile", verts=8)
        cyl("botella_cuello_%d" % k, 0.012, 0.060, (x, 0.0, 0.68 + 0.03), "M_Crema", verts=8)
    for k, x in enumerate((-0.15, 0.0, 0.15, 0.30)):                          # frascos (tabla baja)
        cyl("frasco_%d" % k, 0.040, 0.100, (x, 0.0, 0.14 + 0.05), "M_Turquesa", verts=8)
        cyl("frasco_tapa_%d" % k, 0.042, 0.020, (x, 0.0, 0.24 + 0.01), "M_Crema", verts=8)
    return finalize("SM_Repisa_Frascos")


def build_lampara():
    """Lámpara colgante de herrería con bombilla mango (0.28 x 0.28 x 0.635). Pivote en el techo; cuelga hacia -Z.
    La bombilla usa M_Mango con emisión apagada: Unity la enciende."""
    cyl("roseton", 0.05, 0.02, (0, 0, -0.01), "M_Herreria", verts=8)
    cyl("cable", 0.010, 0.48, (0, 0, -0.26), "M_Herreria", verts=8)
    cone("pantalla", 0.14, 0.04, 0.12, (0, 0, -0.56), "M_Herreria", verts=8)
    sphere("bombilla", 0.055, (0, 0, -0.58), "M_Mango", seg=8, rings=6)
    return finalize("SM_Lampara_Colgante")


def build_mostrador():
    """Módulo de mostrador de 1 m: zócalo de herrería, cuerpo de caliza, tapa de caoba y panel turquesa enmarcado
    (1.00 x 0.75 x 0.85). Se repite en X; para cubrir 9.8 m, escalar X a 0.98 en cada uno de los 10 módulos."""
    box("zocalo", (1.0, 0.68, 0.06), (0, 0, 0.03), "M_Herreria")
    box("cuerpo", (1.0, 0.70, 0.74), (0, 0, 0.43), "M_Piedra_Caliza")
    box("tapa", (1.0, 0.75, 0.05), (0, 0, 0.825), "M_Caoba")
    box("panel_marco", (0.88, 0.012, 0.62), (0, -0.356, 0.43), "M_Crema")
    box("panel", (0.80, 0.012, 0.54), (0, -0.364, 0.43), "M_Turquesa")
    return finalize("SM_Mostrador_Modulo")


def build_all():
    out = {}
    out["SM_Refrigerador"] = [build_refrigerador()]
    piezas, alturas = build_hamburguesa_piezas()
    out["SM_Hamburguesa_Piezas"] = piezas
    out["SM_Hamburguesa_Completa"] = [build_hamburguesa_completa()]
    out["SM_Tabla_Corte"] = [build_tabla()]
    out["SM_Campana_Extraccion"] = [build_campana()]
    out["SM_Repisa_Frascos"] = [build_repisa()]
    out["SM_Lampara_Colgante"] = [build_lampara()]
    out["SM_Mostrador_Modulo"] = [build_mostrador()]
    return out, alturas


PIVOT_EXCEPTIONS = {"SM_Campana_Extraccion": "borde inferior de la campana; colocar a z = 2.10",
                    "SM_Lampara_Colgante": "anclaje al techo; el modelo cuelga hacia z negativa"}

LINEUP = {"SM_Refrigerador": (-2.6, 0.0), "SM_Mostrador_Modulo": (-1.4, 0.0), "SM_Tabla_Corte": (-0.5, 0.85),
          "SM_Hamburguesa_Completa": (0.0, 0.85), "SM_Repisa_Frascos": (0.9, 0.9),
          "SM_Campana_Extraccion": (2.3, 1.7), "SM_Lampara_Colgante": (3.5, 2.4)}


def main():
    import numpy as np  # noqa: F401
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0
    groups, alturas = build_all()

    stats = {}
    for name, objs in groups.items():
        if name == "SM_Hamburguesa_Piezas":
            stats[name] = {"piezas": {o.name: stat(o) for o in objs}, "alturas_apilado_m": alturas,
                           "orden_de_apilado": ["SM_Burger_" + k for k in STACK],
                           "triangulos_total": sum(stat(o)["triangulos"] for o in objs),
                           "presupuesto_triangulos": PIECES_BUDGET}
            stats[name]["dentro_de_presupuesto"] = stats[name]["triangulos_total"] <= PIECES_BUDGET
        else:
            stats[name] = stat(objs[0], BUDGET[name])
            if name in PIVOT_EXCEPTIONS:
                stats[name]["pivote"]["excepcion"] = PIVOT_EXCEPTIONS[name]
        stats[name]["fbx"] = os.path.relpath(export_fbx(objs, name), P.PROJECT)       # en el origen
    stats["paleta"] = {k: v[0] for k, v in P.MATS.items()}
    with open(os.path.join(HERE, "stats_interior.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)

    # lineup: ahora sí se desplazan (los FBX ya están escritos con cada modelo en el origen)
    for name, (x, z) in LINEUP.items():
        for o in groups[name]:
            o.location.x, o.location.z = x, z
    for i, o in enumerate(groups["SM_Hamburguesa_Piezas"]):                  # piezas sueltas, en el suelo
        o.location.x, o.location.y = 4.6 + 0.22 * i, -0.5
    xc = 1.65
    P.ctx_box("Suelo", (11, 5, 0.1), (xc, 0, -0.05), "#B8A78C")
    P.add_world(scene)
    P.add_lights()
    P.setup_render(scene, 1920, 1080)
    P.render_to(scene, P.add_camera("Cam_frontal", (xc, -13.0, 1.3), (xc, 0, 1.2), 32),
                os.path.join(P.PREVIEW_DIR, "Interior_lineup_frontal.png"))
    P.render_to(scene, P.add_camera("Cam_tres_cuartos", (xc - 7.0, -10.5, 3.6), (xc, 0, 1.0), 36),
                os.path.join(P.PREVIEW_DIR, "Interior_lineup_tres_cuartos.png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Interior_Cocina.blend"))
    for k, v in stats.items():
        if k.startswith("SM_"):
            t = v.get("triangulos", v.get("triangulos_total"))
            print("[%s] tris=%s tope=%s dims=%s" % (k, t, v.get("presupuesto_triangulos"), v.get("dimensiones_XxYxZ_m")))
    print("LISTO")


if __name__ == "__main__":
    main()
