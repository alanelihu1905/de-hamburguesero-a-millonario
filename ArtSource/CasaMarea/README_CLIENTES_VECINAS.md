# Casa Marea — clientes y casas vecinas, tanda 4 (`build_clientes_vecinas.py`)

Los clientes que el ciclo de venta va a necesitar y las dos variantes de casa vecina que sustituyen a las cajas provisionales de 6 × 6 × 2.8 m a ambos lados de la casa. Seguimos dentro del primer barrio: no se amplía la ciudad (regla del proyecto: primero el barrio pequeño y el ciclo de venta).

## Estado: ejecutado en el host, defecto de brazos corregido y verificado

- Blender ejecutó `build_clientes_vecinas.py` dos veces (la segunda, 14:51, con la corrección de brazos): existen los cinco FBX, `Clientes_Vecinas.blend`, las tres vistas y `stats_clientes_vecinas.json`.
- Los triángulos y las dimensiones medidos coinciden con la simulación en los cinco modelos, incluido el ancho de los clientes tras la corrección (0.731 m).
- **Defecto corregido:** los brazos estaban mal encadenados (manga y antebrazo inclinados cada uno por su cuenta) y se veían partidos en la silueta. Ahora son una cadena hombro → manga → antebrazo → mano; en `Clientes_silueta_3m.png` los brazos forman una línea continua. No cambió ningún triángulo ni la altura.
- Los tres clientes son personajes **ficticios y originales**; no representan a ninguna persona real.
- Se distinguen a 3 m por silueta: sombrero de ala ancha, vestido con chongo y gorra con visera. Las casas vecinas se leen bien y se distinguen entre sí.
- **Versiones con paleta:** los FBX de `Paleta/` de los clientes se exportaron a las 14:49, antes de esta corrección, así que aún traen los brazos anteriores. Hay que volver a ejecutar `tools/apply_palette.py` (ver `README_PALETA.md`).

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_clientes_vecinas.py
python3 ArtSource/CasaMarea/tools/precheck_clientes.py      # sin Blender
```

## Cifras verificadas (Blender, `stats_clientes_vecinas.json`)

Dimensiones en metros, X (ancho) × Y (fondo) × Z (alto). Frente hacia −Y; base en z = 0.

| Modelo | Triángulos (tope) | Dimensiones X × Y × Z | Desfase del centro XY |
| --- | --- | --- | --- |
| `SM_Cliente_Pescador` | 396 (700) | 0.731 × 0.54 × 1.80 | (0, 0) |
| `SM_Cliente_Vecina` | 440 (700) | 0.731 × 0.54 × 1.75 | (0, 0) |
| `SM_Cliente_Joven` | 372 (700) | 0.731 × 0.44 × 1.72 | (0, −0.05) por la visera |
| `SM_Casa_Vecina_A` | 336 (600) | 6.20 × 6.24 × 3.45 | (0, −0.02) |
| `SM_Casa_Vecina_B` | 432 (600) | 6.20 × 6.24 × 3.45 | (0, −0.02) |

Suma: 1 976 triángulos, medidos. Tras la corrección de brazos el ancho de los clientes medido es 0.731 m.

## Qué es cada modelo

| Modelo | Descripción | Colores |
| --- | --- | --- |
| Pescador | camisa turquesa, pantalón pizarra, sombrero de paja con cinta chile | `M_Piel_Media`, `M_Turquesa`, `M_Pizarra`, `M_Madera_Clara`, `M_Chile`, `M_Herreria` |
| Vecina | vestido y delantal, cabello gris con chongo | `M_Piel_Clara`, `M_Chile`, `M_Crema`, `M_Acero_Claro`, `M_Herreria` |
| Joven | camiseta crema, shorts de caliza, gorra chile con visera | `M_Piel_Oscura`, `M_Crema`, `M_Piedra_Caliza`, `M_Chile`, `M_Herreria` |
| Casa vecina A | arena con zócalo turquesa; puerta al centro y dos ventanas con postigos chile cerrados | `M_Cal_Arena`, `M_Turquesa`, `M_Crema`, `M_Caoba`, `M_Chile`, `M_Herreria` |
| Casa vecina B | caliza con zócalo chile; puerta a la izquierda y dos ventanas con reja | `M_Piedra_Caliza`, `M_Chile`, `M_Crema`, `M_Caoba`, `M_Herreria` |

Colores nuevos: solo tres tonos de piel (`M_Piel_Clara` `#E3B58D`, `M_Piel_Media` `#C98B5E`, `M_Piel_Oscura` `#6B4530`). El resto es la paleta existente. La paleta única (`make_palette.py`) ya conserva la celda de cada color anterior al añadir estos tres, así que los FBX de `Paleta/` ya exportados siguen siendo válidos.

## Uso en Unity

1. Usar los `.fbx`. **Casas vecinas:** el origen es el centro de la huella; colocarlas donde hoy están las cajas provisionales (centro en x = ±12, z = 1 de la escena). Son sólidas y la fachada mira a −Y, hacia la calle. Para variar, la A y la B se pueden alternar en cada lado.
2. **Clientes:** estáticos, de pie, con los brazos ligeramente separados y sin esqueleto. Sirven para colocarlos en la cola del despacho (documento 04, sección 2.6: puntos (6.1, 1.75), (6.1, 0.4) y (6.1, −0.9) en el callejón, mirando a −X; para eso hay que girarlos 90° en Y). No hay animación de caminar ni de comprar: es una tanda aparte.
3. Colliders no incluidos: una cápsula para los clientes y una caja para las casas.
4. Orientación en Unity sin verificar.

## Límites

- Sin esqueleto, sin animación, sin expresiones; manos de esfera, ojos de caja. El estilo es muy simple a propósito; una lectura a 3 m depende de la silueta y los colores.
- Sin verificar en Unity: importación, orientación, escala frente al jugador (1.8 m) y lectura con la iluminación URP.
- Las casas vecinas son macizas: no tienen interior ni puerta que se abra.
