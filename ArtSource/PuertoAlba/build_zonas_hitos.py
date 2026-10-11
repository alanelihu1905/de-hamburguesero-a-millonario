"""Genera seis prefabs low-poly propios para las zonas de Puerto Alba que aún no tienen hito visible.

Uso (desde la raíz del proyecto, en el host):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/build_zonas_hitos.py

Piezas (una por zona; mapeo de zona en Docs/Handoffs/Claude/09_KIT_VISUAL_CIUDAD.md, sección 4 quinquies):
    SM_Mercado_Pabellon_Reloj   Mercado del Sol    pabellón abierto con torre de reloj
    SM_Muelle_Almacen_Grua      Muelle Farol       almacén con puerta de rodillo y grúa de pórtico
    SM_Boulevard_Terraza        Centro del Faro    terraza con pérgola, sombrillas y mesas
    SM_Taller_Brisa             Talleres Brisa     taller con chimenea y rótulo
    SM_Parque_Quiosco           Paseo Arena        quiosco octogonal con banco
    SM_Centro_Torre             Centro del Faro    torre de oficinas con retranqueo y acceso acristalado

Convenciones igual que el kit: 1 u = 1 m, Z arriba, origen en el centro de la huella (z = 0), frente hacia −Y.
Los materiales de muros, tejados, toldos, vidrios, zócalos y cornisas usan los nombres del kit; Unity los sustituye
por los materiales de zona (Models/PuertoAlba/Zonas). El resto son materiales neutros.

Salidas:
    Assets/_Game/Models/PuertoAlba/<pieza>.fbx
    ArtSource/PuertoAlba/stats_zonas_hitos.json
"""
import bpy
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba")
os.makedirs(MODEL_DIR, exist_ok=True)

# mismos colores base que el kit (build_kit_ciudad.py) para que la vista previa de Blender coincida
MATS = {
    "M_Muro_Base": ("#EADFC8", 0.80), "M_Zocalo": ("#6E8FA0", 0.70), "M_Cornisa": ("#B9663F", 0.75),
    "M_Marco": ("#F4F1EA", 0.55), "M_Madera": ("#8A5A3C", 0.65), "M_Puerta": ("#2F6F73", 0.55),
    "M_Vidrio": ("#5FA9B8", 0.15), "M_Toldo": ("#D9573B", 0.70), "M_Metal_Oscuro": ("#3C4048", 0.45),
    "M_Tejado": ("#8E4A2F", 0.80), "M_Letrero_Fondo": ("#3B2F4A", 0.60), "M_Letrero_Texto": ("#F4C95D", 0.50),
}

# nombre: (triángulos máximos, dimensiones esperadas X × Y × Z en Blender)
PIEZAS = {
    "SM_Mercado_Pabellon_Reloj": (900, (10.6, 8.6, 9.4)),
    "SM_Muelle_Almacen_Grua": (900, (14.1, 6.4, 9.6)),   # alero hasta x = -7.2; viga de la grúa hasta x = 6.9
    "SM_Boulevard_Terraza": (700, (6.2, 4.0, 3.11)),
    "SM_Taller_Brisa": (900, (8.4, 6.4, 7.5)),   # 700 + rótulo TALLER en bloques (~710 tris)
    "SM_Parque_Quiosco": (600, (4.6, 5.45, 4.0)),   # alero r = 2.3; banco hasta y = -3.15
    "SM_Centro_Torre": (900, (8.0, 9.0, 28.3)),          # marquesina de acceso hasta y = -5.0
}


# glifos 5 × 7 para los rótulos de estas piezas (mismo estilo que el kit)
GLIFOS = {
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "L": ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
}


def rotulo(texto, cx, cz, celda, y, depth, mat):
    """Texto en bloques centrado en (cx, cz) sobre un plano de normal −Y. Cada racha de celdas es una caja."""
    cols = 5 * len(texto) + (len(texto) - 1)
    x0 = cx - cols * celda / 2.0
    z_top = cz + 7 * celda / 2.0
    cajas = []
    for i, letra in enumerate(texto):
        ox = i * 6
        for r, fila in enumerate(GLIFOS[letra]):
            c = 0
            while c < 5:
                if fila[c] != "#":
                    c += 1
                    continue
                ini = c
                while c < 5 and fila[c] == "#":
                    c += 1
                ancho = (c - ini) * celda
                cajas.append(box((ancho, depth, celda), (x0 + (ox + ini) * celda + ancho / 2.0, y, z_top - (r + .5) * celda), mat))
    return cajas


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


def cyl(r, depth, loc, mat, verts=12, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.location, o.rotation_euler = loc, rot
    o.data.materials.append(get_mat(mat))
    _apply(o)
    return o


def cone(r1, depth, loc, mat, verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=0.0, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    o.data.materials.append(get_mat(mat))
    _apply(o)
    return o


def join(parts, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    return obj


# ------------------------------------------------------------------ piezas

def build_mercado():
    p = [
        box((10.0, 8.0, 0.4), (0, 0, 0.2), "M_Zocalo"),                   # plataforma
        box((10.6, 8.6, 0.12), (0, 0, 4.36), "M_Cornisa"),                # alero del pabellón (4.3–4.42)
        box((10.4, 8.4, 0.3), (0, 0, 4.15), "M_Tejado"),                  # cubierta 4.0–4.3
    ]
    for x in (-4.4, 4.4):
        for y in (-3.4, 3.4):
            p.append(cyl(0.15, 4.0, (x, y, 2.0), "M_Metal_Oscuro", verts=8))   # pilares 0–4
    # torre de reloj al fondo (+Y): fuste, remate y esfera del reloj hacia el frente (−Y)
    p.append(box((2.2, 2.2, 9.0), (0, 3.0, 4.5), "M_Muro_Base"))          # 0–9
    p.append(box((2.6, 2.6, 0.4), (0, 3.0, 9.2), "M_Tejado"))              # remate 9.0–9.4
    p.append(cyl(0.9, 0.12, (0, 1.85, 6.8), "M_Marco", verts=16, rot=(math.radians(90), 0, 0)))  # esfera
    p.append(box((0.08, 0.05, 0.6), (0, 1.8, 6.9), "M_Metal_Oscuro"))     # manecilla de horas
    p.append(box((0.05, 0.03, 0.8), (0.25, 1.76, 6.95), "M_Metal_Oscuro"))  # manecilla de minutos
    for h in range(12):
        ang = 2 * math.pi * h / 12.0
        # marca radial en la cara de la esfera (plano XZ, frente −Y): gira alrededor de Y
        p.append(box((0.05, 0.02, 0.12), (0.74 * math.sin(ang), 1.77, 6.8 + 0.74 * math.cos(ang)), "M_Letrero_Fondo",
                     rot=(0, ang, 0)))
    return join(p, "SM_Mercado_Pabellon_Reloj")


def build_muelle():
    p = [
        box((10.0, 6.0, 4.5), (-2, 0, 2.25), "M_Muro_Base"),              # almacén 0–4.5
        box((10.4, 6.4, 0.3), (-2, 0, 4.65), "M_Tejado"),                 # cubierta 4.5–4.8
        box((3.0, 0.06, 3.0), (-2, -3.03, 1.5), "M_Metal_Oscuro"),        # puerta de rodillo al frente
        box((10.0, 0.04, 0.5), (-2, -3.02, 4.2), "M_Cornisa"),            # franja
    ]
    for y in (-2.5, 2.5):
        p.append(box((0.5, 0.5, 9.0), (6.5, y, 4.5), "M_Metal_Oscuro"))   # patas de la grúa 0–9
    p.append(box((0.8, 6.0, 0.8), (6.5, 0, 9.2), "M_Metal_Oscuro"))       # viga 8.8–9.6
    p.append(box((0.06, 0.06, 2.0), (6.5, 1.0, 7.6), "M_Metal_Oscuro"))   # cable
    p.append(box((0.3, 0.3, 0.3), (6.5, 1.0, 6.6), "M_Cornisa"))          # gancho
    return join(p, "SM_Muelle_Almacen_Grua")


def build_terraza():
    p = []
    for x in (-2.8, 2.8):
        for y in (-1.8, 1.8):
            p.append(cyl(0.08, 3.0, (x, y, 1.5), "M_Madera", verts=8))    # postes 0–3
    for y in (-1.8, -0.9, 0.0, 0.9, 1.8):
        p.append(box((6.2, 0.15, 0.12), (0, y, 3.05), "M_Madera"))        # vigas 3.0–3.11
    p.append(box((6.0, 0.05, 0.9), (0, -2.1, 0.45), "M_Vidrio"))          # barandilla al frente
    for x in (-1.5, 1.5):
        p.append(cyl(0.04, 1.6, (x, 0.6, 0.8), "M_Metal_Oscuro", verts=6))  # mástil
        p.append(cone(1.2, 0.5, (x, 0.6, 2.4), "M_Toldo"))                 # sombrilla 2.15–2.65
        p.append(cyl(0.45, 0.06, (x, 0.6, 0.75), "M_Madera"))              # mesa
    return join(p, "SM_Boulevard_Terraza")


def build_taller():
    p = [
        box((8.0, 6.0, 4.2), (0, 0, 2.1), "M_Muro_Base"),                 # nave 0–4.2
        box((8.4, 6.4, 0.3), (0, 0, 4.35), "M_Tejado"),                   # cubierta 4.2–4.5
        box((3.5, 0.08, 3.0), (-1.5, -3.04, 1.5), "M_Metal_Oscuro"),      # puerta de rodillo
        box((5.0, 0.1, 0.6), (0, -3.05, 3.6), "M_Letrero_Fondo"),         # rótulo
        cyl(0.35, 3.0, (2.8, 2.0, 6.0), "M_Muro_Base", verts=10),          # chimenea 4.5–7.5
    ]
    p += rotulo("TALLER", 0, 3.6, 0.055, -3.11, 0.02, "M_Letrero_Texto")  # rótulo en bloques sobre el panel
    return join(p, "SM_Taller_Brisa")


def build_quiosco():
    p = [
        cyl(2.0, 0.4, (0, 0, 0.2), "M_Zocalo", verts=8),                  # base 0–0.4
        cyl(1.8, 2.6, (0, 0, 1.7), "M_Muro_Base", verts=8),               # muros 0.4–3.0
        cone(2.3, 1.0, (0, 0, 3.5), "M_Tejado", verts=8),                 # cubierta 3.0–4.0
        box((0.9, 0.06, 2.0), (0, -1.8, 1.4), "M_Puerta"),                # abertura al frente
        box((1.6, 0.5, 0.45), (0, -2.9, 0.225), "M_Madera"),              # banco al frente
    ]
    return join(p, "SM_Parque_Quiosco")


def build_torre():
    p = [
        box((8.0, 8.0, 24.0), (0, 0, 12.0), "M_Muro_Base"),               # fuste 0–24
        box((6.0, 6.0, 4.0), (0, 0, 26.0), "M_Muro_Base"),                # retranqueo 24–28
        box((6.4, 6.4, 0.3), (0, 0, 28.15), "M_Tejado"),                  # remate 28.0–28.3
        box((6.0, 0.05, 22.0), (0, -4.03, 12.0), "M_Vidrio"),             # franja acristalada al frente
        box((4.0, 0.1, 3.0), (0, -4.0, 1.5), "M_Vidrio"),                 # acceso acristalado 0–3
        box((5.0, 1.0, 0.2), (0, -4.5, 3.0), "M_Cornisa"),                # marquesina del acceso (y -5.0 a -4.0)
    ]
    return join(p, "SM_Centro_Torre")


BUILDERS = {
    "SM_Mercado_Pabellon_Reloj": build_mercado,
    "SM_Muelle_Almacen_Grua": build_muelle,
    "SM_Boulevard_Terraza": build_terraza,
    "SM_Taller_Brisa": build_taller,
    "SM_Parque_Quiosco": build_quiosco,
    "SM_Centro_Torre": build_torre,
}


def bounds(obj):
    # desde los vértices de la malla: bound_box puede quedar desactualizado tras transformar los datos
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))


def tri_count(obj):
    return sum(max(0, len(p.vertices) - 2) for p in obj.data.polygons)


def export_fbx(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(MODEL_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=False)
    return path


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for n in MATS:
        get_mat(n)
    stats, fallos = {}, []
    for nombre, build in BUILDERS.items():
        obj = build()
        budget, esperado = PIEZAS[nombre]
        # centra la huella en X (el origen queda en el centro del ancho); no cambia medidas ni la base
        (x0, x1), _, _ = bounds(obj)
        if abs((x0 + x1) / 2) > 1e-6:
            obj.data.transform(__import__("mathutils").Matrix.Translation((-(x0 + x1) / 2, 0, 0)))
            obj.data.update()
        (x0, x1), (y0, y1), (z0, z1) = bounds(obj)
        dims = (round(x1 - x0, 3), round(y1 - y0, 3), round(z1 - z0, 3))
        tris = tri_count(obj)
        if any(abs(a - b) > 0.02 for a, b in zip(dims, esperado)):
            fallos.append("%s: medido %s, esperado %s" % (nombre, dims, esperado))
        if abs(z0) > 1e-4:
            fallos.append("%s: base fuera de z = 0 (%.4f)" % (nombre, z0))
        if tris > budget:
            fallos.append("%s: %d triángulos (tope %d)" % (nombre, tris, budget))
        stats[nombre] = {
            "triangulos": tris, "presupuesto_triangulos": budget, "dimensiones_XxYxZ_m": list(dims),
            "base_z": round(z0, 4), "pivote_correcto": abs(z0) < 1e-4 and abs((x0 + x1) / 2) < 1e-3,
            "bbox_min_XYZ_m": [round(x0, 4), round(y0, 4), round(z0, 4)], "bbox_max_XYZ_m": [round(x1, 4), round(y1, 4), round(z1, 4)],
            "materiales": sorted({s.material.name for s in obj.material_slots if s.material}),
            "fbx": os.path.relpath(export_fbx(obj), PROJECT),
        }
    with open(os.path.join(HERE, "stats_zonas_hitos.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, ensure_ascii=False, indent=2)
    if fallos:
        raise SystemExit("COMPROBACIONES FALLIDAS:\n" + "\n".join(fallos))
    print("LISTO_ZONAS_HITOS", len(stats), "piezas")


main()
