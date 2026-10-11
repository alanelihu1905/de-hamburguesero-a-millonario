"""Genera el kit modular de la ciudad Puerto Alba (fachadas residenciales, tienda de barrio y gasolinera).

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU para las vistas):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/build_kit_ciudad.py

Diseño y reglas: Docs/Handoffs/Claude/09_KIT_VISUAL_CIUDAD.md.
Licencia: obra propia del proyecto generada por este script (ArtSource/PuertoAlba/LICENCIA.md).

Salidas:
    ArtSource/PuertoAlba/Kit_Ciudad.blend                        fuente con el kit alineado para las vistas
    Assets/_Game/Models/PuertoAlba/SM_*.fbx                      piezas importables
    ArtSource/PuertoAlba/Preview/Kit_residencial_tienda.png      vista frontal de fachadas, puerta y tienda
    ArtSource/PuertoAlba/Preview/Kit_gasolinera.png              vista frontal de la gasolinera
    ArtSource/PuertoAlba/stats_kit.json                          triángulos, dimensiones, pivote y comprobaciones

Convenciones: 1 u = 1 m, Z arriba, frente hacia -Y, origen en el centro de la huella sobre el suelo (z = 0).
Las piezas se generan en el origen; solo el .blend las desplaza para las vistas.
"""
import bpy
import math
import os
import json

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
MODEL_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "PuertoAlba")
PREVIEW_DIR = os.path.join(HERE, "Preview")
for d in (MODEL_DIR, PREVIEW_DIR):
    os.makedirs(d, exist_ok=True)

# ---------------------------------------------------------------- materiales
# nombre: (hex, rugosidad, metálico, color_emisión, fuerza_emisión)
# Paleta base neutra de Puerto Alba; las ocho zonas la sustituyen por variaciones (zonas_variacion.json).
MATS = {
    "M_Muro_Base":        ("#EADFC8", 0.80, 0.00, None, 0.0),
    "M_Zocalo":           ("#6E8FA0", 0.70, 0.00, None, 0.0),
    "M_Cornisa":          ("#B9663F", 0.75, 0.00, None, 0.0),
    "M_Marco":            ("#F4F1EA", 0.55, 0.00, None, 0.0),
    "M_Madera":           ("#8A5A3C", 0.65, 0.00, None, 0.0),
    "M_Puerta":           ("#2F6F73", 0.55, 0.00, None, 0.0),
    "M_Vidrio":           ("#5FA9B8", 0.15, 0.00, None, 0.0),
    "M_Toldo":            ("#D9573B", 0.70, 0.00, None, 0.0),
    "M_Metal_Oscuro":     ("#3C4048", 0.45, 0.60, None, 0.0),
    "M_Surtidor":         ("#E4572E", 0.50, 0.00, None, 0.0),
    "M_Pantalla":         ("#15171C", 0.30, 0.00, None, 0.0),
    "M_Marquesina":       ("#F7F3EA", 0.45, 0.00, None, 0.0),
    "M_Marquesina_Luz":   ("#F4C95D", 0.40, 0.00, "#F4C95D", 0.0),   # emisión apagada; Unity la enciende
    "M_Letrero_Fondo":    ("#3B2F4A", 0.60, 0.00, None, 0.0),
    "M_Letrero_Texto":    ("#F4C95D", 0.50, 0.00, None, 0.0),        # plano para la textura del rótulo
    "M_Tejado":           ("#8E4A2F", 0.80, 0.00, None, 0.0),
    "M_Preview_Suelo":    ("#CDBFA4", 0.90, 0.00, None, 0.0),
    "M_Casino_Oro":       ("#C9A227", 0.35, 0.80, None, 0.0),        # marco y bordes del hito
    "M_Casino_Neon":      ("#7B2D8E", 0.40, 0.00, "#B47CC9", 0.0),   # tubos apagados; Unity los enciende
    "M_Casino_Rojo":      ("#8E1B2B", 0.60, 0.00, None, 0.0),        # torres y basamento
}

# presupuesto de triángulos por pieza (propuesta del documento 09; el script avisa si se pasa)
BUDGET = {
    "SM_Fachada_Residencial": 400,
    "SM_Fachada_Residencial_B": 400,
    "SM_Puerta_Residencial": 300,
    "SM_Tienda_Barrio": 1000,   # 700 + rótulo TIENDA en bloques (~150 tris)
    "SM_Gasolinera_Marquesina": 300,
    "SM_Gasolinera_Surtidor": 300,
    "SM_Gasolinera_Caseta": 300,
    "SM_Gasolinera_Letrero": 500,   # 200 + rótulo GAS en bloques (~200 tris)
    "SM_Casa_Dos_Plantas": 500,
    "SM_Hito_Casino": 2000,   # incluye las letras: más cajas que el resto del hito
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


def frame(w, h, t, depth, cx, y, cz, mat):
    """Marco de cuatro listones alrededor de un hueco de ancho w y alto h, centrado en (cx, y, cz)."""
    return [
        box((w + 2 * t, depth, t), (cx, y, cz + h / 2 + t / 2), mat),   # dintel
        box((w + 2 * t, depth, t), (cx, y, cz - h / 2 - t / 2), mat),   # alféizar
        box((t, depth, h), (cx - w / 2 - t / 2, y, cz), mat),           # jamba izquierda
        box((t, depth, h), (cx + w / 2 + t / 2, y, cz), mat),           # jamba derecha
    ]


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


# ---------------------------------------------------------------- piezas
def build_fachada():
    """Módulo de fachada residencial: un tramo de 4 m, una planta de 3.2 m, dos ventanas con postigos."""
    p = [
        box((4.0, 0.4, 3.2), (0, 0, 1.6), "M_Muro_Base"),
        box((4.0, 0.44, 0.35), (0, 0, 0.175), "M_Zocalo"),
        box((4.2, 0.52, 0.18), (0, 0, 3.29), "M_Cornisa"),
    ]
    for cx in (-0.9, 0.9):
        p += frame(1.0, 1.0, 0.07, 0.06, cx, -0.21, 1.9, "M_Marco")
        p.append(box((0.9, 0.03, 0.9), (cx, -0.23, 1.9), "M_Vidrio"))
        p.append(box((0.32, 0.03, 1.1), (cx - 0.7, -0.21, 1.9), "M_Madera"))   # postigo izquierdo
        p.append(box((0.32, 0.03, 1.1), (cx + 0.7, -0.21, 1.9), "M_Madera"))   # postigo derecho
    return join(p, "SM_Fachada_Residencial")


def build_fachada_b():
    """Segunda variante del tramo residencial: toldos sobre las ventanas, alféizares y un solo postigo.
    Misma huella que la fachada A (4.2 m de ancho, 3.38 m de alto) pero con 0.76 m de saliente por los toldos."""
    p = [
        box((4.0, 0.4, 3.2), (0, 0, 1.6), "M_Muro_Base"),
        box((4.0, 0.44, 0.35), (0, 0, 0.175), "M_Zocalo"),
        box((4.2, 0.52, 0.18), (0, 0, 3.29), "M_Cornisa"),
    ]
    for cx in (-0.9, 0.9):
        p += frame(1.0, 1.0, 0.07, 0.06, cx, -0.21, 1.9, "M_Marco")
        p.append(box((0.9, 0.03, 0.9), (cx, -0.23, 1.9), "M_Vidrio"))
        p.append(box((1.2, 0.3, 0.06), (cx, -0.35, 2.5), "M_Toldo"))          # toldo sobre la ventana, 0.3 m de saliente
        p.append(box((1.1, 0.1, 0.05), (cx, -0.22, 1.35), "M_Marco"))         # alféizar
    p.append(box((0.32, 0.03, 1.1), (-1.6, -0.21, 1.9), "M_Madera"))          # un solo postigo, a la izquierda
    return join(p, "SM_Fachada_Residencial_B")


def build_puerta():
    """Puerta residencial de 1.1 m con marco, capialzado y visera. Pivote en el centro de la huella."""
    p = [
        box((1.1, 0.06, 2.3), (0, -0.19, 1.15), "M_Puerta"),
        box((0.2, 0.1, 0.1), (0.4, -0.23, 1.1), "M_Metal_Oscuro"),        # tirador
        box((1.5, 0.12, 0.3), (0, -0.2, 2.45), "M_Cornisa"),              # capialzado
        box((1.7, 0.5, 0.06), (0, -0.47, 2.62), "M_Marco"),               # visera (sale 0.5 m hacia -Y)
        box((0.06, 0.08, 0.5), (-0.7, -0.3, 2.3), "M_Metal_Oscuro"),      # soporte visera izq.
        box((0.06, 0.08, 0.5), (0.7, -0.3, 2.3), "M_Metal_Oscuro"),       # soporte visera der.
    ]
    fr = frame(1.1, 2.3, 0.07, 0.08, 0, -0.18, 1.15, "M_Marco")
    p += fr[:1] + fr[2:]          # dintel y jambas; sin alféizar, para que la base quede en z = 0
    return join(p, "SM_Puerta_Residencial")


def build_tienda():
    """Tienda de barrio de 6 × 4 m: escaparate, puerta lateral, toldo a rayas sólido y rótulo."""
    p = [
        box((6.0, 4.0, 4.2), (0, 0, 2.1), "M_Muro_Base"),
        box((6.1, 4.1, 0.25), (0, 0, 4.325), "M_Tejado"),                 # losa de azotea
        box((6.0, 0.44, 0.35), (0, 0, 0.175), "M_Zocalo"),
        box((4.2, 0.06, 1.6), (-0.6, -2.02, 1.2), "M_Vidrio"),            # escaparate
        box((6.2, 0.9, 0.08), (0, -2.45, 2.85), "M_Toldo"),               # toldo
        box((1.0, 0.06, 2.2), (2.1, -2.02, 1.1), "M_Madera"),             # puerta
        box((5.0, 0.12, 0.7), (0, -2.06, 3.7), "M_Letrero_Fondo"),        # rótulo
    ]
    p += rotulo("TIENDA", 0, 3.7, 0.06, -2.13, 0.02, "M_Letrero_Texto")   # rótulo en bloques, centrado en el panel
    p += frame(4.2, 1.6, 0.08, 0.1, -0.6, -2.06, 1.2, "M_Marco")
    return join(p, "SM_Tienda_Barrio")


def build_marquesina():
    """Marquesina de 12 × 8 m a 5 m de altura con cuatro pilares y banda luminosa apagada."""
    p = [
        box((12.0, 8.0, 0.5), (0, 0, 5.25), "M_Marquesina"),
        box((12.0, 8.0, 0.12), (0, 0, 4.94), "M_Marquesina_Luz"),
    ]
    for x in (-5.4, 5.4):
        for y in (-3.4, 3.4):
            p.append(box((0.4, 0.4, 4.9), (x, y, 2.45), "M_Marquesina"))
    return join(p, "SM_Gasolinera_Marquesina")


def build_surtidor():
    """Surtidor de dos mangueras sobre una isla de 1.6 × 1.0 × 0.15 m con bordillo (el surtidor mide 0.9 × 0.6 m)."""
    p = [
        box((1.6, 1.0, 0.15), (0, 0, 0.075), "M_Cornisa"),                # isla con bordillo 0–0.15
        box((0.9, 0.6, 0.25), (0, 0, 0.125), "M_Metal_Oscuro"),           # base
        box((0.8, 0.5, 1.2), (0, 0, 0.85), "M_Surtidor"),                 # cuerpo
        box((0.8, 0.5, 0.1), (0, 0, 1.5), "M_Marquesina"),                # tapa
        box((0.6, 0.05, 0.35), (0, -0.27, 1.12), "M_Pantalla"),           # pantalla
        box((0.04, 0.04, 0.6), (0.3, -0.27, 0.8), "M_Metal_Oscuro"),      # manguera
        box((0.08, 0.1, 0.16), (0.3, -0.3, 0.45), "M_Metal_Oscuro"),      # pistola
    ]
    return join(p, "SM_Gasolinera_Surtidor")


def build_caseta():
    """Caseta de cajas de 3.0 × 2.5 m con escaparate y techo de la misma familia que la marquesina."""
    p = [
        box((3.0, 2.5, 2.8), (0, 0, 1.4), "M_Muro_Base"),
        box((3.3, 2.8, 0.2), (0, 0, 2.9), "M_Marquesina"),
        box((1.6, 0.05, 1.1), (0, -1.26, 1.6), "M_Vidrio"),
    ]
    p += frame(1.6, 1.1, 0.06, 0.08, 0, -1.24, 1.6, "M_Marco")
    return join(p, "SM_Gasolinera_Caseta")


def build_letrero():
    """Letrero de poste de 2.4 × 1.2 m, con la cara inferior a 3.0 m."""
    p = [
        box((0.2, 0.2, 3.6), (-0.9, 0, 1.8), "M_Metal_Oscuro"),
        box((0.2, 0.2, 3.6), (0.9, 0, 1.8), "M_Metal_Oscuro"),
        box((2.4, 0.16, 1.2), (0, 0, 3.6), "M_Letrero_Fondo"),
    ]
    p += rotulo("GAS", 0, 3.6, 0.09, -0.09, 0.02, "M_Letrero_Texto")
    return join(p, "SM_Gasolinera_Letrero")


def build_casa_dos_plantas():
    """Casa de dos plantas de 5 × 6 m: planta baja con puerta, planta alta con balcón y dos ventanas por planta."""
    p = [
        box((5.0, 6.0, 6.2), (0, 0, 3.1), "M_Muro_Base"),
        box((5.3, 6.3, 0.25), (0, 0, 6.325), "M_Tejado"),                 # alero del tejado
        box((5.0, 6.04, 0.35), (0, 0, 0.175), "M_Zocalo"),
        box((2.4, 0.9, 0.12), (0, -3.45, 3.6), "M_Cornisa"),              # balcón (losa)
        box((2.4, 0.06, 0.9), (0, -3.87, 4.1), "M_Marco"),                # barandilla frontal
        box((0.06, 0.9, 0.9), (-1.17, -3.45, 4.1), "M_Marco"),            # barandilla lateral izq.
        box((0.06, 0.9, 0.9), (1.17, -3.45, 4.1), "M_Marco"),             # barandilla lateral der.
    ]
    # puerta en planta baja (centrada, el frente es -Y)
    fr = frame(1.1, 2.3, 0.07, 0.1, 0, -3.02, 1.15, "M_Marco")
    p += fr[:1] + fr[2:]
    p.append(box((1.1, 0.06, 2.3), (0, -3.0, 1.15), "M_Puerta"))
    # ventanas: dos por planta, con postigos
    for z in (1.9, 4.6):
        for cx in (-1.6, 1.6):
            p += frame(1.0, 1.0, 0.07, 0.06, cx, -3.02, z, "M_Marco")
            p.append(box((0.9, 0.03, 0.9), (cx, -3.04, z), "M_Vidrio"))
            p.append(box((0.32, 0.03, 1.1), (cx - 0.7, -3.02, z), "M_Madera"))
            p.append(box((0.32, 0.03, 1.1), (cx + 0.7, -3.02, z), "M_Madera"))
    return join(p, "SM_Casa_Dos_Plantas")


def build_hito_casino():
    """Hito propio del casino: basamento, dos torres con remate y un rótulo ámbar entre ellas. No reproduce el edificio del casino."""
    p = [
        box((12.0, 4.0, 1.0), (0, 0, 0.5), "M_Casino_Rojo"),              # basamento 0–1 m
        box((2.0, 2.0, 9.0), (-4.2, 0, 5.5), "M_Casino_Rojo"),            # torre izquierda 1–10 m
        box((2.0, 2.0, 9.0), (4.2, 0, 5.5), "M_Casino_Rojo"),             # torre derecha 1–10 m
        box((2.2, 2.2, 0.3), (-4.2, 0, 10.15), "M_Casino_Oro"),           # remate izquierdo
        box((2.2, 2.2, 0.3), (4.2, 0, 10.15), "M_Casino_Oro"),            # remate derecho
        box((8.2, 0.2, 2.6), (0, -2.2, 7.0), "M_Casino_Oro"),             # marco del rótulo
        box((8.0, 0.4, 2.4), (0, -2.2, 7.0), "M_Letrero_Fondo"),          # fondo del rótulo
        box((8.0, 0.06, 0.12), (0, -2.36, 8.1), "M_Casino_Neon"),         # tubo superior
        box((8.0, 0.06, 0.12), (0, -2.36, 5.9), "M_Casino_Neon"),         # tubo inferior
    ]
    p += letras_casino()
    return join(p, "SM_Hito_Casino")


# Letras geométricas de 5 × 7 celdas: cada racha de celdas encendidas es una caja en la cara del rótulo.
# No depende de fuentes ni de TMP; se lee desde la vista frontal.
GLIFOS = {
    "C": [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "I": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####"],
    "N": ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "F": ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
    "X": ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "D": ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
    "L": ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
    "G": [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####"],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
}
TEXTO_CASINO = ["CASINO", "FOX"]
CELDA = 0.125          # lado de cada celda del texto (m); 9 letras caben en 6.9 m de los 8 m del rótulo
LETRAS_Z_CENTRO = 7.0  # centro vertical del rótulo (ver build_hito_casino)
LETRAS_Y = -2.44       # cara de las letras, delante del fondo del rótulo (−2.40)


def rotulo(texto, cx, cz, celda, y, depth, mat):
    """Texto en bloques centrado en (cx, cz). Igual que letras_casino, para un texto cualquiera de GLIFOS."""
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


def letras_casino():
    """Cajas de las letras de «CASINO FOX», centradas en x y en el rótulo."""
    # columnas totales: 5 por letra, 1 de separación entre letras y 3 entre palabras
    cols = 0
    for i, palabra in enumerate(TEXTO_CASINO):
        cols += 5 * len(palabra) + (len(palabra) - 1)
    cols += 3 * (len(TEXTO_CASINO) - 1)
    x0 = -cols * CELDA / 2.0
    z_top = LETRAS_Z_CENTRO + 7 * CELDA / 2.0
    cajas = []
    cursor = 0
    for pi, palabra in enumerate(TEXTO_CASINO):
        for li, letra in enumerate(palabra):
            filas = GLIFOS[letra]
            for r, fila in enumerate(filas):
                c = 0
                while c < 5:
                    if fila[c] != "#":
                        c += 1
                        continue
                    ini = c
                    while c < 5 and fila[c] == "#":
                        c += 1
                    ancho = (c - ini) * CELDA
                    cx = x0 + (cursor + ini) * CELDA + ancho / 2.0
                    cz = z_top - (r + .5) * CELDA
                    cajas.append(box((ancho, .06, CELDA), (cx, LETRAS_Y, cz), "M_Letrero_Texto"))
            cursor += 5
            if li < len(palabra) - 1:
                cursor += 1
        if pi < len(TEXTO_CASINO) - 1:
            cursor += 3
    return cajas


BUILDERS = [build_fachada, build_fachada_b, build_puerta, build_tienda, build_marquesina, build_surtidor, build_caseta, build_letrero,
            build_casa_dos_plantas, build_hito_casino]


# ---------------------------------------------------------------- medidas y comprobaciones
def tri_count(obj):
    return sum(max(0, len(p.vertices) - 2) for p in obj.data.polygons)


def bounds(obj):
    xs = [v[0] for v in obj.bound_box]
    ys = [v[1] for v in obj.bound_box]
    zs = [v[2] for v in obj.bound_box]
    return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))


def stat(obj):
    (x0, x1), (y0, y1), (z0, z1) = bounds(obj)
    tris = tri_count(obj)
    budget = BUDGET[obj.name]
    d = {
        "triangulos": tris,
        "presupuesto_triangulos": budget,
        "dentro_de_presupuesto": tris <= budget,
        "dimensiones_XxYxZ_m": [round(x1 - x0, 3), round(y1 - y0, 3), round(z1 - z0, 3)],
        "centro_huella_xy": [round((x0 + x1) / 2, 4), round((y0 + y1) / 2, 4)],
        "base_z": round(z0, 4),
        "materiales": sorted({s.material.name for s in obj.material_slots if s.material}),
    }
    # pivote: base en z = 0 y centro en X. Y no se fuerza: toldos y viseras sobresalen hacia -Y (frente).
    d["pivote_correcto"] = abs(z0) < 1e-4 and abs((x0 + x1) / 2) < 1e-3
    return d


def export_fbx(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(MODEL_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=False)
    return path


# ---------------------------------------------------------------- render
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
        scene.eevee.taa_render_samples = 32
    except Exception:
        pass
    scene.view_settings.view_transform = "Standard"


def add_world(scene, hexcol="#9FCFD6", strength=0.9):
    w = bpy.data.worlds.new("Mundo_Preview_Kit")
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (*hexlin(hexcol), 1.0)
    bg.inputs["Strength"].default_value = strength
    scene.world = w


def add_sun():
    from mathutils import Vector
    d = Vector((0.555, 0.667, -0.5))
    sd = bpy.data.lights.new("Sol_Tarde", "SUN")
    sd.energy, sd.color = 3.4, hexlin("#FFC47E")
    sun = bpy.data.objects.new("Sol_Tarde", sd)
    bpy.context.scene.collection.objects.link(sun)
    sun.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()


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


def render_to(scene, cam, path):
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def lineup(objs, gap=1.0):
    """Coloca copias de las piezas en fila, en el .blend, para las vistas. Devuelve el ancho total."""
    x = 0.0
    for o in objs:
        (x0, x1), _, _ = bounds(o)
        o.location.x = x - x0
        x += (x1 - x0) + gap
    return x - gap


# ---------------------------------------------------------------- principal
def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name, spec in MATS.items():
        get_mat(name, spec)

    stats = {}
    objs = {}
    for builder in BUILDERS:
        obj = builder()
        objs[obj.name] = obj
        stats[obj.name] = stat(obj)
        stats[obj.name]["fbx"] = os.path.relpath(export_fbx(obj), PROJECT)

    # comprobaciones de escala: cada pieza debe medir lo que promete el documento 09
    # medidas de diseño (documento 09, sección 3); incluyen los salientes (toldo, visera, pistola)
    expect = {
        "SM_Fachada_Residencial": (4.2, 0.52, 3.38),
        "SM_Fachada_Residencial_B": (4.2, 0.76, 3.38),   # toldos hasta y = −0.5; cornisa hasta y = +0.26
        "SM_Puerta_Residencial": (1.7, 0.58, 2.65),
        "SM_Tienda_Barrio": (6.2, 4.95, 4.45),     # toldo hasta y = -2.9; losa de azotea hasta y = +2.05
        "SM_Gasolinera_Marquesina": (12.0, 8.0, 5.5),
        "SM_Gasolinera_Surtidor": (1.6, 1.0, 1.55),      # isla de 1.6 × 1.0 m
        "SM_Gasolinera_Caseta": (3.3, 2.8, 3.0),
        "SM_Gasolinera_Letrero": (2.4, 0.2, 4.2),
        "SM_Casa_Dos_Plantas": (5.3, 7.05, 6.45),   # balcón hasta y = -3.9; alero hasta y = +3.15; techo a z = 6.45
        "SM_Hito_Casino": (12.0, 4.47, 10.3),       # letras hasta y = -2.47; torres y remates hasta z = 10.3
    }
    fallos = []
    for name, dims in expect.items():
        real = stats[name]["dimensiones_XxYxZ_m"]
        if any(abs(a - b) > 0.02 for a, b in zip(real, dims)):
            fallos.append("%s: medido %s, esperado %s" % (name, real, list(dims)))
        if not stats[name]["pivote_correcto"]:
            fallos.append("%s: pivote o base fuera de z=0 / centro" % name)
        if not stats[name]["dentro_de_presupuesto"]:
            fallos.append("%s: %d triángulos (tope %d)" % (name, stats[name]["triangulos"], BUDGET[name]))
    if fallos:
        raise SystemExit("COMPROBACIONES FALLIDAS:\n" + "\n".join(fallos))

    # escena de vista previa con el kit alineado
    scene = bpy.context.scene
    add_world(scene)
    add_sun()
    residencial = [objs["SM_Fachada_Residencial"], objs["SM_Puerta_Residencial"], objs["SM_Tienda_Barrio"]]
    gasolinera = [objs["SM_Gasolinera_Marquesina"], objs["SM_Gasolinera_Surtidor"],
                  objs["SM_Gasolinera_Caseta"], objs["SM_Gasolinera_Letrero"]]
    for grupo, nombre in ((residencial, "Kit_residencial_tienda.png"), (gasolinera, "Kit_gasolinera.png")):
        ancho = lineup(grupo)
        xc = ancho / 2
        # solo se ve el grupo de esta vista; las demás piezas quedan ocultas en el render
        for o in objs.values():
            o.hide_render = o not in grupo
        suelo = box((ancho + 6, 14, 0.1), (xc, 0, -0.05), "M_Preview_Suelo")
        suelo.name = "Preview_Suelo"
        dist = (ancho / 2 + 3) / math.tan(math.radians(45 / 2)) * 0.9
        setup_render(scene, 1920, 1080)
        render_to(scene, add_camera("Cam_" + nombre, (xc, -dist, 2.4), (xc, 0, 1.6), 45),
                  os.path.join(PREVIEW_DIR, nombre))
        bpy.data.objects.remove(suelo, do_unlink=True)
    for o in objs.values():
        o.hide_render = False

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Kit_Ciudad.blend"))
    with open(os.path.join(HERE, "stats_kit.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, ensure_ascii=False, indent=2)
    for k, v in stats.items():
        print("[%s] tris=%d (tope %d) dims=%s pivote_ok=%s" % (k, v["triangulos"], v["presupuesto_triangulos"],
                                                                v["dimensiones_XxYxZ_m"], v["pivote_correcto"]))
    print("LISTO")


main()
