# Handoff Claude 12 — Escena de integración PREVIEW del interior del casino

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Base:** Unity 6000.0.84f1, URP. Blender 5.2 LTS.

> **ARTE, NO MECÁNICA.** Esta escena es vista previa. No contiene apuestas, pagos, RNG ni tablas de pago, no se conecta a `Assets/_Game/Scripts/Casino` y no decide la ubicación del casino (ver `11_CONFLICTO_UBICACION_CASINO.md`). No edita escenas compartidas ni el hito `SM_Hito_Casino`.

## 1. Qué cambia respecto a `bbe21b1`

- **Letrero JACKPOT grande** (`SM_Letrero_Jackpot`, nueva pieza): tablero rojo de 10.8 × 2.0 m con marco dorado, letras de 0.25 m (1.75 m de alto) y dos patas. Antes, el JACKPOT era una cabecera de 0.6 m en cada tragamonedas.
- **Distribución:** dos bancos de tragamonedas separados por un pasillo libre de 9 m. Banco rojo (3 máquinas) a la izquierda, dorado (3) a la derecha, frentes hacia el pasillo. Ruleta a la izquierda al fondo, blackjack a la derecha al fondo.
- **Entrada y acceso:** la entrada (z = −8) y el pasillo (z −8…8.8) quedan libres. El letrero está al fondo, de frente a la entrada.
- **Referencia humana:** cápsula de 1.80 m junto al pasillo (x = −2, z = −6), fuera de la línea de visión a la cámara frontal.
- **Iluminación de vista previa:** luz direccional y tres luces puntuales (dos laterales y una al letrero). Todas marcadas «no build».

## 2. Archivos

| Ruta | Contenido |
| --- | --- |
| `ArtSource/PuertoAlba/Casino/build_casino_interior.py` | Generador: ahora incluye `SM_Letrero_Jackpot` (cuatro piezas) |
| `ArtSource/PuertoAlba/Casino/stats_casino.json` | Triángulos y dimensiones medidos por Blender |
| `ArtSource/PuertoAlba/Casino/Kit_Casino_Interior.blend` | Fuente |
| `Assets/_Game/Models/PuertoAlba/Casino/SM_Letrero_Jackpot.fbx` | Letrero (900 triángulos) |
| `Assets/_Game/Models/PuertoAlba/Casino/Prefabs/PF_Letrero_Jackpot.prefab` | Prefab con tres colisiones (tablero y patas) |
| `Assets/Editor/PuertoAlbaCasinoIntegracionBuilder.cs` | Constructor y verificación de la escena (`Build`, `Verify`) |
| `Assets/_Game/Scenes/PuertoAlba_CasinoIntegracionPreview.unity` | Escena de integración preview, fuera de la build |
| `ArtSource/PuertoAlba/Casino/unity_integracion_check.txt` | Informe de verificación |
| `ArtSource/PuertoAlba/Preview/Unity/Casino_integracion_frontal_unity.png` | Captura frontal |
| `ArtSource/PuertoAlba/Preview/Unity/Casino_integracion_general_unity.png` | Captura general |

## 3. Marcadores (Unity 6000.0.84f1, batch)

- `CASINO_INTEGRACION_BUILD_OK: escena Assets/_Game/Scenes/PuertoAlba_CasinoIntegracionPreview.unity` (código 0).
- `TYCOON_CASINO_INTEGRACION_PREVIEW_OK: escena, pasillos, escala, colisiones, letrero y capturas comprobados.` (código 0). Informe: **25 OK, 0 FALLO**.

Comprobaciones del informe:

- **Letrero:** FBX importa con los mismos triángulos que Blender (900); prefab con 3 colisiones; tamaño 10.8 × 4.1 × 0.205 m.
- **Escena:** abre, no está en la build; 3 bancos rojos, 3 dorados, zona de ruleta, mesa de blackjack y letrero presentes; cada pieza es instancia de prefab.
- **Pasillo libre:** ninguna pieza de la sala cruza el rectángulo x −4.5…4.5, z −8…8.8.
- **Bancos separados:** 11.27 m de hueco entre bancos (mínimo exigido 8 m).
- **Escala humana:** cápsula de 1.800 m y tragamonedas de 1.800 m.
- **Colisiones:** 17 colisiones de piezas distintas, 0 solapes entre sí.
- **Letrero visible:** dentro del encuadre frontal, y el primer obstáculo del rayo desde la cámara frontal es el propio letrero.
- **Captura frontal:** letras ámbar 12 048 px (35.3 % del letrero), tablero rojo 58.6 %; dorado 0.90 % y rojo 2.70 % de la imagen.
- **Captura general:** generada.

Un fallo previo de la verificación era mío: el criterio pedía fondo oscuro, pero el tablero del letrero es rojo. Ahora se mide el tablero rojo.

## 4. Límites

- **No es mecánica:** ningún rodillo gira, ninguna apuesta existe, ningún pago se calcula. Los prefabs no están conectados a `Scripts/Casino`.
- **Iluminación de vista previa:** la luz y los materiales de la escena no equivalen a la luz del juego; las capturas son de vista previa.
- **Colisiones solo estáticas:** existen y no se solapan; no hay prueba de que el jugador camine por el pasillo.
- **Captura general lejana:** a esa distancia el interior se lee como maqueta; la legibilidad fina se evalúa en la captura frontal.
- **Ubicación no decidida:** esta escena no dice si el casino va en Casa Marea o en la zona 8 (ver handoff 11). Tampoco es integración con la cuadrícula 21×21.
- **Referencia humana:** es una cápsula geométrica, no un modelo de personaje; no se compara con el modelo real del jugador.
- **No verificado:** comportamiento en tiempo de ejecución, rendimiento, audio, interacción.
