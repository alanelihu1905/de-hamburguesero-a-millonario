"""Genera la fachada exterior de Casa Marea (hito 1) en Blender.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_fachada.py

Especificación: Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md, secciones 2.3, 2.5 y 4.

Salidas:
    ArtSource/CasaMarea/Fachada_CasaMarea.blend           fuente (modelos + contexto de vista previa)
    Assets/_Game/Models/CasaMarea/SM_Fachada_CasaMarea.fbx  fachada sur + azotea + pretil + chimenea + banderín + banqueta
    Assets/_Game/Models/CasaMarea/SM_Despacho_Este.fbx      muro este con ventanilla, repisa y toldo
    ArtSource/CasaMarea/Preview/Fachada_*.png, Despacho_*.png
    ArtSource/CasaMarea/stats_fachada.json

Sistema de coordenadas: los modelos se construyen YA en las coordenadas de la escena del documento 04, de modo que
el prefab se coloca en (0, 0, 0). Blender (x, y, z) equivale a escena (x, z, y): +Y = norte (fondo de la casa),
+X = este, el frente de la fachada mira a -Y (hacia la calle). Z arriba. 1 u = 1 m.

Puerta libre: el vano del portón (x -1..1, hasta z 2.3) queda sin geometría; las hojas están abiertas hacia
afuera. El script lo comprueba con rayos y lo escribe en stats_fachada.json.
"""
import bpy
import bmesh
import math
import os
import json
from mathutils import Vector
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "CasaMarea")
PREVIEW_DIR = os.path.join(HERE, "Preview")
for d in (MODEL_DIR, PREVIEW_DIR):
    os.makedirs(d, exist_ok=True)

# ---------------------------------------------------------------- materiales
# nombre: (hex, rugosidad, metálico, color_emisión, fuerza_emisión)
MATS = {
    "M_Cal_Arena":     ("#E8C4A0", 0.85, 0.00, None, 0.0),
    "M_Turquesa":      ("#2BA6A0", 0.50, 0.00, None, 0.0),
    "M_Crema":         ("#F7EEDD", 0.60, 0.00, None, 0.0),
    "M_Caoba":         ("#8A5A3C", 0.65, 0.00, None, 0.0),
    "M_Herreria":      ("#2E2538", 0.70, 0.10, None, 0.0),
    "M_Chile":         ("#E4572E", 0.55, 0.00, None, 0.0),
    "M_Mango":         ("#F4C95D", 0.40, 0.00, "#F4C95D", 0.0),
    "M_Piedra_Caliza": ("#CDBFA4", 0.90, 0.00, None, 0.0),
    "M_Metal_Plancha": ("#3E4A50", 0.50, 0.25, None, 0.0),
    "M_Letrero":       ("#2BA6A0", 0.60, 0.00, None, 0.0),   # solo el plano del texto; Unity lo sustituye por la textura del nombre
}


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hexlin(h):
    h = h.lstrip("#")
    return tuple(_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def get_mat(name, spec=None):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    hexcol, rough, metal, emis, estr = spec or MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    col = (*hexlin(hexcol), 1.0)
    b.inputs["Base Color"].default_value = col
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emis:
        b.inputs["Emission Color"].default_value = (*hexlin(emis), 1.0)
        b.inputs["Emission Strength"].default_value = estr
    m.diffuse_color = col
    return m


# ---------------------------------------------------------------- primitivas
PARTS = []


def _apply(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def _finish(o, name, mat, collect=True):
    o.name = name
    o.data.materials.append(get_mat(mat))
    _apply(o)
    if collect:
        PARTS.append(o)
    return o


def box(name, size, loc, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.scale, o.rotation_euler, o.location = size, rot, loc
    return _finish(o, name, mat)


def cyl(name, r, depth, loc, mat, axis="Z", verts=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    if axis == "Y":
        o.rotation_euler = (math.pi / 2, 0, 0)
    elif axis == "X":
        o.rotation_euler = (0, math.pi / 2, 0)
    return _finish(o, name, mat)


def cone(name, r1, r2, depth, loc, mat, verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    return _finish(o, name, mat)


def prism(name, pts, thick, center, mat, plane="XZ"):
    """Prisma extruido de un polígono CONVEXO. plane 'XZ': (u,v)=(x,z), grosor en Y. plane 'YZ': (u,v)=(y,z), grosor en X.
    center = (x, y, z) del centro del grosor."""
    n, h = len(pts), thick / 2.0

    def P(u, v, w):
        return (u, w, v) if plane == "XZ" else (w, u, v)

    verts = [P(u, v, -h) for u, v in pts] + [P(u, v, h) for u, v in pts]
    faces = [list(range(n)), list(range(2 * n - 1, n - 1, -1))]
    faces += [[i, (i + 1) % n, n + (i + 1) % n, n + i] for i in range(n)]
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    o.location = center
    return _finish(o, name, mat)


def rot_z(v, a):
    c, s = math.cos(a), math.sin(a)
    return (v[0] * c - v[1] * s, v[0] * s + v[1] * c, v[2])


def box_hinge(name, size, hinge, local_center, angle, mat):
    """Caja girada un ángulo Z alrededor de una bisagra; local_center está en el marco local de la hoja."""
    w = rot_z(local_center, angle)
    return box(name, size, (hinge[0] + w[0], hinge[1] + w[1], hinge[2] + w[2]), mat, (0, 0, angle))


def finalize(name):
    bpy.ops.object.select_all(action="DESELECT")
    for p in PARTS:
        p.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    o.data.name = name + "_Mesh"
    PARTS.clear()
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))
    except Exception:
        bpy.ops.object.shade_smooth()
    return o


# ---------------------------------------------------------------- iconos planos
def pbox(name, plane, c, su, sv, thick, mat):
    u, v, w = c
    if plane == "XZ":
        return box(name, (su, thick, sv), (u, w, v), mat)
    return box(name, (thick, su, sv), (w, u, v), mat)


def burger_icon(tag, plane, cu, cv, w, s, thick=0.02):
    """Icono plano de hamburguesa: pan mango, salsa chile, carne caoba y base mango.
    (cu, cv) = centro en el plano; w = posición del centro del grosor sobre el eje normal; s = escala."""
    pbox(tag + "_base", plane, (cu, cv - 0.075 * s, w), 0.26 * s, 0.040 * s, thick, "M_Mango")
    pbox(tag + "_carne", plane, (cu, cv - 0.030 * s, w), 0.28 * s, 0.045 * s, thick, "M_Caoba")
    pbox(tag + "_salsa", plane, (cu, cv + 0.003 * s, w), 0.29 * s, 0.018 * s, thick, "M_Chile")
    pts = [(-0.13 * s, 0), (-0.11 * s, 0.05 * s), (-0.06 * s, 0.085 * s),
           (0.06 * s, 0.085 * s), (0.11 * s, 0.05 * s), (0.13 * s, 0)]
    cx, cy, cz = (cu, w, cv + 0.014 * s) if plane == "XZ" else (w, cu, cv + 0.014 * s)
    prism(tag + "_pan", pts, thick, (cx, cy, cz), "M_Mango", plane)


# ---------------------------------------------------------------- fachada sur
def build_fachada():
    # -- muros (cara exterior en y = -2.1; grosor 0.2), vano del portón libre en x -1..1 --
    Y, T = -2.0, 0.2
    box("dintel_porton", (2.0, T, 0.7), (0, Y, 2.65), "M_Cal_Arena")
    for sx in (-1, 1):
        c = sx * 3.0                                   # centro de la ventana de ese lado
        # el muro llega a x = ±5.1 para cerrar la esquina con el muro este (cara exterior en x = ±5.1)
        box("muro_bajo", (4.1, T, 0.95), (sx * 3.05, Y, 0.475), "M_Cal_Arena")
        box("muro_alto", (4.1, T, 0.75), (sx * 3.05, Y, 2.625), "M_Cal_Arena")
        box("muro_lado_ext", (1.5, T, 1.30), (c + sx * 1.35, Y, 1.60), "M_Cal_Arena")
        box("muro_lado_int", (1.4, T, 1.30), (c - sx * 1.3, Y, 1.60), "M_Cal_Arena")
        # ventana falsa hundida: vidrio oscuro, reja, marco y postigos abiertos
        box("vidrio", (1.2, 0.02, 1.30), (c, -1.95, 1.60), "M_Herreria")
        for i in range(8):
            box("reja_barra", (0.025, 0.025, 1.30), (c - 0.5 + i * (1.0 / 7), -2.06, 1.60), "M_Herreria")
        for zr in (1.25, 1.95):
            box("reja_riel", (1.2, 0.03, 0.03), (c, -2.07, zr), "M_Herreria")
        box("marco_alto", (1.4, 0.06, 0.10), (c, -2.13, 2.30), "M_Crema")
        box("marco_alfeizar", (1.5, 0.12, 0.06), (c, -2.16, 0.92), "M_Crema")
        for sk in (-1, 1):
            box("marco_lado", (0.10, 0.06, 1.30), (c + sk * 0.65, -2.13, 1.60), "M_Crema")
            box("postigo", (0.60, 0.04, 1.30), (c + sk * 1.0, -2.14, 1.60), "M_Turquesa")
            box("postigo_travesano", (0.60, 0.05, 0.06), (c + sk * 1.0, -2.145, 1.60), "M_Caoba")
    # zócalo turquesa en dos tramos (el portón queda libre)
    for sx in (-1, 1):
        box("zocalo", (4.1, 0.04, 0.9), (sx * 3.05, -2.12, 0.45), "M_Turquesa")
    # pilastras crema
    for x in (-4.95, -1.35, 1.35, 4.95):
        box("pilastra", (0.35, 0.12, 2.9), (x, -2.16, 1.45), "M_Crema")
    # jambas del portón (fuera del vano)
    for sx in (-1, 1):
        box("jamba", (0.12, 0.06, 2.3), (sx * 1.06, -2.13, 1.15), "M_Crema")
    box("jamba_dintel", (2.24, 0.06, 0.10), (0, -2.13, 2.35), "M_Crema")
    # hojas del portón abiertas ~100° hacia la banqueta; no invaden el vano
    for sx in (-1, 1):
        ang = sx * math.radians(10)
        hinge = (sx * 1.12, -2.16, 0.0)
        box_hinge("hoja_porton", (0.06, 1.0, 2.3), hinge, (0, -0.5, 1.15), ang, "M_Caoba")
        for face in (-1, 1):
            for zc, hh in ((0.70, 0.80), (1.70, 0.70)):
                box_hinge("hoja_panel", (0.02, 0.62, hh), hinge, (face * 0.04, -0.5, zc), ang, "M_Caoba")
    # cornisa (vuela 0.1), azotea y pretil
    box("cornisa", (10.4, 0.2, 0.2), (0, -2.1, 3.0), "M_Crema")
    box("azotea", (10.4, 6.4, 0.25), (0, 1.0, 3.18), "M_Piedra_Caliza")
    box("pretil_frente", (10.4, 0.2, 0.45), (0, -2.1, 3.53), "M_Crema")
    box("pretil_fondo", (10.4, 0.2, 0.45), (0, 4.1, 3.53), "M_Crema")
    for sx in (-5.1, 5.1):
        box("pretil_lado", (0.2, 6.0, 0.45), (sx, 1.0, 3.53), "M_Crema")
    # chimenea sobre la plancha (x 0, y 1.9)
    cyl("chimenea", 0.15, 0.90, (0, 1.9, 3.75), "M_Metal_Plancha", verts=12)
    cyl("chimenea_banda", 0.17, 0.06, (0, 1.9, 3.95), "M_Chile", verts=12)
    cone("chimenea_sombrerete", 0.24, 0.15, 0.10, (0, 1.9, 4.25), "M_Herreria", verts=12)
    # rótulo: marco crema con cara turquesa, filetes mango y dos iconos; el centro queda libre para el nombre
    zs = 2.62
    box("letrero_marco", (3.5, 0.20, 0.60), (0, -2.20, zs), "M_Crema")
    box("letrero_cara", (3.3, 0.02, 0.42), (0, -2.31, zs), "M_Turquesa")
    for dz in (-0.17, 0.17):
        box("letrero_filete", (3.3, 0.01, 0.025), (0, -2.322, zs + dz), "M_Mango")
    for sx in (-1, 1):
        burger_icon("letrero_icono", "XZ", sx * 1.35, zs, -2.33, 1.35)
    # banderín perpendicular en la esquina SE (visible al caminar por la calle)
    box("banderin_brazo", (0.04, 0.90, 0.04), (5.0, -2.55, 2.70), "M_Herreria")
    for yy in (-2.35, -2.85):
        box("banderin_cadena", (0.015, 0.015, 0.06), (5.0, yy, 2.65), "M_Herreria")
    box("banderin_placa", (0.06, 0.80, 0.70), (5.0, -2.60, 2.30), "M_Turquesa")
    box("banderin_borde_sup", (0.07, 0.80, 0.03), (5.0, -2.60, 2.635), "M_Chile")
    box("banderin_borde_inf", (0.07, 0.80, 0.03), (5.0, -2.60, 1.965), "M_Chile")
    for yy in (-2.215, -2.985):
        box("banderin_borde_lado", (0.07, 0.03, 0.70), (5.0, yy, 2.30), "M_Chile")
    for face in (-1, 1):
        burger_icon("banderin_icono", "YZ", -2.60, 2.30, 5.0 + face * 0.04, 1.8)
    # banqueta de caliza (tapa a z = 0, a ras del piso interior)
    box("banqueta", (18.0, 1.2, 0.15), (0, -2.6, -0.075), "M_Piedra_Caliza")
    return finalize("SM_Fachada_CasaMarea")


def build_letrero_texto():
    """Plano de 2 triángulos para la textura con el nombre del negocio (pendiente de decisión narrativa)."""
    bpy.ops.mesh.primitive_plane_add(size=1)
    o = bpy.context.active_object
    o.scale = (1.9, 0.34, 1.0)
    o.rotation_euler = (math.pi / 2, 0, 0)       # normal hacia -Y
    o.location = (0, -2.3215, 2.62)
    return _finish(o, "SM_Letrero_Texto", "M_Letrero", collect=False)


# ---------------------------------------------------------------- despacho este
def build_despacho():
    X, T = 5.0, 0.2
    # muro este (y -2..4) con ventanilla y 0.85..2.65, z 0.85..2.15
    box("muro_sur", (T, 2.85, 3.0), (X, -0.575, 1.5), "M_Cal_Arena")
    box("muro_norte", (T, 1.35, 3.0), (X, 3.325, 1.5), "M_Cal_Arena")
    box("muro_bajo", (T, 1.80, 0.85), (X, 1.75, 0.425), "M_Cal_Arena")
    box("muro_alto", (T, 1.80, 0.85), (X, 1.75, 2.575), "M_Cal_Arena")
    box("zocalo", (0.04, 6.0, 0.85), (5.12, 1.0, 0.425), "M_Turquesa")
    box("cornisa_este", (0.2, 6.2, 0.2), (5.1, 1.0, 3.0), "M_Crema")
    # marco de la ventanilla y repisa exterior de despacho (continúa el mostrador a z 0.85)
    box("marco_alto", (0.06, 2.0, 0.10), (5.13, 1.75, 2.20), "M_Crema")
    for yy in (0.80, 2.70):
        box("marco_lado", (0.06, 0.10, 1.30), (5.13, yy, 1.50), "M_Crema")
    box("repisa", (0.50, 1.80, 0.12), (5.35, 1.75, 0.79), "M_Piedra_Caliza")
    # toldo: se ancla en el muro a z 2.55 y baja a z 2.15 en x 6.7; 8 franjas turquesa/crema + festón triangular
    run, drop = 1.6, 0.4
    slope = math.atan(drop / run)
    length = math.hypot(run, drop)
    for i in range(8):
        y = 0.35 + 0.35 * (i + 0.5)
        mat = "M_Turquesa" if i % 2 == 0 else "M_Crema"
        box("toldo_franja", (length, 0.35, 0.02), (5.1 + run / 2, y, 2.55 - drop / 2), mat, (0, slope, 0))
        prism("toldo_feston", [(-0.175, 0.0), (0.175, 0.0), (0.0, -0.20)], 0.02, (6.70, y, 2.15), mat, "YZ")
    box("toldo_riel", (0.05, 2.8, 0.05), (5.13, 1.75, 2.57), "M_Herreria")
    for yy in (0.45, 3.05):
        cyl("toldo_poste", 0.025, 2.15, (6.62, yy, 1.075), "M_Herreria", verts=6)
    return finalize("SM_Despacho_Este")


# ---------------------------------------------------------------- utilidades
def tri_count(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    e = obj.evaluated_get(dg)
    me = e.to_mesh()
    n = sum(len(p.vertices) - 2 for p in me.polygons)
    e.to_mesh_clear()
    return n


def bounds(obj):
    vs = [v.co for v in obj.data.vertices]
    return ((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)),
            (max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))


def door_check(obj):
    """Lanza rayos a lo largo de +Y por el vano del portón: no debe haber geometría."""
    tree = BVHTree.FromObject(obj, bpy.context.evaluated_depsgraph_get())

    def hit(x, z):
        return tree.ray_cast(Vector((x, -6.0, z)), Vector((0, 1, 0)), 9.0)[0] is not None

    xs = [-0.98 + 0.14 * i for i in range(15)]
    free_h, z = 0.0, 0.05
    while z <= 2.6:
        if any(hit(x, z) for x in xs):
            break
        free_h, z = z, z + 0.01
    half_w, step, x = 0.0, 0.005, 0.0
    while x <= 1.4:
        if any(hit(s * x, zz) for s in (-1, 1) for zz in (0.3, 1.0, 1.8)):
            break
        half_w, x = x, x + step
    return {"altura_libre_m": round(free_h, 3), "ancho_libre_m": round(2 * half_w, 3),
            "rayos_con_golpe_en_vano": sum(hit(x, zz) for x in xs for zz in (0.3, 1.0, 1.8, 2.2))}


def setup_render(scene, w, h):
    r = scene.render
    r.resolution_x, r.resolution_y, r.resolution_percentage = w, h, 100
    r.image_settings.file_format = "PNG"
    r.image_settings.color_mode = "RGBA"
    r.film_transparent = False
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH"):
        try:
            r.engine = eng
            break
        except TypeError:
            continue
    try:
        scene.eevee.taa_render_samples = 48
    except Exception:
        pass
    scene.view_settings.view_transform = "Standard"
    try:
        scene.view_settings.look = "None"
    except Exception:
        pass


def add_world(scene, hexcol="#9FCFD6", strength=0.9):
    w = bpy.data.worlds.new("Mundo_Preview")
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (*hexlin(hexcol), 1.0)
    bg.inputs["Strength"].default_value = strength
    scene.world = w


def add_lights():
    # misma dirección que el sol del documento 04 (escena Euler 30°, 40°): viaja hacia el noreste
    d = Vector((0.555, 0.667, -0.5))
    sd = bpy.data.lights.new("Sol_Tarde", "SUN")
    sd.energy, sd.color = 3.4, hexlin("#FFC47E")
    sun = bpy.data.objects.new("Sol_Tarde", sd)
    bpy.context.scene.collection.objects.link(sun)
    sun.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    fd = bpy.data.lights.new("Relleno_Ciruela", "AREA")
    fd.energy, fd.size, fd.color = 400, 8.0, hexlin("#B79BC4")
    fill = bpy.data.objects.new("Relleno_Ciruela", fd)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = Vector((-6, -9, 5))
    fill.rotation_euler = (Vector((0, 0, 1.5)) - fill.location).to_track_quat("-Z", "Y").to_euler()


def ctx_box(name, size, loc, hexcol):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.name = "Preview_" + name
    o.scale, o.location = size, loc
    o.data.materials.append(get_mat("M_Preview_" + hexcol.lstrip("#"), (hexcol, 0.9, 0.0, None, 0.0)))
    return o


def add_context():
    """Entorno solo para las vistas previas (no se exporta): calle, vecinas, paredes y piso interior."""
    ctx_box("Calle", (18, 7.8, 0.1), (0, -7.1, -0.12), "#B8A78C")
    ctx_box("Callejon", (4.0, 6.0, 0.1), (7.0, 1.0, -0.05), "#CDBFA4")
    ctx_box("Piso_Casa", (9.8, 6.0, 0.1), (0, 1.0, -0.05), "#A9744F")
    ctx_box("Pared_Oeste", (0.2, 6.0, 3.0), (-5.0, 1.0, 1.5), "#E8C4A0")
    ctx_box("Pared_Fondo", (10.2, 0.2, 3.0), (0, 4.0, 1.5), "#E8C4A0")
    ctx_box("Vecina_Izq", (6, 6, 2.8), (-12, 1, 1.4), "#D7B592")
    ctx_box("Vecina_Der", (6, 6, 2.8), (12, 1, 1.4), "#D7B592")
    ctx_box("Tapia_Callejon", (4.0, 0.2, 2.4), (7.0, 4.0, 1.2), "#CDBFA4")


def add_camera(name, loc, target, fov_deg=60):
    cd = bpy.data.cameras.new(name)
    cd.sensor_fit = "VERTICAL"
    cd.angle_y = math.radians(fov_deg)
    cam = bpy.data.objects.new(name, cd)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = Vector(loc)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
    return cam


def render_to(scene, cam, path):
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def export_fbx(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path = os.path.join(MODEL_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=False)
    return path


def stat(obj, extra=None):
    lo, hi = bounds(obj)
    d = {"triangulos": tri_count(obj),
         "dimensiones_XxYxZ_m": [round(hi[i] - lo[i], 3) for i in range(3)],
         "min": [round(v, 3) for v in lo], "max": [round(v, 3) for v in hi],
         "materiales": [m.name for m in obj.data.materials]}
    if extra:
        d.update(extra)
    return d


# ---------------------------------------------------------------- principal
def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0

    fachada = build_fachada()
    letrero = build_letrero_texto()
    despacho = build_despacho()

    stats = {
        "SM_Fachada_CasaMarea": stat(fachada, {"puerta_libre": door_check(fachada),
                                               "presupuesto_doc04": "fachada 2 500 + banderín 300",
                                               "objetos_extra": ["SM_Letrero_Texto (2 triángulos, M_Letrero)"]}),
        "SM_Letrero_Texto": stat(letrero),
        "SM_Despacho_Este": stat(despacho, {"presupuesto_doc04": "toldo + postes 700"}),
    }
    stats["SM_Fachada_CasaMarea"]["fbx"] = os.path.relpath(export_fbx([fachada, letrero], "SM_Fachada_CasaMarea"), PROJECT)
    stats["SM_Despacho_Este"]["fbx"] = os.path.relpath(export_fbx([despacho], "SM_Despacho_Este"), PROJECT)
    stats["paleta"] = {k: v[0] for k, v in MATS.items()}
    with open(os.path.join(HERE, "stats_fachada.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)

    add_context()
    add_world(scene)
    add_lights()
    views = [
        ("Fachada_postal", (0, -8.5, 1.65), (0, 0, 1.9), 60, 1920, 1080),
        ("Fachada_tres_cuartos", (-10.5, -9.0, 3.2), (0.5, -2.0, 1.6), 38, 1600, 1000),
        ("Fachada_porton_detalle", (0, -5.8, 1.7), (0, -2.0, 2.0), 55, 1600, 1000),
        ("Despacho_callejon", (8.6, -1.4, 1.65), (5.4, 1.75, 1.3), 60, 1600, 1000),
        ("Despacho_toldo_alto", (8.8, -1.8, 3.6), (6.0, 1.8, 1.9), 55, 1600, 1000),
    ]
    setup_render(scene, 1920, 1080)
    for name, loc, tgt, fov, w, h in views:
        scene.render.resolution_x, scene.render.resolution_y = w, h
        render_to(scene, add_camera("Cam_" + name, loc, tgt, fov), os.path.join(PREVIEW_DIR, name + ".png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Fachada_CasaMarea.blend"))
    for k, v in stats.items():
        if isinstance(v, dict) and "triangulos" in v:
            print("[%s] tris=%d dims=%s" % (k, v["triangulos"], v["dimensiones_XxYxZ_m"]))
    print("puerta_libre:", stats["SM_Fachada_CasaMarea"]["puerta_libre"])
    print("LISTO")


main()
