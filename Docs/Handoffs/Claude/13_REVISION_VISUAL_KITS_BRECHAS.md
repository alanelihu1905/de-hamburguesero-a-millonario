# Handoff Claude 13 — Revisión visual de los kits y brechas para el siguiente sprint

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Alcance:** revisión visual de las vistas ya generadas (no hay cambios de geometría en esta entrega). Cada brecha dice qué archivo y qué línea la causan.

## 1. Qué revisé

| Vista | Archivo | Piezas que muestra |
| --- | --- | --- |
| Blender, residencial y tienda | `ArtSource/PuertoAlba/Preview/Kit_residencial_tienda.png` | fachada de tramo, puerta, tienda |
| Blender, gasolinera | `ArtSource/PuertoAlba/Preview/Kit_gasolinera.png` | marquesina, surtidor, caseta, letrero |
| Blender, hoja por zona | `ArtSource/PuertoAlba/Preview/Zonas/hoja_zonas.png` | casa, gasolinera, casa e hito en las ocho zonas |
| Unity, vista general | `ArtSource/PuertoAlba/Preview/Unity/Kit_vista_general_unity.png` | seis piezas de zona, muestras, hito, gasolinera |
| Unity, hito | `ArtSource/PuertoAlba/Preview/Unity/Kit_hito_casino_unity.png` | hito con letras |

Las seis piezas de zona **no tienen** una vista de cerca propia; solo se ven en la vista general de Unity, a unos 70 m.

## 2. Brechas concretas

### 2.1 Rótulos sin letras (la misma falla que ya corregí en el hito)

Estos rótulos son paneles ámbar sin texto. En la vista de Blender se ven como cajas amarillas.

| Pieza | Dónde | Causa |
| --- | --- | --- |
| `SM_Tienda_Barrio` | `build_kit_ciudad.py` l. 178 | plano `M_Letrero_Texto` de 4.6 × 0.45 m sin letras |
| `SM_Gasolinera_Letrero` | `build_kit_ciudad.py` l. 226 | plano `M_Letrero_Texto` de 2.2 × 0.7 m sin letras |
| `SM_Taller_Brisa` | `build_zonas_hitos.py` l. 175 | plano `M_Letrero_Texto` de 4.6 × 0.4 m sin letras |

**Corrección propuesta:** reutilizar `letras()` (ya usado en el hito y en el letrero JACKPOT) con celdas de 0.06–0.08 m. Riesgo bajo; cambia solo la geometría de esos rótulos.

### 2.2 Mercado (`SM_Mercado_Pabellon_Reloj`)

- **Reloj sin esfera legible:** el disco es un cilindro de 0.12 m con una manecilla; no tiene marcas de hora ni números. En la vista general no se distingue de un remate.
- **Sin puestos ni mercancía:** el pabellón es un techo sobre cuatro pilares; falta una fila de mostradores o cajas que diga «mercado».
- **Sin acceso marcado:** el pabellón no tiene un paso visible hacia su interior.

### 2.3 Gasolinera (`PF_Gasolinera_Completa`)

- **Surtidor demasiado pequeño y sin isla:** 0.9 × 0.65 × 1.55 m sobre el suelo sin bordillo ni isla; en la vista se confunde con una caja roja.
- **Banda luminosa sin marca:** `M_Marquesina_Luz` tiene emisión apagada; no hay letrero de marca en el canopy (la marquesina es blanca sin rótulo).
- **Caseta sin puerta:** el escaparate se ve, pero no hay acceso para el jugador.

### 2.4 Fachadas residenciales (`SM_Fachada_Residencial`, `SM_Casa_Dos_Plantas`)

- **Un solo tipo de tramo:** la fachada de tramo no tiene variación de cornisa, ventanas ni balcón; repetir varias da una calle idéntica.
- **Ventanas planas:** el vidrio es un plano casi a ras del muro; no hay alféizar ni sombra de marco, así que se leen como pegatinas.
- **Casa de dos plantas sin entrada elevada:** la puerta está a nivel de calle, sin escalón ni porche; el balcón es una losa con barandilla simple.

### 2.5 Generales (todos los kits)

- **Sin texturas:** todo es color plano; la lectura depende solo de contraste de color.
- **Sin LOD ni colisión fina:** las colisiones son cajas; la gasolinera tiene pilares, pero las mesas de la terraza son cajas enteras.
- **Vista de cerca sin revisar:** las seis piezas de zona no tienen captura de cerca; el hito tiene una captura frontal y la gasolinera solo la vista de Blender.

## 3. Lo que sí se ve bien

- Escala y pivote correctos en las vistas (base en el suelo, frentes hacia la calle).
- Paletas por zona separadas y reconocibles en la hoja (`hoja_zonas.png`).
- Hito con letras legibles y sin oclusión (`Kit_hito_casino_unity.png`).

## 4. Propuesta para el siguiente sprint (ordenada por valor/riesgo)

1. **Letras en los tres rótulos vacíos** (2.1). Bajo riesgo, alto impacto visual. Archivos: `build_kit_ciudad.py`, `build_zonas_hitos.py`.
2. **Captura de cerca de las seis piezas de zona** en la escena de integración, con cámara de 6–8 m por pieza. Sin cambio de geometría; solo verificación.
3. **Variación de fachadas** (2.4): dos tramos más (con balcón y con cornisa distinta) y alféizares de 0.05 m.
4. **Mercado** (2.2): reloj con marcas de hora y mostradores frente al pabellón.
5. **Gasolinera** (2.3): isla con bordillo para los surtidores y un rótulo de marca vacío (sin nombre comercial real).
6. **Texturas simples** (2.5): solo si el rendimiento lo requiere; hoy no hay evidencia de que el color plano sea un problema.

## 5. Límites

- Revisión visual de vistas generadas, no de la escena jugable.
- Sin mediciones de rendimiento ni de legibilidad a resolución de juego.
- Las brechas de la sección 2 son observaciones de las vistas, no pruebas automáticas.

## 6. Correcciones aplicadas (esta tanda)

Cada brecha de las secciones 2.1–2.4 tiene ahora su corrección. Antes y después son renders de Blender con el mismo encuadre (`render_piezas.py`) y capturas de Unity de la misma escena de vista previa.

| Brecha | Corrección | Dónde |
| --- | --- | --- |
| 2.1 Tienda sin letras | rótulo «TIENDA» en bloques de 0.06 m, centrado en el panel | `build_kit_ciudad.py`, `build_tienda()` y función `rotulo()` |
| 2.1 Letrero de gasolinera sin letras | rótulo «GAS» en bloques de 0.09 m | `build_kit_ciudad.py`, `build_letrero()` |
| 2.1 Taller sin letras | rótulo «TALLER» en bloques de 0.055 m | `build_zonas_hitos.py`, `build_taller()` y función `rotulo()` |
| 2.2 Reloj sin marcas | 12 marcas horarias oscuras en la esfera y manecilla de minutos | `build_zonas_hitos.py`, `build_mercado()` |
| 2.3 Surtidor sin isla | isla de 1.6 × 1.0 × 0.15 m con bordillo bajo el surtidor | `build_kit_ciudad.py`, `build_surtidor()` |
| 2.4 Una sola fachada | nueva variante `SM_Fachada_Residencial_B`: toldos sobre las ventanas, alféizares y un solo postigo | `build_kit_ciudad.py`, `build_fachada_b()` |

**Topes de triángulos subidos, con motivo:** `SM_Tienda_Barrio` pasa de 700 a 1000 (852 con «TIENDA»); `SM_Gasolinera_Letrero` de 200 a 500 (396 con «GAS»); `SM_Taller_Brisa` de 700 a 900 (708 con «TALLER»). Las letras son lo pedido; no se recortaron.

**Medidas nuevas (Blender, comprobadas en el script):** surtidor 1.6 × 1.0 × 1.55 m (isla); fachada B 4.2 × 0.76 × 3.38 m (toldos a y = −0.5). El resto no cambia.

### 6.1 Archivos de esta tanda

| Ruta | Estado |
| --- | --- |
| `ArtSource/PuertoAlba/build_kit_ciudad.py` | modificado: rótulos, isla, fachada B, topes |
| `ArtSource/PuertoAlba/build_zonas_hitos.py` | modificado: rótulo del taller, marcas del reloj, topes |
| `ArtSource/PuertoAlba/render_piezas.py` | nuevo: vistas individuales antes/después |
| `ArtSource/PuertoAlba/validar_kit.py` | modificado: 10 piezas base (antes 9) |
| `ArtSource/PuertoAlba/stats_kit.json`, `stats_zonas_hitos.json` | regenerados |
| `ArtSource/PuertoAlba/Preview/antes/*.png` | vistas de antes (6 piezas + 2 capturas Unity) |
| `ArtSource/PuertoAlba/Preview/despues/*.png` | vistas de después (7 piezas + 2 capturas Unity) |
| `Assets/_Game/Models/PuertoAlba/*.fbx` (+ `.meta`) | regenerados; `SM_Fachada_Residencial_B.fbx` nuevo |
| `Assets/_Game/Models/PuertoAlba/Prefabs/PF_Fachada_Residencial*.prefab` | nuevos (A y B) |
| `Assets/_Game/Models/PuertoAlba/Prefabs/PF_Gasolinera_Completa.prefab` | regenerado: colisión de la isla |
| `Assets/Editor/PuertoAlbaKitImportCheck.cs` | surtidor retirado de la lista de frente (la isla es simétrica en Y) |
| `Assets/Editor/PuertoAlbaKitPreviewBuilder.cs` | prefabs de fachadas A y B; colisión de la isla; fila de variación |
| `Assets/_Game/Scenes/PuertoAlba_KitPreview.unity` | regenerada (escena propia, fuera de la build) |
| `ArtSource/PuertoAlba/unity_import_check.txt`, `unity_preview_check.txt` | informes de esta tanda |

### 6.2 Marcadores y pruebas ejecutadas

- `build_kit_ciudad.py`: `LISTO` (10 piezas, medidas comprobadas en el script).
- `build_zonas_hitos.py`: `LISTO_ZONAS_HITOS 6 piezas`.
- `render_piezas.py antes` y `despues`: 6 y 7 vistas, respectivamente (la fachada B no existía antes).
- Unity 6000.0.84f1, batch:
  - `TYCOON_KIT_IMPORT_OK: 16 FBX importados y 48 materiales de zona creados` (10 del kit + 6 de zona).
  - `KIT_PREVIEW_BUILD_OK` (construcción de prefabs y escena).
  - `TYCOON_KIT_PREVIEW_OK` con **173 comprobaciones OK y 0 FALLO** en `unity_preview_check.txt`.
- `validar_kit.py`: `FALLOS (0)`, código de salida 0.

### 6.3 Antes y después (vistas revisadas)

- `Preview/antes/SM_Gasolinera_Letrero.png` → `Preview/despues/SM_Gasolinera_Letrero.png`: panel amarillo vacío → «GAS» legible.
- `Preview/despues/SM_Mercado_Pabellon_Reloj.png`: reloj con 12 marcas y dos manecillas.
- `Preview/despues/SM_Fachada_Residencial_B.png`: segunda variante con toldos y alféizares.
- `Preview/Unity/Kit_vista_general_unity.png` (después): fila de fachadas A y B alternadas, isla bajo los surtidores, rótulos con letras.

## 7. Pasos de integración (cómo se usa esto)

1. Regenerar desde fuente, si cambia algo: `build_kit_ciudad.py`, `build_zonas_hitos.py` (Blender en el host, con GPU).
2. Unity 6000.0.84f1, batch: `PuertoAlbaKitImportCheck.Run` (importa FBX, genera `.meta`, crea los 48 materiales de zona).
3. Unity, batch: `PuertoAlbaKitPreviewBuilder.Build` y luego `Verify` (prefabs, escena de vista previa y capturas).
4. Los prefabs `PF_Fachada_Residencial`, `PF_Fachada_Residencial_B`, `PF_Gasolinera_Completa` y los de zona quedan listos para colocarse. **No se colocan en la ciudad todavía**: la ubicación del casino y la cuadrícula 21×21 siguen pendientes (handoffs 09 y 11).

## 8. Lo que no verifiqué

- Legibilidad de los rótulos a resolución de juego. Las letras se ven en las vistas de cerca y en la captura general a 1600×900; no se probó en pantalla real.
- Colisiones caminando: existen; no hay prueba de movimiento.
- Las seis piezas de zona siguen sin captura de cerca propia en Unity; solo se ven en la vista general.
- La fachada B tiene un solo postigo y toldos planos: es una variante, no un tipo nuevo de edificio.
- Ninguna pieza está colocada en la ciudad.
