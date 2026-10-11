"""Rig, animaciones y expresiones de los tres clientes de Casa Marea (Blender).

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_clientes_animados.py

Qué genera, para SM_Cliente_Pescador, SM_Cliente_Vecina y SM_Cliente_Joven (mismo diseño que los estáticos de
build_clientes_vecinas.py, ahora con pesos por hueso):
  - una armadura de 16 huesos (rigdata.py), idéntica en los tres, para compartir animaciones;
  - tres clips: Idle (60 f), Caminar (32 f, en el sitio) y Pedir (90 f), a 30 fps;
  - seis formas de expresión (shape keys / blend shapes): Feliz, Triste, Sorpresa, Enojado, Hablar, Parpadeo.

Salidas:
    Assets/_Game/Models/CasaMarea/Animados/SM_Cliente_<Nombre>.fbx            malla + armadura + blend shapes (sin animación)
    Assets/_Game/Models/CasaMarea/Animados/SM_Cliente_<Nombre>@Idle|Caminar|Pedir.fbx   solo armadura + un clip
    ArtSource/CasaMarea/Clientes_Animados.blend
    ArtSource/CasaMarea/Preview/Animados_*.png, Expresiones_*.png
    ArtSource/CasaMarea/stats_clientes_animados.json

Convenciones: 1 u = 1 m, Z arriba, frente a -Y, origen en el centro de los pies (z = 0). Los FBX usan las mismas opciones de
eje que el resto del arte de Casa Marea (en Unity la raíz llega con -90° en X, como los demás modelos).
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import rigdata  # noqa: E402
import build_props_barrio as P  # noqa: E402
from build_props_barrio import box, cyl, cone, sphere, finalize, stat  # noqa: E402

bpy = P.bpy

ANIM_DIR = os.path.join(P.PROJECT, "Assets", "_Game", "Models", "CasaMarea", "Animados")

P.MATS.update({
    "M_Piel_Clara":   ("#E3B58D", 0.75, 0.00, None, 0.0),
    "M_Piel_Media":   ("#C98B5E", 0.75, 0.00, None, 0.0),
    "M_Piel_Oscura":  ("#6B4530", 0.75, 0.00, None, 0.0),
    "M_Madera_Clara": ("#B88556", 0.70, 0.00, None, 0.0),
    "M_Acero_Claro":  ("#8E9AA0", 0.40, 0.45, None, 0.0),
    "M_Pizarra":      ("#3B2F4A", 0.80, 0.00, None, 0.0),
})

PRESUPUESTO = 1000
CLIENTES = {
    "SM_Cliente_Pescador": dict(piel="M_Piel_Media", camisa="M_Turquesa", pantalon="M_Pizarra", tipo="pescador"),
    "SM_Cliente_Vecina": dict(piel="M_Piel_Clara", camisa="M_Chile", pantalon=None, tipo="vecina"),
    "SM_Cliente_Joven": dict(piel="M_Piel_Oscura", camisa="M_Crema", pantalon="M_Piedra_Caliza", tipo="joven"),
}
GRUPOS_CARA = ["cara_ojo_I", "cara_ojo_D", "cara_ceja_I", "cara_ceja_D", "cara_boca_c", "cara_boca_I", "cara_boca_D"]


# ---------------------------------------------------------------- geometría con pesos
def _parte(obj, hueso, extras=()):
    """Asigna todos los vértices de la pieza a un hueso (peso 1.0) y, opcionalmente, a grupos de cara."""
    idx = list(range(len(obj.data.vertices)))
    for g in (hueso,) + tuple(extras):
        obj.vertex_groups.new(name=g).add(idx, 1.0, "REPLACE")
    return obj


def _lado(sx):
    return "I" if sx > 0 else "D"


def construir_cliente(nombre, piel, camisa, pantalon, tipo):
    """Mismo diseño que los clientes estáticos, con piernas y brazos articulados y cara con piezas separadas."""
    vestido = pantalon is None
    abajo = piel if vestido else pantalon
    # piernas
    for sx in (-0.09, 0.09):
        L = _lado(sx)
        _parte(box("muslo", (0.12, 0.14, 0.40), (sx, 0.0, 0.64), abajo if not vestido else piel), "muslo_" + L)
        _parte(sphere("rodilla", 0.075, (sx, 0.0, 0.44), abajo if not vestido else piel, seg=6, rings=4),
               "pantorrilla_" + L)
        _parte(box("pantorrilla", (0.12, 0.14, 0.36), (sx, 0.0, 0.26), abajo if not vestido else piel),
               "pantorrilla_" + L)
        _parte(box("zapato", (0.13, 0.25, 0.08), (sx, -0.045, 0.04), "M_Herreria"), "pie_" + L)
    # pelvis y tronco
    _parte(box("pelvis", (0.38, 0.21, 0.16), (0, 0, 0.84), camisa if vestido else pantalon), "pelvis")
    if vestido:
        _parte(cone("vestido", 0.27, 0.17, 0.80, (0, 0, 0.95), camisa, verts=12), "columna")
    else:
        _parte(box("torso", (0.40, 0.22, 0.55), (0, 0, 1.075), camisa), "columna")
    # brazos en cadena: hombro -> manga -> codo -> antebrazo -> mano
    th = rigdata.ANGULO_BRAZO
    for sx in (-1, 1):
        L = _lado(sx)
        punto = rigdata._brazo(sx)
        phi = -sx * th
        _parte(sphere("hombro", 0.065, punto(0.0), camisa, seg=6, rings=4), "brazo_" + L)
        _parte(box("manga", (0.11, 0.11, 0.27), punto(0.135), camisa, (0, phi, 0)), "brazo_" + L)
        _parte(sphere("codo", 0.05, punto(0.27), piel, seg=6, rings=4), "antebrazo_" + L)
        _parte(box("antebrazo", (0.085, 0.085, 0.29), punto(0.415), piel, (0, phi, 0)), "antebrazo_" + L)
        _parte(sphere("mano", 0.05, punto(0.59), piel, seg=6, rings=4), "mano_" + L)
    # cuello, cabeza y cara
    _parte(cyl("cuello", 0.05, 0.08, (0, 0, 1.39), piel, verts=8), "cabeza")
    _parte(sphere("cabeza", 0.15, (0, 0, 1.55), piel, (1, 1, 1), 8, 6), "cabeza")
    for sx in (-0.055, 0.055):
        L = _lado(sx)
        _parte(box("ojo", (0.03, 0.012, 0.035), (sx, -0.146, 1.57), "M_Herreria"), "cabeza", ("cara_ojo_" + L,))
        _parte(box("ceja", (0.045, 0.012, 0.010), (sx, -0.147, 1.615), "M_Herreria"), "cabeza", ("cara_ceja_" + L,))
    _parte(box("boca", (0.05, 0.012, 0.014), (0, -0.147, 1.505), "M_Herreria"), "cabeza", ("cara_boca_c",))
    for sx in (-0.04, 0.04):
        L = _lado(sx)
        _parte(box("boca_comisura", (0.022, 0.012, 0.014), (sx, -0.147, 1.505), "M_Herreria"), "cabeza",
               ("cara_boca_" + L,))
    # accesorios por cliente
    if tipo == "pescador":
        _parte(cone("sombrero_ala", 0.27, 0.19, 0.04, (0, 0, 1.67), "M_Madera_Clara", verts=12), "cabeza")
        _parte(cyl("sombrero_copa", 0.14, 0.11, (0, 0, 1.745), "M_Madera_Clara", verts=8), "cabeza")
        _parte(box("sombrero_cinta", (0.29, 0.29, 0.025), (0, 0, 1.705), "M_Chile"), "cabeza")
    elif tipo == "vecina":
        _parte(sphere("cabello", 0.16, (0, 0.012, 1.60), "M_Acero_Claro", (1, 1.05, 0.75), 8, 4), "cabeza")
        _parte(sphere("chongo", 0.07, (0, 0.13, 1.68), "M_Acero_Claro", seg=6, rings=4), "cabeza")
        _parte(box("delantal", (0.30, 0.012, 0.36), (0, -0.115, 0.95), "M_Crema"), "columna")
    else:
        _parte(sphere("gorra", 0.16, (0, 0.0, 1.60), "M_Chile", (1, 1.05, 0.75), 8, 4), "cabeza")
        _parte(box("gorra_visera", (0.24, 0.14, 0.02), (0, -0.20, 1.60), "M_Chile"), "cabeza")
    return finalize(nombre)


# ---------------------------------------------------------------- armadura y animación
def crear_armadura(nombre):
    from mathutils import Vector
    datos = bpy.data.armatures.new(nombre + "_datos")
    arm = bpy.data.objects.new(nombre.replace("SM_", "Armadura_"), datos)
    bpy.context.scene.collection.objects.link(arm)
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = datos.edit_bones
    for n, padre, cab, col in rigdata.HUESOS:
        e = eb.new(n)
        e.head, e.tail = Vector(cab), Vector(col)
    for n, padre, cab, col in rigdata.HUESOS:
        if padre:
            eb[n].parent = eb[padre]
            eb[n].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def enlazar_malla(arm, malla):
    malla.parent = arm
    mod = malla.modifiers.new("Armadura", "ARMATURE")
    mod.object = arm
    mod.use_vertex_groups = True


def _poner_pose(arm, pose, fotograma):
    from mathutils import Euler, Vector
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        rot = pose.get("rot", {}).get(pb.name, (0.0, 0.0, 0.0))
        loc = pose.get("loc", {}).get(pb.name, (0.0, 0.0, 0.0))
        reposo = pb.bone.matrix_local.to_3x3()
        rw = Euler(rot, "XYZ").to_matrix()
        pb.rotation_quaternion = (reposo.inverted() @ rw @ reposo).to_quaternion()
        pb.location = reposo.inverted() @ Vector(loc)
        pb.keyframe_insert("rotation_quaternion", frame=fotograma)
        pb.keyframe_insert("location", frame=fotograma)


def asignar_accion(arm, accion):
    ad = arm.animation_data or arm.animation_data_create()
    ad.action = accion
    try:
        if getattr(ad, "action_slot", None) is None and len(accion.slots) > 0:
            ad.action_slot = accion.slots[0]
    except Exception:
        pass
    return ad


def crear_clips(arm):
    acciones = {}
    for nombre, fn in rigdata.CLIPS.items():
        clip = fn()
        accion = bpy.data.actions.new(nombre)
        accion.use_fake_user = True
        asignar_accion(arm, accion)
        for f in sorted(clip["llaves"]):
            _poner_pose(arm, clip["llaves"][f], f + 1)           # Blender empieza en 1
        arm.animation_data.action = None
        acciones[nombre] = (accion, clip)
    return acciones


# ---------------------------------------------------------------- expresiones
def _indices_por_grupo(malla):
    nombres = {g.index: g.name for g in malla.vertex_groups}
    por_grupo = {}
    for v in malla.data.vertices:
        for g in v.groups:
            por_grupo.setdefault(nombres[g.group], []).append(v.index)
    return por_grupo


def crear_expresiones(malla):
    """Seis shape keys que solo mueven las piezas de la cara (ojos, cejas, boca)."""
    from mathutils import Vector
    grupos = _indices_por_grupo(malla)
    base = [v.co.copy() for v in malla.data.vertices]
    malla.shape_key_add(name="Neutro", from_mix=False)

    def idx(*nombres):
        return [i for n in nombres for i in grupos.get(n, [])]

    def centro(ids):
        return sum((base[i] for i in ids), Vector()) / max(len(ids), 1)

    def mover(sk, ids, d):
        for i in ids:
            sk.data[i].co = sk.data[i].co + Vector(d)

    def escalar(sk, ids, sx, sy, sz):
        c = centro(ids)
        for i in ids:
            p = sk.data[i].co
            sk.data[i].co = Vector((c.x + (p.x - c.x) * sx, c.y + (p.y - c.y) * sy, c.z + (p.z - c.z) * sz))

    def inclinar(sk, lado, k):
        """Inclina una ceja: k > 0 sube el extremo interior (más cerca de la nariz), k < 0 lo baja."""
        ids = idx("cara_ceja_" + lado)
        c = centro(ids)
        s = 1 if lado == "I" else -1
        for i in ids:
            interior = -s * (base[i].x - c.x)
            sk.data[i].co = sk.data[i].co + Vector((0, 0, k * interior))

    ojos, cejas = idx("cara_ojo_I", "cara_ojo_D"), idx("cara_ceja_I", "cara_ceja_D")
    comisuras = {"I": idx("cara_boca_I"), "D": idx("cara_boca_D")}
    centro_boca = idx("cara_boca_c")

    def feliz(sk):
        escalar(sk, ojos, 1.0, 1.0, 0.55)
        mover(sk, cejas, (0, 0, 0.006))
        escalar(sk, centro_boca, 1.5, 1.0, 1.0)
        for L, s in (("I", 1), ("D", -1)):
            mover(sk, comisuras[L], (s * 0.012, 0, 0.016))

    def triste(sk):
        for L in ("I", "D"):
            inclinar(sk, L, 0.5)
        mover(sk, cejas, (0, 0, 0.004))
        mover(sk, ojos, (0, 0, -0.004))
        escalar(sk, centro_boca, 0.8, 1.0, 1.0)
        for L in ("I", "D"):
            mover(sk, comisuras[L], (0, 0, -0.016))

    def sorpresa(sk):
        escalar(sk, ojos, 1.3, 1.0, 1.6)
        mover(sk, cejas, (0, 0, 0.03))
        escalar(sk, centro_boca, 0.7, 1.0, 4.0)
        mover(sk, centro_boca, (0, 0, -0.008))
        for L, s in (("I", 1), ("D", -1)):
            mover(sk, comisuras[L], (-s * 0.012, 0, -0.004))

    def enojado(sk):
        for L in ("I", "D"):
            inclinar(sk, L, -0.8)
        mover(sk, cejas, (0, 0, -0.008))
        escalar(sk, ojos, 1.0, 1.0, 0.7)
        escalar(sk, centro_boca, 0.9, 1.0, 1.0)
        for L in ("I", "D"):
            mover(sk, comisuras[L], (0, 0, -0.01))

    def hablar(sk):
        escalar(sk, centro_boca, 1.0, 1.0, 3.0)
        mover(sk, centro_boca, (0, 0, -0.004))
        for L in ("I", "D"):
            mover(sk, comisuras[L], (0, 0, -0.004))

    def parpadeo(sk):
        escalar(sk, ojos, 1.0, 1.0, 0.1)

    claves = {"Feliz": feliz, "Triste": triste, "Sorpresa": sorpresa, "Enojado": enojado, "Hablar": hablar,
              "Parpadeo": parpadeo}
    caras = set(idx(*GRUPOS_CARA))
    informe = {}
    for n, fn in claves.items():
        sk = malla.shape_key_add(name=n, from_mix=False)
        fn(sk)
        movidos = [i for i in range(len(base)) if (sk.data[i].co - base[i]).length > 1e-9]
        desplazamiento = max(((sk.data[i].co - base[i]).length for i in movidos), default=0.0)
        informe[n] = {"vertices_movidos": len(movidos), "solo_cara": all(i in caras for i in movidos),
                      "desplazamiento_max_m": round(desplazamiento, 4)}
    return list(claves), informe


# ---------------------------------------------------------------- exportación
def exportar(objs, ruta, animacion):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    tipos = {"ARMATURE"} if animacion else {"ARMATURE", "MESH"}
    bpy.ops.export_scene.fbx(filepath=ruta, use_selection=True, object_types=tipos,
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP", embed_textures=False,
                             add_leaf_bones=False, use_armature_deform_only=False, armature_nodetype="NULL",
                             bake_anim=animacion, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_step=1.0,
                             bake_anim_simplify_factor=0.0)
    return ruta


def muestrear(arm, malla, accion, clip):
    """Evalúa la malla deformada en cada fotograma: altura mínima (suelas) y extensión total."""
    dg = bpy.context.evaluated_depsgraph_get
    asignar_accion(arm, accion)
    escena = bpy.context.scene
    minz, maxz, tope_min_z = [], [], []
    ext = [[9e9, -9e9], [9e9, -9e9], [9e9, -9e9]]
    for f in range(clip["frames"] + 1):
        escena.frame_set(f + 1)
        bpy.context.view_layer.update()
        e = malla.evaluated_get(dg())
        me = e.to_mesh()
        zs = [v.co.z for v in me.vertices]
        minz.append(min(zs))
        maxz.append(max(zs))
        for k, eje in enumerate("xyz"):
            vals = [getattr(v.co, eje) for v in me.vertices]
            ext[k][0], ext[k][1] = min(ext[k][0], min(vals)), max(ext[k][1], max(vals))
        e.to_mesh_clear()
    arm.animation_data.action = None
    return {"min_z_peor_caso_m": round(min(minz), 4), "min_z_mejor_caso_m": round(max(minz), 4),
            "altura_max_m": round(max(maxz), 4),
            "extension_m": {"x": [round(ext[0][0], 3), round(ext[0][1], 3)], "y": [round(ext[1][0], 3), round(ext[1][1], 3)],
                            "z": [round(ext[2][0], 3), round(ext[2][1], 3)]}}


# ---------------------------------------------------------------- vistas previas
def componer(rutas, salida, columnas):
    import numpy as np
    imgs = []
    for r in rutas:
        im = bpy.data.images.load(r)
        im.colorspace_settings.name = "Non-Color"
        w, h = im.size
        imgs.append(np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4))
    filas = int(math.ceil(len(imgs) / float(columnas)))
    h, w = imgs[0].shape[:2]
    hoja = np.ones((filas * h, columnas * w, 4), dtype=np.float32)
    for i, a in enumerate(imgs):
        r, c = divmod(i, columnas)
        y0 = (filas - 1 - r) * h
        hoja[y0:y0 + h, c * w:(c + 1) * w] = a
    out = bpy.data.images.new(os.path.basename(salida), columnas * w, filas * h, alpha=False)
    out.colorspace_settings.name = "Non-Color"
    out.pixels = hoja.ravel().tolist()
    out.filepath_raw = salida
    out.file_format = "PNG"
    out.save()
    for r in rutas:
        os.remove(r)


def hoja_de_clip(scene, arm, accion, frames, camara, nombre_salida):
    asignar_accion(arm, accion)
    rutas = []
    for k, f in enumerate(frames):
        scene.frame_set(f + 1)
        bpy.context.view_layer.update()
        ruta = os.path.join(P.PREVIEW_DIR, "_tmp_%s_%d.png" % (nombre_salida, k))
        P.render_to(scene, camara, ruta)
        rutas.append(ruta)
    arm.animation_data.action = None
    componer(rutas, os.path.join(P.PREVIEW_DIR, nombre_salida + ".png"), len(frames))


# ---------------------------------------------------------------- principal
def main():
    os.makedirs(ANIM_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0
    scene.render.fps = rigdata.FPS
    informe = {"fps": rigdata.FPS, "huesos": [h[0] for h in rigdata.HUESOS], "clientes": {}}
    sets = {}
    for nombre, spec in CLIENTES.items():
        malla = construir_cliente(nombre, **spec)
        arm = crear_armadura(nombre)
        enlazar_malla(arm, malla)
        acciones = crear_clips(arm)
        formas, expr = crear_expresiones(malla)
        sin_peso = [v.index for v in malla.data.vertices if not any(g.weight > 0 for g in v.groups)]
        grupos_hueso = {g.name for g in malla.vertex_groups} & set(informe["huesos"])
        datos = {"triangulos": P.tri_count(malla), "presupuesto": PRESUPUESTO,
                 "dentro_de_presupuesto": P.tri_count(malla) <= PRESUPUESTO,
                 "vertices": len(malla.data.vertices), "vertices_sin_peso": len(sin_peso),
                 "huesos_con_vertices": sorted(grupos_hueso), "huesos_sin_vertices": sorted(set(informe["huesos"]) - grupos_hueso),
                 "dimensiones_reposo_m": stat(malla)["dimensiones_XxYxZ_m"],
                 "min_z_reposo_m": stat(malla)["min"][2], "materiales": [m.name for m in malla.data.materials],
                 "expresiones": expr, "clips": {}, "fbx": {}}
        datos["fbx"]["modelo"] = os.path.relpath(
            exportar([arm, malla], os.path.join(ANIM_DIR, nombre + ".fbx"), False), P.PROJECT)
        for cn, (accion, clip) in acciones.items():
            asignar_accion(arm, accion)
            scene.frame_start, scene.frame_end = 1, clip["frames"] + 1
            scene.frame_set(1)
            ruta = os.path.join(ANIM_DIR, "%s@%s.fbx" % (nombre, cn))
            datos["fbx"][cn] = os.path.relpath(exportar([arm], ruta, True), P.PROJECT)
            arm.animation_data.action = None
            m = muestrear(arm, malla, accion, clip)
            datos["clips"][cn] = {"fotogramas": clip["frames"] + 1, "fps": rigdata.FPS,
                                  "duracion_s": round(clip["frames"] / float(rigdata.FPS), 3), "bucle": clip["bucle"],
                                  "llaves": sorted(f + 1 for f in clip["llaves"]), "muestreo": m}
        informe["clientes"][nombre] = datos
        sets[nombre] = (arm, malla, acciones)
    informe["ciclo_de_caminar"] = rigdata.estadisticas_paso()
    informe["huesos_total"] = len(informe["huesos"])
    with open(os.path.join(HERE, "stats_clientes_animados.json"), "w", encoding="utf-8") as f:
        json.dump(informe, f, indent=2, ensure_ascii=False)

    # ----- vistas previas (después de exportar: ahora sí se mueven los modelos)
    posiciones = {"SM_Cliente_Pescador": -1.2, "SM_Cliente_Vecina": 0.0, "SM_Cliente_Joven": 1.2}
    for n, (arm, malla, _) in sets.items():
        arm.location.x = posiciones[n]
    P.ctx_box("Suelo", (9, 6, 0.1), (0, 0, -0.05), "#B8A78C")
    P.add_world(scene)
    P.add_lights()
    P.setup_render(scene, 480, 640)
    cam_t = P.add_camera("Cam_3q", (2.6, -3.4, 1.15), (0, 0, 0.95), 32)
    cam_l = P.add_camera("Cam_lado", (4.2, 0.0, 1.0), (0, 0, 0.9), 32)

    def solo(nombre):
        for n, (arm, malla, _) in sets.items():
            arm.hide_render = malla.hide_render = (n != nombre)

    solo("SM_Cliente_Pescador")
    arm, malla, acc = sets["SM_Cliente_Pescador"]
    for n in sets:
        sets[n][0].location.x = 0.0
    hoja_de_clip(scene, arm, acc["Idle"][0], [0, 15, 30, 45], cam_t, "Animados_Idle")
    hoja_de_clip(scene, arm, acc["Caminar"][0], [0, 8, 16, 24], cam_l, "Animados_Caminar_lado")
    hoja_de_clip(scene, arm, acc["Caminar"][0], [0, 8, 16, 24], cam_t, "Animados_Caminar_3q")
    hoja_de_clip(scene, arm, acc["Pedir"][0], [0, 10, 24, 36, 60, 74], cam_t, "Animados_Pedir")
    # expresiones: primer plano de la cabeza, un cliente por hoja
    cam_cara = P.add_camera("Cam_cara", (0, -1.05, 1.56), (0, 0, 1.56), 22)
    for n, (arm, malla, acciones) in sets.items():
        solo(n)
        rutas = []
        nombres = ["Neutro", "Feliz", "Triste", "Sorpresa", "Enojado", "Hablar", "Parpadeo"]
        for k, ex in enumerate(nombres):
            for sk in malla.data.shape_keys.key_blocks:
                sk.value = 1.0 if sk.name == ex and ex != "Neutro" else 0.0
            ruta = os.path.join(P.PREVIEW_DIR, "_tmp_expr_%s_%d.png" % (n, k))
            P.render_to(scene, cam_cara, ruta)
            rutas.append(ruta)
        for sk in malla.data.shape_keys.key_blocks:
            sk.value = 0.0
        componer(rutas, os.path.join(P.PREVIEW_DIR, "Expresiones_%s.png" % n.replace("SM_Cliente_", "")), 4)
    # los tres juntos, cada uno en una pose distinta
    for n, (arm, malla, _) in sets.items():
        arm.hide_render = malla.hide_render = False
        arm.location.x = posiciones[n]
    P.setup_render(scene, 1280, 720)
    for n, clip, f in (("SM_Cliente_Pescador", "Idle", 15), ("SM_Cliente_Vecina", "Caminar", 4),
                       ("SM_Cliente_Joven", "Pedir", 36)):
        asignar_accion(sets[n][0], sets[n][2][clip][0])
    scene.frame_set(1)
    # cada armadura tiene su acción; se fija el fotograma de cada una con un desplazamiento de tiempo por clip
    for n, clip, f in (("SM_Cliente_Pescador", "Idle", 15), ("SM_Cliente_Vecina", "Caminar", 4),
                       ("SM_Cliente_Joven", "Pedir", 36)):
        sets[n][0].animation_data.action = None
        _poner_pose(sets[n][0], rigdata.CLIPS[clip]()["llaves"][min(rigdata.CLIPS[clip]()["llaves"], key=lambda k: abs(k - f))], 1000)
    scene.frame_set(1000)
    P.render_to(scene, P.add_camera("Cam_equipo", (0, -4.6, 1.05), (0, 0, 0.9), 34),
                os.path.join(P.PREVIEW_DIR, "Animados_equipo.png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Clientes_Animados.blend"))
    for n, d in informe["clientes"].items():
        print("[%s] tris=%d tope=%d sin_peso=%d dims=%s" % (n, d["triangulos"], d["presupuesto"],
                                                           d["vertices_sin_peso"], d["dimensiones_reposo_m"]))
    print("LISTO")


if __name__ == "__main__":
    main()
