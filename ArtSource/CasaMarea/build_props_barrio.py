"""Genera los props del barrio Casa Marea (tanda 1) en Blender.

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_props_barrio.py

Props: banco de calle, maceta tropical, farol, cubeta (con hielo y huachinango) y cartel de precios.
Paleta y materiales: Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md, sección 4.

Salidas:
    ArtSource/CasaMarea/Props_Barrio.blend                    fuente (los cinco props alineados para el lineup)
    Assets/_Game/Models/CasaMarea/SM_Banco_Calle.fbx
    Assets/_Game/Models/CasaMarea/SM_Maceta_Tropical.fbx
    Assets/_Game/Models/CasaMarea/SM_Farol_Calle.fbx
    Assets/_Game/Models/CasaMarea/SM_Cubeta.fbx
    Assets/_Game/Models/CasaMarea/SM_Cartel_Precios.fbx       (incluye SM_Cartel_Texto, plano para la textura de precios)
    ArtSource/CasaMarea/Preview/Props_lineup_*.png            frontal, tres cuartos y silueta a 3 m
    ArtSource/CasaMarea/stats_props.json                      triángulos, dimensiones, pivote y materiales

Convenciones: 1 u = 1 m, Z arriba, frente hacia -Y, origen en el centro de la huella sobre el suelo (z = 0).
Cada FBX se exporta con el prop en el origen; solo después se desplaza para el lineup.
"""
import bpy
import bmesh
import math
import os
import json

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "CasaMarea")
PREVIEW_DIR = os.path.join(HERE, "Preview")
for d in (MODEL_DIR, PREVIEW_DIR):
    os.makedirs(d, exist_ok=True)

# ---------------------------------------------------------------- materiales
# nombre: (hex, rugosidad, metálico, color_emisión, fuerza_emisión)
MATS = {
    "M_Caoba":         ("#8A5A3C", 0.65, 0.00, None, 0.0),
    "M_Herreria":      ("#2E2538", 0.70, 0.10, None, 0.0),
    "M_Chile":         ("#E4572E", 0.55, 0.00, None, 0.0),
    "M_Crema":         ("#F7EEDD", 0.60, 0.00, None, 0.0),
    "M_Turquesa":      ("#2BA6A0", 0.50, 0.00, None, 0.0),
    "M_Mango":         ("#F4C95D", 0.40, 0.00, "#F4C95D", 0.0),   # emisión apagada; Unity la anima (farol)
    "M_Piedra_Caliza": ("#CDBFA4", 0.90, 0.00, None, 0.0),
    "M_Barro":         ("#B5623B", 0.85, 0.00, None, 0.0),        # maceta de barro: chile desaturado
    "M_Vegetacion":    ("#4F7A3A", 0.60, 0.00, None, 0.0),
    "M_Hoja_Clara":    ("#7BAF4B", 0.60, 0.00, None, 0.0),
    "M_Pizarra":       ("#3B2F4A", 0.80, 0.00, None, 0.0),        # ciruela del documento 04
    "M_Cartel_Texto":  ("#3B2F4A", 0.80, 0.00, None, 0.0),        # solo el plano del texto; Unity pone la textura
}

# presupuesto de triángulos por prop (doc. 04: banca 250; el resto es propuesta de esta tanda)
BUDGET = {"SM_Banco_Calle": 250, "SM_Maceta_Tropical": 600, "SM_Farol_Calle": 400,
          "SM_Cubeta": 300, "SM_Cartel_Precios": 400}


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
PARTS = []      # piezas del modelo principal
EXTRA = []      # piezas del plano de texto (objeto aparte)


def _apply(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def _finish(o, name, mat, bucket=None):
    o.name = name
    o.data.materials.append(get_mat(mat))
    _apply(o)
    (PARTS if bucket is None else bucket).append(o)
    return o


def box(name, size, loc, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.scale, o.rotation_euler, o.location = size, rot, loc
    return _finish(o, name, mat)


def cyl(name, r, depth, loc, mat, verts=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    return _finish(o, name, mat)


def cone(name, r1, r2, depth, loc, mat, verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    return _finish(o, name, mat)


def sphere(name, r, loc, mat, scale=(1, 1, 1), seg=8, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=r)
    o = bpy.context.active_object
    o.location, o.scale = loc, scale
    return _finish(o, name, mat)


def prism(name, pts, thick, center, mat, plane="XZ", rot=(0, 0, 0)):
    """Prisma extruido de un polígono CONVEXO. plane 'XZ': (u,v)=(x,z), grosor en Y. plane 'YZ': (u,v)=(y,z), grosor en X.
    center = (x, y, z) del centro del grosor; rot gira el prisma alrededor de ese centro."""
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
    o.location, o.rotation_euler = center, rot
    return _finish(o, name, mat)


def text_plane(name, size, loc, rot, mat):
    """Plano de 2 triángulos con UV 0..1 para una textura (se junta aparte del modelo principal)."""
    bpy.ops.mesh.primitive_plane_add(size=1)
    o = bpy.context.active_object
    o.scale, o.rotation_euler, o.location = (size[0], size[1], 1.0), rot, loc
    return _finish(o, name, mat, EXTRA)


def rotate_group(objs, pivot, rx=0.0, ry=0.0, rz=0.0):
    """Gira mallas ya aplicadas (coordenadas de mundo) alrededor de un pivote; orden XYZ como Blender."""
    cx, sx, cy, sy, cz, sz = math.cos(rx), math.sin(rx), math.cos(ry), math.sin(ry), math.cos(rz), math.sin(rz)
    px, py, pz = pivot
    for o in objs:
        for v in o.data.vertices:
            x, y, z = v.co.x - px, v.co.y - py, v.co.z - pz
            y, z = y * cx - z * sx, y * sx + z * cx            # X
            x, z = x * cy + z * sy, -x * sy + z * cy           # Y
            x, y = x * cz - y * sz, x * sz + y * cz            # Z
            v.co.x, v.co.y, v.co.z = x + px, y + py, z + pz
        o.data.update()


def join(parts, name):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    o.data.name = name + "_Mesh"
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))
    except Exception:
        bpy.ops.object.shade_smooth()
    return o


def finalize(name):
    o = join(PARTS, name)
    PARTS.clear()
    return o


def pbox(plane, c, su, sv, thick, mat):
    u, v, w = c
    if plane == "XZ":
        return box("icono_caja", (su, thick, sv), (u, w, v), mat)
    return box("icono_caja", (thick, su, sv), (w, u, v), mat)


def burger_icon(plane, cu, cv, w, s, thick=0.02):
    """Icono plano de hamburguesa (pan mango, salsa chile, carne caoba, base mango)."""
    pbox(plane, (cu, cv - 0.075 * s, w), 0.26 * s, 0.040 * s, thick, "M_Mango")
    pbox(plane, (cu, cv - 0.030 * s, w), 0.28 * s, 0.045 * s, thick, "M_Caoba")
    pbox(plane, (cu, cv + 0.003 * s, w), 0.29 * s, 0.018 * s, thick, "M_Chile")
    pts = [(-0.13 * s, 0), (-0.11 * s, 0.05 * s), (-0.06 * s, 0.085 * s),
           (0.06 * s, 0.085 * s), (0.11 * s, 0.05 * s), (0.13 * s, 0)]
    c = (cu, w, cv + 0.014 * s) if plane == "XZ" else (w, cu, cv + 0.014 * s)
    prism("icono_pan", pts, thick, c, "M_Mango", plane)


# ---------------------------------------------------------------- props
def build_banco():
    """Banco de calle: asiento y respaldo de tablones de caoba sobre armazón de herrería (1.50 x 0.50 x 0.85).
    El asiento da al frente (-Y); el respaldo queda en +Y."""
    for i, y in enumerate((-0.17, -0.055, 0.055, 0.17)):
        box("asiento_%d" % i, (1.50, 0.10, 0.04), (0, y, 0.43), "M_Caoba")
    for i, z in enumerate((0.58, 0.69, 0.80)):
        box("respaldo_%d" % i, (1.50, 0.03, 0.10), (0, 0.225, z), "M_Caoba")
    for sx in (-0.65, 0.65):
        box("poste_trasero", (0.05, 0.05, 0.82), (sx, 0.225, 0.41), "M_Herreria")
        box("pata_delantera", (0.05, 0.05, 0.41), (sx, -0.20, 0.205), "M_Herreria")
        box("travesano", (0.04, 0.48, 0.04), (sx, 0.0, 0.37), "M_Herreria")
        box("remate", (0.07, 0.05, 0.03), (sx, 0.225, 0.835), "M_Chile")
    return finalize("SM_Banco_Calle")


def build_maceta():
    """Maceta de barro con planta tropical de hojas anchas y cuatro flores (≈ 0.9 x 0.9 x 0.95)."""
    cone("maceta", 0.14, 0.20, 0.30, (0, 0, 0.15), "M_Barro", verts=12)
    cyl("maceta_borde", 0.215, 0.05, (0, 0, 0.325), "M_Barro", verts=12)
    cyl("tierra", 0.19, 0.02, (0, 0, 0.345), "M_Caoba", verts=12)
    base_z = 0.355
    # (azimut°, inclinación°, largo, ancho, material)
    leaves = [(0, 38, 0.55, 0.22, "M_Vegetacion"), (42, 58, 0.45, 0.19, "M_Hoja_Clara"),
              (84, 34, 0.58, 0.23, "M_Vegetacion"), (128, 60, 0.42, 0.18, "M_Hoja_Clara"),
              (170, 40, 0.54, 0.21, "M_Vegetacion"), (214, 55, 0.46, 0.20, "M_Hoja_Clara"),
              (256, 36, 0.57, 0.23, "M_Vegetacion"), (298, 58, 0.44, 0.19, "M_Hoja_Clara"),
              (338, 44, 0.50, 0.21, "M_Vegetacion")]
    tips = []
    for i, (az, inc, L, W, mat) in enumerate(leaves):
        yaw, pitch = math.radians(az), math.radians(inc)
        pts = [(0, 0), (W * 0.5, L * 0.3), (W * 0.35, L * 0.75), (0, L), (-W * 0.35, L * 0.75), (-W * 0.5, L * 0.3)]
        prism("hoja_%d" % i, pts, 0.025, (0, 0, base_z), mat, "XZ", (pitch, 0, yaw))
        tips.append((L * math.sin(pitch) * math.sin(yaw), -L * math.sin(pitch) * math.cos(yaw),
                     base_z + L * math.cos(pitch)))
    for k, i in enumerate((1, 3, 5, 7)):
        x, y, z = tips[i]
        sphere("flor_%d" % k, 0.045, (x, y, z), "M_Chile" if k % 2 == 0 else "M_Crema", seg=6, rings=4)
    return finalize("SM_Maceta_Tropical")


def build_farol():
    """Farol de calle de hierro con pedestal de caliza y linterna de cristal mango (0.42 x 0.42 x 2.78).
    El cristal usa M_Mango con emisión apagada: Unity la enciende al atardecer."""
    box("pedestal", (0.30, 0.30, 0.35), (0, 0, 0.175), "M_Piedra_Caliza")
    box("pedestal_remate", (0.22, 0.22, 0.06), (0, 0, 0.38), "M_Herreria")
    cyl("poste", 0.035, 1.87, (0, 0, 1.345), "M_Herreria", verts=8)
    cyl("poste_banda_1", 0.05, 0.04, (0, 0, 0.60), "M_Herreria", verts=8)
    cyl("poste_banda_2", 0.05, 0.04, (0, 0, 2.15), "M_Chile", verts=8)
    box("linterna_base", (0.26, 0.26, 0.03), (0, 0, 2.265), "M_Herreria")
    cyl("linterna_cristal", 0.15, 0.30, (0, 0, 2.43), "M_Mango", verts=8)
    for k in range(8):
        a = math.radians(45 * k)
        box("linterna_costilla", (0.025, 0.025, 0.32), (0.15 * math.cos(a), 0.15 * math.sin(a), 2.43), "M_Herreria",
            (0, 0, a))
    cone("linterna_techo", 0.21, 0.03, 0.14, (0, 0, 2.65), "M_Herreria", verts=8)
    sphere("remate", 0.03, (0, 0, 2.75), "M_Herreria", seg=6, rings=4)
    return finalize("SM_Farol_Calle")


def build_cubeta():
    """Cubeta turquesa con hielo y un huachinango sobre el hielo (0.37 x 0.35 x 0.42)."""
    cone("cubeta", 0.12, 0.17, 0.30, (0, 0, 0.15), "M_Turquesa", verts=12)
    cyl("cubeta_borde", 0.175, 0.025, (0, 0, 0.3125), "M_Crema", verts=12)
    sphere("hielo", 0.15, (0, 0, 0.325), "M_Crema", (1, 1, 0.45), 8, 4)
    cuerpo = [(-0.10, 0), (-0.06, 0.045), (0.05, 0.045), (0.10, 0), (0.05, -0.045), (-0.06, -0.045)]
    prism("pez_cuerpo", cuerpo, 0.02, (0.02, 0.0, 0.385), "M_Chile", "XZ", (math.pi / 2, 0, math.radians(25)))
    prism("pez_cola", [(-0.09, 0), (-0.16, 0.05), (-0.16, -0.05)], 0.02, (0.02, 0.0, 0.385), "M_Chile", "XZ",
          (math.pi / 2, 0, math.radians(25)))
    # asa de herrería: elipse de 6 segmentos (semieje x 0.17, z 0.12) anclada en dos orejas
    a, b, z0 = 0.17, 0.12, 0.30
    pts = [(a * math.cos(math.pi * k / 6), z0 + b * math.sin(math.pi * k / 6)) for k in range(7)]
    for k in range(6):
        (x1, z1), (x2, z2) = pts[k], pts[k + 1]
        dx, dz = x2 - x1, z2 - z1
        length = math.hypot(dx, dz)
        beta = math.atan2(-dz, dx)
        box("asa_%d" % k, (length, 0.012, 0.012), ((x1 + x2) / 2, 0.0, (z1 + z2) / 2), "M_Herreria", (0, beta, 0))
    for sx in (-1, 1):
        box("asa_oreja", (0.03, 0.03, 0.04), (sx * 0.17, 0.0, 0.30), "M_Herreria")
    return finalize("SM_Cubeta")


def build_cartel():
    """Cartel de precios tipo caballete: dos paneles de caoba con encabezado turquesa e iconos, y pizarra ciruela
    (0.62 x ≈0.45 x 0.97). SM_Cartel_Texto es un plano por cara donde Unity pone la textura de precios."""
    theta = math.radians(12)
    y_base = 0.20
    for sign in (-1, 1):                       # -1 = cara delantera (mira a -Y), +1 = cara trasera
        n0 = len(PARTS)
        y0 = sign * y_base
        yo = y0 + sign * 0.035                 # cara exterior de la pizarra
        box("marco", (0.60, 0.05, 0.95), (0, y0, 0.475), "M_Caoba")
        box("pizarra", (0.50, 0.02, 0.62), (0, yo, 0.40), "M_Pizarra")
        box("encabezado", (0.50, 0.02, 0.16), (0, yo, 0.795), "M_Turquesa")
        for sx in (-1, 1):
            burger_icon("XZ", sx * 0.19, 0.795, yo + sign * 0.02, 0.7)
        text_plane("texto_%d" % sign, (0.44, 0.54), (0, yo + sign * 0.0115, 0.40),
                   (-sign * math.pi / 2, 0, 0), "M_Cartel_Texto")
        group = PARTS[n0:] + EXTRA[(0 if sign == -1 else 1):]
        # la cara delantera se inclina hacia +Y y la trasera hacia -Y, de modo que las puntas se tocan arriba
        rotate_group(group, (0, y0 - sign * 0.025, 0.0), rx=(-theta if sign == -1 else theta))
    box("remate_apice", (0.62, 0.08, 0.04), (0, 0.0, 0.945), "M_Caoba")
    texto = join(EXTRA, "SM_Cartel_Texto")
    EXTRA.clear()
    cartel = finalize("SM_Cartel_Precios")
    return cartel, texto


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


def stat(obj, budget=None):
    lo, hi = bounds(obj)
    tris = tri_count(obj)
    d = {"triangulos": tris,
         "dimensiones_XxYxZ_m": [round(hi[i] - lo[i], 3) for i in range(3)],
         "min": [round(v, 3) for v in lo], "max": [round(v, 3) for v in hi],
         "pivote": {"desfase_centro_XY_m": [round((lo[0] + hi[0]) / 2, 3), round((lo[1] + hi[1]) / 2, 3)],
                    "base_z_m": round(lo[2], 4), "base_en_cero": abs(lo[2]) < 0.001},
         "materiales": [m.name for m in obj.data.materials]}
    if budget is not None:
        d["presupuesto_triangulos"] = budget
        d["dentro_de_presupuesto"] = tris <= budget
    return d


def setup_render(scene, w, h, transparent=False):
    r = scene.render
    r.resolution_x, r.resolution_y, r.resolution_percentage = w, h, 100
    r.image_settings.file_format = "PNG"
    r.image_settings.color_mode = "RGBA"
    r.film_transparent = transparent
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
    from mathutils import Vector
    d = Vector((0.555, 0.667, -0.5))                         # sol del documento 04: viaja hacia el noreste
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


def add_camera(name, loc, target, fov_deg):
    from mathutils import Vector
    cd = bpy.data.cameras.new(name)
    cd.sensor_fit = "VERTICAL"
    cd.angle_y = math.radians(fov_deg)
    cam = bpy.data.objects.new(name, cd)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = Vector(loc)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
    return cam


def ctx_box(name, size, loc, hexcol):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.name = "Preview_" + name
    o.scale, o.location = size, loc
    o.data.materials.append(get_mat("M_Preview_" + hexcol.lstrip("#"), (hexcol, 0.9, 0.0, None, 0.0)))
    return o


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


# ---------------------------------------------------------------- principal
LINEUP_X = {"SM_Banco_Calle": -2.30, "SM_Maceta_Tropical": -1.15, "SM_Farol_Calle": -0.30,
            "SM_Cubeta": 0.50, "SM_Cartel_Precios": 1.40}


def main():
    import numpy as np
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0

    banco = build_banco()
    maceta = build_maceta()
    farol = build_farol()
    cubeta = build_cubeta()
    cartel, cartel_texto = build_cartel()

    stats, groups = {}, {}
    for obj, extra in ((banco, []), (maceta, []), (farol, []), (cubeta, []), (cartel, [cartel_texto])):
        stats[obj.name] = stat(obj, BUDGET[obj.name])
        if extra:
            stats[obj.name]["objeto_extra"] = {extra[0].name: stat(extra[0])}
        stats[obj.name]["fbx"] = os.path.relpath(export_fbx([obj] + extra, obj.name), PROJECT)   # en el origen
        groups[obj.name] = [obj] + extra
    stats["paleta"] = {k: v[0] for k, v in MATS.items()}
    stats["materiales_unicos_en_la_tanda"] = sorted({m for k, v in stats.items() if k.startswith("SM_")
                                                    for m in v["materiales"]})
    with open(os.path.join(HERE, "stats_props.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)

    # lineup: ahora sí se desplazan (el FBX ya está escrito con cada prop en el origen)
    for name, objs in groups.items():
        for o in objs:
            o.location.x = LINEUP_X[name]
    xc = -0.45
    ctx_box("Suelo", (12, 6, 0.1), (xc, 0, -0.05), "#B8A78C")
    pilar = ctx_box("Escala_1.8m", (0.12, 0.12, 1.8), (-3.75, 0.5, 0.9), "#8C8C8C")
    add_world(scene)
    add_lights()
    setup_render(scene, 1920, 1080)
    render_to(scene, add_camera("Cam_frontal", (xc, -10.0, 1.35), (xc, 0, 1.3), 28),
              os.path.join(PREVIEW_DIR, "Props_lineup_frontal.png"))
    render_to(scene, add_camera("Cam_tres_cuartos", (xc - 6.0, -8.5, 2.8), (xc, 0, 1.0), 32),
              os.path.join(PREVIEW_DIR, "Props_lineup_tres_cuartos.png"))
    # silueta a 3 m con la cámara del jugador (ojos a 1.65 m, FOV 60°): negro sobre blanco, solo los props
    for o in bpy.data.objects:
        if o.name.startswith("Preview_"):
            o.hide_render = True
    scene.render.film_transparent = True
    tmp = os.path.join(PREVIEW_DIR, "_tmp_sil.png")
    render_to(scene, add_camera("Cam_3m", (xc, -3.0, 1.65), (xc, 0, 1.55), 60), tmp)
    img = bpy.data.images.load(tmp)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    mask = (np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[..., 3] > 0.5).astype(np.float32)
    out = bpy.data.images.new("Props_lineup_silueta_3m.png", w, h, alpha=False)
    out.colorspace_settings.name = "Non-Color"
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., :3] = (1.0 - mask)[..., None]
    out.pixels = rgba.ravel().tolist()
    out.filepath_raw = os.path.join(PREVIEW_DIR, "Props_lineup_silueta_3m.png")
    out.file_format = "PNG"
    out.save()
    os.remove(tmp)
    for o in bpy.data.objects:
        if o.name.startswith("Preview_"):
            o.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Props_Barrio.blend"))
    for k, v in stats.items():
        if k.startswith("SM_"):
            print("[%s] tris=%d (tope %d) dims=%s pivote=%s" % (k, v["triangulos"], v["presupuesto_triangulos"],
                                                                  v["dimensiones_XxYxZ_m"], v["pivote"]))
    print("LISTO")


if __name__ == "__main__":
    main()
