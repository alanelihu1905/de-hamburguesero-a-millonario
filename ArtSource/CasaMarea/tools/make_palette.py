"""Genera la paleta única de Casa Marea a partir de los materiales declarados en los scripts de Blender.

    python3 ArtSource/CasaMarea/tools/make_palette.py

Lee los diccionarios MATS de build_estaciones, build_fachada, build_props_barrio, build_interior_cocina y
build_props_barrio_2 (por texto; no importa Blender) y escribe en ArtSource/CasaMarea/Palette/:
    Palette_CasaMarea_Albedo.png        128 x 64, celdas de 16 px (8 x 4), color base
    Palette_CasaMarea_MetalSuavidad.png 128 x 64, R = metálico, A = suavidad (1 - rugosidad), para URP/Lit
    palette.json                        nombre de material -> celda, UV del centro de la celda, color, rugosidad, ...
Después se verifica leyendo los PNG de vuelta y comparando el centro de cada celda con el color esperado.

La asignación de celdas es ESTABLE: si ya existe palette.json, los materiales conocidos conservan su celda (y su UV) y
los nuevos ocupan las celdas libres en orden. Así, añadir colores no invalida los FBX ya exportados con la paleta.

Los materiales de decal (M_Letrero, M_Cartel_Texto) no entran en la paleta: llevan su propia textura en Unity.
M_Mango y M_Brasa están marcados como emisivos: en Unity conviene darles un material aparte (ver README_PALETA.md).
"""
import colorsys
import json
import os
import re
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, ".."))
OUT = os.path.join(ROOT, "Palette")
SCRIPTS = ["build_estaciones.py", "build_fachada.py", "build_props_barrio.py", "build_interior_cocina.py",
           "build_props_barrio_2.py", "build_clientes_vecinas.py"]
EXCLUIR = {"M_Letrero", "M_Cartel_Texto", "M_Preview_Suelo"}
COLS, ROWS, CELL = 8, 4, 16

LINE = re.compile(r'"(M_[A-Za-z_]+)":\s*\(\s*"(#[0-9A-Fa-f]{6})"\s*,\s*([0-9.]+)\s*,\s*([0-9.]+)\s*,\s*(None|"#[0-9A-Fa-f]{6}")\s*,\s*([0-9.]+)\s*\)')


def collect():
    mats, conflictos = {}, []
    for f in SCRIPTS:
        text = open(os.path.join(ROOT, f), encoding="utf-8").read()
        for name, hexc, rough, metal, emis, estr in LINE.findall(text):
            spec = {"hex": hexc.upper(), "rugosidad": float(rough), "metalico": float(metal),
                    "emision_hex": None if emis == "None" else emis.strip('"').upper()}
            if name in mats and mats[name]["spec"] != spec:
                conflictos.append((name, mats[name]["spec"], spec, f))
            mats.setdefault(name, {"spec": spec, "scripts": []})["scripts"].append(f)
    return mats, conflictos


def orden(item):
    name, d = item
    r, g, b = (int(d["spec"]["hex"][i:i + 2], 16) / 255 for i in (1, 3, 5))
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    return (0, v, 0) if s < 0.12 else (1, round(h, 2), v)


def png(path, width, height, rows_rgba):
    raw = b"".join(b"\x00" + bytes(row) for row in rows_rgba)

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def read_png(path):
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n"
    pos, idat, w = 8, b"", None
    while pos < len(data):
        n, tag = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        if tag == b"IHDR":
            w, h = struct.unpack(">II", body[:8])
        elif tag == b"IDAT":
            idat += body
        pos += 12 + n
    raw, stride = zlib.decompress(idat), w * 4 + 1
    return w, h, [list(raw[y * stride + 1:(y + 1) * stride]) for y in range(h)]


def main():
    mats, conflictos = collect()
    previas = {}
    ruta_previa = os.path.join(OUT, "palette.json")
    if os.path.exists(ruta_previa):
        previas = {n: tuple(m["celda"]) for n, m in json.load(open(ruta_previa, encoding="utf-8"))["materiales"].items()}
    candidatos = sorted(((n, d) for n, d in mats.items() if n not in EXCLUIR), key=orden)
    assert len(candidatos) <= COLS * ROWS, "la paleta no cabe: %d materiales" % len(candidatos)
    ocupadas = {c for n, c in previas.items() if n in dict(candidatos)}
    libres = iter(c for c in ((i % COLS, i // COLS) for i in range(COLS * ROWS)) if c not in ocupadas)
    celdas = {n: previas[n] if n in previas else next(libres) for n, _ in candidatos}
    items = candidatos
    nuevos = [n for n, _ in candidatos if n not in previas]
    albedo = [[0] * (COLS * CELL * 4) for _ in range(ROWS * CELL)]
    mask = [[0] * (COLS * CELL * 4) for _ in range(ROWS * CELL)]
    for y in range(ROWS * CELL):                                    # celdas libres: gris medio, opacas
        for x in range(COLS * CELL):
            albedo[y][x * 4:x * 4 + 4] = [128, 128, 128, 255]
            mask[y][x * 4:x * 4 + 4] = [0, 0, 0, 255 * 0]
    palette = {}
    for name, d in items:
        col, row = celdas[name]
        sp = d["spec"]
        rgb = [int(sp["hex"][k:k + 2], 16) for k in (1, 3, 5)]
        met, suav = round(sp["metalico"] * 255), round((1 - sp["rugosidad"]) * 255)
        for y in range(row * CELL, (row + 1) * CELL):
            for x in range(col * CELL, (col + 1) * CELL):
                albedo[y][x * 4:x * 4 + 4] = rgb + [255]
                mask[y][x * 4:x * 4 + 4] = [met, 0, 0, suav]
        palette[name] = {"hex": sp["hex"], "rugosidad": sp["rugosidad"], "metalico": sp["metalico"],
                         "emisivo": sp["emision_hex"] is not None, "emision_hex": sp["emision_hex"],
                         "celda": [col, row],
                         "uv": [round((col + 0.5) / COLS, 5), round(1 - (row + 0.5) / ROWS, 5)],
                         "usado_en": sorted(set(d["scripts"]))}
    os.makedirs(OUT, exist_ok=True)
    png(os.path.join(OUT, "Palette_CasaMarea_Albedo.png"), COLS * CELL, ROWS * CELL, albedo)
    png(os.path.join(OUT, "Palette_CasaMarea_MetalSuavidad.png"), COLS * CELL, ROWS * CELL, mask)
    meta = {"rejilla": {"columnas": COLS, "filas": ROWS, "celda_px": CELL, "imagen_px": [COLS * CELL, ROWS * CELL],
                        "celdas_libres": COLS * ROWS - len(palette)},
            "uv_convencion": "origen abajo-izquierda (Unity/Blender); uv = centro de la celda",
            "excluidos": sorted(EXCLUIR), "materiales_nuevos_en_esta_generacion": nuevos, "conflictos_entre_scripts": [c[0] for c in conflictos],
            "materiales": palette}
    with open(os.path.join(OUT, "palette.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2, ensure_ascii=False)

    # verificación: leer los PNG de vuelta y comparar el centro de cada celda
    w, h, a = read_png(os.path.join(OUT, "Palette_CasaMarea_Albedo.png"))
    w2, h2, m = read_png(os.path.join(OUT, "Palette_CasaMarea_MetalSuavidad.png"))
    assert (w, h, w2, h2) == (COLS * CELL, ROWS * CELL) * 2
    errores = 0
    for name, p in palette.items():
        cx, cy = p["celda"][0] * CELL + CELL // 2, p["celda"][1] * CELL + CELL // 2
        rgb = a[cy][cx * 4:cx * 4 + 3]
        if rgb != [int(p["hex"][k:k + 2], 16) for k in (1, 3, 5)]:
            errores += 1
            print("ERROR de color:", name, rgb, p["hex"])
        if m[cy][cx * 4] != round(p["metalico"] * 255) or m[cy][cx * 4 + 3] != round((1 - p["rugosidad"]) * 255):
            errores += 1
            print("ERROR de máscara:", name)
    print("nuevos en esta generación:", nuevos)
    print("materiales en la paleta: %d de %d celdas (libres: %d); conflictos entre scripts: %d; errores de lectura: %d"
          % (len(palette), COLS * ROWS, COLS * ROWS - len(palette), len(conflictos), errores))
    for c in conflictos:
        print("  conflicto:", c[0], c[1], c[2], "en", c[3])
    for name, p in palette.items():
        print("  %-22s celda %s uv %s %s%s" % (name, p["celda"], p["uv"], p["hex"], "  (emisivo)" if p["emisivo"] else ""))


main()
