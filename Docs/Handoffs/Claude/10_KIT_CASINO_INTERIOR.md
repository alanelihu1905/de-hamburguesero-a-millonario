# Handoff Claude 10 — Kit visual interior del casino de Puerto Alba

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Base técnica:** Unity 6 (6000.0.84f1), URP, PC Windows. Blender 5.2 LTS.

> **ARTE, NO MECÁNICA.** Estas piezas son formas estáticas. No contienen ni implementan apuestas, pagos, RNG, tablas de pago ni economía. Eso vive en `Assets/_Game/Scripts/Casino` y no se toca aquí. Mostrar un rodillo o una ruleta no es una mecánica de juego, y tampoco prueba que el juego funcione.

## 1. Piezas

| Pieza (FBX) | Descripción | Tris Blender = Unity | Tope | Dimensiones X × Y × Z (m) |
| --- | --- | --- | --- | --- |
| `SM_Slot_Tres_Rodillos` | Tragamonedas de 1.80 m: cabecera JACKPOT en bloques, tres rodillos legibles (sietes dorados, cerezas rojas, BAR azul), línea de pago, palanca lateral y botón | 1260 | 2500 | 0.948 × 0.735 × 1.80 |
| `SM_Mesa_Ruleta` | Mesa de ruleta europea (37 celdas: cero verde, 1–36 rojas y negras alternando), cuenco y eje | 952 | 1000 | 2.24 × 1.44 × 0.89 |
| `SM_Mesa_Blackjack` | Mesa de blackjack con tapete, canto dorado, zapatero de cartas y pilas de fichas | 172 | 800 | 1.84 × 0.92 × 0.86 |

- Escala humana: tragamonedas de 1.80 m; tapetes a 0.76 m. Origen en el centro de la base, frente hacia −Y en Blender (+Z en Unity).
- Las letras JACKPOT son bloques de 5 × 7 celdas (sin TMP ni fuentes). Son legibles de cerca; a 6 m en la captura se leen con dificultad.

## 2. Variantes de color

`ArtSource/PuertoAlba/Casino/casino_variantes.json` define dos paletas coherentes con CASINO FOX:

- **rojo**: cuerpo `#8E1B2B`, acento dorado `#C9A227`, fieltro `#7A1420`, madera `#4A2A1E`.
- **dorado**: cuerpo dorado `#C9A227`, acento rojo `#8E1B2B`, fieltro verde `#1E5B3A`, madera `#3A2416`.

Unity crea 8 materiales en `Assets/_Game/Models/PuertoAlba/Casino/Variantes/` (`Casino_<variante>_<rol>.mat`). Los símbolos, la pantalla y las celdas no cambian de color.

## 3. Archivos

| Ruta | Contenido |
| --- | --- |
| `ArtSource/PuertoAlba/Casino/build_casino_interior.py` | Generador de Blender (medidas y conteos comprobados dentro del script) |
| `ArtSource/PuertoAlba/Casino/casino_variantes.json` | Paletas rojo y dorado |
| `ArtSource/PuertoAlba/Casino/stats_casino.json` | Triángulos, dimensiones y pivote medidos por Blender |
| `ArtSource/PuertoAlba/Casino/Kit_Casino_Interior.blend` | Fuente |
| `ArtSource/PuertoAlba/Casino/unity_casino_check.txt` | Informe de verificación en Unity |
| `Assets/_Game/Models/PuertoAlba/Casino/*.fbx` | Tres piezas importables (con `.meta`) |
| `Assets/_Game/Models/PuertoAlba/Casino/Prefabs/PF_*.prefab` | Prefabs con colisión simple |
| `Assets/_Game/Models/PuertoAlba/Casino/Variantes/*.mat` | Ocho materiales de variante |
| `Assets/Editor/PuertoAlbaCasinoPreviewBuilder.cs` | Constructor y verificación (`Build`, `Verify`) |
| `Assets/_Game/Scenes/PuertoAlba_CasinoInteriorPreview.unity` | Escena de vista previa separada, fuera de la build |
| `ArtSource/PuertoAlba/Preview/Unity/Casino_interior_unity.png` | Captura real de la escena |

## 4. Colisiones

Cada prefab tiene `BoxCollider` simples: el cuerpo de la tragamonedas (y su cabecera), y el cuerpo de cada mesa. No se modelan las patas ni las sillas.

## 5. Marcadores (Unity 6000.0.84f1, batch)

- `CASINO_INTERIOR_BUILD_OK: variantes, prefabs y escena Assets/_Game/Scenes/PuertoAlba_CasinoInteriorPreview.unity` (código 0).
- `TYCOON_CASINO_INTERIOR_PREVIEW_OK: importación, prefabs, variantes, escena y captura comprobados.` (código 0). Informe: 45 líneas `OK`, 0 `FALLO`.
  - Importación: triángulos de Unity iguales a los de Blender en las tres piezas, y tamaños iguales a los esperados.
  - Prefabs: existen, tienen colisión y malla.
  - Variantes: 8 materiales presentes; cada pieza de la escena usa solo su variante.
  - Escena: abre y no está en la build; hay 8 piezas, todas instancias de prefab.
  - Captura tras `Shader.WarmupAllShaders()` y dos renders: se ve dorado (1.64 %) y rojo (2.71 %).

Un fallo previo de la verificación era mío: buscaba «rojo» en el nombre de «Ruleta roja». Ahora la detección usa la raíz «roj»/«dorad».

## 6. Lo que no verifica esto

- **Arte no equivale a mecánica:** ningún rodillo gira, ninguna ruleta se mueve, ninguna apuesta existe ni se paga. Los prefabs no están conectados a `Assets/_Game/Scripts/Casino`.
- Escala frente a un personaje real: las medidas son de diseño humano, no se comparan con el modelo de jugador.
- Colisiones probadas caminando: solo existen; no hay prueba de movimiento del jugador.
- Iluminación y materiales en la build; solo en la escena de vista previa.
- La fila dorada queda parcialmente tapada por la roja en la captura (la cámara no la encuadra del todo). Las dos filas sí están en la escena.
- La cabecera JACKPOT es pequeña en la captura; la legibilidad se verificó por la presencia de color, no por lectura de letras.
- Integración: esta escena no está en el casino de la ciudad ni en la cuadrícula 21×21. No cambia el hito del casino (`SM_Hito_Casino`), que sigue siendo el marcador central de la ciudad.

## 7. Siguiente paso

Decidir si estos props entran en el interior real del casino (escena de integración y ubicación en el plano). Hasta entonces, la verificación solo cubre importación, prefabs, variantes y una captura de vista previa.
