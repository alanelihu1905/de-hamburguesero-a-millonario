"""Inspecciona FBX binarios (sin Blender): huesos, malla, piel, blend shapes y clips de animación.

    python3 ArtSource/CasaMarea/tools/inspeccionar_fbx.py Assets/_Game/Models/CasaMarea/Animados/*.fbx

Lee la estructura del archivo y comprueba lo que de verdad se exportó, no lo que dice el script.
"""
import struct
import sys
import zlib


def leer(ruta):
    d = open(ruta, "rb").read()
    version = struct.unpack("<I", d[23:27])[0]
    grande = version >= 7500

    def nodo(pos):
        if grande:
            fin, nprops, plen, nl = struct.unpack("<QQQB", d[pos:pos + 25]); cab = 25
        else:
            fin, nprops, plen, nl = struct.unpack("<IIIB", d[pos:pos + 13]); cab = 13
        if fin == 0:
            return None, pos + cab
        nombre = d[pos + cab:pos + cab + nl].decode("latin1")
        p = pos + cab + nl
        props = []
        for _ in range(nprops):
            t = chr(d[p]); p += 1
            if t in "YCFDIL":
                fmt = {"Y": "<h", "C": "<?", "F": "<f", "D": "<d", "I": "<i", "L": "<q"}[t]
                n = struct.calcsize(fmt); props.append(struct.unpack(fmt, d[p:p + n])[0]); p += n
            elif t in "fdilb":
                ln, enc, cl = struct.unpack("<III", d[p:p + 12]); p += 12
                raw = d[p:p + cl]; p += cl
                if enc:
                    raw = zlib.decompress(raw)
                fmt = {"f": "f", "d": "d", "i": "i", "l": "q", "b": "?"}[t]
                props.append(struct.unpack("<%d%s" % (ln, fmt), raw))
            elif t in "SR":
                ln = struct.unpack("<I", d[p:p + 4])[0]; p += 4
                props.append(d[p:p + ln].decode("latin1") if t == "S" else d[p:p + ln]); p += ln
        hijos = []
        while p < fin:
            c, p = nodo(p)
            if c is None:
                break
            hijos.append(c)
        return (nombre, props, hijos), fin

    nodos, pos = [], 27
    while pos < len(d) - 30:
        n, pos = nodo(pos)
        if n is None:
            break
        nodos.append(n)
    return version, nodos


def buscar(nodos, nombre):
    for n in nodos:
        if n[0] == nombre:
            yield n
        yield from buscar(n[2], nombre)


def limpio(s):
    return str(s).split("\x00")[0]


def inspeccionar(ruta):
    version, nodos = leer(ruta)
    modelos = {}
    for m in buscar(nodos, "Model"):
        tipo = limpio(m[1][2]) if len(m[1]) > 2 else "?"
        modelos.setdefault(tipo, []).append(limpio(m[1][1]).replace("Model::", ""))
    deformers = {}
    for df in buscar(nodos, "Deformer"):
        tipo = limpio(df[1][2]) if len(df[1]) > 2 else "?"
        deformers.setdefault(tipo, []).append(limpio(df[1][1]).replace("SubDeformer::", "").replace("Deformer::", ""))
    pilas = []
    for st in buscar(nodos, "AnimationStack"):
        nombre = limpio(st[1][1]).replace("AnimStack::", "")
        t0 = t1 = None
        for p70 in buscar(st[2], "P"):
            if p70[1] and p70[1][0] in ("LocalStart", "LocalStop"):
                valor = p70[1][4]
                if p70[1][0] == "LocalStart": t0 = valor
                else: t1 = valor
        pilas.append((nombre, t0, t1))
    curvas = sum(1 for _ in buscar(nodos, "AnimationCurve"))
    tris = 0
    for g in buscar(nodos, "Geometry"):
        for pv in buscar(g[2], "PolygonVertexIndex"):
            idx = pv[1][0]
            # cada polígono termina en un índice negativo
            tris += sum(1 for i in idx if i < 0)
    return {"version": version, "modelos": modelos, "deformers": deformers, "pilas_de_animacion": pilas,
            "curvas": curvas, "poligonos": tris}


if __name__ == "__main__":
    for ruta in sys.argv[1:]:
        r = inspeccionar(ruta)
        huesos = r["modelos"].get("LimbNode", []) + r["modelos"].get("Null", []) + r["modelos"].get("Root", [])
        print("== %s (FBX %d)" % (ruta.split("/")[-1], r["version"]))
        print("   modelos:", {k: len(v) for k, v in r["modelos"].items()}, "| huesos≈", len(huesos),
              "| polígonos:", r["poligonos"])
        print("   deformers:", {k: (len(v), v[:7] if k == "BlendShapeChannel" else "") for k, v in r["deformers"].items()})
        for nombre, t0, t1 in r["pilas_de_animacion"]:
            seg = (t1 - t0) / 46186158000.0 if t0 is not None and t1 is not None else None
            print("   clip:", nombre, "duración≈%.3f s" % seg if seg is not None else "")
        print("   curvas de animación:", r["curvas"])
