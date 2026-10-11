"""Casas de Puerto Alba, rediseño: tres tipos con volumen, tejado, porche o balcón, escalones, chimenea y ventanas con marco.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU para las vistas):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/build_casas.py

Piezas (todas low-poly propias, 1 u = 1 m, Z arriba, origen en el centro de la huella, frente hacia −Y):
    SM_Casa_Colonial    dos plantas, tejado a cuatro aguas, porche con columnas, balcón con barandilla, chimenea, postigos
    SM_Casa_Moderna     volúmenes de azotea plana, planta alta en voladizo sobre el acceso, garaje, ventanal y muro bajo
    SM_Casa_Cabana      cabaña de frontón al frente, porche con barandilla, buhardilla redonda y chimenea

Los nombres de material (M_Muro_Base, M_Zocalo, M_Cornisa, M_Tejado, M_Toldo, M_Vidrio) coinciden con el kit: Unity los
sustituye por los de cada zona. La escala se comprueba con rangos de diseño (anchos de puerta de 1.0–1.2 m, puertas de
2.2–2.4 m, plantas de 3.0–3.4 m), no con una medida única, porque cada casa tiene salientes (porche, escalones, tejado).

Salidas:
    Assets/_Game/Models/PuertoAlba/Casas/<pieza>.fbx
    ArtSource/PuertoAlba/stats_casas.json
    ArtSource/PuertoAlba/Preview/Casas/<pieza>_tres_cuartos.png y SM_Casa_Dos_Plantas_anterior_tres_cuartos.png (comparación)
"""
import bpy
import bmesh
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba", "Casas")
PREVIEW_DIR = os.path.join(HERE, "Preview", "Casas")
for d in (MODEL_DIR, PREVIEW_DIR):
    os.makedirs(d, exist_ok=True)

# mismos nombres y colores base que el kit; algunos nuevos son neutros (ladrillo, hierba)
MATS = {
    "M_Muro_Base": ("#EADFC8", 0.80), "M_Zocalo": ("#6E8FA0", 0.70), "M_Cornisa": ("#B9663F", 0.75),
    "M_Marco": ("#F4F1EA", 0.55), "M_Madera": ("#8A5A3C", 0.65), "M_Puerta": ("#2F6F73", 0.55),
    "M_Vidrio": ("#5FA9B8", 0.15), "M_Toldo": ("#D9573B", 0.70), "M_Metal_Oscuro": ("#3C4048", 0.45),
    "M_Tejado": ("#8E4A2F", 0.80), "M_Ladrillo": ("#A8553A", 0.85), "M_Hierba": ("#7BAF4B", 0.90),
}

# pieza -> (presupuesto de triángulos, (X mín, X máx), (Y mín, Y máx), (Z mín, Z máx)), en Blender
PIEZAS = {
    "SM_Casa_Colonial": (2500, (8.4, 8.9), (10.2, 10.9), (9.5, 9.9)),
    "SM_Casa_Moderna": (2500, (7.0, 7.6), (10.4, 11.0), (7.0, 7.5)),
    "SM_Casa_Cabana": (2500, (5.4, 6.0), (9.2, 9.9), (6.3, 6.9)),
}


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hexlin(h):
    h = h.lstrip("#")
    return tuple(_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def get_mat(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    hexcol, rough = MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    col = (*hexlin(hexcol), 1.0)
    b.inputs["Base Color"].default_value = col
    b.inputs["Roughness"].default_value = rough
    m.diffuse_color = col
    return m


def _apply(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def box(size, loc, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.scale, o.rotation_euler, o.location = size, rot, loc
    o.data.materials.append(get_mat(mat))
    _apply(o)
    return o


def cyl(r, depth, loc, mat, verts=10, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.location, o.rotation_euler = loc, rot
    o.data.materials.append(get_mat(mat))
    _apply(o)
    return o


def _malla(nombre, verts, caras, mat):
    """Malla cerrada a partir de vértices y caras; recalcula normales hacia fuera."""
    me = bpy.data.meshes.new(nombre)
    me.from_pydata(verts, [], caras)
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(nombre, me)
    bpy.context.scene.collection.objects.link(o)
    o.data.materials.append(get_mat(mat))
    return o


def tejado_cuatro_aguas(cx, cy, z0, ancho, fondo, alto, mat):
    """Pirámide de base rectangular: tejado a cuatro aguas."""
    x, y = ancho / 2, fondo / 2
    v = [(cx - x, cy - y, z0), (cx + x, cy - y, z0), (cx + x, cy + y, z0), (cx - x, cy + y, z0), (cx, cy, z0 + alto)]
    f = [(0, 3, 2, 1), (0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4)]
    return _malla("tejado4", v, f, mat)


def tejado_a_dos_aguas_y(cx, cy, z0, ancho, fondo, alto, mat):
    """Tejado a dos aguas con la cumbrera paralela a Y: los hastiales miran a −Y y +Y."""
    x, y = ancho / 2, fondo / 2
    v = [(cx - x, cy - y, z0), (cx + x, cy - y, z0), (cx + x, cy + y, z0), (cx - x, cy + y, z0),
         (cx, cy - y, z0 + alto), (cx, cy + y, z0 + alto)]
    f = [(1, 2, 5, 4), (3, 0, 4, 5)]   # solo las dos vertientes: así el hastial de muro se ve desde el frente
    return _malla("tejado2", v, f, mat)


def hastial_al_frente(cx, y, z0, ancho, alto, grosor, mat):
    """Triángulo de muro en el hastial frontal, con grosor en Y."""
    x = ancho / 2
    v = [(cx - x, y, z0), (cx + x, y, z0), (cx, y, z0 + alto),
         (cx - x, y + grosor, z0), (cx + x, y + grosor, z0), (cx, y + grosor, z0 + alto)]
    f = [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
    return _malla("hastial", v, f, mat)


def marco(w, h, t, depth, cx, y, cz, mat="M_Marco", suelo=True):
    """Marco de cuatro listones (sin el inferior si suelo=False, para puertas)."""
    p = [
        box((w + 2 * t, depth, t), (cx, y, cz + h / 2 + t / 2), mat),
        box((t, depth, h), (cx - w / 2 - t / 2, y, cz), mat),
        box((t, depth, h), (cx + w / 2 + t / 2, y, cz), mat),
    ]
    if suelo:
        p.append(box((w + 2 * t, depth, t), (cx, y, cz - h / 2 - t / 2), mat))
    return p


def ventana(cx, y_pared, cz, w=1.0, h=1.3, postigos=True, alfeizar=True):
    """Ventana con marco, vidrio, dintel, alféizar y postigos. y_pared es la cara exterior del muro (−Y)."""
    p = marco(w, h, 0.07, 0.08, cx, y_pared - 0.03, cz)
    p.append(box((w, 0.03, h), (cx, y_pared - 0.01, cz), "M_Vidrio"))
    p.append(box((0.04, 0.035, h), (cx, y_pared - 0.03, cz), "M_Marco"))                    # travesaño central
    p.append(box((w + 0.2, 0.1, 0.06), (cx, y_pared - 0.06, cz + h / 2 + 0.14), "M_Cornisa"))   # dintel
    if alfeizar:
        p.append(box((w + 0.3, 0.16, 0.07), (cx, y_pared - 0.08, cz - h / 2 - 0.12), "M_Marco"))
    if postigos:
        for s in (-1, 1):
            p.append(box((w * .45, 0.04, h + 0.1), (cx + s * (w / 2 + w * .22 + 0.08), y_pared - 0.03, cz), "M_Madera"))
    return p


def puerta(cx, y_pared, w=1.2, h=2.4, z_base=0.4, mat="M_Puerta"):
    cz = z_base + h / 2
    p = marco(w, h, 0.1, 0.12, cx, y_pared - 0.04, cz, suelo=False)
    p.append(box((w, 0.08, h), (cx, y_pared - 0.02, cz), mat))
    p.append(box((0.07, 0.07, 0.18), (cx + w * 0.32, y_pared - 0.09, cz), "M_Marco"))           # tirador
    return p


def join(parts, nombre):
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = nombre
    obj.data.name = nombre
    return obj


# ------------------------------------------------------------------ casa colonial

def casa_colonial():
    p = [
        box((8.2, 7.2, 0.4), (0, 0, 0.2), "M_Zocalo"),
        box((8.0, 7.0, 3.2), (0, 0, 2.0), "M_Muro_Base"),                    # planta baja 0.4–3.6
        box((8.3, 7.3, 0.2), (0, 0, 3.7), "M_Cornisa"),                      # cornisa entre plantas 3.6–3.8
        box((7.6, 6.6, 3.0), (0, 0, 5.3), "M_Muro_Base"),                    # planta alta 3.8–6.8
        box((8.0, 7.0, 0.2), (0, 0, 6.9), "M_Cornisa"),                      # alero 6.8–7.0
    ]
    p.append(tejado_cuatro_aguas(0, 0, 7.0, 8.6, 7.6, 2.0, "M_Tejado"))     # 7.0–9.0
    # chimenea de ladrillo y remate
    p.append(box((0.7, 0.7, 2.2), (2.4, 1.2, 8.4), "M_Ladrillo"))
    p.append(box((0.9, 0.9, 0.12), (2.4, 1.2, 9.56), "M_Cornisa"))
    # porche: losa, dos columnas, balcón encima con barandilla
    p.append(box((4.4, 2.4, 0.3), (0, -4.7, 0.15), "M_Zocalo"))             # y −5.9…−3.5
    for x in (-1.9, 1.9):
        p.append(cyl(0.13, 3.3, (x, -5.6, 1.95), "M_Marco", verts=8))        # z 0.3–3.6
    p.append(box((4.4, 2.4, 0.2), (0, -4.7, 3.7), "M_Cornisa"))             # losa del balcón 3.6–3.8
    p.append(box((4.4, 0.06, 0.08), (0, -5.87, 4.7), "M_Marco"))            # pasamanos
    for i in range(11):
        p.append(box((0.05, 0.05, 0.85), (-2.0 + i * 0.4, -5.87, 4.22), "M_Marco"))   # balaustres
    for sx in (-1, 1):
        p.append(box((0.06, 2.4, 0.08), (sx * 2.17, -4.7, 4.7), "M_Marco"))
        for i in range(5):
            p.append(box((0.05, 0.05, 0.85), (sx * 2.17, -5.7 + i * 0.5, 4.22), "M_Marco"))
    # escalones
    p.append(box((2.6, 0.4, 0.2), (0, -6.1, 0.1), "M_Zocalo"))              # y −6.3…−5.9
    p.append(box((2.6, 0.4, 0.1), (0, -6.5, 0.05), "M_Zocalo"))             # y −6.7…−6.3
    # puerta principal y puerta-ventana del balcón
    p += puerta(0, -3.5)
    p += marco(1.1, 2.2, 0.08, 0.1, 0, -3.35, 4.9)
    p.append(box((1.1, 0.03, 2.2), (0, -3.31, 4.9), "M_Vidrio"))
    # ventanas de planta baja y alta (fachada)
    for x in (-2.7, 2.7):
        p += ventana(x, -3.5, 2.2)
        p += ventana(x, -3.3, 5.4)
    # ventanas laterales
    for s in (-1, 1):
        for z in (2.2, 5.4):
            p.append(box((0.03, 1.0, 1.2), (s * (4.02 if z < 3 else 3.82), 0.2, z), "M_Vidrio"))
            p.append(box((0.07, 1.2, 0.07), (s * (4.04 if z < 3 else 3.84), 0.2, z + 0.65), "M_Cornisa"))
    return join(p, "SM_Casa_Colonial")


# ------------------------------------------------------------------ casa moderna

def casa_moderna():
    p = [
        box((7.2, 8.2, 0.3), (0, 0, 0.15), "M_Zocalo"),
        box((7.0, 8.0, 3.2), (0, 0, 1.9), "M_Muro_Base"),                    # planta baja 0.3–3.5
        box((7.2, 8.2, 0.2), (0, 0, 3.6), "M_Metal_Oscuro"),                 # azotea baja
        box((6.0, 6.0, 3.0), (0, -1.0, 5.1), "M_Cornisa"),                   # planta alta en voladizo 3.6–6.6, y −4…2
        box((6.2, 6.2, 0.2), (0, -1.0, 6.7), "M_Metal_Oscuro"),              # azotea alta 6.6–6.8
        box((1.2, 0.8, 0.5), (-1.5, 0.0, 7.05), "M_Metal_Oscuro"),           # equipo de azotea
    ]
    # voladizo: la planta alta sobresale 1 m al frente (y −4…−5 queda sobre el acceso); columna que lo sostiene
    p.append(box((6.0, 1.0, 3.0), (0, -4.5, 5.1), "M_Cornisa"))
    p.append(cyl(0.12, 3.3, (2.8, -4.85, 1.95), "M_Metal_Oscuro", verts=8))
    # garaje: puerta seccional con ranuras
    p.append(box((3.0, 0.06, 2.4), (-1.6, -4.03, 1.5), "M_Marco"))
    for i in range(5):
        p.append(box((3.0, 0.02, 0.04), (-1.6, -4.07, 0.7 + i * 0.4), "M_Metal_Oscuro"))
    # puerta de entrada con visera de madera
    p += puerta(2.0, -4.0, w=1.0, h=2.3, z_base=0.3, mat="M_Puerta")
    p.append(box((1.8, 1.2, 0.12), (2.0, -4.6, 2.8), "M_Madera"))
    # ventanal de la planta alta con travesaños
    p += marco(4.0, 1.8, 0.08, 0.1, 0, -5.03, 5.1, "M_Metal_Oscuro")
    p.append(box((4.0, 0.04, 1.8), (0, -5.02, 5.1), "M_Vidrio"))
    for x in (-1.0, 0.0, 1.0):
        p.append(box((0.05, 0.05, 1.8), (x, -5.06, 5.1), "M_Metal_Oscuro"))
    # ventana alta lateral y ventana de planta baja
    p.append(box((0.04, 2.0, 1.2), (3.02, -1.0, 5.1), "M_Vidrio"))
    p.append(box((0.04, 2.0, 1.2), (-3.02, -1.0, 5.1), "M_Vidrio"))
    # muro bajo con hueco para el camino y camino de acceso
    for x in (-2.1, 2.1):
        p.append(box((2.8, 0.2, 0.9), (x, -6.5, 0.45), "M_Zocalo"))
        p.append(box((2.9, 0.26, 0.08), (x, -6.5, 0.94), "M_Marco"))
    p.append(box((1.4, 2.4, 0.05), (0, -5.4, 0.025), "M_Zocalo"))
    return join(p, "SM_Casa_Moderna")


# ------------------------------------------------------------------ casa cabaña

def casa_cabana():
    p = [
        box((5.2, 7.2, 0.3), (0, 0, 0.15), "M_Zocalo"),
        box((5.0, 7.0, 3.0), (0, 0, 1.8), "M_Muro_Base"),                    # paredes 0.3–3.3
    ]
    p.append(tejado_a_dos_aguas_y(0, 0, 3.3, 5.6, 7.8, 2.4, "M_Tejado"))     # hastial al frente, cumbrera en 5.7
    p.append(hastial_al_frente(0, -3.58, 3.3, 5.6, 2.4, 0.1, "M_Muro_Base"))   # coincide con el plano del tejado
    # buhardilla redonda
    p.append(cyl(0.5, 0.06, (0, -3.64, 4.2), "M_Marco", verts=16, rot=(math.radians(90), 0, 0)))
    p.append(cyl(0.4, 0.07, (0, -3.68, 4.2), "M_Vidrio", verts=16, rot=(math.radians(90), 0, 0)))
    # chimenea de ladrillo a un lado de la cumbrera
    p.append(box((0.6, 0.6, 2.1), (1.5, 1.5, 5.35), "M_Ladrillo"))
    p.append(box((0.8, 0.8, 0.1), (1.5, 1.5, 6.45), "M_Cornisa"))
    # porche
    p.append(box((3.6, 1.8, 0.25), (0, -4.4, 0.125), "M_Madera"))
    for x in (-1.6, 1.6):
        p.append(box((0.18, 0.18, 2.6), (x, -5.1, 1.55), "M_Marco"))
    p.append(box((3.9, 2.1, 0.12), (0, -4.55, 2.9), "M_Tejado"))
    for sx in (-1, 1):   # barandal en dos tramos, con hueco central para los escalones
        cx = sx * 1.25
        p.append(box((1.1, 0.05, 0.08), (cx, -5.2, 0.95), "M_Marco"))
        for i in range(3):
            p.append(box((0.05, 0.05, 0.7), (cx - 0.4 + i * 0.4, -5.2, 0.6), "M_Marco"))
    p.append(box((1.4, 0.35, 0.12), (0, -5.5, 0.06), "M_Madera"))
    p.append(box((1.4, 0.35, 0.24), (0, -5.15, 0.12), "M_Madera"))
    # puerta con montante de vidrio y ventanas con postigos
    p += puerta(0, -3.5, w=1.0, h=2.2, z_base=0.3, mat="M_Puerta")
    p.append(box((1.0, 0.03, 0.4), (0, -3.53, 2.75), "M_Vidrio"))
    for x in (-1.8, 1.8):
        p += ventana(x, -3.5, 1.9, w=0.9, h=1.3)
    # ventanas laterales
    for s in (-1, 1):
        for y in (-1.2, 1.2):
            p.append(box((0.03, 0.9, 1.2), (s * 2.52, y, 1.9), "M_Vidrio"))
            p.append(box((0.07, 1.1, 0.07), (s * 2.54, y, 2.58), "M_Cornisa"))
    return join(p, "SM_Casa_Cabana")


BUILDERS = {
    "SM_Casa_Colonial": casa_colonial,
    "SM_Casa_Moderna": casa_moderna,
    "SM_Casa_Cabana": casa_cabana,
}


def limites(obj):
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))


def triangulos(obj):
    return sum(max(0, len(p.vertices) - 2) for p in obj.data.polygons)


def exportar(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(MODEL_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=False)
    return path


def vista(escena, obj, nombre):
    """Vista de tres cuartos de una casa, delante a la izquierda."""
    from mathutils import Vector
    (x0, x1), (y0, y1), (z0, z1) = limites(obj)
    centro = Vector(((x0 + x1) / 2, (y0 + y1) / 2, z0 + (z1 - z0) * 0.45))
    d = max(x1 - x0, y1 - y0, z1 - z0) * 1.9 + 4.0
    cd = bpy.data.cameras.new("Cam_" + nombre)
    cd.sensor_fit = "VERTICAL"
    cd.angle_y = math.radians(38)
    cam = bpy.data.objects.new("Cam_" + nombre, cd)
    escena.collection.objects.link(cam)
    cam.location = centro + Vector((-d * 0.55, -d * 0.85, d * 0.30))
    cam.rotation_euler = (centro - cam.location).to_track_quat("-Z", "Y").to_euler()
    escena.camera = cam
    escena.render.filepath = os.path.join(PREVIEW_DIR, nombre + "_tres_cuartos.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    escena = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH"):
        try:
            escena.render.engine = eng
            break
        except TypeError:
            continue
    escena.render.resolution_x, escena.render.resolution_y = 1280, 900
    escena.render.image_settings.file_format = "PNG"
    escena.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("Mundo_casas")
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (0.62, 0.81, 0.84, 1.0)
    bg.inputs["Strength"].default_value = 0.9
    escena.world = w
    sd = bpy.data.lights.new("Sol", "SUN")
    sd.energy = 3.4
    sol = bpy.data.objects.new("Sol", sd)
    escena.collection.objects.link(sol)
    sol.rotation_euler = (math.radians(52), 0, math.radians(35))

    for n in MATS:
        get_mat(n)

    stats, fallos = {}, []
    for nombre, build in BUILDERS.items():
        obj = build()
        presupuesto, rx, ry, rz = PIEZAS[nombre]
        # origen en el centro de la huella en X (la huella incluye porche y escalones, así que Y no se centra)
        (x0, x1), _, _ = limites(obj)
        obj.data.transform(__import__("mathutils").Matrix.Translation((-(x0 + x1) / 2, 0, 0)))
        obj.data.update()
        (x0, x1), (y0, y1), (z0, z1) = limites(obj)
        dims = (round(x1 - x0, 3), round(y1 - y0, 3), round(z1 - z0, 3))
        tris = triangulos(obj)
        for eje, v, (lo, hi) in zip("XYZ", dims, (rx, ry, rz)):
            if not lo <= v <= hi:
                fallos.append("%s: %s = %.3f fuera del rango de diseño %.2f–%.2f" % (nombre, eje, v, lo, hi))
        if abs(z0) > 1e-4:
            fallos.append("%s: base fuera de z = 0 (%.4f)" % (nombre, z0))
        if tris > presupuesto:
            fallos.append("%s: %d triángulos (tope %d)" % (nombre, tris, presupuesto))
        stats[nombre] = {
            "triangulos": tris, "presupuesto_triangulos": presupuesto, "dimensiones_XxYxZ_m": list(dims),
            "bbox_min_XYZ_m": [round(x0, 4), round(y0, 4), round(z0, 4)],
            "bbox_max_XYZ_m": [round(x1, 4), round(y1, 4), round(z1, 4)],
            "base_z": round(z0, 4), "pivote_correcto": abs(z0) < 1e-4 and abs((x0 + x1) / 2) < 1e-3,
            "materiales": sorted({s.material.name for s in obj.material_slots if s.material}),
            "fbx": os.path.relpath(exportar(obj), PROJECT),
        }
        vista(escena, obj, nombre)
        obj.hide_render = True
    # comparación: la casa de dos plantas anterior, importada desde su FBX, con el mismo encuadre
    anterior = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba", "SM_Casa_Dos_Plantas.fbx")
    if os.path.isfile(anterior):
        antes = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=anterior, axis_forward="-Z", axis_up="Y")
        nuevos = [o for o in bpy.data.objects if o not in antes and o.type == "MESH"]
        if nuevos:
            vista(escena, nuevos[0], "SM_Casa_Dos_Plantas_anterior")
            for o in nuevos:
                bpy.data.objects.remove(o, do_unlink=True)
    with open(os.path.join(HERE, "stats_casas.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, ensure_ascii=False, indent=2)
    if fallos:
        raise SystemExit("COMPROBACIONES FALLIDAS:\n" + "\n".join(fallos))
    print("LISTO_CASAS", len(stats), "piezas")


main()
