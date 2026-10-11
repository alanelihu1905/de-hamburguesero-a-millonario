# Casa Marea — interior de la cocina, tanda 2 (`build_interior_cocina.py`)

Ocho entregas low-poly para la cocina de la casa: los modelos que el documento 04 (`Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md`, secciones 2.4 y 6) priorizaba después de las estaciones. Colores planos, sin texturas, paleta de Casa Marea.

## Estado: ejecutado en el host y verificado

- Blender ejecutó `build_interior_cocina.py`: existen los ocho FBX, `Interior_Cocina.blend`, las dos vistas y `stats_interior.json`.
- **Todas las cifras medidas por Blender coinciden con la simulación sin Blender** (triángulos, dimensiones, pivotes y base en z = 0, salvo las dos excepciones de pivote).
- Todos los modelos están dentro de presupuesto. Las siete piezas de la hamburguesa suman 360 triángulos y usan los materiales esperados.
- Único defecto: en las vistas del lineup el refrigerador quedaba cortado a la izquierda y las piezas de la hamburguesa a la derecha. **Corregido y verificado** al regenerar (14:52): el lineup ya sale completo. Era solo un problema de la vista previa.

Para regenerar, en el host y desde la raíz del proyecto:

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_interior_cocina.py
```

## Salidas

| Archivo | Contenido |
| --- | --- |
| `Assets/_Game/Models/CasaMarea/SM_Refrigerador.fbx` | refrigerador |
| `…/SM_Hamburguesa_Piezas.fbx` | siete piezas sueltas de la hamburguesa, cada una un objeto con su pivote |
| `…/SM_Hamburguesa_Completa.fbx` | la hamburguesa ya armada, en una sola malla |
| `…/SM_Tabla_Corte.fbx` | tabla de corte con cuchillo |
| `…/SM_Campana_Extraccion.fbx` | campana y ducto |
| `…/SM_Repisa_Frascos.fbx` | repisa doble con botellas y frascos |
| `…/SM_Lampara_Colgante.fbx` | lámpara colgante |
| `…/SM_Mostrador_Modulo.fbx` | módulo de mostrador de 1 m |
| `ArtSource/CasaMarea/Interior_Cocina.blend`, `Preview/Interior_lineup_frontal.png`, `Preview/Interior_lineup_tres_cuartos.png`, `stats_interior.json` | fuente, dos vistas y estadísticas medidas |

Cada FBX se exporta con el modelo **en el origen**; el lineup del `.blend` se organiza después.

## Cifras verificadas (`stats_interior.json`)

Dimensiones en metros, X (ancho) × Y (fondo) × Z (alto). Frente hacia −Y.

| Modelo | Triángulos (tope) | Dimensiones X × Y × Z | Pivote |
| --- | --- | --- | --- |
| `SM_Refrigerador` | 96 (200) | 0.80 × 0.70 × 1.75 | centro de la huella, base z = 0 |
| `SM_Hamburguesa_Piezas` (7 objetos) | 360 (400) | cada pieza, ver abajo | centro de cada pieza, base z = 0 |
| `SM_Hamburguesa_Completa` | 360 (400) | 0.18 × 0.18 × 0.157 | centro, base z = 0 |
| `SM_Tabla_Corte` | 84 (150) | 0.40 × 0.28 × 0.059 | centro, base z = 0 |
| `SM_Campana_Extraccion` | 48 (150) | 1.52 × 0.90 × 0.955 | **excepción:** centro del borde inferior de la campana; colocar a z = 2.10 |
| `SM_Repisa_Frascos` | 408 (450) | 1.00 × 0.25 × 0.74 | centro, base z = 0 (extremo bajo de las ménsulas) |
| `SM_Lampara_Colgante` | 164 (250) | 0.28 × 0.28 × 0.635 | **excepción:** anclaje al techo; el modelo cuelga hacia z negativa |
| `SM_Mostrador_Modulo` | 60 (200) | 1.00 × 0.75 × 0.85 | centro, base z = 0 |

Suma de entregas distintas: 1 580 triángulos medidos (la hamburguesa cuenta dos veces: piezas y armada). La campana y la lámpara conservan su excepción de pivote (base z = 0 y z = −0.635).

### Hamburguesa modular: piezas y orden de apilado

| Orden | Objeto | Altura (m) | Triángulos | Material |
| --- | --- | --- | --- | --- |
| 1 | `SM_Burger_pan_base` | 0.025 | 44 | `M_Pan` |
| 2 | `SM_Burger_carne` | 0.030 | 44 | `M_Carne` |
| 3 | `SM_Burger_queso` | 0.006 | 12 | `M_Queso` |
| 4 | `SM_Burger_lechuga` | 0.010 | 44 | `M_Lechuga` |
| 5 | `SM_Burger_jitomate` (2 rodajas) | 0.010 | 56 | `M_Jitomate` |
| 6 | `SM_Burger_cebolla` | 0.006 | 28 | `M_Cebolla` |
| 7 | `SM_Burger_pan_tapa` | 0.070 | 132 | `M_Pan` |

Para armar una hamburguesa por código, colocar cada pieza en `z` acumulado: `z₁ = 0`, `z₂ = 0.025`, `z₃ = 0.055`, `z₄ = 0.061`, `z₅ = 0.071`, `z₆ = 0.081`, `z₇ = 0.087` (altura total 0.157). Se pueden omitir capas para variantes (sin queso, sin cebolla).

## Materiales

Todos de color plano. Los nuevos respecto de lotes anteriores: `M_Carne` `#5A3524`, `M_Queso` `#F2B63A`, `M_Cebolla` `#8E4A78`, `M_Madera_Clara` `#B88556`, `M_Acero_Claro` `#8E9AA0` (el de la campana en el documento 04). `M_Pan`, `M_Lechuga` y `M_Jitomate` repiten los de las estaciones. El resto es paleta Casa Marea (`M_Crema`, `M_Chile`, `M_Turquesa`, `M_Caoba`, `M_Herreria`, `M_Piedra_Caliza`, `M_Mango`, `M_Metal_Plancha`).

| Modelo | Materiales |
| --- | --- |
| Refrigerador | `M_Crema`, `M_Herreria`, `M_Chile`, `M_Turquesa` |
| Hamburguesa | `M_Pan`, `M_Carne`, `M_Queso`, `M_Lechuga`, `M_Jitomate`, `M_Cebolla` |
| Tabla de corte | `M_Herreria`, `M_Madera_Clara`, `M_Acero_Claro`, `M_Caoba` |
| Campana | `M_Acero_Claro`, `M_Chile`, `M_Metal_Plancha` |
| Repisa | `M_Caoba`, `M_Herreria`, `M_Chile`, `M_Crema`, `M_Turquesa` |
| Lámpara | `M_Herreria`, `M_Mango` |
| Mostrador | `M_Herreria`, `M_Piedra_Caliza`, `M_Caoba`, `M_Crema`, `M_Turquesa` |

## Uso en Unity

1. Usar los `.fbx`; escala 1 u = 1 m. Colocar a mano las dos excepciones de pivote: la campana a z = 2.10 y la lámpara en el techo (z ≈ 3.05).
2. **Lámpara:** `M_Mango` (bombilla) lleva color de emisión pero intensidad 0; hay que activarla en Unity.
3. **Mostrador:** el módulo mide 1.0 m. La barra del documento 04 mide 9.8 m: usar 10 módulos con escala X 0.98 cada uno, o ajustar a otra medida. El panel turquesa del frente mira a −Y.
4. **Refrigerador:** aparece como decoración al fondo (documento 04); su puerta no está animada.
5. **Hamburguesa:** usar `SM_Hamburguesa_Completa` para el estado «lista para vender» y las piezas para componerla por pasos.
6. **Colliders:** no incluidos. Una caja basta para refrigerador, mostrador y repisa.
7. Orientación en Unity sin verificar; si algo queda de espaldas, girar 180° en Y.

## Límites

- Sin texturas ni UV pensadas para pintar, sin animaciones, sin LOD; el cristal de la lámpara y los frascos no son transparentes.
- La orientación y la lectura dentro de la escena de Unity no están verificadas.
- **Más materiales de los permitidos.** Con esta tanda, entre las cuatro series (estaciones, fachada, props y cocina) hay 25 materiales con nombre (contados en los scripts), muy por encima del tope de 12 del documento 04. Siguen siendo colores planos, así que conviene consolidarlos en un único material con una textura-paleta pequeña (es la propuesta ya anotada en `README_PROPS.md`).
- La simulación no valida materiales por cara ni normales, y el color de las piezas pequeñas (queso, cebolla) está por verse en vista previa.
