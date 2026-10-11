# Casa Marea — props del barrio, tanda 3 (`build_props_barrio_2.py`)

Variaciones y piezas de ambiente: banco de callejón, caja de pescado (sola y en pila), bolardo, barril, farol de pared y maceta de patio con agave. Solo usa colores de la paleta existente (ningún material nuevo). Misma convención que las otras tandas: 1 u = 1 m, frente hacia −Y, pivote en el centro de la base con z = 0.

## Estado: ejecutado en el host y verificado

- Blender ejecutó `build_props_barrio_2.py`: existen los siete FBX, `Props_Barrio_2.blend`, las dos vistas y `stats_props_2.json`.
- **Las cifras medidas por Blender coinciden con la simulación en los siete modelos.** Todos están dentro de presupuesto y con la base en z = 0 (el farol de pared tiene su excepción de pivote).
- Las vistas se leen bien: el banco con patas de caliza y azulejo, las cajas apiladas, el bolardo con su banda, el barril con tres aros, el farol de pared con su sombra sobre el muro y el agave con sus hojas abiertas. No encontré defectos de modelo.
- Detalle menor: los huachinangos dentro de las cajas se ven poco desde el frente; en tres cuartos se distinguen.

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_props_barrio_2.py
```

## Cifras verificadas (`stats_props_2.json`)

| Modelo | Triángulos (tope) | Dimensiones X × Y × Z (m) | Materiales |
| --- | --- | --- | --- |
| `SM_Banco_Callejon` | 84 (120) | 1.40 × 0.45 × 0.40 | `M_Caoba`, `M_Piedra_Caliza`, `M_Turquesa` |
| `SM_Caja_Pescado` | 212 (250) | 0.50 × 0.30 × 0.17 | `M_Turquesa`, `M_Crema`, `M_Chile` |
| `SM_Cajas_Pescado_Pila` | 396 (450) | 0.54 × 0.30 × 0.34 | los mismos; la caja de arriba va desplazada 4 cm en X |
| `SM_Bolardo` | 176 (200) | 0.30 × 0.30 × 0.68 | `M_Piedra_Caliza`, `M_Turquesa` |
| `SM_Barril` | 308 (350) | 0.53 × 0.53 × 0.70 | `M_Caoba`, `M_Herreria` |
| `SM_Farol_Pared` | 140 (200) | 0.28 × 0.34 × 0.55 | `M_Herreria`, `M_Mango` |
| `SM_Maceta_Patio` | 260 (300) | 0.40 × 0.40 × 0.62 | `M_Barro`, `M_Caoba`, `M_Vegetacion`, `M_Hoja_Clara` |

Suma: 1 576 triángulos, medidos. El banco de callejón es el de 1.4 × 0.45 × 0.4 del documento 04, sin respaldo.

## Uso en Unity

1. Usar los `.fbx`. **Excepción de pivote:** `SM_Farol_Pared` tiene el origen en el centro de la placa trasera, sobre el plano del muro (y = 0); sobresale 0.34 m hacia −Y, es decir, hacia la calle. Se coloca en la cara exterior del muro (y = −2.1 en la fachada) a la altura deseada (el origen es su punto más bajo).
2. **Farol de pared:** `M_Mango` (cristal) con emisión apagada; Unity la enciende al atardecer.
3. **Cajas de pescado:** `SM_Cajas_Pescado_Pila` ya trae dos cajas; la caja suelta sirve para pilas de otra altura (apilar a 0.17 m). La pila tiene el centro desfasado 2 cm en X por el desplazamiento de la caja superior.
4. Colliders no incluidos: una caja basta para banco, cajas, barril y bolardo (cilindro).
5. Orientación en Unity sin verificar.

## Límites

- Sin texturas ni UV pensadas para pintar, sin animación.
- No verificados en Unity; las cifras son simuladas hasta ejecutar el script.
