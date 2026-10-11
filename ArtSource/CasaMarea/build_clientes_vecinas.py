"""Genera los clientes y las casas vecinas del primer barrio de Casa Marea (tanda 4) en Blender.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_clientes_vecinas.py

Modelos:
  - SM_Cliente_Pescador, SM_Cliente_Vecina, SM_Cliente_Joven: tres clientes estáticos, ficticios y caricaturescos, de
    1.72 a 1.80 m (con sombrero o gorra), de pie y mirando a -Y. Sin esqueleto ni animación (eso es una tanda aparte).
  - SM_Casa_Vecina_A, SM_Casa_Vecina_B: dos variantes de la casa vecina que hoy es una caja de 6 x 6 x 2.8 m a ambos
    lados de la casa (x = ±12). Sólido por dentro; fachada al estilo Casa Marea.
Reutiliza los ayudantes de build_props_barrio.py. Colores: los de la paleta y tres tonos de piel nuevos.

Salidas:
    ArtSource/CasaMarea/Clientes_Vecinas.blend
    Assets/_Game/Models/CasaMarea/SM_Cliente_Pescador.fbx, SM_Cliente_Vecina.fbx, SM_Cliente_Joven.fbx,
        SM_Casa_Vecina_A.fbx, SM_Casa_Vecina_B.fbx
    ArtSource/CasaMarea/Preview/Clientes_vecinas_frontal.png, Clientes_vecinas_tres_cuartos.png,
        Clientes_silueta_3m.png
    ArtSource/CasaMarea/stats_clientes_vecinas.json

Convenciones: 1 u = 1 m, Z arriba, frente hacia -Y, origen en el centro de la huella con la base en z = 0.
Las casas vecinas se colocan con su origen en el centro de su caja actual (x = ±12, y = 1 en la escena).
"""
import math
import os
import sys
import json

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_props_barrio as P  # noqa: E402
from build_props_barrio import (box, cyl, cone, sphere, finalize, stat, export_fbx)  # noqa: E402

bpy = P.bpy

P.MATS.update({
    "M_Piel_Clara":   ("#E3B58D", 0.75, 0.00, None, 0.0),
    "M_Piel_Media":   ("#C98B5E", 0.75, 0.00, None, 0.0),
    "M_Piel_Oscura":  ("#6B4530", 0.75, 0.00, None, 0.0),
    "M_Cal_Arena":    ("#E8C4A0", 0.85, 0.00, None, 0.0),
    "M_Madera_Clara": ("#B88556", 0.70, 0.00, None, 0.0),
    "M_Acero_Claro":  ("#8E9AA0", 0.40, 0.45, None, 0.0),
    "M_Pizarra":      ("#3B2F4A", 0.80, 0.00, None, 0.0),
})

BUDGET = {"SM_Cliente_Pescador": 700, "SM_Cliente_Vecina": 700, "SM_Cliente_Joven": 700,
          "SM_Casa_Vecina_A": 600, "SM_Casa_Vecina_B": 600}


# ---------------------------------------------------------------- clientes
def _cuerpo(piel, pantalon, camisa, zapato="M_Herreria", vestido=False, falda=None):
    """Cuerpo común: piernas, zapatos, torso, brazos con manga, manos, cuello y cabeza con ojos (1.70 m en total)."""
    cuerpo_inferior = pantalon if not vestido else None
    for sx in (-0.09, 0.09):
        if not vestido:
            box("pierna", (0.12, 0.14, 0.76), (sx, 0.0, 0.46), pantalon)
        else:
            box("pierna", (0.10, 0.12, 0.60), (sx, 0.0, 0.30 + 0.10), piel)
        box("zapato", (0.13, 0.25, 0.08), (sx, -0.045, 0.04), zapato)
    if vestido:
        cone("vestido", 0.27, 0.17, 0.80, (0, 0, 0.95), falda, verts=12)         # de 0.55 a 1.35
    else:
        box("torso", (0.40, 0.22, 0.55), (0, 0, 1.075), camisa)
    th = math.radians(8)                                         # brazo en cadena: hombro -> manga -> antebrazo -> mano
    for sx in (-1, 1):
        hombro = (sx * 0.24, 0.0, 1.34)
        dx, dz = sx * math.sin(th), -math.cos(th)

        def en(t, hombro=hombro, dx=dx, dz=dz):
            return (hombro[0] + dx * t, 0.0, hombro[2] + dz * t)

        phi = -sx * th
        box("manga", (0.11, 0.11, 0.27), en(0.135), camisa, (0, phi, 0))
        box("antebrazo", (0.085, 0.085, 0.29), en(0.415), piel, (0, phi, 0))
        sphere("mano", 0.05, en(0.59), piel, seg=6, rings=4)
    cyl("cuello", 0.05, 0.08, (0, 0, 1.39), piel, verts=8)
    sphere("cabeza", 0.15, (0, 0, 1.55), piel, (1, 1, 1), 8, 6)
    for sx in (-0.055, 0.055):
        box("ojo", (0.03, 0.012, 0.035), (sx, -0.146, 1.57), "M_Herreria")
    return cuerpo_inferior


def build_pescador():
    """Pescador: camisa turquesa, pantalón de pizarra, sombrero de paja y piel media."""
    _cuerpo("M_Piel_Media", "M_Pizarra", "M_Turquesa")
    cone("sombrero_ala", 0.27, 0.19, 0.04, (0, 0, 1.67), "M_Madera_Clara", verts=12)
    cyl("sombrero_copa", 0.14, 0.11, (0, 0, 1.745), "M_Madera_Clara", verts=8)
    box("sombrero_cinta", (0.29, 0.29, 0.025), (0, 0, 1.705), "M_Chile")
    return finalize("SM_Cliente_Pescador")


def build_vecina():
    """Vecina: vestido chile, cabello gris con chongo, piel clara."""
    _cuerpo("M_Piel_Clara", None, "M_Chile", vestido=True, falda="M_Chile")
    sphere("cabello", 0.16, (0, 0.012, 1.60), "M_Acero_Claro", (1, 1.05, 0.75), 8, 4)
    sphere("chongo", 0.07, (0, 0.13, 1.68), "M_Acero_Claro", seg=6, rings=4)
    box("delantal", (0.30, 0.012, 0.36), (0, -0.115, 0.95), "M_Crema")
    return finalize("SM_Cliente_Vecina")


def build_joven():
    """Joven: camiseta crema, shorts de caliza, gorra chile y piel oscura."""
    _cuerpo("M_Piel_Oscura", "M_Piedra_Caliza", "M_Crema")
    sphere("gorra", 0.16, (0, 0.0, 1.60), "M_Chile", (1, 1.05, 0.75), 8, 4)
    box("gorra_visera", (0.24, 0.14, 0.02), (0, -0.20, 1.60), "M_Chile")
    return finalize("SM_Cliente_Joven")


# ---------------------------------------------------------------- casas vecinas
def _casa(nombre, muro, zocalo, postigo, puerta_x, ventanas_x, cerrados):
    """Casa vecina de 6.0 x 6.0 x 3.45 m con fachada hacia -Y (cuerpo sólido)."""
    Y = -3.0
    box("cuerpo", (6.0, 6.0, 3.0), (0, 0, 1.5), muro)
    box("zocalo", (6.0, 0.04, 0.9), (0, Y - 0.02, 0.45), zocalo)
    for sx in (-2.85, 2.85):
        box("pilastra", (0.3, 0.10, 2.9), (sx, Y - 0.05, 1.45), "M_Crema")
    box("cornisa", (6.2, 0.2, 0.2), (0, Y, 3.0), "M_Crema")
    box("pretil_frente", (6.2, 0.2, 0.45), (0, Y, 3.225), "M_Crema")
    box("pretil_fondo", (6.2, 0.2, 0.45), (0, 3.0, 3.225), "M_Crema")
    for sx in (-3.0, 3.0):
        box("pretil_lado", (0.2, 5.8, 0.45), (sx, 0.0, 3.225), "M_Crema")
    # puerta cerrada de caoba con marco crema y dos paneles
    box("puerta_marco", (1.30, 0.08, 2.40), (puerta_x, Y - 0.04, 1.20), "M_Crema")
    box("puerta", (1.10, 0.06, 2.25), (puerta_x, Y - 0.07, 1.125), "M_Caoba")
    for z in (0.65, 1.65):
        box("puerta_panel", (0.80, 0.02, 0.75 if z < 1 else 0.70), (puerta_x, Y - 0.11, z), "M_Caoba")
    box("puerta_aldaba", (0.06, 0.04, 0.06), (puerta_x + 0.40, Y - 0.12, 1.05), "M_Herreria")
    # ventanas: marco crema + vidrio oscuro + reja o postigos cerrados
    for x in ventanas_x:
        box("ventana_vidrio", (1.0, 0.02, 1.2), (x, Y - 0.01, 1.60), "M_Herreria")
        box("ventana_marco_alto", (1.2, 0.06, 0.10), (x, Y - 0.03, 2.25), "M_Crema")
        box("ventana_alfeizar", (1.3, 0.10, 0.06), (x, Y - 0.05, 0.97), "M_Crema")
        for sx in (-0.55, 0.55):
            box("ventana_marco_lado", (0.10, 0.06, 1.30), (x + sx, Y - 0.03, 1.60), "M_Crema")
        if cerrados:
            for sx in (-0.25, 0.25):
                box("postigo", (0.50, 0.04, 1.20), (x + sx, Y - 0.04, 1.60), postigo)
        else:
            for k in range(6):
                box("reja_barra", (0.025, 0.025, 1.20), (x - 0.4 + k * 0.16, Y - 0.04, 1.60), "M_Herreria")
    return finalize(nombre)


def build_casa_a():
    """Variante A: arena con zócalo turquesa, puerta al centro, dos ventanas con postigos chile cerrados."""
    return _casa("SM_Casa_Vecina_A", "M_Cal_Arena", "M_Turquesa", "M_Chile", 0.0, (-1.9, 1.9), True)


def build_casa_b():
    """Variante B: caliza con zócalo chile, puerta a la izquierda, dos ventanas con reja a la derecha."""
    return _casa("SM_Casa_Vecina_B", "M_Piedra_Caliza", "M_Chile", "M_Turquesa", -1.7, (0.3, 2.0), False)


def build_all():
    return {"SM_Cliente_Pescador": [build_pescador()], "SM_Cliente_Vecina": [build_vecina()],
            "SM_Cliente_Joven": [build_joven()], "SM_Casa_Vecina_A": [build_casa_a()],
            "SM_Casa_Vecina_B": [build_casa_b()]}


LINEUP = {"SM_Cliente_Pescador": (-2.0, 0.0), "SM_Cliente_Vecina": (-1.0, 0.0), "SM_Cliente_Joven": (0.0, 0.0),
          "SM_Casa_Vecina_A": (-1.0, 8.0), "SM_Casa_Vecina_B": (6.5, 8.0)}


def main():
    import numpy as np
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0
    groups = build_all()

    stats = {}
    for name, objs in groups.items():
        stats[name] = stat(objs[0], BUDGET[name])
        stats[name]["fbx"] = os.path.relpath(export_fbx(objs, name), P.PROJECT)       # en el origen
    stats["paleta"] = {k: v[0] for k, v in P.MATS.items()}
    with open(os.path.join(HERE, "stats_clientes_vecinas.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)

    for name, (x, y) in LINEUP.items():
        groups[name][0].location.x, groups[name][0].location.y = x, y
    P.ctx_box("Calle", (22, 22, 0.1), (2.0, 4.0, -0.05), "#B8A78C")
    P.add_world(scene)
    P.add_lights()
    P.setup_render(scene, 1920, 1080)
    P.render_to(scene, P.add_camera("Cam_frontal", (2.2, -14.0, 1.8), (2.2, 0, 1.6), 38),
                os.path.join(P.PREVIEW_DIR, "Clientes_vecinas_frontal.png"))
    P.render_to(scene, P.add_camera("Cam_tres_cuartos", (-6.0, -12.0, 4.0), (2.2, 3.0, 1.4), 40),
                os.path.join(P.PREVIEW_DIR, "Clientes_vecinas_tres_cuartos.png"))
    # silueta a 3 m de los tres clientes, con la cámara del jugador (ojos a 1.65 m, FOV 60°)
    for n in ("SM_Casa_Vecina_A", "SM_Casa_Vecina_B"):
        groups[n][0].hide_render = True
    for o in bpy.data.objects:
        if o.name.startswith("Preview_"):
            o.hide_render = True
    scene.render.film_transparent = True
    tmp = os.path.join(P.PREVIEW_DIR, "_tmp_sil.png")
    P.render_to(scene, P.add_camera("Cam_3m", (-1.0, -3.0, 1.65), (-1.0, 0, 1.2), 60), tmp)
    img = bpy.data.images.load(tmp)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    mask = (np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[..., 3] > 0.5).astype(np.float32)
    out = bpy.data.images.new("Clientes_silueta_3m.png", w, h, alpha=False)
    out.colorspace_settings.name = "Non-Color"
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., :3] = (1.0 - mask)[..., None]
    out.pixels = rgba.ravel().tolist()
    out.filepath_raw = os.path.join(P.PREVIEW_DIR, "Clientes_silueta_3m.png")
    out.file_format = "PNG"
    out.save()
    os.remove(tmp)
    for n in ("SM_Casa_Vecina_A", "SM_Casa_Vecina_B"):
        groups[n][0].hide_render = False
    for o in bpy.data.objects:
        if o.name.startswith("Preview_"):
            o.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Clientes_Vecinas.blend"))
    for k, v in stats.items():
        if k.startswith("SM_"):
            print("[%s] tris=%d tope=%d dims=%s" % (k, v["triangulos"], v["presupuesto_triangulos"],
                                                    v["dimensiones_XxYxZ_m"]))
    print("LISTO")


if __name__ == "__main__":
    main()
