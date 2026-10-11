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
