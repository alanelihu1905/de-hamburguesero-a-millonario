"""Genera la tanda 3 de props del barrio Casa Marea (variaciones y piezas de ambiente) en Blender.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_props_barrio_2.py

Modelos: banco de callejón (sin respaldo), caja de pescado y pila de cajas, bolardo de caliza, barril, farol de pared y
maceta pequeña de patio con agave. Solo usa colores de la paleta ya existente. Reutiliza los ayudantes de
build_props_barrio.py.

Salidas:
    ArtSource/CasaMarea/Props_Barrio_2.blend
    Assets/_Game/Models/CasaMarea/SM_Banco_Callejon.fbx, SM_Caja_Pescado.fbx, SM_Cajas_Pescado_Pila.fbx,
        SM_Bolardo.fbx, SM_Barril.fbx, SM_Farol_Pared.fbx, SM_Maceta_Patio.fbx
    ArtSource/CasaMarea/Preview/Props2_lineup_frontal.png, Props2_lineup_tres_cuartos.png
    ArtSource/CasaMarea/stats_props_2.json

Convenciones: 1 u = 1 m, Z arriba, frente hacia -Y, origen en el centro de la huella con la base en z = 0.
Excepción: SM_Farol_Pared tiene el origen en el centro de la placa trasera, sobre el plano del muro (y = 0); el farol
sobresale hacia -Y, es decir, hacia la calle.
"""
import math
import os
import sys
import json

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_props_barrio as P  # noqa: E402
from build_props_barrio import (box, cyl, cone, prism, finalize, stat, export_fbx)  # noqa: E402

bpy = P.bpy

BUDGET = {"SM_Banco_Callejon": 120, "SM_Caja_Pescado": 250, "SM_Cajas_Pescado_Pila": 450, "SM_Bolardo": 200,
          "SM_Barril": 350, "SM_Farol_Pared": 200, "SM_Maceta_Patio": 300}


def build_banco_callejon():
    """Banco de callejón (el del documento 04: 1.40 x 0.45 x 0.40): tres tablones sobre dos patas de caliza."""
    for k, y in enumerate((-0.15, 0.0, 0.15)):
        box("asiento_%d" % k, (1.40, 0.15, 0.04), (0, y, 0.38), "M_Caoba")
    for sx in (-0.55, 0.55):
        box("pata", (0.12, 0.40, 0.36), (sx, 0.0, 0.18), "M_Piedra_Caliza")
        box("pata_azulejo", (0.10, 0.012, 0.20), (sx, -0.206, 0.20), "M_Turquesa")
    return finalize("SM_Banco_Callejon")


def _caja(z0, dx=0.0, peces=2):
    """Una caja de pescado de 0.50 x 0.30 x 0.17 sobre z0 (apilable: el fondo encaja en el borde de la de abajo)."""
    box("caja_fondo", (0.50, 0.30, 0.02), (dx, 0, z0 + 0.01), "M_Turquesa")
    for sx in (-0.235, 0.235):
        for sy in (-0.135, 0.135):
            box("caja_poste", (0.03, 0.03, 0.17), (dx + sx, sy, z0 + 0.085), "M_Crema")
    for z in (0.06, 0.13):
        for sy in (-0.144, 0.144):
            box("caja_tabla_larga", (0.50, 0.012, 0.04), (dx, sy, z0 + z), "M_Turquesa")
        for sx in (-0.244, 0.244):
            box("caja_tabla_corta", (0.012, 0.30, 0.04), (dx + sx, 0, z0 + z), "M_Turquesa")
    cuerpo = [(-0.10, 0), (-0.06, 0.045), (0.05, 0.045), (0.10, 0), (0.05, -0.045), (-0.06, -0.045)]
    for k in range(peces):
        yaw = math.radians(15 - 35 * k)
        px, py = dx - 0.05 + 0.10 * k, -0.05 + 0.10 * k
        prism("pez_cuerpo", cuerpo, 0.02, (px, py, z0 + 0.04), "M_Chile", "XZ", (math.pi / 2, 0, yaw))
        prism("pez_cola", [(-0.09, 0), (-0.16, 0.05), (-0.16, -0.05)], 0.02, (px, py, z0 + 0.04), "M_Chile", "XZ",
              (math.pi / 2, 0, yaw))


def build_caja_pescado():
    """Caja de pescado con dos huachinangos (0.50 x 0.30 x 0.17)."""
    _caja(0.0)
    return finalize("SM_Caja_Pescado")


def build_cajas_pila():
    """Dos cajas apiladas, la de arriba desplazada 4 cm y con un solo pez (0.54 x 0.30 x 0.34)."""
    _caja(0.0, 0.0, 2)
    _caja(0.17, 0.04, 1)
    return finalize("SM_Cajas_Pescado_Pila")


def build_bolardo():
    """Bolardo de caliza con banda turquesa y remate abovedado (0.30 x 0.30 x 0.68)."""
    cyl("base", 0.15, 0.06, (0, 0, 0.03), "M_Piedra_Caliza", verts=12)
    cone("cuerpo", 0.13, 0.11, 0.46, (0, 0, 0.29), "M_Piedra_Caliza", verts=12)
    cyl("banda", 0.115, 0.05, (0, 0, 0.42), "M_Turquesa", verts=12)
    cone("remate", 0.11, 0.04, 0.12, (0, 0, 0.62), "M_Piedra_Caliza", verts=12)
    return finalize("SM_Bolardo")


def build_barril():
    """Barril de madera con tres aros de herrería (0.54 x 0.54 x 0.70), tapa superior visible."""
    cone("panza_baja", 0.22, 0.26, 0.22, (0, 0, 0.11), "M_Caoba", verts=12)
    cyl("panza_media", 0.26, 0.26, (0, 0, 0.35), "M_Caoba", verts=12)
    cone("panza_alta", 0.26, 0.22, 0.22, (0, 0, 0.59), "M_Caoba", verts=12)
    cyl("tapa", 0.215, 0.02, (0, 0, 0.69), "M_Caoba", verts=12)
    for z, r in ((0.08, 0.235), (0.35, 0.266), (0.62, 0.235)):
        cyl("aro", r, 0.03, (0, 0, z), "M_Herreria", verts=12)
    return finalize("SM_Barril")


def build_farol_pared():
    """Farol de pared: placa, brazo y linterna con cristal mango (0.28 x 0.34 x 0.55).
    Origen en el centro de la placa trasera, sobre el muro; sobresale hacia -Y. M_Mango con emisión apagada."""
    box("placa", (0.12, 0.02, 0.30), (0, -0.01, 0.15), "M_Herreria")
    box("brazo", (0.04, 0.22, 0.04), (0, -0.11, 0.32), "M_Herreria")
    box("linterna_base", (0.18, 0.18, 0.03), (0, -0.20, 0.215), "M_Herreria")
    cyl("linterna_cristal", 0.10, 0.20, (0, -0.20, 0.33), "M_Mango", verts=8)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        box("linterna_costilla", (0.02, 0.02, 0.21), (0.10 * math.cos(a), -0.20 + 0.10 * math.sin(a), 0.33),
            "M_Herreria", (0, 0, a))
    cone("linterna_techo", 0.14, 0.02, 0.12, (0, -0.20, 0.49), "M_Herreria", verts=8)
    return finalize("SM_Farol_Pared")


def build_maceta_patio():
    """Maceta pequeña de barro con un agave de hojas puntiagudas (≈ 0.40 x 0.40 x 0.62)."""
    cone("maceta", 0.11, 0.15, 0.24, (0, 0, 0.12), "M_Barro", verts=12)
    cyl("maceta_borde", 0.16, 0.04, (0, 0, 0.26), "M_Barro", verts=12)
    cyl("tierra", 0.14, 0.02, (0, 0, 0.275), "M_Caoba", verts=12)
    base_z = 0.285
    # (azimut°, inclinación°, largo, ancho, material): hojas erguidas al centro y arqueadas hacia afuera
    hojas = [(0, 12, 0.34, 0.07, "M_Vegetacion"), (90, 14, 0.32, 0.07, "M_Hoja_Clara"),
             (180, 12, 0.34, 0.07, "M_Vegetacion"), (270, 14, 0.32, 0.07, "M_Hoja_Clara"),
             (45, 38, 0.30, 0.075, "M_Hoja_Clara"), (135, 40, 0.28, 0.075, "M_Vegetacion"),
             (225, 38, 0.30, 0.075, "M_Hoja_Clara"), (315, 40, 0.28, 0.075, "M_Vegetacion")]
    hojas += [(22.5 + 45 * k, 62 + 2 * (k % 2), 0.24, 0.07, "M_Vegetacion" if k % 2 == 0 else "M_Hoja_Clara")
              for k in range(8)]                                              # anillo exterior arqueado
    for i, (az, inc, L, W, mat) in enumerate(hojas):
        pts = [(-W / 2, 0), (W / 2, 0), (0, L)]
        prism("hoja_%d" % i, pts, 0.014, (0, 0, base_z), mat, "XZ", (math.radians(inc), 0, math.radians(az)))
    return finalize("SM_Maceta_Patio")


def build_all():
    out = {}
    out["SM_Banco_Callejon"] = [build_banco_callejon()]
    out["SM_Caja_Pescado"] = [build_caja_pescado()]
    out["SM_Cajas_Pescado_Pila"] = [build_cajas_pila()]
    out["SM_Bolardo"] = [build_bolardo()]
    out["SM_Barril"] = [build_barril()]
    out["SM_Farol_Pared"] = [build_farol_pared()]
    out["SM_Maceta_Patio"] = [build_maceta_patio()]
    return out


PIVOT_EXCEPTIONS = {"SM_Farol_Pared": "centro de la placa trasera sobre el muro (y = 0); sobresale hacia -Y"}
LINEUP_X = {"SM_Banco_Callejon": -2.3, "SM_Caja_Pescado": -1.0, "SM_Cajas_Pescado_Pila": -0.2, "SM_Bolardo": 0.6,
            "SM_Barril": 1.3, "SM_Farol_Pared": 2.1, "SM_Maceta_Patio": 2.9}
LINEUP_Z = {"SM_Farol_Pared": 1.2}


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0
    groups = build_all()

    stats = {}
    for name, objs in groups.items():
        stats[name] = stat(objs[0], BUDGET[name])
        if name in PIVOT_EXCEPTIONS:
            stats[name]["pivote"]["excepcion"] = PIVOT_EXCEPTIONS[name]
        stats[name]["fbx"] = os.path.relpath(export_fbx(objs, name), P.PROJECT)       # en el origen
    stats["paleta"] = {k: v[0] for k, v in P.MATS.items()}
    with open(os.path.join(HERE, "stats_props_2.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)

    for name, objs in groups.items():
        objs[0].location.x = LINEUP_X[name]
        objs[0].location.z = LINEUP_Z.get(name, 0.0)
    xc = 0.3
    P.ctx_box("Suelo", (9, 4, 0.1), (xc, 0, -0.05), "#B8A78C")
    P.ctx_box("Muro_Referencia", (9, 0.2, 2.4), (xc, 0.4, 1.2), "#E8C4A0")
    P.add_world(scene)
    P.add_lights()
    P.setup_render(scene, 1920, 1080)
    P.render_to(scene, P.add_camera("Cam_frontal", (xc, -8.0, 1.0), (xc, 0, 0.9), 30),
                os.path.join(P.PREVIEW_DIR, "Props2_lineup_frontal.png"))
    P.render_to(scene, P.add_camera("Cam_tres_cuartos", (xc - 4.5, -6.5, 2.4), (xc, 0, 0.7), 34),
                os.path.join(P.PREVIEW_DIR, "Props2_lineup_tres_cuartos.png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Props_Barrio_2.blend"))
    for k, v in stats.items():
        if k.startswith("SM_"):
            print("[%s] tris=%d tope=%d dims=%s" % (k, v["triangulos"], v["presupuesto_triangulos"],
                                                    v["dimensiones_XxYxZ_m"]))
    print("LISTO")


if __name__ == "__main__":
    main()
