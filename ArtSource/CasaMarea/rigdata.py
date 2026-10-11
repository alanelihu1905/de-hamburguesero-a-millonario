"""Esqueleto y animaciones de los clientes de Casa Marea, en Python puro (sin Blender).

Lo usan build_clientes_animados.py (Blender) y tools/precheck_animacion.py (comprobación sin Blender). Así la lógica del
rig y de las poses se puede verificar con cinemática directa antes de abrir Blender.

Convenciones
  - Coordenadas de Blender: Z arriba, el personaje mira a -Y, 1 u = 1 m.
  - Lado «D» = derecha del personaje (x < 0); lado «I» = izquierda (x > 0).
  - Cada rotación de una pose es un Euler XYZ (radianes) en EJES DEL MUNDO, aplicado alrededor de la cabeza del hueso
    con el padre sin girar; equivale a R = Rz·Ry·Rx. Blender lo convierte a la rotación local del hueso con
    q = reposo⁻¹ · R · reposo. Con padres girados, las rotaciones se componen de la forma habitual (cinemática directa).
  - Para un hueso que cuelga hacia abajo (brazo, pierna): rx negativo = hacia delante; rx positivo = hacia atrás.
    Para un hueso que sube (columna, cabeza): rx positivo = inclinarse hacia delante.
  - Un «offset» de un hueso es una traslación en ejes del mundo (solo se usa en pelvis para el rebote).
Fotogramas: 30 fps, numerados desde 0 en este módulo (en Blender se desplazan +1).
"""
import math

FPS = 30
ANGULO_BRAZO = math.radians(8)

# ---------------------------------------------------------------- esqueleto
LADOS = (("I", 1), ("D", -1))


def _brazo(sx):
    d = (sx * math.sin(ANGULO_BRAZO), 0.0, -math.cos(ANGULO_BRAZO))
    hombro = (sx * 0.24, 0.0, 1.34)
    return lambda t: (hombro[0] + d[0] * t, 0.0, hombro[2] + d[2] * t)


def construir_huesos():
    huesos = [
        ("root", None, (0.0, 0.0, 0.0), (0.0, -0.18, 0.0)),
        ("pelvis", "root", (0.0, 0.0, 0.84), (0.0, 0.0, 0.98)),
        ("columna", "pelvis", (0.0, 0.0, 0.98), (0.0, 0.0, 1.36)),
        ("cabeza", "columna", (0.0, 0.0, 1.36), (0.0, 0.0, 1.70)),
    ]
    for lado, sx in LADOS:
        p = _brazo(sx)
        huesos += [("brazo_%s" % lado, "columna", p(0.0), p(0.27)),
                   ("antebrazo_%s" % lado, "brazo_%s" % lado, p(0.27), p(0.56)),
                   ("mano_%s" % lado, "antebrazo_%s" % lado, p(0.56), p(0.66)),
                   ("muslo_%s" % lado, "pelvis", (sx * 0.09, 0.0, 0.84), (sx * 0.09, 0.0, 0.44)),
                   ("pantorrilla_%s" % lado, "muslo_%s" % lado, (sx * 0.09, 0.0, 0.44), (sx * 0.09, 0.0, 0.08)),
                   ("pie_%s" % lado, "pantorrilla_%s" % lado, (sx * 0.09, 0.0, 0.08), (sx * 0.09, -0.16, 0.04))]
    return huesos


HUESOS = construir_huesos()
POR_NOMBRE = {h[0]: h for h in HUESOS}
PADRE = {h[0]: h[1] for h in HUESOS}
CABEZA = {h[0]: h[2] for h in HUESOS}

# puntos de la suela del zapato en reposo (pie_X): talón y punta, para comprobar el contacto con el suelo
SUELA = {"pie_I": [(0.09, 0.08, 0.0), (0.09, -0.17, 0.0)], "pie_D": [(-0.09, 0.08, 0.0), (-0.09, -0.17, 0.0)]}


# ---------------------------------------------------------------- cinemática directa
def _rot(rx, ry, rz):
    cx, sx, cy, sy, cz, sz = math.cos(rx), math.sin(rx), math.cos(ry), math.sin(ry), math.cos(rz), math.sin(rz)
    # R = Rz · Ry · Rx
    return [[cz * cy, cz * sy * sx - sz * cx, cz * sy * cx + sz * sx],
            [sz * cy, sz * sy * sx + cz * cx, sz * sy * cx - cz * sx],
            [-sy, cy * sx, cy * cx]]


def _mv(m, v):
    return tuple(sum(m[i][j] * v[j] for j in range(3)) for i in range(3))


def posicion(hueso, punto, pose):
    """Posición en el espacio de la armadura de un punto del reposo, fijo al hueso, tras aplicar la pose."""
    p = punto
    b = hueso
    while b is not None:
        h = CABEZA[b]
        rot = pose.get("rot", {}).get(b, (0.0, 0.0, 0.0))
        off = pose.get("loc", {}).get(b, (0.0, 0.0, 0.0))
        r = _mv(_rot(*rot), tuple(p[i] - h[i] for i in range(3)))
        p = tuple(r[i] + h[i] + off[i] for i in range(3))
        b = PADRE[b]
    return p


def suela_min_z(pose):
    return min(posicion(pie, pt, pose)[2] for pie, pts in SUELA.items() for pt in pts)


def suela_centro_y(pose, pie):
    pts = [posicion(pie, pt, pose) for pt in SUELA[pie]]
    return sum(p[1] for p in pts) / len(pts)


# ---------------------------------------------------------------- utilidades de poses
def reflejar(pose):
    """Intercambia D e I: las rotaciones sobre X no cambian; la guiñada (rz) y el desplazamiento en x se invierten."""
    def cambiar(n):
        return n[:-1] + ("D" if n.endswith("I") else "I") if n[-2:] in ("_I", "_D") else n
    rot = {cambiar(n): ((v[0], -v[1], -v[2]) if n[-2:] not in ("_I", "_D") else (v[0], -v[1], -v[2]))
           for n, v in pose.get("rot", {}).items()}
    loc = {cambiar(n): (-v[0], v[1], v[2]) for n, v in pose.get("loc", {}).items()}
    return {"rot": rot, "loc": loc}


def con_suelo(pose):
    """Ajusta el desplazamiento vertical de la pelvis para que la suela más baja quede exactamente en z = 0."""
    base = {"rot": dict(pose.get("rot", {})), "loc": dict(pose.get("loc", {}))}
    x, y, _ = base["loc"].get("pelvis", (0.0, 0.0, 0.0))
    base["loc"]["pelvis"] = (x, y, 0.0)
    dz = -suela_min_z(base)
    base["loc"]["pelvis"] = (x, y, dz)
    return base


def _interpolar(a, b, t):
    def mezcla(da, db):
        return {n: tuple(da.get(n, (0.0, 0.0, 0.0))[i] + (db.get(n, (0.0, 0.0, 0.0))[i] - da.get(n, (0.0, 0.0, 0.0))[i]) * t
                         for i in range(3)) for n in set(da) | set(db)}
    return {"rot": mezcla(a.get("rot", {}), b.get("rot", {})), "loc": mezcla(a.get("loc", {}), b.get("loc", {}))}


def densificar(llaves, paso):
    """Añade llaves intermedias cada `paso` fotogramas (interpolación lineal) y vuelve a apoyar la suela en z = 0 en cada una.
    Evita que la interpolación de Blender hunda o levante los pies entre llaves lejanas."""
    frames = sorted(llaves)
    salida = dict(llaves)
    for f0, f1 in zip(frames, frames[1:]):
        for f in range(f0 + paso, f1, paso):
            salida[f] = con_suelo(_interpolar(llaves[f0], llaves[f1], (f - f0) / float(f1 - f0)))
    return salida


# ---------------------------------------------------------------- clip: Idle (60 fotogramas, bucle)
def _idle(fase):
    """fase en [-1, 1]: respiración y ligero balanceo."""
    s = fase
    return {"rot": {"columna": (0.020 * s, 0.0, 0.012 * s),
                    "cabeza": (-0.012 * s, 0.0, -0.020 * s),
                    "brazo_I": (0.050 * s, 0.0, 0.0), "brazo_D": (-0.050 * s, 0.0, 0.0),
                    "antebrazo_I": (-0.10 - 0.03 * s, 0.0, 0.0), "antebrazo_D": (-0.10 + 0.03 * s, 0.0, 0.0),
                    "pelvis": (0.0, 0.0, 0.015 * s)},
            "loc": {}}


def clip_idle():
    # 0 (neutro) -> 15 (inhala) -> 30 (neutro) -> 45 (exhala) -> 60 = 0
    llaves = {0: _idle(0.0), 15: _idle(1.0), 30: _idle(0.0), 45: _idle(-1.0), 60: _idle(0.0)}
    return {"nombre": "Idle", "frames": 60, "bucle": True, "llaves": llaves, "descripcion":
            "De pie, respiración lenta y balanceo mínimo; los pies no se mueven."}


# ---------------------------------------------------------------- clip: Caminar (32 fotogramas, bucle, en el sitio)
ALFA = 0.42        # amplitud del muslo (rad) = 24°


def _contacto_D_adelante():
    muslo_d, muslo_i = -ALFA, ALFA
    pant_d, pant_i = 0.08, 0.35
    pie_d = -0.15 - (muslo_d + pant_d)        # punta arriba al apoyar el talón
    pie_i = 0.45 - (muslo_i + pant_i)         # talón arriba al despegar
    return {"rot": {"pelvis": (0.0, 0.0, 0.07), "columna": (0.05, 0.0, -0.05), "cabeza": (0.0, 0.0, -0.02),
                    "muslo_D": (muslo_d, 0, 0), "muslo_I": (muslo_i, 0, 0),
                    "pantorrilla_D": (pant_d, 0, 0), "pantorrilla_I": (pant_i, 0, 0),
                    "pie_D": (pie_d, 0, 0), "pie_I": (pie_i, 0, 0),
                    "brazo_D": (0.40, 0, 0), "brazo_I": (-0.40, 0, 0),
                    "antebrazo_D": (-0.20, 0, 0), "antebrazo_I": (-0.45, 0, 0)},
            "loc": {}}


def _paso_D_apoyo():
    muslo_d, muslo_i = 0.05, -0.30
    pant_d, pant_i = 0.10, 0.95
    pie_d = 0.0 - (muslo_d + pant_d)          # pie plano en el apoyo
    pie_i = 0.30 - (muslo_i + pant_i)         # el pie en vuelo cuelga con la punta ligeramente abajo
    return {"rot": {"pelvis": (0.0, 0.0, 0.0), "columna": (0.05, 0.0, 0.0), "cabeza": (0.0, 0.0, 0.0),
                    "muslo_D": (muslo_d, 0, 0), "muslo_I": (muslo_i, 0, 0),
                    "pantorrilla_D": (pant_d, 0, 0), "pantorrilla_I": (pant_i, 0, 0),
                    "pie_D": (pie_d, 0, 0), "pie_I": (pie_i, 0, 0),
                    "brazo_D": (0.05, 0, 0), "brazo_I": (-0.05, 0, 0),
                    "antebrazo_D": (-0.30, 0, 0), "antebrazo_I": (-0.30, 0, 0)},
            "loc": {"pelvis": (-0.02, 0.0, 0.0)}}


def clip_caminar():
    k0 = con_suelo(_contacto_D_adelante())
    k8 = con_suelo(_paso_D_apoyo())
    k16 = con_suelo(reflejar(_contacto_D_adelante()))
    k24 = con_suelo(reflejar(_paso_D_apoyo()))
    llaves = densificar({0: k0, 8: k8, 16: k16, 24: k24, 32: k0}, 2)      # llaves principales cada 8 y apoyadas cada 2
    return {"nombre": "Caminar", "frames": 32, "bucle": True, "llaves": llaves, "descripcion":
            "Ciclo de paso en el sitio (sin movimiento de raíz); la pelvis sube y baja para que la suela toque el suelo."}


# ---------------------------------------------------------------- clip: Pedir (90 fotogramas, no es bucle)
def _pedir(brazo_alto, antebrazo, columna, cabeza, izq_brazo=(0.0, 0.0), izq_antebrazo=-0.10, sway=0.0, abduccion=0.0):
    return {"rot": {"brazo_D": (brazo_alto, 0.0, 0.0) if not abduccion else (brazo_alto, abduccion, 0.0),
                    "antebrazo_D": (antebrazo, 0.0, 0.0),
                    "mano_D": (0.0, 0.0, 0.0),
                    "brazo_I": (izq_brazo[0], izq_brazo[1], 0.0), "antebrazo_I": (izq_antebrazo, 0.0, 0.0),
                    "columna": (columna, 0.0, 0.0), "cabeza": (cabeza, 0.0, 0.0), "pelvis": (0.0, 0.0, sway)},
            "loc": {}}


def clip_pedir():
    reposo = _pedir(0.0, -0.10, 0.0, 0.0)
    llaves = {
        0: reposo,
        10: _pedir(0.05, -0.20, -0.03, -0.05, sway=0.02),                      # anticipación: respira y se echa atrás
        24: _pedir(-1.30, -0.55, 0.08, 0.12, (-0.30, -0.10), -1.00, 0.03, 0.12),  # sube el brazo derecho; la izquierda al vientre
        36: _pedir(-1.30, -0.90, 0.08, 0.18, (-0.30, -0.10), -1.00, 0.03, 0.12),  # cabeceo + gesto de pedir (1)
        48: _pedir(-1.30, -0.55, 0.08, 0.05, (-0.30, -0.10), -1.00, 0.03, 0.12),
        60: _pedir(-1.30, -0.90, 0.08, 0.18, (-0.30, -0.10), -1.00, 0.03, 0.12),  # gesto (2)
        74: _pedir(-0.40, -0.35, 0.02, 0.0, (-0.10, 0.0), -0.40, 0.0, 0.04),      # baja los brazos
        90: reposo,
    }
    return {"nombre": "Pedir", "frames": 90, "bucle": False, "llaves": llaves, "descripcion":
            "Se inclina, levanta la mano derecha con dos gestos de «una hamburguesa, por favor» y cabecea; la izquierda "
            "descansa en el vientre. Empieza y termina en el reposo para encadenar con Idle."}


CLIPS = {"Idle": clip_idle, "Caminar": clip_caminar, "Pedir": clip_pedir}


# ---------------------------------------------------------------- verificaciones
LIMITES = {  # (mínimo, máximo) de rx en rad; las rodillas solo se doblan hacia atrás y los codos hacia delante
    "muslo": (-1.2, 0.6), "pantorrilla": (0.0, 2.2), "pie": (-0.9, 0.9),
    "brazo": (-1.8, 0.7), "antebrazo": (-2.3, 0.0), "columna": (-0.3, 0.4), "cabeza": (-0.4, 0.5)}


def _limite(nombre):
    base = nombre.rsplit("_", 1)[0]
    return LIMITES.get(base)


def verificar_clip(clip):
    informe = {"nombre": clip["nombre"], "frames": clip["frames"], "bucle": clip["bucle"], "errores": [], "avisos": []}
    llaves = clip["llaves"]
    frames = sorted(llaves)
    if frames[0] != 0 or frames[-1] != clip["frames"]:
        informe["errores"].append("las llaves deben ir de 0 a %d" % clip["frames"])
    a, b = llaves[frames[0]], llaves[frames[-1]]
    cerrado = all(abs(a["rot"].get(n, (0, 0, 0))[i] - b["rot"].get(n, (0, 0, 0))[i]) < 1e-9
                  for n in set(a["rot"]) | set(b["rot"]) for i in range(3)) and \
        all(abs(a["loc"].get(n, (0, 0, 0))[i] - b["loc"].get(n, (0, 0, 0))[i]) < 1e-9
            for n in set(a["loc"]) | set(b["loc"]) for i in range(3))
    informe["primera_igual_a_ultima"] = cerrado
    if (clip["bucle"] or clip["nombre"] == "Pedir") and not cerrado:
        informe["errores"].append("la última llave debe ser igual a la primera")
    for f, pose in llaves.items():
        for n, (rx, ry, rz) in pose["rot"].items():
            lim = _limite(n)
            if lim and not (lim[0] - 1e-9 <= rx <= lim[1] + 1e-9):
                informe["errores"].append("f%d %s rx=%.2f fuera de [%.2f, %.2f]" % (f, n, rx, lim[0], lim[1]))
    suelos = {f: round(suela_min_z(pose), 4) for f, pose in llaves.items()}
    informe["suela_min_z_por_llave"] = suelos
    huesos = sorted({n for p in llaves.values() for n in p["rot"]})
    informe["huesos_animados"] = huesos
    return informe


def estadisticas_paso():
    """Velocidad de avance de referencia del ciclo de caminar: recorrido de la suela de un pie entre llaves de contacto."""
    c = clip_caminar()["llaves"]
    y0 = suela_centro_y(c[0], "pie_D")
    y16 = suela_centro_y(c[16], "pie_D")
    recorrido = y16 - y0
    return {"recorrido_pie_D_0_a_16_m": round(recorrido, 3),
            "velocidad_referencia_m_s": round(recorrido / (16.0 / FPS), 3),
            "longitud_de_paso_m": round(abs(suela_centro_y(c[0], "pie_D") - suela_centro_y(c[0], "pie_I")), 3)}
