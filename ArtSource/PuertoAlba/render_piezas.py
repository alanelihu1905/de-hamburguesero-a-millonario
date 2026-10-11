"""Vistas frontales de piezas individuales del kit de Puerto Alba, para comparar antes y después.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/render_piezas.py -- antes
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/render_piezas.py -- despues

Lee los FBX de Assets/_Game/Models/PuertoAlba (los mismos que usa Unity) y renderiza cada pieza sola, de frente,
con el mismo encuadre en las dos fases. Salida: ArtSource/PuertoAlba/Preview/<fase>/<pieza>.png
"""
import bpy
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba")

PIEZAS = [
    "SM_Tienda_Barrio",
    "SM_Gasolinera_Letrero",
    "SM_Gasolinera_Surtidor",
    "SM_Taller_Brisa",
    "SM_Mercado_Pabellon_Reloj",
    "SM_Fachada_Residencial",
    "SM_Fachada_Residencial_B",
]


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _mundo_luz():
    scene = bpy.context.scene
    w = bpy.data.worlds.new("Mundo_piezas")
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (0.62, 0.81, 0.84, 1.0)
    bg.inputs["Strength"].default_value = 0.9
    scene.world = w
    sd = bpy.data.lights.new("Sol", "SUN")
    sd.energy = 3.4
    sun = bpy.data.objects.new("Sol", sd)
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))


def main():
    fase = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "antes"
    salida = os.path.join(HERE, "Preview", fase)
    os.makedirs(salida, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH"):
        try:
            scene.render.engine = eng
            break
        except TypeError:
            continue
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "Standard"
    _mundo_luz()

    for nombre in PIEZAS:
        ruta = os.path.join(MODEL_DIR, nombre + ".fbx")
        if not os.path.isfile(ruta):
            print("OMITIDA (sin FBX):", nombre)
            continue
        antes = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=ruta, axis_forward="-Z", axis_up="Y")
        nuevos = [o for o in bpy.data.objects if o not in antes and o.type == "MESH"]
        xs = [v.co.x for o in nuevos for v in o.data.vertices]
        ys = [v.co.y for o in nuevos for v in o.data.vertices]
        zs = [v.co.z for o in nuevos for v in o.data.vertices]
        if not xs:
            print("OMITIDA (sin mallas):", nombre)
            continue
        cx = (min(xs) + max(xs)) / 2
        cz = (min(zs) + max(zs)) / 2
        alto = max(zs) - min(zs)
        ancho = max(xs) - min(xs)
        dist = max(alto, ancho) * 1.6 + 3.0

        cd = bpy.data.cameras.new("Cam_" + nombre)
        cd.sensor_fit = "VERTICAL"
        cd.angle_y = math.radians(40)
        cam = bpy.data.objects.new("Cam_" + nombre, cd)
        scene.collection.objects.link(cam)
        # frente hacia −Y en Blender: la cámara va delante, a la altura media de la pieza
        cam.location = (cx, -dist, cz + alto * 0.1)
        cam.rotation_euler = (math.radians(90), 0, 0)
        scene.camera = cam
        scene.render.filepath = os.path.join(salida, nombre + ".png")
        bpy.ops.render.render(write_still=True)

        for o in nuevos:
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.objects.remove(cam, do_unlink=True)
        print("VISTA_OK", fase, nombre)
    print("LISTO_PIEZAS", fase)


main()
