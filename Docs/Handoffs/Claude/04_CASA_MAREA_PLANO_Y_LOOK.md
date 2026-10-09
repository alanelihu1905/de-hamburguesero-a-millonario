# 04 — Casa Marea: plano, siluetas, look y HUD (hito 1)

**Autor:** Claude, dirección artística. **Fecha:** 2026-10-09. **Estado:** propuesta de dirección lista para integrar; **nada de esto está verificado en Unity** (aún no hay compilación ni Play Mode). Todos los valores de luz y color son puntos de partida que se ajustan viendo la escena.

Unifica `02_RESUMEN_PROPUESTA_CLAUDE.md` (Casa Marea) y `03_AJUSTE_GOLFO_CLAUDE.md` (Campeche / Golfo) y las baja a medidas sobre la escena actual de `PrototypeSceneBuilder.cs`. No toca ningún otro archivo.

---

## 1. Decisiones de dirección

1. **Casa pequeña con fachada colonial, sin patio (opción b).** Patio y aljibe quedan para una ampliación; el callejón lateral este se reserva como ese futuro patio.
2. **El negocio se lee desde la calle por tres señales:** portón abierto de par en par, chimenea con humo en la azotea y un banderín perpendicular a la fachada que apunta al despacho.
3. **El despacho al cliente es una ventanilla en el muro este**, a la altura del mostrador, que da a un callejón lateral con toldo turquesa y crema. El flujo de cocina queda en línea recta: huacal → plancha → venta → ventanilla.
4. **Las tres estaciones se distinguen por silueta, no solo por color** (ancha con bultos / baja con tubo / alta con festón). Prueba de aceptación: deben reconocerse en escala de grises a 3 m.
5. **Luz de tarde dorada desde el suroeste**, con sombras ciruela vía ambiente en gradiente. El mar queda al oeste (izquierda del jugador al mirar al norte).
6. **Regla de color 60/30/10:** arena rosada 60 %, turquesa + crema 30 %, chile + mango 10 % (solo acentos y guía de interacción).
7. **Desgaste visible:** salitre en el muro bajo, cal descascarada, herrería oxidada. Nada liso ni nuevo.

---

## 2. Plano acotado

### 2.1 Convenciones (compatibles con la escena actual)

- Unidades Unity = metros. **+Z = norte (fondo de la casa)**, **+X = este**, **−X = oeste (mar)**.
- Huella de la casa: x −5…+5, z −2…+4 (10 × 6 m). Piso interior en y = 0. Muros de 0.2 m de grosor; cara exterior sur en z = −2.1. Altura de muro 3.0 m; azotea (`Techo`) con tapa en y ≈ 3.305.
- Calle: x −9…+9, z −11…−1 (corre este-oeste frente a la casa). Las casas vecinas (x ±9…±15) no cambian de posición.
- Cotas en la tabla mandan sobre el dibujo (el dibujo es esquemático).

### 2.2 Planta esquemática

```
 N (+Z)                         OESTE ← mar              ESTE →
 z=4   ┌────────────────────────────────────┐ ┌ tapia ┐ ┌─────────────
       │ [Ref]  estantes + azulejo          │ │       │ │  casa
       │ ═══════════ MOSTRADOR (z 1.78–2.53)═══╗       │ │  vecina
       │  [Huacal]    [PLANCHA]    [VENTA]  ◄══╬═ VENTANILLA (x=5, z 0.85–2.65)
 z=1.75│    -3.3          0          +3.3      ║ [Repisa] cliente ● (x 6.1, z 1.75)
       │                                       ║ toldo → (x 5.1–6.7)
       │         ░░ eje de entrada ░░          ║  ● cola (6.1, 0.4)  ● (6.1, -0.9)
       │              ░░░░░░░                  ║  banca (8.6, 0.2)
 z=-2  └─────────────┤ PORTÓN ├────────────────┘◄ banderín (x 5.0, z -2.1…-3.0)
       ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ banqueta (z -3.2…-2.0, 1.2 m) ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
                       CALLE  (adoquín / pasta gris cálido)
 z=-8.5 ● toma postal (cámara 1.65 m, mirando +Z)
```

### 2.3 Fachada (cara sur, z = −2.1)

| Elemento | Posición / medida (m) | Material / color |
| --- | --- | --- |
| Muros | los actuales (`Frente izquierda/derecha`) | `M_Cal_Arena` `#E8C4A0` |
| Zócalo | dos bandas x −5…−1 y 1…5; alto 0.9; fino 0.04 delante del muro | `M_Zocalo_Turquesa` `#2BA6A0` desgastado |
| Pilastras | 0.35 × 3.0 × 0.12 en x = ±1.35 y ±4.8; z centro −2.16 | `M_Crema_Moldura` `#F7EEDD` |
| Portón | vano 2.0 × 2.3 en x −1…1 (el actual); **dos hojas abiertas ~100°**, cada una 1.0 × 2.3 × 0.06, pivote en jamba, hacia la banqueta | `M_Caoba` `#8A5A3C` |
| Dintel + arco rebajado | dintel actual (y 2.3–3.0); arco solo en el modelo final | crema |
| Ventanas falsas (×2) | centro x = ±3.0, y 0.95–2.25 (1.2 × 1.3), hundidas 0.05 con vidrio oscuro; reja de 10 barras ⌀0.03; postigos abiertos 0.6 × 1.3 × 0.04 a cada lado | reja `M_Herreria`, postigos turquesa |
| Rótulo (`Letrero provisional`) | **subir a y = 2.6**, 3.4 × 0.55 × 0.08, z = −2.15 (libra el dintel y la cornisa) | `M_Rotulo`: fondo turquesa, texto crema + filete mango. Texto: `[NOMBRE_NEGOCIO]` pendiente de decisión narrativa |
| Cornisa | 10.4 × 0.2 × 0.2, centro y = 3.0, vuela 0.1 | crema |
| Pretil de azotea | 4 piezas, 0.2 de grosor, alto 0.45 sobre la tapa (top y ≈ 3.75) | crema por fuera, cal por dentro |
| Chimenea | cilindro ⌀0.3, base en el techo sobre la plancha (x 0, z 1.9) hasta y = 4.2; remate con sombrerete | `M_Metal_Plancha`; humo VFX = negocio activo (hito 2) |
| Banderín | brazo de herrería de 0.9 desde la esquina SE: centro (5.0, 2.6, −2.55); placa 0.06 × 0.7 × 0.8 (visible de frente al caminar por la calle) con hamburguesa y flecha hacia +X | turquesa con bordes chile |
| Banqueta | 18 × 0.15 × 1.2, tapa a y = 0 (a ras del piso), z centro −2.6; umbral de 0.03 | `M_Piedra_Caliza` `#CDBFA4` |

> Calle: hoy `Suelo` usa crema `#F7EEDD`, igual que los muros; pasa a **`#B8A78C`** (adoquín/pasta gris cálido) para separar plano de suelo y fachada.

### 2.4 Interior

| Elemento | Medida / posición | Notas |
| --- | --- | --- |
| Mostrador | **9.8 × 0.85 × 0.75, centro (0, 0.425, 2.15)** (toca ambos muros) | tapa y = 0.85, caoba; frente de caliza encalada |
| Alicatado trasero | muro z = 3.9, y 0.85–1.6, azulejo 10 × 10 cm turquesa/crema | refleja la paleta |
| Repisas | 2 repisas 1.0 × 0.05 × 0.25 en y 1.9 y 2.3, x ±1.5 | frascos, salsas (props baratos) |
| Refrigerador | 0.8 × 1.75 × 0.7 en (−4.1, 0.875, 3.5), crema con manija chile | adelanta el flujo refrigerador → corte (hito 2) |
| Campana de extracción | 1.5 × 0.5 × 0.9 en (0, 2.35, 1.9), base y = 2.1; ducto a la chimenea | acero `#8E9AA0`; fuera del campo visual normal |
| Vigas | sección 0.15 × 0.2, cada 1.0 m a lo largo de x, y 2.85–3.05 | `M_Caoba` |
| Lámparas colgantes | 2 bulbos emisivos mango en (±2, 2.5, 0.3) | solo emisión, sin luz real |
| Piso | pasta de cemento 20 × 20 cm, turquesa/crema con acento chile | franja central de 1.4 m (x −0.7…0.7) con diseño diagonal que conduce del portón a la plancha: **eje de entrada** |

### 2.5 Ventanilla y callejón de servicio (muro este)

- **Vano** en `Pared derecha` (x = 5): z 0.85…2.65 (1.8 de ancho), y 0.85…2.15 (1.3 de alto). La `Pared derecha` pasa de 1 cubo a 4 (izq., der., dintel; el antepecho no se necesita porque el vano arranca a la altura del mostrador).
- **Repisa exterior de despacho:** x 5.1…5.6, z 0.85…2.65, tapa en y = 0.85 (continúa el mostrador), grosor 0.12, caliza.
- **Toldo:** se ancla al muro en y = 2.55, cae a y = 2.15 en x = 6.7; ancho z 0.35…3.15. Rayas turquesa/crema de 0.35 m perpendiculares al muro, festón de 0.2 en el borde. Dos postes de herrería ⌀0.05 en (6.6, 0.45) y (6.6, 3.05).
- **Cierre del callejón:** tapia de caliza en z = 4, alto 2.4, x 5…9 (futura puerta al patio).
- **Props de ambiente:** banca de madera 1.4 × 0.45 × 0.4 en (8.6, 0.2); dos cajas de pescado 0.5 × 0.3 × 0.35 apiladas en (8.4, 3.3).

### 2.6 Recorridos

- **Jugador:** aparece en la calle, entra por el portón (eje x = 0), recorre el eje de entrada hasta la plancha y camina a lo largo del mostrador. Puede salir al callejón por el portón y rodear la fachada.
- **Cliente (hito 2):** camina por la calle de oeste a este, ve el banderín, gira al callejón y espera en la cola (6.1, −0.9) → (6.1, 0.4) → punto de servicio (6.1, 1.75), mirando −X. Puede sentarse en la banca (8.6, 0.2) mientras espera.

### 2.7 Toma postal (primera impresión)

Mover el punto de aparición de `(0, 1, −5)` a **`(0, 1, −8.5)`** mirando +Z. A 3 m, con FOV 60° vertical, la fachada no cabe; a 8.5 m sí, con el rótulo centrado. Es la primera imagen de producto; conviene verificarla en Play Mode.

---

## 3. Las tres estaciones

Reglas comunes:

- Base asentada **exactamente en la tapa del mostrador (y = 0.85)**. Hoy el centro está en y = 1.08 con alto 0.35 → base en 0.905, **flotan 5.5 cm**. Fórmula: `centro.y = 0.85 + alto/2`.
- El componente `BurgerStation` y el collider principal se quedan en la **raíz**; los hijos decorativos pueden tener collider porque `PlayerInteractor` busca `IInteractable` en los padres (`GetComponentsInParent`).
- Posiciones X: **huacal −3.3, plancha 0, venta +3.3** (z = 1.75). Simétrico respecto del eje de entrada.
- Los tres deben distinguirse en escala de grises a 2–3 m.

### 3.1 Huacal de ingredientes — "ancha, con bultos"

- **Caja:** 0.90 × 0.50 × 0.55, con paredes ligeramente abiertas hacia arriba (trapecio, ~8°).
- **Cuerpo:** tablillas pintadas `#E4572E` con cantos de caoba. Pictograma de jitomate en el frente.
- **Contenido visible:** medio pan ⌀0.28 (dorado/crema), lechuga rizada (esfera irregular verde), 2 jitomates ⌀0.14 rojos, cebolla morada. Sobresale hasta 0.50 de alto.
- **Silueta:** trapecio ancho coronado por bultos redondos. Es la única con contorno irregular arriba.
- **Greybox:** 1 cubo escalado + 4 esferas.

### 3.2 Plancha — "baja, con tubo"

- **Cuerpo:** 1.10 × 0.30 × 0.60, acero oscuro `#3E4A50`; superficie de cocción `#2B3438`, más lisa (smoothness 0.55).
- **Frente:** panel con dos perillas ⌀0.07 en chile y una ranura de brasa (emisión apagada por defecto).
- **Tubo de extracción:** cilindro ⌀0.10 en la esquina trasera derecha (x +0.45, z +0.2 local), sube hasta 0.75 de alto total, con sombrerete.
- **Silueta:** losa baja y ancha con un solo tubo vertical en un extremo.
- **Greybox:** 1 cubo + 1 cilindro + 2 cilindros pequeños.

### 3.3 Punto de venta — "alta, con festón"

- **Base:** 0.90 × 0.12 × 0.55 en turquesa.
- **Retablo trasero:** panel 0.90 × 0.70 × 0.05 turquesa, con borde superior festoneado crema (mini toldo). Es lo que lo hace alto: 0.80 en total.
- **Props:** riel de comandas con 2 papeles crema (0.12 × 0.2) y campanilla de servicio ⌀0.11 en mango, adelante a la izquierda.
- **Silueta:** la única alta y con tope ondulado, claramente distinta de las otras dos.
- **Greybox:** 2 cubos + 1 esfera + 2 cubos finos (tickets).

### 3.4 Estados por paso del ciclo (guía visual)

Solo la estación **activa** muestra guía; las demás quedan neutras. Implementación mínima: emisión o contorno sobre la estación activa (no depende de color únicamente: lleva pulso de escala ~3 %).

| `BurgerStep` | Huacal | Plancha | Venta |
| --- | --- | --- | --- |
| `NeedIngredients` | **activo:** borde mango pulsante | frío, sin brasa | apagado |
| `NeedCooking` | normal | **activa:** brasa `#F4C95D → #E4572E`, vapor leve | apagado |
| `ReadyToSell` | normal | hamburguesa terminada sobre la plancha | **activa:** campanilla con destello, festón brillante |

---

## 4. Materiales y desgaste

12 materiales como máximo en el hito 1 (reutilizar tintes antes de crear otros). Shader URP/Lit; sin mapas normales por ahora.

| ID | Color base | Smoothness / Metallic | Textura | Nota de desgaste |
| --- | --- | --- | --- | --- |
| `M_Cal_Arena` | `#E8C4A0` | 0.15 / 0 | `T_Cal_Salitre` 1024, tile 2 m | **línea de salitre** irregular de 0 a 0.9 m, cal descascarada en esquinas |
| `M_Zocalo_Turquesa` | `#2BA6A0` | 0.2 / 0 | misma hoja (franja) | desvanecido hacia `#3E8F8A` abajo |
| `M_Crema_Moldura` | `#F7EEDD` | 0.2 / 0 | liso | manchas leves bajo cornisa |
| `M_Caoba` | `#8A5A3C` | 0.35 / 0 | `T_Madera` 512 | vetas pintadas a mano |
| `M_Piso_Pasta` | turquesa/crema + acento `#E4572E` | 0.45 / 0 | `T_Piso_Pasta` 512, 20 cm por loseta | losetas rotas puntuales |
| `M_Metal_Plancha` | `#3E4A50` | 0.5 / ≤ 0.3 | 256 | sin sondas de reflejo: no depender del metal |
| `M_Herreria` | `#2E2538` | 0.3 / 0.1 | 256 | vetas de óxido `#A8553A` bajo rejas |
| `M_Toldo` | turquesa/crema | 0.1 / 0 | `T_Toldo_Rayas` 256 | descolorido arriba |
| `M_Emisivo_Mango` | `#F4C95D` | emisión 2.0 | — | lámparas y brasa |
| `M_Piedra_Caliza` | `#CDBFA4` | 0.1 / 0 | tile 2 m | banqueta, tapia, repisa; también la calle tintada `#B8A78C` |
| `M_Rotulo` | turquesa | 0.2 / 0 | `T_Rotulo` 512 | pintado a mano |
| `M_Vegetacion` | `#4F7A3A` | 0.1 / 0 | liso | árbol: reemplazar la esfera por copa tipo flamboyán (3 esferas aplastadas) |

Reglas: el `Tejado` actual (mismo color caoba que la madera) pasa a cal clara `#C9B79C` con pretil crema; desde la calle solo se ve el pretil.

**Tipografía** (propuesta, licencia por verificar al importar): *Fredoka* para dinero y títulos, *Nunito* para cuerpo. Ambas figuran como SIL OFL en Google Fonts; confirmar archivo de licencia antes de incluirlas en el repositorio. El rótulo de la fachada es textura pintada, no fuente.

---

## 5. Iluminación URP — receta "Tarde_Golfo_01"

Valores de partida; ajustar en Play Mode.

### 5.1 Sol (Directional Light)

| Parámetro | Valor |
| --- | --- |
| Rotación | **Euler (30, 40, 0)**: elevación 30°, el sol queda al suroeste y la luz viaja hacia el noreste |
| Color | `#FFC47E` |
| Intensidad | 1.6 (rango de ajuste 1.3–2.0) |
| Sombras | Soft, strength 0.85, bias 1 / normal bias 0.6 |
| Efecto buscado | franja dorada que entra por el portón y cae sobre el eje de entrada; fachada sur bien iluminada; el callejón este queda en buena parte bajo la sombra de la casa (zona fresca de espera; **a confirmar en Play Mode**) |

### 5.2 Ambiente

- **Source:** Gradient. **Sky** `#8FC7CE`, **Equator** `#9A7F9E` (lila ciruela claro), **Ground** `#3B2F4A`. Las sombras ciruela salen de aquí, porque URP no tiene un color de sombra directo.
- Si lo ciruela se ve apagado o morado, desaturar un 15 %.
- **Skybox:** Procedural. Sun Size 0.04, Atmosphere Thickness 1.2, Sky Tint `#8CC4CC`, Ground `#D8C7A8`, Exposure 1.15.
- **Niebla:** Exponential Squared, color `#F2D6B8`, densidad 0.006 (bruma salina; ~3 % a 30 m).

### 5.3 Luces secundarias

- **Foco del mostrador:** Point, `#F4C95D`, intensidad ~1.5, rango 5.5, en (0, 2.55, 1.2), sin sombras.
- Presupuesto: sol + 1 luz puntual, máximo 3 luces en tiempo real a la vista. Sin horneado hasta que la geometría sea definitiva.

### 5.4 Ajustes del URP Asset

HDR activado; MSAA 4x; render scale 1.0; sombras principal 2048, distancia 35 m, 2 cascadas; límite de luces adicionales por píxel: 4; SSAO desactivado en el hito 1 (opcional si sobra presupuesto).

### 5.5 Volume global

| Efecto | Valores |
| --- | --- |
| Tonemapping | Neutral |
| Color Adjustments | Post Exposure +0.15, Contrast +8, Color Filter `#FFF1DC`, Saturation +6 |
| Shadows Midtones Highlights | Shadows `#E8D9F2` (lila suave), Highlights `#FFF0D8` (cálido) |
| Bloom | Threshold 1.0, Intensity 0.20, Scatter 0.6, Tint `#FFE2B0` (solo emisivos) |
| Vignette | Intensity 0.20, Smoothness 0.4 |

### 5.6 Otros momentos (referencia para después)

| Momento | Sol | Ambiente / niebla |
| --- | --- | --- |
| Mediodía duro | elevación ~70°, `#FFF4E0`, int. 2.2, sombras cortas y marcadas | cielo más pálido, niebla 0.004 |
| Tarde dorada (base) | lo anterior | lo anterior |
| Norte (tormenta) | int. 0.4, `#B9C3C8` | ambiente gris, niebla 0.02, postigos cerrados |

---

## 6. Presupuesto visual (PC de gama media)

Objetivo: **60 fps a 1080p en una GTX 1650 / RX 570 o equivalente**, con margen del 30 %. (Estimaciones de diseño, no medidas.)

| Concepto | Objetivo | Tope duro |
| --- | --- | --- |
| Triángulos visibles en el hito 1 | ≤ 30 000 | 60 000 |
| Draw calls (SRP Batcher activo) | ≤ 120 | 200 |
| Materiales únicos | ≤ 12 | 16 |
| Luces en tiempo real a la vista | 2 | 3 |
| Texturas | ≤ 6 mapas de ≤ 1024 px | 1 mapa de 2048 |
| Memoria de texturas (BC7 + mips) | ≤ 8 MB | 16 MB |

Triángulos por activo:

| Activo | Tris |
| --- | --- |
| Fachada completa (con portón, rejas, cornisa) | 2 500 |
| Casco interior + vigas | 1 500 |
| Huacal / Plancha / Punto de venta | 900 / 1 100 / 1 000 |
| Refrigerador | 600 |
| Toldo + postes | 700 |
| Banderín | 300 |
| Banca / cajas de pescado | 250 / 150 c/u |
| Hamburguesa modular (5 capas) | 900 |
| Árbol | 1 500 |
| Cliente (hito 2) | 3 500 |

Importación (para Codex): FBX, 1 u = 1 m, pivote en la base centrada, +Z hacia adelante, sin LOD en el hito 1. Nombres `SM_` (malla), `M_` (material), `T_` (textura), `PF_` (prefab).

---

## 7. HUD (teclado y ratón)

Referencia 1920 × 1080, margen 40 px, escala por altura (Canvas Scaler 1920 × 1080, match 0.5). Recomendación: uGUI + TextMeshPro en lugar de IMGUI; la decisión técnica es de Codex.

### 7.1 Wireframe

```
┌────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────┐   ┌───────────────────────────────┐    ┌────────────┐ │
│ │ $ 1,250   ▲ +$50 │   │ ①Comprar ─── ②Cocinar ─── ③Vender │    │ ☀ 16:30  │ │
│ │ Ventas: 12       │   └───────────────────────────────┘    │ Día 1      │ │
│ │ ★★★☆☆ (hito 2)   │                                        └────────────┘ │
│ └──────────────────┘                                          (hito 2)      │
│                                                                            │
│                                  ·  (retícula)                             │
│                                                                            │
│                  ┌─────────────────────────────────────┐                   │
│                  │ [E]  Comprar ingredientes     $15   │  ← prompt         │
│                  └─────────────────────────────────────┘                   │
│                                                                            │
│                  ┌─────────────────────────────────────┐                   │
│                  │ ✓ ¡Venta completada! +$50           │  ← mensaje (3 s)   │
│                  └─────────────────────────────────────┘                   │
│ [?] Ayuda                                         [herramientas · hito 2]  │
└────────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Jerarquía

1. **Dinero** (48 px, Fredoka, esquina superior izquierda): el número más grande de la pantalla.
2. **Tira de pasos** (arriba al centro): dónde estás en el pedido.
3. **Prompt de interacción** (debajo de la retícula, y ≈ 62 % de alto): qué hace E ahora.
4. **Mensaje** (toast, 3 s): resultado de la acción. Viene de `BurgerLoop.Message`.
5. Ayuda de teclas, reputación y reloj: secundarios; los dos últimos son hito 2 y **no existen en el código actual**.

### 7.3 Estados

**Retícula:** punto de 4 px blanco al 60 % → sobre algo interactuable, anillo de 16 px mango → bloqueado, anillo chile con cruz ×.

**Tira de pasos** (forma + color, nunca solo color):

| Estado | Forma | Color |
| --- | --- | --- |
| Pendiente | contorno | ciruela sobre crema |
| Activo | relleno + anillo pulsante | mango, texto ciruela |
| Hecho | relleno + ✓ | turquesa, texto ciruela |

Iconos por paso: carrito (comprar), llama (cocinar), campana (vender). Se corresponden con `NeedIngredients`, `NeedCooking`, `ReadyToSell`.

**Prompt:** `[E]` como keycap + verbo + precio en chip.

| Estado | Aspecto |
| --- | --- |
| Disponible | fondo crema, texto ciruela, chip de precio mango |
| No es su turno | fondo gris cálido, candado 🔒, texto: la razón (p. ej. "Primero compra ingredientes") |
| Sin dinero | chip de precio con borde chile y ícono ⚠, texto "Te faltan $N" |

**Dinero:** al cobrar, `+$50` sube desde el contador con ▲ turquesa; al gastar, `−$15` con ▼ chile; el contador cuenta hacia el nuevo valor en 0.4 s. Formato `$1,250` (es-MX).

**Mensaje:** ícono por tipo — éxito ✓ en círculo turquesa, aviso ⚠ triángulo chile, info ℹ círculo ciruela.

**Ayuda:** al inicio se muestra 20 s `WASD mover · Ratón mirar · E interactuar · R reiniciar · Esc cursor`; después queda colapsada en `[?] Ayuda`.

### 7.4 Legibilidad y accesibilidad

- Cuerpo mínimo 24 px (a 1080p); dinero 48 px; teclas en keycap de 32 px.
- Contrastes calculados: ciruela/crema ≈ 10.8:1; ciruela/mango ≈ 7.9:1; ciruela/turquesa ≈ 4.2:1 (solo texto grande o negrita); crema/chile ≈ 3.2:1 (solo íconos y números grandes, nunca texto corrido).
- Los temporizadores futuros usarán **anillo + número**, además del color.
- Sin parpadeo por encima de 3 Hz.

### 7.5 Lo que necesita el código (propuesta, la implementación es de Codex)

- `PlayerInteractor` / `BurgerStation`: exponer `IsAvailable` y un motivo (`BlockedReason`) para los estados del prompt; hoy el prompt es el mismo sin importar el paso.
- `BurgerLoop`: exponer el paso actual (ya existe `Step`) y un tipo de mensaje (éxito / aviso / info) además del texto.
- `Wallet`: evento al cambiar el saldo, para animar el contador y el `+/−`.

---

## 8. Orden de integración sugerido (para Codex)

| Prioridad | Contenido | Riesgo |
| --- | --- | --- |
| **P0** | Solo valores, sin geometría: §5 completo (sol, ambiente, skybox, niebla, volume), colores de materiales (§4), calle a `#B8A78C`, aparición en la toma postal (§2.7) | casi nulo; mayor impacto visual por esfuerzo |
| **P1** | Layout: mostrador 9.8 m, ventanilla + repisa + toldo + tapia, pilastras/cornisa/pretil/zócalo, rótulo a y = 2.6, estaciones a x −3.3 / 0 / +3.3 sobre y = 0.85 | bajo; cubos y cilindros |
| **P2** | Siluetas de §3 en greybox, estados de §3.4, props del callejón, árbol, chimenea | medio; requiere lógica de estado |
| **P3** | HUD uGUI según §7 | medio; toca `PrototypeHud`, `PlayerInteractor`, `BurgerStation` |
| **P4** | Modelos finales y texturas pintadas (§4, §6) | depende de la herramienta de arte |

---

## 9. Criterios de aceptación visual (para revisión de Antigravity)

Cada punto requiere captura de Play Mode; **ninguno está verificado todavía**.

1. Desde la toma postal se leen fachada, portón abierto, rótulo y banderín sin ruido.
2. A 3 m, en escala de grises, las tres estaciones se distinguen por silueta.
3. La estación activa (§3.4) se identifica en menos de 1 s sin leer texto.
4. Hay una franja de luz cálida sobre el eje de entrada y sombras con tinte ciruela, no negro.
5. Los muros muestran la línea de salitre; no hay superficies lisas y nuevas.
6. Las estaciones están asentadas sobre el mostrador (sin hueco).
7. El HUD cumple §7.4; los tres pasos se distinguen en blanco y negro.
8. Presupuesto de §6 respetado (Stats / Frame Debugger en el equipo objetivo).

---

## 10. Pendientes y decisiones por registrar

**Decisiones para `Docs/DECISIONS.md`** (las registra Codex; este archivo no las escribe):

- Casa pequeña con fachada colonial sin patio en el hito 1; patio y aljibe en ampliación posterior.
- Despacho por ventanilla en el muro este con callejón de servicio.
- Luz de tarde dorada desde el suroeste con sombras ciruela por gradiente de ambiente.
- Regla de color 60/30/10 y estaciones reconocibles por silueta.

**Pendiente de este encargo (`01_BARRIO_Y_COCINA.md`) que este documento no cubre:**

- Tablero de referencias con fuentes y licencias. No inventaré fuentes: requiere recopilarlas y verificarlas (fotografía propia, dominio público o con licencia clara).
- Hojas ilustradas de las estaciones (aquí solo hay especificación textual y de greybox).
- Lista de animaciones; modelos del cliente.
- Nombre del negocio para el rótulo (`[NOMBRE_NEGOCIO]`).
- Mar y malecón al extremo oeste de la calle (plano de agua turquesa `#2BA6A0 → #7FD0C8` y muro bajo de caliza): P2 o posterior.
- Verificar la licencia de las tipografías propuestas.

**Notas de honestidad:**

- Todo se basa en lectura del código y de los documentos, no en imágenes de la escena.
- `03_AJUSTE_GOLFO_CLAUDE.md` indica que no tenía a la vista el texto exacto de la propuesta original; la paleta y el flujo de §1 vienen del resumen de Codex en `02_...md`. Si el texto original difiere, hay que reconciliarlo.
