"""Aplica la paleta única de Casa Marea a los modelos y los exporta a una carpeta nueva (Blender).

Uso (desde la raíz del proyecto, en el host; Blender necesita GPU; ANTES: python3 tools/make_palette.py):
    /Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/tools/apply_palette.py

Qué hace, para cada fuente .blend verificada (estaciones, fachada, props, interior y props 2):
  1. abre el .blend EN MEMORIA (no lo guarda: las fuentes quedan intactas);
  2. pone cada objeto SM_* en el origen (el lineup los había desplazado), tal como se exportaron los FBX originales;
  3. para cada cara, toma el material original, busca su celda en Palette/palette.json y asigna a todos sus vértices
     de cara la UV del centro de esa celda;
  4. sustituye los materiales por M_Casa_Marea (y M_Casa_Marea_Emisivo para las caras de M_Mango y M_Brasa), ambos con
     la textura de paleta (filtro «Closest»);
  5. exporta a Assets/_Game/Models/CasaMarea/Paleta/<mismo nombre>.fbx (los FBX originales no se tocan). Las texturas
     de paleta se copian UNA sola vez a Paleta/Texturas/ y los FBX solo guardan el nombre del archivo (path_mode STRIP);
     una versión anterior copiaba la textura a una carpeta .fbm por FBX (25 copias idénticas), y este script borra
     esas carpetas .fbm de Paleta/ al empezar.
Los planos de decal (SM_Letrero_Texto, SM_Cartel_Texto) conservan su material y su UV.

Comprueba antes y después, por objeto: triángulos y dimensiones (no deben cambiar), que todas las UV caigan en el
centro de su celda y que ningún material falte en la paleta. El resultado va a ArtSource/CasaMarea/stats_paleta.json.
"""
import json
import os
import shutil

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, ".."))
PROJECT = os.path.abspath(os.path.join(ROOT, "..", ".."))
OUT_DIR = os.path.join(PROJECT, "Assets", "_Game", "Models", "CasaMarea", "Paleta")
PALETTE_DIR = os.path.join(ROOT, "Palette")
ALBEDO = os.path.join(PALETTE_DIR, "Palette_CasaMarea_Albedo.png")
BLENDS = ["Huacal_Ingredientes", "Plancha_Tubo", "PuntoVenta_Feston", "Fachada_CasaMarea", "Props_Barrio",
          "Interior_Cocina", "Props_Barrio_2", "Clientes_Vecinas"]
DECALES = {"SM_Letrero_Texto", "SM_Cartel_Texto"}


def grupo_fbx(nombre):
    """Nombre del FBX al que pertenece cada objeto (los originales agrupaban así)."""
    if nombre.startswith("SM_Burger_"):
        return "SM_Hamburguesa_Piezas"
    if nombre == "SM_Cartel_Texto":
        return "SM_Cartel_Precios"
    if nombre == "SM_Letrero_Texto":
        return "SM_Fachada_CasaMarea"
    return nombre


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def crear_materiales(img):
    mats = {}
    for nombre in ("M_Casa_Marea", "M_Casa_Marea_Emisivo"):
        m = bpy.data.materials.new(nombre)
        m.use_nodes = True
        nt = m.node_tree
        bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        tex.interpolation = "Closest"
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = 0.6
        mats[nombre] = m
    return mats


def remapear(obj, palette, mats):
    """Asigna UV y materiales de paleta. Devuelve (ok, faltantes, caras_emisivas)."""
    me = obj.data
    nombres = [s.material.name if s.material else None for s in obj.material_slots]
    plan, faltantes = [], set()
    for p in me.polygons:
        nombre = nombres[p.material_index] if p.material_index < len(nombres) else None
        entrada = palette.get(nombre)
        if entrada is None:
            faltantes.add(str(nombre))
            plan.append(None)
        else:
            plan.append((entrada["uv"], entrada["emisivo"]))
    if faltantes:
        return False, sorted(faltantes), 0
    for capa in list(me.uv_layers):
        me.uv_layers.remove(capa)
    uv = me.uv_layers.new(name="UV_Paleta")
    emisivas = 0
    for p, (celda_uv, emisivo) in zip(me.polygons, plan):
        for i in range(p.loop_start, p.loop_start + p.loop_total):
            uv.data[i].uv = celda_uv
        emisivas += 1 if emisivo else 0
    me.materials.clear()
    me.materials.append(mats["M_Casa_Marea"])
    hay_emisivas = emisivas > 0
    if hay_emisivas:
        me.materials.append(mats["M_Casa_Marea_Emisivo"])
    for p, (celda_uv, emisivo) in zip(me.polygons, plan):
        p.material_index = 1 if (emisivo and hay_emisivas) else 0
    return True, [], emisivas


def uv_correctas(obj, esperado_por_cara):
    """Cada loop de cada cara debe tener exactamente la UV de su celda."""
    uv = obj.data.uv_layers["UV_Paleta"]
    for p, esp in zip(obj.data.polygons, esperado_por_cara):
        for i in range(p.loop_start, p.loop_start + p.loop_total):
            u, v = uv.data[i].uv
            if abs(u - esp[0]) > 1e-6 or abs(v - esp[1]) > 1e-6:
                return False
    return True


def exportar(objs, nombre):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    ruta = os.path.join(OUT_DIR, nombre + ".fbx")
    bpy.ops.export_scene.fbx(filepath=ruta, use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0,
                             apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP", embed_textures=False)
    return ruta


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for carpeta in os.listdir(OUT_DIR):                       # copias de textura de una ejecución anterior
        ruta = os.path.join(OUT_DIR, carpeta)
        if carpeta.endswith(".fbm") and os.path.isdir(ruta):
            shutil.rmtree(ruta)
    tex_dir = os.path.join(OUT_DIR, "Texturas")
    os.makedirs(tex_dir, exist_ok=True)
    for archivo in ("Palette_CasaMarea_Albedo.png", "Palette_CasaMarea_MetalSuavidad.png"):
        shutil.copy2(os.path.join(PALETTE_DIR, archivo), os.path.join(tex_dir, archivo))
    with open(os.path.join(PALETTE_DIR, "palette.json"), encoding="utf-8") as f:
        palette = json.load(f)["materiales"]
    informe = {"fbx": {}, "errores": []}
    for nombre_blend in BLENDS:
        ruta_blend = os.path.join(ROOT, nombre_blend + ".blend")
        if not os.path.exists(ruta_blend):
            informe["errores"].append("falta " + ruta_blend)
            continue
        bpy.ops.wm.open_mainfile(filepath=ruta_blend)
        img = bpy.data.images.load(ALBEDO, check_existing=True)
        img.colorspace_settings.name = "sRGB"
        mats = crear_materiales(img)
        objetos = [o for o in bpy.context.scene.objects if o.type == "MESH" and o.name.startswith("SM_")]
        grupos = {}
        for o in objetos:
            o.location = (0.0, 0.0, 0.0)
            antes = {"triangulos": tri_count(o), "dimensiones": [round(d, 6) for d in o.dimensions]}
            esperado = None
            if o.name in DECALES:
                grupos.setdefault(grupo_fbx(o.name), []).append((o, antes, None, 0, True))
                continue
            nombres = [s.material.name if s.material else None for s in o.material_slots]
            esperado = [palette[nombres[p.material_index]]["uv"] if nombres[p.material_index] in palette else None
                        for p in o.data.polygons]
            ok, faltantes, emisivas = remapear(o, palette, mats)
            if not ok:
                informe["errores"].append("%s (%s): materiales fuera de la paleta: %s" % (o.name, nombre_blend, faltantes))
                continue
            grupos.setdefault(grupo_fbx(o.name), []).append((o, antes, esperado, emisivas, False))
        for grupo, items in grupos.items():
            objs = [it[0] for it in items]
            ruta = exportar(objs, grupo)
            piezas = {}
            for o, antes, esperado, emisivas, decal in items:
                despues = {"triangulos": tri_count(o), "dimensiones": [round(d, 6) for d in o.dimensions]}
                piezas[o.name] = {
                    "decal_sin_cambios": decal,
                    "triangulos_antes": antes["triangulos"], "triangulos_despues": despues["triangulos"],
                    "dimensiones_antes": antes["dimensiones"], "dimensiones_despues": despues["dimensiones"],
                    "sin_regresion": antes == despues,
                    "uv_en_centro_de_celda": True if decal else uv_correctas(o, esperado),
                    "caras_emisivas": emisivas,
                    "materiales": [m.name for m in o.data.materials]}
            informe["fbx"][grupo] = {"origen": nombre_blend + ".blend", "ruta": os.path.relpath(ruta, PROJECT),
                                     "piezas": piezas}
    informe["texturas_compartidas"] = sorted(os.listdir(tex_dir))
    informe["carpetas_fbm_restantes"] = sorted(f for f in os.listdir(OUT_DIR) if f.endswith(".fbm"))
    total = sum(len(v["piezas"]) for v in informe["fbx"].values())
    malos = [n for v in informe["fbx"].values() for n, p in v["piezas"].items()
             if not (p["sin_regresion"] and p["uv_en_centro_de_celda"])]
    informe["resumen"] = {"fbx_exportados": len(informe["fbx"]), "objetos": total, "objetos_con_problema": malos,
                          "errores": len(informe["errores"])}
    with open(os.path.join(ROOT, "stats_paleta.json"), "w", encoding="utf-8") as f:
        json.dump(informe, f, indent=2, ensure_ascii=False)
    print("RESUMEN:", informe["resumen"])
    for e in informe["errores"]:
        print("ERROR:", e)
    print("LISTO")


if __name__ == "__main__":
    main()
