"""Genera los assets propios del Casino FOX en Python puro (sin numpy ni PIL).

    python3 ArtSource/CasaMarea/casino_assets/generar_assets.py

Salidas (todo original, creado por este script; no hay material de terceros):
    Assets/_Game/Art/Casino/Textures/slot_symbols.png   640 x 256: cinco símbolos (celdas de 128 x 256)
    Assets/_Game/Art/Casino/Textures/casino_floor.png   128 x 128: piso a cuadros ciruela con filete mango
    Assets/_Game/Art/Casino/Textures/cobble.png         256 x 256: adoquín cálido para la calle y la plaza
    Assets/_Game/Art/Casino/Audio/*.wav                 efectos y ambiente (16 bits, 22.05 kHz, mono)

Orden de símbolos = enum SlotSymbol: Chile, Mango, Concha, Hamburguesa, Fox. Los símbolos se distinguen por FORMA y
por color (accesibilidad: no dependen solo del color).
"""
import math
import os
import random
import struct
import wave
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
TEX = os.path.join(ROOT, "Assets", "_Game", "Art", "Casino", "Textures")
AUD = os.path.join(ROOT, "Assets", "_Game", "Art", "Casino", "Audio")
os.makedirs(TEX, exist_ok=True)
os.makedirs(AUD, exist_ok=True)

PLUM = (59, 47, 74)
CREAM = (247, 238, 221)
CHILE = (228, 87, 46)
MANGO = (244, 201, 93)
TEAL = (43, 166, 160)
BUN = (217, 154, 78)
GREEN = (79, 122, 58)
LEAF = (111, 168, 63)
PATTY = (90, 53, 36)


# ---------------------------------------------------------------- lienzo y PNG
class Canvas:
    def __init__(self, w, h, color):
        self.w, self.h = w, h
        self.px = bytearray(bytes(color) * (w * h))

    def blend(self, x, y, color, a):
        if a <= 0 or not (0 <= x < self.w and 0 <= y < self.h):
            return
        i = (y * self.w + x) * 3
        for k in range(3):
            self.px[i + k] = int(self.px[i + k] * (1 - a) + color[k] * a + .5)

    def fill_sdf(self, sdf, color, bbox, alpha=1.0):
        x0, y0, x1, y1 = bbox
        for y in range(max(0, y0), min(self.h, y1)):
            for x in range(max(0, x0), min(self.w, x1)):
                d = sdf(x + .5, y + .5)
                a = max(0.0, min(1.0, .5 - d)) * alpha
                if a > 0:
                    self.blend(x, y, color, a)

    def save(self, path):
        raw = b"".join(b"\x00" + bytes(self.px[y * self.w * 3:(y + 1) * self.w * 3]) for y in range(self.h))

        def chunk(tag, data):
            c = struct.pack(">I", len(data)) + tag + data
            return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

        with open(path, "wb") as f:
            f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", self.w, self.h, 8, 2, 0, 0, 0))
                    + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


# ---------------------------------------------------------------- distancias con signo (en píxeles)
def sd_circle(cx, cy, r):
    return lambda x, y: math.hypot(x - cx, y - cy) - r


def sd_ellipse(cx, cy, a, b, deg=0.0):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))

    def f(x, y):
        dx, dy = x - cx, y - cy
        u, v = dx * c + dy * s, -dx * s + dy * c
        return (math.hypot(u / a, v / b) - 1.0) * min(a, b)
    return f


def sd_capsule(x0, y0, x1, y1, r0, r1=None):
    r1 = r0 if r1 is None else r1

    def f(x, y):
        px, py, dx, dy = x - x0, y - y0, x1 - x0, y1 - y0
        t = max(0.0, min(1.0, (px * dx + py * dy) / (dx * dx + dy * dy)))
        return math.hypot(px - dx * t, py - dy * t) - (r0 + (r1 - r0) * t)
    return f


def sd_box(cx, cy, hw, hh, rad=0.0):
    def f(x, y):
        qx, qy = abs(x - cx) - hw + rad, abs(y - cy) - hh + rad
        return math.hypot(max(qx, 0), max(qy, 0)) + min(max(qx, qy), 0) - rad
    return f


def sd_poly(pts):
    n = len(pts)

    def f(x, y):
        d = 1e9
        inside = False
        j = n - 1
        for i in range(n):
            xi, yi = pts[i]
            xj, yj = pts[j]
            ex, ey = xj - xi, yj - yi
            wx, wy = x - xi, y - yi
            t = max(0.0, min(1.0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
            d = min(d, math.hypot(wx - ex * t, wy - ey * t))
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                inside = not inside
            j = i
        return -d if inside else d
    return f


def union(*fs):
    return lambda x, y: min(f(x, y) for f in fs)


def inter(a, b):
    return lambda x, y: max(a(x, y), b(x, y))


def grow(f, r):
    return lambda x, y: f(x, y) - r


def bezier_chain(p0, p1, p2, r0, r1, n=22):
    pts = []
    for i in range(n + 1):
        t = i / n
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        pts.append((x, y, r0 + (r1 - r0) * t))
    return union(*[sd_circle(x, y, r) for x, y, r in pts])


# ---------------------------------------------------------------- símbolos (cada celda 128 x 256, símbolo en el centro)
def draw_symbol(cv, index, ox):
    cx, cy = ox + 64, 128
    bb = (ox + 2, cy - 96, ox + 126, cy + 96)
    cv_rect = (ox, 0, ox + 128, 256)
    cv.fill_sdf(sd_box(ox + 64, 128, 64, 128), CREAM, cv_rect)
    cv.fill_sdf(sd_box(ox + 64, 3, 64, 3), PLUM, cv_rect)           # filete de separación arriba y abajo
    cv.fill_sdf(sd_box(ox + 64, 253, 64, 3), PLUM, cv_rect)

    def at(f, dx=0, dy=0):
        return lambda x, y: f(x - dx, y - dy)

    def painted(shapes, outline=PLUM, w=4.0):
        """shapes: lista de (sdf, color). Dibuja un contorno ciruela grueso alrededor de la unión y luego los rellenos."""
        cv.fill_sdf(grow(union(*[s for s, _ in shapes]), w), outline, bb)
        for s, c in shapes:
            cv.fill_sdf(s, c, bb)

    if index == 0:      # Chile: cuerpo curvo, tapa verde y rabito
        body = at(bezier_chain((32, -52), (-40, -26), (-30, 52), 22, 5), cx, cy)
        cap = at(sd_ellipse(32, -54, 15, 11, 25), cx, cy)
        stem = at(sd_capsule(40, -58, 54, -76, 5), cx, cy)
        shade = inter(body, at(bezier_chain((40, -44), (-24, -22), (-22, 40), 7, 2), cx, cy))
        painted([(body, CHILE), (cap, GREEN), (stem, GREEN)])
        cv.fill_sdf(shade, (255, 140, 100), bb, .8)
    elif index == 1:    # Mango: elipse inclinada con rubor y hoja
        fruit = at(sd_ellipse(0, 8, 46, 36, -25), cx, cy)
        blush = inter(fruit, at(sd_ellipse(-16, 18, 36, 28, -25), cx, cy))
        leaf = at(sd_ellipse(16, -34, 20, 8, -50), cx, cy)
        painted([(fruit, MANGO), (leaf, LEAF)])
        cv.fill_sdf(blush, CHILE, bb, .75)
        cv.fill_sdf(at(sd_ellipse(14, -4, 10, 5, -30), cx, cy), (255, 247, 210), bb, .9)
    elif index == 2:    # Concha: pan dulce con borde festoneado y estrías curvas desde un punto superior
        def scallop(x, y):
            dx, dy = x - cx, y - cy
            return math.hypot(dx, dy) - (42 + 4 * math.sin(9 * math.atan2(dy, dx)))

        painted([(scallop, TEAL)])

        def ribs(x, y):
            dx, dy = x - cx, y - (cy - 6)
            ang = math.atan2(dy, dx)
            r = math.hypot(dx, dy)
            return 1.5 if math.sin(ang * 6.0 + r * 0.05) > 0.25 and r > 8 else -1.5

        cv.fill_sdf(inter(scallop, ribs), (127, 208, 200), bb, .95)
        cv.fill_sdf(inter(scallop, at(sd_ellipse(-10, -14, 14, 8, -30), cx, cy)), CREAM, bb, .55)
    elif index == 3:    # Hamburguesa: pan con ajonjolí, lechuga ondulada, carne y pan inferior
        top = inter(at(sd_ellipse(0, -6, 46, 34), cx, cy), lambda x, y: (y - (cy - 6)))
        lettuce = lambda x, y: abs(y - (cy + 6 + 4 * math.sin((x - cx) / 6.0))) - 6
        lettuce = inter(lettuce, at(sd_box(0, 6, 48, 20), cx, cy))
        patty = at(sd_box(0, 20, 46, 8, 6), cx, cy)
        bottom = at(sd_box(0, 38, 44, 8, 8), cx, cy)
        painted([(top, BUN), (lettuce, LEAF), (patty, PATTY), (bottom, BUN)])
        for sx, sy in ((-24, -22), (-8, -30), (10, -24), (26, -16), (0, -14)):
            cv.fill_sdf(at(sd_ellipse(sx, sy, 5, 3, 20), cx, cy), CREAM, bb)
    else:               # Fox: cabeza triangular con orejas, hocico crema y ojos rasgados
        head = at(sd_poly([(-44, -16), (44, -16), (0, 52)]), cx, cy)
        ear_l = at(sd_poly([(-44, -16), (-14, -16), (-40, -58)]), cx, cy)
        ear_r = at(sd_poly([(14, -16), (44, -16), (40, -58)]), cx, cy)
        painted([(head, CHILE), (ear_l, CHILE), (ear_r, CHILE)])
        cv.fill_sdf(inter(head, at(sd_poly([(-24, 12), (24, 12), (0, 56)]), cx, cy)), CREAM, bb)
        cv.fill_sdf(at(sd_poly([(-34, -4), (-14, -10), (-16, -2)]), cx, cy), PLUM, bb)
        cv.fill_sdf(at(sd_poly([(14, -10), (34, -4), (16, -2)]), cx, cy), PLUM, bb)
        cv.fill_sdf(at(sd_poly([(-6, 40), (6, 40), (0, 50)]), cx, cy), PLUM, bb)
        cv.fill_sdf(at(sd_poly([(-34, -48), (-22, -22), (-40, -22)]), cx, cy), (255, 200, 170), bb, .8)
        cv.fill_sdf(at(sd_poly([(34, -48), (40, -22), (22, -22)]), cx, cy), (255, 200, 170), bb, .8)


def make_symbols():
    cv = Canvas(640, 256, CREAM)
    for i in range(5):
        draw_symbol(cv, i, i * 128)
    cv.save(os.path.join(TEX, "slot_symbols.png"))


def make_floor():
    cv = Canvas(128, 128, PLUM)
    dark, light = (52, 40, 66), (74, 60, 92)
    for ty in range(2):
        for tx in range(2):
            col = dark if (tx + ty) % 2 == 0 else light
            cv.fill_sdf(sd_box(tx * 64 + 32, ty * 64 + 32, 32, 32), col, (tx * 64, ty * 64, tx * 64 + 64, ty * 64 + 64))
    for k in (0, 64):                                           # filetes mango entre losetas
        cv.fill_sdf(sd_box(k + .5, 64, 1.0, 64), MANGO, (0, 0, 128, 128), .55)
        cv.fill_sdf(sd_box(64, k + .5, 64, 1.0), MANGO, (0, 0, 128, 128), .55)
    cv.save(os.path.join(TEX, "casino_floor.png"))


def make_cobble():
    rnd = random.Random(2026)
    cv = Canvas(256, 256, (140, 127, 105))
    base = (190, 172, 143)
    for row in range(8):
        offset = 0 if row % 2 == 0 else 32
        for col in range(-1, 5):
            x0 = col * 64 + offset
            tone = rnd.randint(-14, 10)
            color = tuple(max(0, min(255, c + tone)) for c in base)
            cv.fill_sdf(sd_box(x0 + 32, row * 32 + 16, 30, 14, 5), color, (x0, row * 32, x0 + 64, row * 32 + 32))
    # el mosaico debe repetirse sin costuras: se duplica el borde que se sale
    cv.save(os.path.join(TEX, "cobble.png"))


# ---------------------------------------------------------------- audio
RATE = 22050


def save_wav(name, samples, peak=0.85):
    top = max(1e-9, max(abs(s) for s in samples))
    scale = peak / top if top > peak else 1.0
    with wave.open(os.path.join(AUD, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s * scale)) * 32767)) for s in samples))


def tone(freq, dur, vol=0.5, shape="sine", decay=6.0, attack=0.004):
    n = int(RATE * dur)
    out = []
    for i in range(n):
        t = i / RATE
        ph = 2 * math.pi * freq * t
        if shape == "sine":
            v = math.sin(ph)
        elif shape == "tri":
            v = 2 / math.pi * math.asin(math.sin(ph))
        elif shape == "bell":
            v = math.sin(ph) + .5 * math.sin(ph * 2.76) + .25 * math.sin(ph * 5.4)
        else:
            v = 1.0 if math.sin(ph) > 0 else -1.0
        env = min(1.0, t / attack) * math.exp(-decay * t)
        out.append(v * env * vol)
    return out


def noise(dur, vol=.5, decay=40.0, lowpass=0.0, seed=1):
    rnd = random.Random(seed)
    out, prev = [], 0.0
    for i in range(int(RATE * dur)):
        v = rnd.uniform(-1, 1)
        prev = prev * lowpass + v * (1 - lowpass) if lowpass else v
        out.append(prev * vol * math.exp(-decay * i / RATE))
    return out


def mix(*tracks, length=None):
    n = length or max(len(t) for t in tracks)
    out = [0.0] * n
    for t in tracks:
        for i, v in enumerate(t[:n]):
            out[i] += v
    return out


def place(track, at, length):
    pad = [0.0] * int(RATE * at)
    return (pad + track + [0.0] * length)[:length]


def make_audio():
    # tic tic de los rodillos girando: bucle de 1 s con 18 clics y un zumbido grave
    n = RATE
    spin = [0.0] * n
    for k in range(18):
        click = mix(noise(0.012, .55, 220, .55, seed=k + 3), tone(1800 + (k % 3) * 300, 0.012, .2, "sine", 160), length=int(RATE * .02))
        for i, v in enumerate(click):
            j = int(k * n / 18) + i
            if j < n:
                spin[j] += v
    hum = [0.12 * math.sin(2 * math.pi * 90 * i / RATE) + 0.06 * math.sin(2 * math.pi * 135 * i / RATE) for i in range(n)]
    save_wav("slot_spin_loop", mix(spin, hum, length=n), .8)

    save_wav("slot_start", mix(tone(70, .25, .9, "sine", 14), noise(.05, .6, 60, .3, seed=9),
                               place(tone(1500, .06, .35, "bell", 40), .04, int(RATE * .25))), .9)
    save_wav("reel_stop", mix(noise(.03, .8, 90, .4, seed=5), tone(210, .14, .8, "sine", 28), tone(900, .05, .25, "bell", 60)), .9)
    save_wav("bet_change", mix(tone(660, .07, .5, "tri", 20), place(tone(880, .09, .5, "tri", 18), .07, int(RATE * .18))), .7)
    save_wav("lose", mix(tone(392, .16, .45, "tri", 10), place(tone(330, .26, .45, "tri", 9), .15, int(RATE * .45))), .6)

    def arpeggio(notes, step, vol, bell=True):
        total = int(RATE * (step * len(notes) + .7))
        tr = []
        for i, f in enumerate(notes):
            tr.append(place(mix(tone(f, .5, vol, "tri", 7), tone(f * 2, .4, vol * .3, "bell" if bell else "sine", 9)), i * step, total))
        return mix(*tr, length=total)

    save_wav("win_small", mix(arpeggio([523, 659, 784], .09, .5),
                              place(tone(2093, .3, .2, "bell", 12), .30, int(RATE * 1.0))), .8)
    save_wav("win_big", mix(arpeggio([392, 494, 587, 784, 988, 1175], .085, .5),
                            *[place(tone(1760 + 220 * (k % 4), .25, .16, "bell", 14), .55 + k * .09, int(RATE * 1.6)) for k in range(8)]), .85)
    jack = []
    for i, chord in enumerate(([392, 494, 587], [440, 554, 659], [523, 659, 784], [587, 740, 880], [784, 988, 1175])):
        total = int(RATE * 3.0)
        for f in chord:
            jack.append(place(mix(tone(f, .75, .22, "sq" if i < 4 else "tri", 3.2), tone(f * 2, .6, .1, "bell", 4)), i * .36, total))
    sparkles = [place(tone(2093 + 330 * (k % 5), .22, .14, "bell", 16), .3 + k * .07, int(RATE * 3.0)) for k in range(26)]
    save_wav("jackpot", mix(*jack, *sparkles, length=int(RATE * 3.0)), .9)
    save_wav("coin", mix(tone(1760, .28, .6, "bell", 14), place(tone(2637, .22, .5, "bell", 16), .05, int(RATE * .3))), .8)

    # ambiente de salón: acordes suaves en bucle de 12 s a 100 BPM con maracas discretas
    dur = 12.0
    n = int(RATE * dur)
    chords = [[261.6, 329.6, 392.0, 493.9], [349.2, 440.0, 523.3, 659.3], [293.7, 370.0, 440.0, 554.4], [392.0, 493.9, 587.3, 740.0]]
    pad = [0.0] * n
    seg = n // 4
    for ci, chord in enumerate(chords):
        for f in chord:
            for i in range(seg):
                t = (ci * seg + i) / RATE
                env = math.sin(math.pi * i / seg) ** 0.6                      # entra y sale suave; empalma con el siguiente
                pad[ci * seg + i] += 0.045 * env * (math.sin(2 * math.pi * f * t) + 0.35 * math.sin(2 * math.pi * f * 2.003 * t))
    beat = RATE * 60 // 100
    shaker = [0.0] * n
    rnd = random.Random(77)
    for k in range(int(dur * 100 / 60) * 2):
        start = int(k * beat / 2)
        vol = 0.055 if k % 2 == 0 else 0.03
        for i in range(int(RATE * .05)):
            if start + i < n:
                shaker[start + i] += rnd.uniform(-1, 1) * vol * math.exp(-60 * i / RATE)
    save_wav("casino_ambient", mix(pad, shaker, length=n), .5)


def make_jackpot_coins():
    """Lluvia de monedas del jackpot: unos 3.6 s de monedas que caen, primero espaciadas, luego densas y al final de nuevo sueltas.
    Original y determinista (semilla fija). Se genera sola con --jackpot-coins para no tocar el resto de los assets."""
    dur = 3.6
    n = int(RATE * dur)
    rnd = random.Random(2026)
    tracks = []
    for k in range(78):
        u = rnd.random()
        # más densidad hacia el centro de la lluvia (campana suave)
        at = .05 + (dur - .5) * (.5 + .5 * math.sin((u - .5) * math.pi) ** 1)
        freq = rnd.choice([1760, 1976, 2093, 2349, 2637, 2794, 3136])
        vol = rnd.uniform(.12, .3)
        clink = mix(tone(freq, .22, vol, "bell", rnd.uniform(14, 24)), noise(.012, vol * .8, 200, .5, seed=k + 11), length=int(RATE * .24))
        tracks.append(place(clink, at, n))
    rumble = [0.05 * math.sin(2 * math.pi * 120 * i / RATE) * math.sin(math.pi * i / n) ** 2 for i in range(n)]
    save_wav("jackpot_coins", mix(*tracks, rumble, length=n), .8)


if __name__ == "__main__":
    if "--jackpot-coins" in __import__("sys").argv:
        make_jackpot_coins()
        print("lluvia de monedas del jackpot generada en", os.path.relpath(AUD, ROOT))
        raise SystemExit(0)
    make_symbols()
    make_floor()
    make_cobble()
    make_audio()
    make_jackpot_coins()
    print("assets del casino generados en", os.path.relpath(TEX, ROOT), "y", os.path.relpath(AUD, ROOT))
