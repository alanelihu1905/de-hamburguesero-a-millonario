"""Vistas previas del kit de Puerto Alba con la variación de color de cada una de las ocho zonas.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/render_variantes_zonas.py

Lee ArtSource/PuertoAlba/Kit_Ciudad.blend (lo genera build_kit_ciudad.py) y zonas_variacion.json.
Cambia solo los colores de los materiales; no toca la geometría ni los FBX.

Salidas:
    ArtSource/PuertoAlba/Preview/Zonas/<id>_residencial_tienda.png
    ArtSource/PuertoAlba/Preview/Zonas/<id>_gasolinera.png
    ArtSource/PuertoAlba/Preview/Zonas/hoja_zonas.png     hoja de contacto: una fila por zona
"""
import bpy
import json
import math
import os

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "Preview", "Zonas")
os.makedirs(OUT, exist_ok=True)

# campo del JSON -> material del kit
CAMPO_A_MATERIAL = {
    "muro": ["M_Muro_Base"],
    "zocalo": ["M_Zocalo"],
    "acento": ["M_Cornisa"],
    "tejado": ["M_Tejado"],
    "toldo": ["M_Toldo"],
    "vidrio": ["M_Vidrio"],
}

RESIDENCIAL = ["SM_Fachada_Residencial", "SM_Puerta_Residencial", "SM_Tienda_Barrio"]
GASOLINERA = ["SM_Gasolinera_Marquesina", "SM_Gasolinera_Surtidor", "SM_Gasolinera_Caseta", "SM_Gasolinera_Letrero"]


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hexlin(h):
    h = h.lstrip("#")
    return tuple(_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def pintar(nombre_material, hexcol):
    m = bpy.data.materials.get(nombre_material)
    if m is None:
        raise SystemExit("Falta el material " + nombre_material + " en Kit_Ciudad.blend")
    col = (*hexlin(hexcol), 1.0)
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = col
    m.diffuse_color = col


def colocar(nombres, gap=1.0):
    x = 0.0
    ancho = 0.0
    for n in nombres:
        o = bpy.data.objects[n]
        o.hide_render = False
        xs = [v[0] for v in o.bound_box]
        x0, x1 = min(xs), max(xs)
        o.location.x = x - x0
        x += (x1 - x0) + gap
    ancho = x - gap
    return ancho


def camara(nombre, loc, target, fov):
    from mathutils import Vector
    cd = bpy.data.cameras.new(nombre)
    cd.sensor_fit = "VERTICAL"
    cd.angle_y = math.radians(fov)
    cam = bpy.data.objects.new(nombre, cd)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = Vector(loc)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
    return cam


def render(nombres, ruta):
    scene = bpy.context.scene
    todos = [o for o in bpy.data.objects if o.name.startswith("SM_")]
    for o in todos:
        o.hide_render = o.name not in nombres
    ancho = colocar(nombres)
    xc = ancho / 2
    dist = (ancho / 2 + 3) / math.tan(math.radians(45 / 2)) * 0.9
    scene.camera = camara("Cam_zona", (xc, -dist, 2.4), (xc, 0, 1.6), 45)
    scene.render.filepath = ruta
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    scene.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    for o in todos:
        o.hide_render = False


def hoja(rutas, ruta_salida):
    """Une las imágenes en una hoja: una fila por zona, dos columnas (residencial y gasolinera). Reduce ×4."""
    filas = []
    for i in range(0, len(rutas), 2):
        celdas = []
        for r in rutas[i:i + 2]:
            img = bpy.data.images.load(r)
            w, h = img.size
            px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1, ::4, :][::4, :, :]
            celdas.append(px)
            bpy.data.images.remove(img)
        filas.append(np.concatenate(celdas, axis=1))
    sheet = np.concatenate(filas, axis=0)
    h, w = sheet.shape[:2]
    out = bpy.data.images.new("hoja_zonas", w, h, alpha=True)
    out.pixels = sheet[::-1].ravel().tolist()   # Blender guarda las filas de abajo arriba
    out.filepath_raw = ruta_salida
    out.file_format = "PNG"
    out.save()
    bpy.data.images.remove(out)


def main():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, "Kit_Ciudad.blend"))
    scene = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH"):
        try:
            scene.render.engine = eng
            break
        except TypeError:
            continue
    try:
        scene.eevee.taa_render_samples = 16
    except Exception:
        pass
    scene.view_settings.view_transform = "Standard"

    with open(os.path.join(HERE, "zonas_variacion.json"), encoding="utf-8") as f:
        zonas = json.load(f)
    zonas = {k: v for k, v in zonas.items() if not k.startswith("_")}

    rutas = []
    for zid, z in zonas.items():
        for campo, mats in CAMPO_A_MATERIAL.items():
            for mat in mats:
                pintar(mat, z[campo])
        base = os.path.join(OUT, zid)
        render(RESIDENCIAL, base + "_residencial_tienda.png")
        render(GASOLINERA, base + "_gasolinera.png")
        rutas += [base + "_residencial_tienda.png", base + "_gasolinera.png"]
        print("ZONA_OK", zid, z["nombre"])

    hoja(rutas, os.path.join(OUT, "hoja_zonas.png"))
    print("LISTO_VARIANTES", len(zonas), "zonas")


main()
