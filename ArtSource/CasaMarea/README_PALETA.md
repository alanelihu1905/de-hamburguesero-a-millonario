# Casa Marea — paleta única (reducir de 25 materiales a 2)

Los scripts de Blender declaran **25 materiales con nombre** (estaciones, fachada, props, interior y props 2), por encima del tope de 12 del documento 04. Todos son colores planos, así que se pueden reunir en una textura-paleta de 128 × 64 px y usar dos materiales de Unity en lugar de 25.

## Qué está hecho y verificado

`python3 ArtSource/CasaMarea/tools/make_palette.py` lee los diccionarios `MATS` de los cinco scripts y genera en `ArtSource/CasaMarea/Palette/`:

| Archivo | Contenido |
| --- | --- |
| `Palette_CasaMarea_Albedo.png` | 128 × 64 px, 8 × 4 celdas de 16 px; color base |
| `Palette_CasaMarea_MetalSuavidad.png` | misma rejilla; R = metálico, A = suavidad (1 − rugosidad), para URP/Lit |
| `palette.json` | por material: celda, UV del centro de la celda, color, rugosidad, metálico, si es emisivo y en qué scripts se usa |

Resultado verificado al ejecutarlo: **26 materiales en la paleta (23 de las cinco primeras series y 3 tonos de piel de la tanda de clientes), 6 celdas libres, 0 conflictos** (ningún nombre tiene valores distintos en scripts distintos) y 0 errores al leer los PNG de vuelta y comparar el centro de cada celda con su color. Se excluyen `M_Letrero` y `M_Cartel_Texto`: son planos de decal que llevan su propia textura.

**Asignación estable:** si ya existe `palette.json`, cada material conserva su celda y su UV, y los nuevos ocupan las celdas libres. Al añadir los tres tonos de piel se comprobó que las 23 celdas anteriores no cambiaron, así que los FBX ya exportados a `Paleta/` siguen siendo válidos.

Orden de las celdas (primera generación): grises por luminosidad y luego colores por tono (fila 0 empieza con `M_Acero_Claro`, `M_Crema`, `M_Jitomate`, …). Los 23 colores y sus UV están en `palette.json`.

## Paso de Blender: `tools/apply_palette.py` (ejecutado)

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/tools/apply_palette.py
```

Qué hace, para las fuentes `.blend` (estaciones, fachada, props, interior, props 2 y, cuando exista, clientes y casas vecinas):

1. Abre cada `.blend` **en memoria y no lo guarda**: las fuentes quedan intactas.
2. Pone los objetos `SM_*` en el origen (el lineup los había desplazado), como se exportaron los FBX originales.
3. A cada cara le asigna la UV del centro de la celda de su material (`palette.json`) y sustituye los materiales por `M_Casa_Marea` y, para las caras de `M_Mango` y `M_Brasa`, `M_Casa_Marea_Emisivo`. Ambos llevan la textura de paleta con filtro «Closest».
4. Exporta a `Assets/_Game/Models/CasaMarea/Paleta/<mismo nombre>.fbx`. **Los FBX originales no se tocan.**
5. Deja los planos de decal (`SM_Letrero_Texto`, `SM_Cartel_Texto`) con su material y su UV.
6. Antes y después compara, por objeto, triángulos y dimensiones (no deben cambiar) y comprueba que cada UV caiga en el centro de su celda. El resultado va a `ArtSource/CasaMarea/stats_paleta.json`.

### Resultado medido (primera ejecución, `stats_paleta.json`)

- **25 FBX exportados, 33 objetos, 0 objetos con problema, 0 errores.**
- En los 33 objetos, **triángulos y dimensiones idénticos antes y después**, y todas las UV en el centro de su celda.
- 7 de los 25 FBX tienen cara emisiva (submaterial `M_Casa_Marea_Emisivo`): plancha (6 caras), punto de venta (54: campanilla y pestillo), fachada (68: iconos de hamburguesa del rótulo y el banderín), farol de calle (10), cartel (56: iconos del encabezado), lámpara colgante (48) y farol de pared (10). El resto usa solo `M_Casa_Marea`. Las caras emisivas llevan la emisión apagada hasta que Unity la use; los iconos de hamburguesa del rótulo, el banderín y el cartel (pan en mango) también quedan ahí. Si molesta, se les cambia el color en los scripts.
- Los planos de decal (`SM_Letrero_Texto`, `SM_Cartel_Texto`) conservaron su material.

### Defecto encontrado y corregido (verificado en la segunda ejecución)

La primera ejecución dejó la misma textura de paleta (mismo MD5) copiada en 25 carpetas `.fbm`, una por FBX; en Unity serían 25 texturas distintas en vez de una. El script ahora copia las dos texturas una sola vez a `Paleta/Texturas/`, los FBX solo guardan el nombre del archivo (`path_mode STRIP`) y se borran las carpetas `.fbm` anteriores (solo dentro de `Paleta/`).

Segunda ejecución, medida: **25 FBX, 33 objetos, 0 problemas, 0 errores; 0 carpetas `.fbm`; `Paleta/Texturas/` con `Palette_CasaMarea_Albedo.png` y `Palette_CasaMarea_MetalSuavidad.png`.**

### Unity (propuesta)

- `M_Casa_Marea`: URP/Lit, mapa base = albedo, mapa metálico = `MetalSuavidad` con «Smoothness Source = Metallic Alpha». Filtro **Point**, **sin mipmaps** (a distancia, los mips de celdas de 16 px mezclarían colores vecinos) y wrap Clamp. El albedo con sRGB activado; el mapa metálico con sRGB desactivado.
- `M_Casa_Marea_Emisivo`: solo para las caras de `M_Mango` y `M_Brasa` (cristal del farol, bombilla de la lámpara, ranura de la plancha, campanilla). Cada pieza se enciende desde su `Renderer` con un `MaterialPropertyBlock`, sin afectar al resto.
- Los decals (`M_Letrero`, `M_Cartel_Texto`) siguen con material propio.

## Ventajas y costes

- De 25 materiales a 2 (más 2 de decal), dos PNG de menos de 1 KB (368 y 289 bytes) y mejor agrupación de draw calls (SRP Batcher y un solo material compartido).
- Se pierde la posibilidad de retocar un color en un solo material de Unity: el color queda en la textura. Cambiar un color implica editar el script y regenerar la paleta.
- Las celdas libres (9) permiten añadir colores sin cambiar el tamaño ni las UV de las ya asignadas.

## Estado

- **Paleta:** generada y verificada (`make_palette.py`, 23 colores, 0 conflictos, 0 errores de lectura).
- **`apply_palette.py`:** ejecutado cuatro veces; la última, a las 14:52 y ya con todo regenerado (clientes con los brazos corregidos, maceta con hojas gruesas): **30 FBX, 38 objetos, 0 problemas, 0 errores, 0 carpetas `.fbm`**. Los 30 FBX de `Assets/_Game/Models/CasaMarea/Paleta/` no tienen regresiones de triángulos ni dimensiones y todas sus UV están en su celda.
- **Sin verificar en Unity:** cómo se ve cada modelo con la paleta frente a su versión original, ni el filtro y los mipmaps de la textura.
- **Decisión:** adoptar o no la paleta es de Codex. Si se adopta, en Unity bastan dos materiales compartidos y reasignarlos en la importación de los FBX de `Paleta/` (los FBX originales siguen en la carpeta superior).
