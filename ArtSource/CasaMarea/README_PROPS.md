# Casa Marea — props del barrio, tanda 1 (`build_props_barrio.py`)

Cinco props low-poly originales para la calle y el callejón de Casa Marea. Paleta y materiales del documento 04 (`Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md`, sección 4). Colores planos, sin texturas.

## Estado: ejecutado en el host y verificado

- Blender ejecutó `build_props_barrio.py` (14:34): existen los cinco FBX, `Props_Barrio.blend`, las tres vistas y `stats_props.json`.
- **Las cifras medidas por Blender coinciden con la precomprobación sin Blender en los cinco props** (triángulos, dimensiones, desfase del centro y base en z = 0). Eso valida la simulación (`tools/precheck_props.py`) para las siguientes tandas.
- Los FBX contienen los objetos y materiales esperados (comprobado leyendo los nombres dentro de los archivos; el cartel incluye `SM_Cartel_Texto`).
- Dos mejoras visuales detectadas al revisar las vistas se corrigieron y se verificaron tras regenerar (14:52; ver «Defectos»).

Para regenerar, en el host y desde la raíz del proyecto:

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_props_barrio.py
```

## Salidas

| Archivo | Contenido |
| --- | --- |
| `Assets/_Game/Models/CasaMarea/SM_Banco_Calle.fbx` | banco de calle |
| `Assets/_Game/Models/CasaMarea/SM_Maceta_Tropical.fbx` | maceta con planta tropical |
| `Assets/_Game/Models/CasaMarea/SM_Farol_Calle.fbx` | farol |
| `Assets/_Game/Models/CasaMarea/SM_Cubeta.fbx` | cubeta con hielo y pescado |
| `Assets/_Game/Models/CasaMarea/SM_Cartel_Precios.fbx` | cartel; incluye `SM_Cartel_Texto` |
| `ArtSource/CasaMarea/Props_Barrio.blend` | fuente, con los cinco props alineados para el lineup |
| `ArtSource/CasaMarea/Preview/Props_lineup_frontal.png`, `Props_lineup_tres_cuartos.png`, `Props_lineup_silueta_3m.png` | las tres vistas de lineup |
| `ArtSource/CasaMarea/stats_props.json` | triángulos, dimensiones, pivote y materiales medidos por Blender |

Los FBX se exportan con cada prop **en el origen**; solo después se desplazan en el `.blend` para el lineup, así que el pivote del FBX es el correcto.

## Cifras verificadas (`stats_props.json`)

Dimensiones en metros: X (ancho) × Y (fondo) × Z (alto). Pivote: centro de la huella, base en z = 0, frente hacia −Y.

| Prop | Triángulos (tope) | Dimensiones X × Y × Z | Desfase del centro XY | Base z |
| --- | --- | --- | --- | --- |
| `SM_Banco_Calle` | 180 (250) | 1.50 × 0.49 × 0.85 | (0, +0.005) | 0 |
| `SM_Maceta_Tropical` | 456 (600) | 0.70 × 0.70 × 0.84 | (−0.02, +0.007) | 0 |
| `SM_Farol_Calle` | 308 (400) | 0.42 × 0.42 × 2.78 | (0, 0) | 0 |
| `SM_Cubeta` | 260 (300) | 0.37 × 0.35 × 0.43 | (0, 0) | 0 |
| `SM_Cartel_Precios` | 308 (400) | 0.62 × 0.45 × 0.965 | (0, 0) | 0 |
| `SM_Cartel_Texto` (aparte) | 4 | 0.44 × 0.44 × 0.53 | (0, 0) | 0.142 |

Todos dentro de presupuesto y con la base en z = 0 (el cartel da −0.0, es decir, nulo). Materiales por prop: los de la tabla «Qué es cada prop».

Total de la tanda: 1 512 triángulos (sin contar los 4 del plano de texto), medidos. El tope del banco (250) es el de la «banca» del documento 04; los demás topes son propuesta de esta tanda. El banco del documento 04 mide 1.4 × 0.45 × 0.4 sin respaldo; este banco de calle es más grande (1.5 × 0.5 × 0.85, con respaldo) y el de 1.4 m del callejón queda para una variante.

## Qué es cada prop

| Prop | Silueta y detalles | Materiales |
| --- | --- | --- |
| Banco de calle | cuatro tablones de asiento y tres de respaldo sobre postes de herrería, con remates chile | `M_Caoba`, `M_Herreria`, `M_Chile` |
| Maceta tropical | maceta de barro con borde y tierra; nueve hojas anchas que caen hacia afuera y cuatro flores (chile y crema) en las puntas | `M_Barro`, `M_Caoba`, `M_Vegetacion`, `M_Hoja_Clara`, `M_Chile`, `M_Crema` |
| Farol | pedestal de caliza, poste de herrería con banda chile y linterna de cristal mango con costillas y techo | `M_Piedra_Caliza`, `M_Herreria`, `M_Chile`, `M_Mango` |
| Cubeta | cubeta turquesa con borde crema, domo de hielo, huachinango sobre el hielo y asa de herrería | `M_Turquesa`, `M_Crema`, `M_Chile`, `M_Herreria` |
| Cartel de precios | caballete de dos caras: marco de caoba, pizarra ciruela, encabezado turquesa con dos hamburguesas y remate | `M_Caoba`, `M_Pizarra`, `M_Turquesa`, `M_Mango`, `M_Chile` (+ `M_Cartel_Texto` en el plano aparte) |

12 materiales distintos en la tanda (11 en los modelos y `M_Cartel_Texto` en el plano). Colores nuevos respecto de los lotes anteriores: `M_Barro` `#B5623B` (chile desaturado), `M_Hoja_Clara` `#7BAF4B` y `M_Pizarra` `#3B2F4A` (la ciruela del documento 04); el resto repite la paleta.

## Uso en Unity

1. Usar los `.fbx`. Escala 1 u = 1 m; el pivote está en el centro de la base, así que el prop se coloca directamente sobre el suelo.
2. **Farol:** `M_Mango` (el cristal) lleva el color de emisión definido pero con intensidad 0. Hay que activar la emisión en Unity para encenderlo al atardecer.
3. **Cartel:** `SM_Cartel_Texto` son dos planos de 0.44 × 0.54 m, uno por cara (4 triángulos en total, UV 0 a 1 en cada plano), con el material `M_Cartel_Texto`. Debe sustituirse por una textura con los precios en cada cara. Los precios no están modelados.
4. **Colliders:** no incluidos. Para el banco, el farol y el cartel basta una caja; para la maceta, un cilindro o caja de la maceta (la planta no necesita collider).
5. **Orientación:** el frente (asiento del banco, cara delantera del cartel) mira a −Y en Blender. No se ha verificado cómo queda en Unity; si queda de espaldas, girar 180° en Y.

## Defectos encontrados en las vistas

1. **Hojas de la maceta casi invisibles de canto.** Medían 1.2 cm de grosor. **Corregido:** 2.5 cm, sin cambiar los triángulos (456); la maceta pasa a medir 0.701 × 0.706 × 0.843 m. Una hoja vista justo de canto sigue leyéndose como una línea fina en la silueta a 3 m; es aceptable y no se cambió más.
2. **La vista de silueta a 3 m cortaba la punta del farol.** **Corregido y verificado:** el farol aparece completo en `Props_lineup_silueta_3m.png`.
3. **Flores de la maceta:** quedan como bolitas en la punta de cuatro hojas; se leen bien en la vista frontal, pero parecen sueltas de cerca. No cambia nada ahora; es candidato para la tanda de mejoras con tallos.

El farol, el banco, el cartel y la cubeta se leen bien en las tres vistas y no necesitan cambios.

## Límites

- Sin texturas ni UV pensadas para pintar (solo las UV por defecto de las primitivas); sin animación; sin LOD.
- La orientación y la lectura dentro de la escena de Unity no están verificadas.
- **Materiales de toda la serie:** entre el lote de estaciones, la fachada y estos props hay más de 12 materiales con nombre, por encima del tope de 12 del documento 04. Como todos son colores planos, conviene consolidar en un único material con una textura-paleta pequeña y reasignar el color por UV. Es una propuesta; no cambia nada ahora.
- La simulación no valida materiales por cara ni normales.

## Para la siguiente tanda (props y variaciones)

El script tiene una función `build_*` por prop y un diccionario `BUDGET` y otro `LINEUP_X`, así que añadir un prop es:

1. Añadir su `build_<nombre>()` con las primitivas existentes (`box`, `cyl`, `cone`, `sphere`, `prism`, `burger_icon`) y terminar con `finalize("SM_<Nombre>")`.
2. Añadirlo a `BUDGET` (tope de triángulos) y a `LINEUP_X` (posición en el lineup), y a la lista de `main()`.
3. Ejecutar `precheck_props.py` para ver triángulos y medidas antes de abrir Blender.

Candidatos sin empezar: variantes del banco (1.4 × 0.45 × 0.4 del callejón, sin respaldo), cajas de pescado apilables, bolardo de caliza, barril, farol de pared, maceta pequeña de patio, banqueta de tienda. Para variaciones de color se puede parametrizar el material de cada función en lugar de duplicar el modelo.
