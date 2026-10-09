"""Genera las tres estaciones de Casa Marea (hito 1) en Blender.

Uso (desde la raíz del proyecto):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_estaciones.py

Salidas:
    ArtSource/CasaMarea/*.blend                  fuentes
    Assets/_Game/Models/CasaMarea/*.glb          modelos para Unity
    Assets/_Game/Models/CasaMarea/*.fbx          uso directo en Unity (el .glb no se importa sin glTFast)
    ArtSource/CasaMarea/Preview/*.png            capturas
    ArtSource/CasaMarea/stats.json               triángulos, dimensiones, materiales

Convenciones: 1 u = 1 m, Z arriba, frente del modelo hacia -Y, origen en el centro de la base.
Medidas y paleta: Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md (secciones 3, 4 y 6).
"""
import bpy
import math
import os
import json
import random
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
GLB_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "CasaMarea")
FBX_DIR = GLB_DIR  # Unity no importa .glb sin glTFast: el FBX es el formato de uso directo
PREVIEW_DIR = os.path.join(HERE, "Preview")
for d in (GLB_DIR, FBX_DIR, PREVIEW_DIR):
    os.makedirs(d, exist_ok=True)

# ---------------------------------------------------------------- materiales
# nombre: (hex, rugosidad, metálico, color_emisión, fuerza_emisión)
MATS = {
    "M_Chile":              ("#E4572E", 0.55, 0.00, None, 0.0),
    "M_Caoba":              ("#8A5A3C", 0.65, 0.00, None, 0.0),
    "M_Pan":                ("#D99A4E", 0.60, 0.00, None, 0.0),
    "M_Lechuga":            ("#6FA83F", 0.60, 0.00, None, 0.0),
    "M_Jitomate":           ("#C0261F", 0.45, 0.00, None, 0.0),
    "M_Metal_Plancha":      ("#3E4A50", 0.50, 0.25, None, 0.0),
    "M_Plancha_Superficie": ("#2B3438", 0.45, 0.20, None, 0.0),
    "M_Brasa":              ("#5A2A1E", 0.50, 0.00, "#E4572E", 0.0),  # emisión apagada; Unity la anima
    "M_Turquesa":           ("#2BA6A0", 0.50, 0.00, None, 0.0),
    "M_Crema":              ("#F7EEDD", 0.60, 0.00, None, 0.0),
    "M_Mango":              ("#F4C95D", 0.40, 0.00, "#F4C95D", 0.0),  # emisión apagada; Unity la anima
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


def _finish(o, name, mat):
    o.name = name
    o.data.materials.append(get_mat(mat))
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
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


def cyl_rot(name, r, depth, loc, mat, rot, verts=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.location, o.rotation_euler = loc, rot
    return _finish(o, name, mat)


def sphere(name, r, loc, mat, scale=(1, 1, 1), seg=8, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=r)
    o = bpy.context.active_object
    o.location, o.scale = loc, scale
    return _finish(o, name, mat)


def cone(name, r1, r2, depth, loc, mat, verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    return _finish(o, name, mat)


def lettuce(name, r, loc, mat, seed=7):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=r)
    o = bpy.context.active_object
    rng = random.Random(seed)
    for v in o.data.vertices:
        v.co *= 1.0 + rng.uniform(-0.12, 0.22)
        v.co.z = max(v.co.z, -r * 0.4)
    o.location, o.scale = loc, (1.15, 1.0, 0.85)
    return _finish(o, name, mat)


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


# ---------------------------------------------------------------- modelos
def build_huacal():
    """Huacal de ingredientes: ancha, trapecio con bultos (0.90 x 0.55 x 0.50)."""
    th = math.atan(0.04 / 0.30)          # inclinación de las paredes (~7.6°)

    def hw(z): return 0.395 + 0.04 * ((z - 0.03) / 0.30)
    def hd(z): return 0.22 + 0.04 * ((z - 0.03) / 0.30)

    # patines y fondo
    for sx in (-0.33, 0.33):
        box("patin", (0.05, 0.46, 0.03), (sx, 0, 0.015), "M_Caoba")
    box("fondo", (0.78, 0.44, 0.02), (0, 0, 0.04), "M_Caoba")
    # tablillas pintadas (3 por pared)
    for zc in (0.075, 0.165, 0.255):
        w, d = hw(zc), hd(zc)
        box("tabla_frente", (2 * w, 0.025, 0.088), (0, -d, zc), "M_Chile", (th, 0, 0))
        box("tabla_fondo", (2 * w, 0.025, 0.088), (0, d, zc), "M_Chile", (-th, 0, 0))
        box("tabla_der", (0.025, 2 * d - 0.03, 0.088), (w, 0, zc), "M_Chile", (0, th, 0))
        box("tabla_izq", (0.025, 2 * d - 0.03, 0.088), (-w, 0, zc), "M_Chile", (0, -th, 0))
    # borde superior de caoba
    zr = 0.315
    w, d = hw(zr), hd(zr)
    box("borde_frente", (2 * w + 0.03, 0.03, 0.03), (0, -d, zr), "M_Caoba", (th, 0, 0))
    box("borde_fondo", (2 * w + 0.03, 0.03, 0.03), (0, d, zr), "M_Caoba", (-th, 0, 0))
    box("borde_der", (0.03, 2 * d - 0.03, 0.03), (w, 0, zr), "M_Caoba", (0, th, 0))
    box("borde_izq", (0.03, 2 * d - 0.03, 0.03), (-w, 0, zr), "M_Caoba", (0, -th, 0))
    # lecho interior donde descansan los ingredientes
    box("lecho", (0.80, 0.46, 0.03), (0, 0, 0.285), "M_Caoba")
    # pictograma de jitomate en el frente
    zp = 0.165
    yp = -(hd(zp) + 0.0125 + 0.004)
    cyl_rot("pict_disco", 0.065, 0.008, (0, yp, zp), "M_Crema", (math.pi / 2 + th, 0, 0), 10)
    cyl_rot("pict_tomate", 0.045, 0.008, (0, yp - 0.004, zp), "M_Jitomate", (math.pi / 2 + th, 0, 0), 10)
    # contenido que sobresale: panes, lechuga, jitomates
    cyl("pan_base", 0.135, 0.045, (-0.24, 0.0, 0.322), "M_Pan", verts=10)
    sphere("pan_tapa", 0.14, (-0.24, 0.0, 0.345), "M_Pan", (1, 1, 0.78), 10, 6)
    lettuce("lechuga", 0.13, (-0.01, 0.01, 0.399), "M_Lechuga")
    sphere("jitomate_1", 0.07, (0.24, -0.09, 0.355), "M_Jitomate", seg=8, rings=6)
    sphere("jitomate_2", 0.07, (0.31, 0.09, 0.345), "M_Jitomate", seg=8, rings=6)
    return finalize("SM_Huacal_Ingredientes")


def build_plancha():
    """Plancha con tubo: losa baja con un tubo vertical (1.10 x 0.60 x 0.75)."""
    for sx in (-0.50, 0.50):
        for sy in (-0.25, 0.25):
            box("pie", (0.07, 0.07, 0.04), (sx, sy, 0.02), "M_Metal_Plancha")
    box("cuerpo", (1.10, 0.56, 0.20), (0, 0, 0.14), "M_Metal_Plancha")
    box("superficie", (1.04, 0.54, 0.03), (0, 0, 0.255), "M_Plancha_Superficie")
    # reborde perimetral
    box("reborde_frente", (1.10, 0.03, 0.03), (0, -0.285, 0.285), "M_Metal_Plancha")
    box("reborde_fondo", (1.10, 0.03, 0.03), (0, 0.285, 0.285), "M_Metal_Plancha")
    for sx in (-0.535, 0.535):
        box("reborde_lado", (0.03, 0.54, 0.03), (sx, 0, 0.285), "M_Metal_Plancha")
    # frente: perillas chile y ranura de brasa (emisión apagada por defecto)
    cyl("perilla_1", 0.035, 0.02, (-0.38, -0.29, 0.15), "M_Chile", axis="Y")
    cyl("perilla_2", 0.035, 0.02, (-0.27, -0.29, 0.15), "M_Chile", axis="Y")
    box("ranura_brasa", (0.50, 0.012, 0.03), (0.15, -0.2795, 0.10), "M_Brasa")
    # tubo de extracción en la esquina trasera derecha
    cyl("tubo_base", 0.065, 0.06, (0.46, 0.235, 0.30), "M_Metal_Plancha", verts=10)
    cyl("tubo", 0.05, 0.38, (0.46, 0.235, 0.52), "M_Metal_Plancha", verts=10)
    cyl("tubo_banda", 0.055, 0.025, (0.46, 0.235, 0.56), "M_Chile", verts=10)
    cone("tubo_sombrerete", 0.065, 0.05, 0.04, (0.46, 0.235, 0.73), "M_Plancha_Superficie", verts=10)
    return finalize("SM_Plancha_Tubo")


def build_venta():
    """Punto de venta: alta, con retablo festoneado (0.90 x 0.55 x 0.80)."""
    box("base", (0.90, 0.54, 0.12), (0, 0, 0.06), "M_Turquesa")
    box("base_filete", (0.86, 0.012, 0.035), (0, -0.272, 0.095), "M_Crema")
    box("retablo", (0.90, 0.05, 0.605), (0, 0.22, 0.4225), "M_Turquesa")
    box("retablo_banda", (0.84, 0.012, 0.06), (0, 0.19, 0.60), "M_Crema")
    # festón crema: 6 arcos en el borde superior
    for i in range(6):
        cyl("feston_%d" % i, 0.075, 0.054, (-0.375 + i * 0.15, 0.22, 0.725), "M_Crema", axis="Y", verts=8)
    # riel de comandas
    cyl("riel", 0.01, 0.50, (0, 0.17, 0.50), "M_Metal_Plancha", axis="X", verts=6)
    for sx in (-0.25, 0.25):
        box("riel_soporte", (0.02, 0.04, 0.02), (sx, 0.19, 0.50), "M_Metal_Plancha")
    box("ticket_1", (0.12, 0.004, 0.20), (-0.08, 0.165, 0.39), "M_Crema", (0, 0.04, 0))
    box("ticket_2", (0.12, 0.004, 0.20), (0.10, 0.165, 0.40), "M_Crema", (0, -0.06, 0))
    # campanilla de servicio (mango) y caja de dinero
    cyl("campana_base", 0.06, 0.02, (-0.30, -0.12, 0.13), "M_Caoba", verts=8)
    sphere("campana", 0.055, (-0.30, -0.12, 0.14), "M_Mango", (1, 1, 0.85), 8, 6)
    box("caja_dinero", (0.20, 0.15, 0.10), (0.27, -0.05, 0.17), "M_Caoba")
    box("caja_pestillo", (0.03, 0.012, 0.03), (0.27, -0.131, 0.17), "M_Mango")
    return finalize("SM_PuntoVenta_Feston")


STATIONS = [
    # (función, nombre de archivo, medidas objetivo W x D x H, tope de triángulos)
    (build_huacal, "Huacal_Ingredientes", (0.90, 0.55, 0.50), 900),
    (build_plancha, "Plancha_Tubo", (1.10, 0.60, 0.75), 1100),
    (build_venta, "PuntoVenta_Feston", (0.90, 0.55, 0.80), 1000),
]


# ---------------------------------------------------------------- utilidades
def tri_count(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    e = obj.evaluated_get(dg)
    me = e.to_mesh()
    n = sum(len(p.vertices) - 2 for p in me.polygons)
    e.to_mesh_clear()
    return n


def bounds(obj):
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    s.unit_settings.system = "METRIC"
    s.unit_settings.scale_length = 1.0
    return s


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


def add_world(scene, hexcol="#DCD0BC", strength=0.75):
    w = bpy.data.worlds.new("Mundo_Preview")
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (*hexlin(hexcol), 1.0)
    bg.inputs["Strength"].default_value = strength
    scene.world = w


def add_lights(center):
    d = Vector((0.55, 0.55, -0.62))
    sun_d = bpy.data.lights.new("Sol_Tarde", "SUN")
    sun_d.energy = 3.2
    sun_d.color = hexlin("#FFC47E")
    sun = bpy.data.objects.new("Sol_Tarde", sun_d)
    bpy.context.scene.collection.objects.link(sun)
    sun.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    fill_d = bpy.data.lights.new("Relleno_Ciruela", "AREA")
    fill_d.energy = 60
    fill_d.size = 2.5
    fill_d.color = hexlin("#B79BC4")
    fill = bpy.data.objects.new("Relleno_Ciruela", fill_d)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = center + Vector((1.8, -2.2, 1.2))
    fill.rotation_euler = (center - fill.location).to_track_quat("-Z", "Y").to_euler()


def add_floor(w, d, hexcol="#E8C4A0"):
    bpy.ops.mesh.primitive_plane_add(size=1)
    f = bpy.context.active_object
    f.name = "Preview_Suelo"
    f.scale = (w, d, 1)
    f.location = (0, 0, -0.001)
    f.data.materials.append(get_mat("M_Preview_Suelo", (hexcol, 0.8, 0.0, None, 0.0)))
    return f


def add_camera(name, loc, target, lens_fov_deg=None, lens=60):
    cd = bpy.data.cameras.new(name)
    if lens_fov_deg:
        cd.sensor_fit = "VERTICAL"
        cd.angle_y = math.radians(lens_fov_deg)
    else:
        cd.lens = lens
    cam = bpy.data.objects.new(name, cd)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (target - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    return cam


def render_to(scene, cam, path):
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def preview_views(obj, name):
    scene = bpy.context.scene
    setup_render(scene, 1024, 1024)
    add_world(scene)
    lo, hi = bounds(obj)
    target = Vector(((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, (lo[2] + hi[2]) / 2))
    size = max(hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2])
    add_floor(size * 2.2, size * 2.0)
    add_lights(target)
    dist = size * 2.7
    out = []
    for tag, az, el in (("frontal", 0.0, 8.0), ("tres_cuartos", -35.0, 24.0)):
        a, e = math.radians(az), math.radians(el)
        loc = (target.x + dist * math.cos(e) * math.sin(a),
               target.y - dist * math.cos(e) * math.cos(a),
               target.z + dist * math.sin(e))
        cam = add_camera("Cam_" + tag, loc, target, lens=70)
        path = os.path.join(PREVIEW_DIR, "%s_%s.png" % (name, tag))
        render_to(scene, cam, path)
        out.append(path)
    return out


def export_glb(obj, name):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(GLB_DIR, "SM_%s.glb" % name)
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True,
                              export_yup=True, export_apply=True, export_materials="EXPORT",
                              export_cameras=False, export_lights=False)
    return path


def export_fbx(obj, name):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(FBX_DIR, "SM_%s.fbx" % name)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="COPY",
                             embed_textures=False)
    return path


# ---------------------------------------------------------------- lineup a 3 m
def lineup():
    """Las tres estaciones juntas, vistas a 3 m con la cámara del jugador (FOV 60°, ojos a 1.65 m sobre el piso,
    base de las estaciones a 0.85 m => ojos 0.80 m sobre la base)."""
    import numpy as np
    scene = reset_scene()
    xs = (-1.3, 0.0, 1.3)
    objs = []
    for (fn, name, _dims, _cap), x in zip(STATIONS, xs):
        o = fn()
        o.location.x = x
        objs.append(o)
    add_world(scene)
    floor = add_floor(4.6, 1.4)
    add_lights(Vector((0, 0, 0.3)))
    setup_render(scene, 1920, 1080)
    cam = add_camera("Cam_Jugador_3m", (0, -3.0, 0.80), Vector((0, 0, 0.30)), lens_fov_deg=60)
    tmp_color = os.path.join(PREVIEW_DIR, "_tmp_color.png")
    render_to(scene, cam, tmp_color)
    floor.hide_render = True
    scene.render.film_transparent = True
    tmp_sil = os.path.join(PREVIEW_DIR, "_tmp_sil.png")
    render_to(scene, cam, tmp_sil)

    def load(path):
        img = bpy.data.images.load(path)
        img.colorspace_settings.name = "Non-Color"
        w, h = img.size
        return np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4), w, h

    def save(arr, w, h, path):
        img = bpy.data.images.new(os.path.basename(path), w, h, alpha=False)
        img.colorspace_settings.name = "Non-Color"
        rgba = np.ones((h, w, 4), dtype=np.float32)
        rgba[..., :3] = arr[..., None] if arr.ndim == 2 else arr
        img.pixels = rgba.ravel().tolist()
        img.filepath_raw = path
        img.file_format = "PNG"
        img.save()

    color, w, h = load(tmp_color)
    lum = 0.2126 * color[..., 0] + 0.7152 * color[..., 1] + 0.0722 * color[..., 2]
    save(lum, w, h, os.path.join(PREVIEW_DIR, "Lineup_3m_grises.png"))
    save(color[..., :3], w, h, os.path.join(PREVIEW_DIR, "Lineup_3m_color.png"))
    sil, w, h = load(tmp_sil)
    mask = (sil[..., 3] > 0.5).astype(np.float32)
    save(1.0 - mask, w, h, os.path.join(PREVIEW_DIR, "Lineup_3m_silueta.png"))
    for t in (tmp_color, tmp_sil):
        os.remove(t)
    floor.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Lineup_Estaciones.blend"))


# ---------------------------------------------------------------- principal
def main():
    stats = {}
    for fn, name, target_dims, cap in STATIONS:
        reset_scene()
        obj = fn()
        tris = tri_count(obj)
        lo, hi = bounds(obj)
        dims = (hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2])
        mats = [m.name for m in obj.data.materials]
        glb = export_glb(obj, name)
        fbx = export_fbx(obj, name)
        shots = preview_views(obj, name)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "%s.blend" % name))
        stats[name] = {
            "triangulos": tris, "tope": cap,
            "dimensiones_WxDxH_m": [round(d, 3) for d in dims],
            "objetivo_WxDxH_m": list(target_dims),
            "min": [round(v, 3) for v in lo], "max": [round(v, 3) for v in hi],
            "materiales": mats, "glb": os.path.relpath(glb, PROJECT),
            "fbx": os.path.relpath(fbx, PROJECT),
            "capturas": [os.path.relpath(s, PROJECT) for s in shots],
        }
        print("[%s] tris=%d (tope %d) dims=%s materiales=%d" % (name, tris, cap, stats[name]["dimensiones_WxDxH_m"], len(mats)))
    with open(os.path.join(HERE, "stats.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)
    lineup()
    print("LISTO")


main()
