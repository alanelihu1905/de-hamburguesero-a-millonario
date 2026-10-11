"""Simulación mínima de bpy/bmesh para precomprobar geometría (NO es Blender).

Cubre solo lo que usan las funciones build_* de los scripts de ArtSource/CasaMarea: primitivas, transform_apply,
join y conteo de triángulos/dimensiones. No renderiza, no exporta y no comprueba materiales ni UV.
Uso: python3 ArtSource/CasaMarea/tools/precheck_props.py
"""
import math, sys, types

class Co:
    def __init__(s, x, y, z): s.x, s.y, s.z = x, y, z
class V:
    def __init__(s, x, y, z): s.co = Co(x, y, z)
class Poly:
    def __init__(s, idx): s.vertices = list(idx)
class Mesh:
    def __init__(s, name=""):
        s.name = name; s.vertices = []; s.polygons = []; s.materials = []
    def from_pydata(s, verts, edges, faces):
        s.vertices = [V(*v) for v in verts]; s.polygons = [Poly(f) for f in faces]
    def update(s): pass
    def transform_bake(s, M):
        for v in s.vertices:
            v.co.x, v.co.y, v.co.z = M(v.co.x, v.co.y, v.co.z)
class VG:
    def __init__(s, name, index): s.name, s.index, s.data = name, index, {}
    def add(s, indices, weight, mode="REPLACE"):
        for i in indices: s.data[i] = weight
class VGroups(list):
    def new(s, name):
        g = VG(name, len(s)); s.append(g); return g
    def get(s, name):
        return next((g for g in s if g.name == name), None)
class Obj:
    def __init__(s, name, data):
        s.vertex_groups = VGroups()
        s.name = name; s.data = data; s.scale = (1, 1, 1); s.rotation_euler = (0, 0, 0); s.location = (0, 0, 0)
        s.sel = False; s.hide_render = False
    def select_set(s, v): s.sel = v
    def evaluated_get(s, dg): return s
    def to_mesh(s): return s.data
    def to_mesh_clear(s): pass

def rot(x, y, z, r):
    rx, ry, rz = r
    cx, sx, cy, sy, cz, sz = math.cos(rx), math.sin(rx), math.cos(ry), math.sin(ry), math.cos(rz), math.sin(rz)
    y, z = y * cx - z * sx, y * sx + z * cx
    x, z = x * cy + z * sy, -x * sy + z * cy
    x, y = x * cz - y * sz, x * sz + y * cz
    return x, y, z

class _List(list):
    def link(s, o): s.append(o)
class Ctx:
    def __init__(s): s.active_object = None
ctx = Ctx()
class _VL:
    class objects:
        pass
ctx.view_layer = _VL()
class _ObjsProxy:
    def __setattr__(s, k, v):
        if k == "active": ctx.active_object = v
    def __getattr__(s, k): return ctx.active_object
ctx.view_layer.objects = _ObjsProxy()
class _Scene:
    class collection: objects = _List()
ctx.scene = _Scene
ctx.evaluated_depsgraph_get = lambda: None
ALL = _Scene.collection.objects

def _new(name, me):
    o = Obj(name, me); ALL.append(o); ctx.active_object = o; return o

class Materials(dict):
    def new(s, name):
        m = types.SimpleNamespace(name=name, use_nodes=False, diffuse_color=None)
        class In(dict):
            def __missing__(s2, k):
                s2[k] = types.SimpleNamespace(default_value=None); return s2[k]
        bsdf = types.SimpleNamespace(type="BSDF_PRINCIPLED", inputs=In())
        m.node_tree = types.SimpleNamespace(nodes=[bsdf]); s[name] = m; return m
class Meshes:
    def new(s, name): return Mesh(name)
class Objects:
    def new(s, name, me): return Obj(name, me)
data = types.SimpleNamespace(materials=Materials(), meshes=Meshes(), objects=Objects())

def ring(n, r, z):  # el primer vértice queda sobre -Y, como en Blender
    return [(r * math.cos(2 * math.pi * k / n - math.pi / 2), r * math.sin(2 * math.pi * k / n - math.pi / 2), z) for k in range(n)]
def mk(name, verts, faces):
    me = Mesh(name); me.from_pydata(verts, [], faces); return _new(name, me)

class OpsMesh:
    def primitive_cube_add(s, size=1):
        h = size / 2; v = [(x, y, z) for x in (-h, h) for y in (-h, h) for z in (-h, h)]
        f = [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]]
        mk("Cube", v, f)
    def primitive_cylinder_add(s, vertices=32, radius=1, depth=2):
        n = vertices; v = ring(n, radius, -depth / 2) + ring(n, radius, depth / 2)
        f = [[i, (i + 1) % n, n + (i + 1) % n, n + i] for i in range(n)] + [list(range(n)), list(range(n, 2 * n))]
        mk("Cylinder", v, f)
    def primitive_cone_add(s, vertices=32, radius1=1, radius2=0, depth=2):
        n = vertices
        if radius2 == 0:
            v = ring(n, radius1, -depth / 2) + [(0, 0, depth / 2)]
            f = [[i, (i + 1) % n, n] for i in range(n)] + [list(range(n))]
        else:
            v = ring(n, radius1, -depth / 2) + ring(n, radius2, depth / 2)
            f = [[i, (i + 1) % n, n + (i + 1) % n, n + i] for i in range(n)] + [list(range(n)), list(range(n, 2 * n))]
        mk("Cone", v, f)
    def primitive_uv_sphere_add(s, segments=32, ring_count=16, radius=1):
        v = [(0, 0, radius), (0, 0, -radius)]
        for j in range(1, ring_count):
            a = math.pi * j / ring_count
            v += ring(segments, radius * math.sin(a), radius * math.cos(a))
        f = []
        for i in range(segments):
            f.append([0, 2 + i, 2 + (i + 1) % segments])
            last = 2 + (ring_count - 2) * segments
            f.append([1, last + (i + 1) % segments, last + i])
        for j in range(ring_count - 2):
            for i in range(segments):
                a = 2 + j * segments; b = a + segments
                f.append([a + i, b + i, b + (i + 1) % segments, a + (i + 1) % segments])
        mk("Sphere", v, f)
    def primitive_ico_sphere_add(s, subdivisions=1, radius=1):
        t = (1 + 5 ** 0.5) / 2
        v = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
        f = [[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11], [1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8],
             [3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9], [4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]]
        norm = lambda p: tuple(c / math.sqrt(sum(q * q for q in p)) for c in p)
        v = [norm(p) for p in v]
        for _ in range(subdivisions):
            cache = {}
            def mid(a, b):
                k = (min(a, b), max(a, b))
                if k not in cache:
                    cache[k] = len(v); v.append(norm(tuple((v[a][i] + v[b][i]) / 2 for i in range(3))))
                return cache[k]
            nf = []
            for a, b, c in f:
                ab, bc, ca = mid(a, b), mid(b, c), mid(c, a)
                nf += [[a, ab, ca], [b, bc, ab], [c, ca, bc], [ab, bc, ca]]
            f = nf
        mk("Ico", [tuple(c * radius for c in p) for p in v], f)
    def primitive_plane_add(s, size=2):
        h = size / 2; mk("Plane", [(-h, -h, 0), (h, -h, 0), (h, h, 0), (-h, h, 0)], [[0, 1, 2, 3]])
class OpsObject:
    def select_all(s, action="DESELECT"):
        for o in ALL: o.sel = (action == "SELECT")
    def transform_apply(s, location=True, rotation=True, scale=True):
        o = ctx.active_object
        sc, r, loc = o.scale, o.rotation_euler, o.location
        o.data.transform_bake(lambda x, y, z: tuple(a + b for a, b in zip(rot(x * sc[0], y * sc[1], z * sc[2], r), loc)))
        o.scale, o.rotation_euler, o.location = (1, 1, 1), (0, 0, 0), (0, 0, 0)
    def join(s):
        act = ctx.active_object
        for o in [o for o in ALL if o.sel and o is not act]:
            act.data.vertices += o.data.vertices
            # reindexar caras
            off = len(act.data.vertices) - len(o.data.vertices)
            act.data.polygons += [Poly([i + off for i in p.vertices]) for p in o.data.polygons]
            for m in o.data.materials:
                if m not in act.data.materials: act.data.materials.append(m)
            for g in o.vertex_groups:
                destino = act.vertex_groups.get(g.name) or act.vertex_groups.new(g.name)
                for i, w in g.data.items(): destino.data[i + off] = w
            ALL.remove(o)
    def shade_smooth_by_angle(s, angle=0): pass
    def shade_smooth(s): pass
ops = types.SimpleNamespace(mesh=OpsMesh(), object=OpsObject(), wm=types.SimpleNamespace(read_factory_settings=lambda use_empty=True: None))
context = ctx

# bmesh
class BM:
    def from_mesh(s, me): s.faces = me.polygons
    def to_mesh(s, me): pass
    def free(s): pass
bmesh = types.ModuleType("bmesh")
bmesh.new = lambda: BM()
bmesh.ops = types.SimpleNamespace(recalc_face_normals=lambda bm, faces=None: None)
sys.modules["bmesh"] = bmesh
