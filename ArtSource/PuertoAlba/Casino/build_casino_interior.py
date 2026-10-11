"""Kit visual interior del casino de Puerto Alba: máquina tragamonedas, mesa de ruleta y mesa de blackjack.

Uso (desde la raíz del proyecto, en el host):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/Casino/build_casino_interior.py

ARTE, NO MECÁNICA: estas piezas son formas estáticas. No contienen ni implementan apuestas, pagos, RNG ni tablas
de pago; eso vive en el código de Casino (Assets/_Game/Scripts/Casino) y no se toca aquí.

Escalas humanas (1 u = 1 m): tragamonedas de 1.80 m de alto; mesas a 0.76 m (tapete). Origen en el centro de la
huella sobre el suelo (z = 0), frente hacia −Y (en Unity, +Z).

Salidas:
    ArtSource/PuertoAlba/Casino/Kit_Casino_Interior.blend   fuente
    Assets/_Game/Models/PuertoAlba/Casino/<pieza>.fbx       piezas importables
    ArtSource/PuertoAlba/Casino/stats_casino.json           triángulos, dimensiones y pivote medidos por Blender
"""
import bpy
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba", "Casino")
os.makedirs(MODEL_DIR, exist_ok=True)

# materiales de base; las variantes rojo y dorado los sustituyen en Unity (casino_variantes.json)
MATS = {
    "M_Casino_Rojo": ("#8E1B2B", 0.55),      # cuerpo principal
    "M_Casino_Oro": ("#C9A227", 0.35),       # acentos, letrero, palanca
    "M_Fieltro": ("#7A1420", 0.90),          # tapetes de las mesas
    "M_Madera": ("#4A2A1E", 0.65),           # bases y canto de las mesas
    "M_Negro": ("#15171C", 0.30),            # pantallas, celdas negras de la ruleta
    "M_Blanco": ("#F4F1EA", 0.50),           # símbolos y celdas blancas
    "M_Verde_Cero": ("#2E8B57", 0.50),       # celda del cero
    "M_Azul_Simbolo": ("#2B5FA8", 0.50),     # símbolo BAR
    "M_Pantalla": ("#0E0F14", 0.25),         # ventana de rodillos
    "M_Letrero_Texto": ("#F4C95D", 0.40),    # letras del jackpot
}

# piezas: nombre -> (triángulos máximos, dimensiones X × Y × Z esperadas en Blender)
PIEZAS = {
    "SM_Slot_Tres_Rodillos": (2500, (0.95, 0.73, 1.80)),
    "SM_Mesa_Ruleta": (1000, (2.24, 1.44, 0.89)),
    "SM_Mesa_Blackjack": (800, (1.84, 0.92, 0.86)),
    # letrero JACKPOT de pie: letras de 0.25 m (7 × 0.25 = 1.75 m de alto), tablero 10.8 m × 2.0 m a 2.0 m del suelo
    "SM_Letrero_Jackpot": (2000, (10.8, 0.205, 4.1)),
}

# glifos 5 × 7 (mismo estilo que el letrero del hito): JACKPOT y CASINO FOX
GLIFOS = {
    "C": [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "I": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####"],
    "N": ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "F": ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
    "X": ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
    "J": ["..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.."],
    "K": ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"],
    "P": ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
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


def cyl(r, depth, loc, mat, verts=12, rot=(0, 0, 0), sx=1.0, sy=1.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth)
    o = bpy.context.active_object
    o.scale = (sx, sy, 1.0)
    o.location, o.rotation_euler = loc, rot
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


def letras(texto, cx, cz, celda, y, depth, mat):
    """Letras de bloque centradas en (cx, cz) sobre un plano de normal −Y. Cada racha de celdas es una caja."""
    cols = sum(5 for _ in texto) + (len(texto) - 1)
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
                x = x0 + (ox + ini) * celda + ancho / 2.0
                z = z_top - (r + .5) * celda
                cajas.append(box((ancho, depth, celda), (x, y, z), mat))
    return cajas


# ------------------------------------------------------------------ piezas

def build_slot():
    p = [
        box((0.8, 0.7, 0.1), (0, 0, 0.05), "M_Madera"),                   # base 0–0.1
        box((0.8, 0.7, 1.4), (0, 0, 0.8), "M_Casino_Rojo"),               # cuerpo 0.1–1.5
        box((0.8, 0.7, 0.3), (0, 0, 1.65), "M_Casino_Oro"),               # cabecera 1.5–1.8
        box((0.66, 0.02, 0.6), (0, -0.355, 1.0), "M_Pantalla"),           # ventana de rodillos
    ]
    # cabecera: JACKPOT en bloques, en la cara frontal (y = −0.36)
    p += letras("JACKPOT", 0, 1.65, 0.015, -0.36, 0.02, "M_Letrero_Texto")
    # tres rodillos legibles: columna 1 = sietes dorados, columna 2 = cerezas rojas, columna 3 = BAR azul
    for ci, cx in enumerate((-0.2, 0.0, 0.2)):
        for z in (1.2, 1.0, 0.8):
            if ci == 0:
                p.append(box((0.15, 0.02, 0.12), (cx, -0.37, z), "M_Casino_Oro"))
            elif ci == 1:
                p.append(box((0.15, 0.02, 0.12), (cx, -0.37, z), "M_Blanco"))
                p.append(cyl(0.04, 0.02, (cx, -0.36, z), "M_Casino_Rojo", verts=8, rot=(math.radians(90), 0, 0)))
            else:
                p.append(box((0.15, 0.02, 0.12), (cx, -0.37, z), "M_Azul_Simbolo"))
                p.append(box((0.11, 0.02, 0.02), (cx, -0.375, z), "M_Blanco"))
    # línea de pago
    p.append(box((0.62, 0.01, 0.01), (0, -0.37, 1.0), "M_Letrero_Texto"))
    # palanca al costado derecho: varilla, conector y bola
    p.append(box((0.1, 0.05, 0.05), (0.45, -0.1, 1.5), "M_Casino_Oro"))
    p.append(cyl(0.02, 0.6, (0.5, -0.1, 1.2), "M_Casino_Oro", verts=8))
    p.append(cyl(0.05, 0.1, (0.5, -0.1, 1.55), "M_Casino_Rojo", verts=10))
    # botón de juego al frente, bajo la ventana
    p.append(cyl(0.05, 0.04, (0, -0.36, 0.55), "M_Casino_Oro", verts=12, rot=(math.radians(90), 0, 0)))
    return join(p, "SM_Slot_Tres_Rodillos")


def build_ruleta():
    p = [
        cyl(1.0, 0.74, (0, 0, 0.37), "M_Madera", verts=32, sx=1.1, sy=0.7),          # cuerpo 0–0.74
        cyl(1.0, 0.04, (0, 0, 0.72), "M_Casino_Oro", verts=32, sx=1.12, sy=0.72),   # canto dorado 0.70–0.74
        cyl(1.0, 0.02, (0, 0, 0.75), "M_Fieltro", verts=32, sx=1.0, sy=0.64),        # tapete 0.74–0.76
        cyl(0.45, 0.08, (0, 0, 0.80), "M_Madera", verts=24),                         # cuenco 0.76–0.84
    ]
    # 37 celdas del cuenco (0 verde; 1–36 alternan rojo y negro)
    for i in range(37):
        ang = 2 * math.pi * i / 37.0
        x, y = 0.38 * math.cos(ang), 0.38 * math.sin(ang)
        if i == 0:
            mat = "M_Verde_Cero"
        else:
            mat = "M_Casino_Rojo" if i % 2 == 1 else "M_Negro"
        p.append(box((0.04, 0.03, 0.04), (x, y, 0.845), mat, rot=(0, 0, ang)))
    p.append(cyl(0.08, 0.06, (0, 0, 0.86), "M_Casino_Oro", verts=12))              # eje
    return join(p, "SM_Mesa_Ruleta")


def build_blackjack():
    p = [
        box((1.8, 0.9, 0.74), (0, 0, 0.37), "M_Madera"),                              # cuerpo 0–0.74
        box((1.7, 0.8, 0.02), (0, 0, 0.75), "M_Fieltro"),                             # tapete 0.74–0.76
        box((1.84, 0.04, 0.04), (0, -0.44, 0.76), "M_Casino_Oro"),                    # canto frontal
        box((1.84, 0.04, 0.04), (0, 0.44, 0.76), "M_Casino_Oro"),                     # canto trasero
        box((0.04, 0.8, 0.04), (-0.9, 0, 0.76), "M_Casino_Oro"),                      # canto izquierdo
        box((0.04, 0.8, 0.04), (0.9, 0, 0.76), "M_Casino_Oro"),                       # canto derecho
        box((0.2, 0.12, 0.1), (0.6, 0.3, 0.81), "M_Negro"),                           # zapatero de cartas
        cyl(0.04, 0.1, (-0.5, -0.25, 0.81), "M_Casino_Oro", verts=12),                # pila de fichas
        cyl(0.04, 0.1, (-0.4, -0.25, 0.81), "M_Casino_Rojo", verts=12),
    ]
    return join(p, "SM_Mesa_Blackjack")


def build_letrero_jackpot():
    """Letrero JACKPOT grande sobre dos patas. Pivote en el suelo, centrado en X; las letras miran a −Y (frente)."""
    p = [
        box((10.8, 0.12, 2.2), (0, 0, 3.0), "M_Casino_Oro"),              # marco dorado 1.9–4.1
        box((10.6, 0.14, 2.0), (0, 0, 3.0), "M_Casino_Rojo"),             # tablero rojo 2.0–4.0
        box((0.2, 0.2, 2.0), (-4.5, 0, 1.0), "M_Madera"),                 # pata izquierda 0–2
        box((0.2, 0.2, 2.0), (4.5, 0, 1.0), "M_Madera"),                  # pata derecha 0–2
    ]
    # letras: 7 × 0.25 m = 1.75 m de alto, centradas en el tablero, delante de su cara (y = −0.07)
    p += letras("JACKPOT", 0, 3.0, 0.25, -0.085, 0.04, "M_Letrero_Texto")
    return join(p, "SM_Letrero_Jackpot")


BUILDERS = {
    "SM_Letrero_Jackpot": build_letrero_jackpot,
    "SM_Slot_Tres_Rodillos": build_slot,
    "SM_Mesa_Ruleta": build_ruleta,
    "SM_Mesa_Blackjack": build_blackjack,
}


def bounds(obj):
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
        (x0, x1), (y0, y1), (z0, z1) = bounds(obj)
        # centra la huella en X y en Y (el origen queda en el centro de la base)
        obj.data.transform(__import__("mathutils").Matrix.Translation((-(x0 + x1) / 2, -(y0 + y1) / 2, 0)))
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
            "base_z": round(z0, 4), "pivote_correcto": abs(z0) < 1e-4,
            "materiales": sorted({s.material.name for s in obj.material_slots if s.material}),
            "fbx": os.path.relpath(export_fbx(obj), PROJECT),
        }
    with open(os.path.join(HERE, "stats_casino.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, ensure_ascii=False, indent=2)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Kit_Casino_Interior.blend"))
    if fallos:
        raise SystemExit("COMPROBACIONES FALLIDAS:\n" + "\n".join(fallos))
    print("LISTO_CASINO_INTERIOR", len(stats), "piezas")


main()
