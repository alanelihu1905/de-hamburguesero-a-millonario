"""Kit modular de sala del Casino FOX: muros con zócalo, esquinas, puertas de ENTRADA y SALIDA, techo con vigas,
mostrador de cambio y taburete. Cierra el interior (defecto DEF-01 del QA) y añade el mobiliario de apoyo (DEF-03).

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU para las vistas):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/Casino/build_casino_sala.py

ARTE, NO MECÁNICA. No hay apuestas, cobros ni RNG. El mostrador de cambio es una forma estática con su letrero.

Convenciones: 1 u = 1 m, Z arriba. Origen en el centro de la huella, base en z = 0. La CARA INTERIOR de muros y puertas
mira a −Y en Blender (a +Z en Unity), igual que el frente de todo el kit.
Muros y puertas: 4 m de ancho × 5 m de alto. El techo es de 4 × 4 m; su origen z = 0 es la cara inferior de las vigas
(en la sala se coloca a 4.6 m, de modo que su techo queda a 5.0 m, la altura de los muros).

Salidas:
    Assets/_Game/Models/PuertoAlba/Casino/Sala/<pieza>.fbx
    ArtSource/PuertoAlba/Casino/stats_sala.json
    ArtSource/PuertoAlba/Preview/Casino_sala/<pieza>_tres_cuartos.png
"""
import bpy
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba", "Casino", "Sala")
PREVIEW_DIR = os.path.join(HERE, "..", "Preview", "Casino_sala")
for d in (MODEL_DIR, PREVIEW_DIR):
    os.makedirs(d, exist_ok=True)

# los cuatro primeros mantienen los nombres del kit del casino: Unity los sustituye por la variante rojo o dorado
MATS = {
    "M_Casino_Rojo": ("#8E1B2B", 0.55), "M_Casino_Oro": ("#C9A227", 0.35), "M_Fieltro": ("#7A1420", 0.90),
    "M_Madera": ("#4A2A1E", 0.65), "M_Vidrio": ("#5FA9B8", 0.15), "M_Negro": ("#15171C", 0.30),
    "M_Blanco": ("#F4F1EA", 0.50), "M_Letrero_Texto": ("#F4C95D", 0.40),
    "M_Entrada_Verde": ("#1E8A4A", 0.50),     # tablero del letrero ENTRADA
    "M_Salida_Rojo": ("#C0262D", 0.50),       # tablero del letrero SALIDA
}

# pieza -> (presupuesto de triángulos, rango X, rango Y, rango Z) en Blender
PIEZAS = {
    "SM_Sala_Muro": (400, (3.98, 4.02), (0.38, 0.42), (4.98, 5.02)),
    "SM_Sala_Esquina": (300, (0.88, 0.92), (0.88, 0.92), (4.98, 5.02)),
    "SM_Sala_Puerta_Entrada": (1500, (3.98, 4.02), (1.40, 1.70), (4.98, 5.02)),
    "SM_Sala_Puerta_Salida": (1500, (3.98, 4.02), (1.40, 1.70), (4.98, 5.02)),
    "SM_Sala_Techo": (400, (3.98, 4.02), (3.98, 4.02), (0.38, 0.42)),
    "SM_Mostrador_Cambio": (1500, (3.18, 3.22), (0.98, 1.02), (2.40, 2.50)),
    "SM_Taburete": (400, (0.41, 0.47), (0.41, 0.47), (0.66, 0.72)),
}

GLIFOS = {
    "C": [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "I": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####"],
    "N": ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "D": ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
    "L": ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
    "M": ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"],
    "B": ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
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


def cyl(r, depth, loc, mat, verts=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.location = loc
    o.data.materials.append(get_mat(mat))
    _apply(o)
    return o


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


def rotulo(texto, cx, cz, celda, y, depth, mat):
    """Texto en bloques centrado en (cx, cz), sobre un plano de normal −Y."""
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


# ------------------------------------------------------------------ piezas

def muro():
    """Módulo de muro de 4 × 0.3 × 5 m: cuerpo rojo, zócalo de madera, rieles dorados y dos paneles hundidos."""
    p = [
        box((4.0, 0.3, 5.0), (0, 0, 2.5), "M_Casino_Rojo"),
        box((4.0, 0.34, 0.4), (0, -0.02, 0.2), "M_Madera"),                  # zócalo 0–0.4
        box((4.0, 0.36, 0.06), (0, -0.03, 1.2), "M_Casino_Oro"),             # riel a 1.2 m
        box((4.0, 0.4, 0.15), (0, -0.05, 4.85), "M_Casino_Oro"),             # cornisa 4.775–4.925
    ]
    for x in (-1.0, 1.0):
        p.append(box((1.6, 0.04, 2.4), (x, -0.17, 2.8), "M_Fieltro"))        # panel hundido
    return join(p, "SM_Sala_Muro")


def esquina():
    p = [
        box((0.8, 0.8, 5.0), (0, 0, 2.5), "M_Casino_Rojo"),
        box((0.9, 0.9, 0.4), (0, 0, 0.2), "M_Madera"),
        box((0.88, 0.88, 0.06), (0, 0, 1.2), "M_Casino_Oro"),
        box((0.9, 0.9, 0.15), (0, 0, 4.85), "M_Casino_Oro"),
    ]
    return join(p, "SM_Sala_Esquina")


def puerta(nombre, texto, mat_tablero):
    """Muro de 4 m con una abertura de 3.0 × 3.2 m, marco dorado, hojas de vaivén entreabiertas y letrero sobre el dintel."""
    p = [
        box((0.5, 0.3, 5.0), (-1.75, 0, 2.5), "M_Casino_Rojo"),              # jamba izquierda
        box((0.5, 0.3, 5.0), (1.75, 0, 2.5), "M_Casino_Rojo"),               # jamba derecha
        box((3.0, 0.3, 1.8), (0, 0, 4.1), "M_Casino_Rojo"),                  # dintel 3.2–5.0
        box((0.5, 0.34, 0.4), (-1.75, -0.02, 0.2), "M_Madera"),
        box((0.5, 0.34, 0.4), (1.75, -0.02, 0.2), "M_Madera"),
        box((4.0, 0.4, 0.15), (0, -0.05, 4.85), "M_Casino_Oro"),             # cornisa
        box((0.1, 0.36, 3.2), (-1.45, -0.03, 1.6), "M_Casino_Oro"),          # marco
        box((0.1, 0.36, 3.2), (1.45, -0.03, 1.6), "M_Casino_Oro"),
        box((3.0, 0.36, 0.1), (0, -0.03, 3.25), "M_Casino_Oro"),
    ]
    # hojas de vaivén abiertas 80° hacia el interior (−Y); sin colisión en Unity
    a = math.radians(80)
    for s in (-1, 1):
        hx = s * 1.45
        cx = hx + (-s) * 0.7 * math.cos(a)
        cy = -0.7 * math.sin(a)
        p.append(box((1.4, 0.05, 2.2), (cx, cy, 1.1), "M_Madera", rot=(0, 0, s * a)))
        p.append(box((1.38, 0.06, 0.4), (cx, cy, 0.3), "M_Casino_Oro", rot=(0, 0, s * a)))     # placa inferior
    # letrero sobre el dintel: tablero de color con letras blancas
    p.append(box((2.6, 0.06, 0.7), (0, -0.18, 4.0), mat_tablero))
    p += rotulo(texto, 0, 4.0, 0.055 if len(texto) > 6 else 0.062, -0.215, 0.03, "M_Blanco")
    return join(p, nombre)


def techo():
    """Módulo de techo de 4 × 4 m: losa oscura con dos vigas y un rosetón dorado; origen en la cara inferior de las vigas."""
    p = [
        box((4.0, 4.0, 0.1), (0, 0, 0.35), "M_Madera"),                      # losa 0.30–0.40
        box((4.0, 0.3, 0.3), (0, -1.0, 0.15), "M_Madera"),                   # viga 0–0.30
        box((4.0, 0.3, 0.3), (0, 1.0, 0.15), "M_Madera"),
        box((4.0, 0.06, 0.04), (0, -1.0, -0.0 + 0.02), "M_Casino_Oro"),      # filete dorado bajo la viga
        box((4.0, 0.06, 0.04), (0, 1.0, 0.02), "M_Casino_Oro"),
        box((0.8, 0.8, 0.05), (0, 0, 0.3), "M_Casino_Oro"),                  # rosetón
    ]
    return join(p, "SM_Sala_Techo")


def mostrador():
    """Mostrador de cambio de 3.2 × 1.0 m, ventanilla de vidrio, cajón y letrero CAMBIO sobre dos postes."""
    p = [
        box((3.0, 0.9, 1.05), (0, 0, 0.525), "M_Casino_Rojo"),
        box((3.2, 1.0, 0.06), (0, 0, 1.08), "M_Casino_Oro"),                 # tablero superior 1.05–1.11
        box((3.0, 0.92, 0.3), (0, 0, 0.15), "M_Madera"),                     # zócalo del mostrador
        box((2.6, 0.04, 0.7), (0, 0.3, 1.46), "M_Vidrio"),                   # ventanilla 1.11–1.81
        box((0.12, 0.12, 0.7), (-1.3, 0.3, 1.46), "M_Casino_Oro"),           # montantes de la ventanilla
        box((0.12, 0.12, 0.7), (1.3, 0.3, 1.46), "M_Casino_Oro"),
        box((0.08, 0.08, 0.65), (-1.0, 0.3, 2.13), "M_Madera"),              # postes del letrero 1.81–2.45
        box((0.08, 0.08, 0.65), (1.0, 0.3, 2.13), "M_Madera"),
        box((2.2, 0.08, 0.5), (0, 0.3, 2.2), "M_Negro"),                     # tablero del letrero
        box((0.6, 0.02, 0.1), (0.9, -0.2, 1.14), "M_Casino_Oro"),            # ranura de monedas
    ]
    p += rotulo("CAMBIO", 0, 2.2, 0.05, 0.255, 0.02, "M_Letrero_Texto")
    return join(p, "SM_Mostrador_Cambio")


def taburete():
    """Taburete de 0.69 m de altura: base, pie, aro reposapiés dorado y asiento rojo con canto dorado."""
    p = [
        cyl(0.22, 0.04, (0, 0, 0.02), "M_Negro", verts=14),                  # base
        cyl(0.035, 0.62, (0, 0, 0.35), "M_Casino_Oro", verts=8),             # pie 0.04–0.66
        cyl(0.15, 0.02, (0, 0, 0.25), "M_Casino_Oro", verts=14),             # reposapiés
        cyl(0.21, 0.02, (0, 0, 0.62), "M_Casino_Oro", verts=14),             # canto del asiento
        cyl(0.2, 0.07, (0, 0, 0.655), "M_Casino_Rojo", verts=14),            # asiento 0.62–0.69
    ]
    return join(p, "SM_Taburete")


BUILDERS = {
    "SM_Sala_Muro": muro,
    "SM_Sala_Esquina": esquina,
    "SM_Sala_Puerta_Entrada": lambda: puerta("SM_Sala_Puerta_Entrada", "ENTRADA", "M_Entrada_Verde"),
    "SM_Sala_Puerta_Salida": lambda: puerta("SM_Sala_Puerta_Salida", "SALIDA", "M_Salida_Rojo"),
    "SM_Sala_Techo": techo,
    "SM_Mostrador_Cambio": mostrador,
    "SM_Taburete": taburete,
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
    from mathutils import Vector
    (x0, x1), (y0, y1), (z0, z1) = limites(obj)
    centro = Vector(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    d = max(x1 - x0, y1 - y0, z1 - z0) * 1.7 + 2.0
    cd = bpy.data.cameras.new("Cam_" + nombre)
    cd.sensor_fit = "VERTICAL"
    cd.angle_y = math.radians(38)
    cam = bpy.data.objects.new("Cam_" + nombre, cd)
    escena.collection.objects.link(cam)
    cam.location = centro + Vector((-d * 0.45, -d * 0.9, d * 0.25))
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
    w = bpy.data.worlds.new("Mundo_sala")
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
        # centra la huella en X (Y no: la cara interior sobresale respecto al cuerpo del muro)
        (x0, x1), _, _ = limites(obj)
        obj.data.transform(__import__("mathutils").Matrix.Translation((-(x0 + x1) / 2, 0, 0)))
        obj.data.update()
        (x0, x1), (y0, y1), (z0, z1) = limites(obj)
        dims = (round(x1 - x0, 3), round(y1 - y0, 3), round(z1 - z0, 3))
        tris = triangulos(obj)
        for eje, v, (lo, hi) in zip("XYZ", dims, (rx, ry, rz)):
            if not lo <= v <= hi:
                fallos.append("%s: %s = %.3f fuera del rango %.2f–%.2f" % (nombre, eje, v, lo, hi))
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
    with open(os.path.join(HERE, "stats_sala.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, ensure_ascii=False, indent=2)
    if fallos:
        raise SystemExit("COMPROBACIONES FALLIDAS:\n" + "\n".join(fallos))
    print("LISTO_SALA", len(stats), "piezas")


main()
