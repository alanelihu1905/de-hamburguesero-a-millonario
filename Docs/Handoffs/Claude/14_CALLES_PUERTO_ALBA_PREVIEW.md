# Handoff Claude 14 — Calles de Puerto Alba: escena de vista previa

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Base:** Unity 6000.0.84f1, URP.

## 1. Qué es

Una escena de vista previa **aislada** con trazado de calles para Puerto Alba: una avenida este–oeste de 120 × 8 m, una calle norte–sur de 8 × 80 m que la cruza, aceras, bordillos, pasos de cebra, 18 farolas y 21 piezas de edificio del kit (casas, fachadas A y B, tiendas, taller, mercado, gasolinera, torre y quiosco), con el frente hacia la calle.

**No es integración.** No cambia `CityMap.cs` ni `CityLayout.cs`, no coloca el casino (decisión pendiente, handoff 11), no está en la lista de escenas de la build y no toca la escena de la ciudad (`PuertoAlba.unity`).

## 2. Archivos

| Ruta | Contenido |
| --- | --- |
| `Assets/Editor/PuertoAlbaCallesBuilder.cs` | Constructor y verificación (`Build`, `Verify`) |
| `Assets/_Game/Scenes/PuertoAlba_CallesPreview.unity` | Escena de vista previa |
| `Assets/_Game/Models/PuertoAlba/Calles/Materiales/*.mat` | 6 materiales de calle (asfalto, acera, bordillo, pintura, poste, luz) y terreno |
| `ArtSource/PuertoAlba/unity_calles_check.txt` | Informe: 106 OK, 0 FALLO |
| `ArtSource/PuertoAlba/Preview/Unity/Calles_calle_unity.png` | Captura a nivel de calle |
| `ArtSource/PuertoAlba/Preview/Unity/Calles_general_unity.png` | Captura general |
| `ArtSource/PuertoAlba/Preview/Unity/Calles_casino_unity.png` | Captura del casino desde la calle cruzada |
| `ArtSource/PuertoAlba/validar_calles.py` | Validador sin Unity del trazado (ver handoff 15) |

## 3. Marcadores (Unity 6000.0.84f1, batch)

- `CALLES_BUILD_OK: escena Assets/_Game/Scenes/PuertoAlba_CallesPreview.unity` (código 0).
- `TYCOON_CALLES_PREVIEW_OK: avenida, cruce, cebras, edificios fuera de la vía, frente a la calle y capturas comprobados.` (código 0). Informe de la versión actual: **106 OK, 0 FALLO**.

Comprobaciones:
- avenida 120 × 8 m y calle cruzada 8 × 80 m, ambas con colisión;
- 8 franjas de cebra y 18 farolas;
- 21 edificios, todos instancias de prefab, con base en el suelo;
- **ningún edificio invade la avenida (z entre −6 y 6) ni el cruce (x entre −6 y 6)**;
- frente hacia la calle: producto punto 1.000 en todos los edificios de avenida y de cruce;
- captura a nivel de calle con asfalto visible.

## 4. Errores que corregí en esta tanda

- **Seis edificios invadían la acera** en la primera ejecución. La causa: los prefabs no están centrados en su fondo (la casa de dos plantas tiene el balcón hacia el frente). Al girarlos, su extensión real cambiaba. La corrección: colocar cada edificio por la **caja medida**, de modo que su cara más cercana quede a 6.3 m del eje de la avenida o del cruce (`Apoyar()`), en vez de usar el origen del prefab.
- El primer conteo de edificios decía 18; la lista tiene 21. La verificación ahora lee el número de la lista.

## 5. Cómo se integra (pasos)

1. Revisar la disposición con Claude/Codex: la calle es una propuesta de trazado, no la ciudad real.
2. Cuando se decida la ubicación del casino (handoff 11), decidir si el casino va sobre la avenida, en la calle cruzada o en una plaza. Hoy la escena no lo ocupa.
3. Si la disposición se acepta, pasar las mismas piezas (prefabs y trazado) a la escena de la ciudad. Eso requiere permiso explícito: toca la escena compartida de la ciudad.
4. Los prefabs de edificios (`PF_*`) son reutilizables; el trazado vive en `PuertoAlbaCallesBuilder.cs` (tabla `Edificios` y constantes `AvenidaLibre`/`CruceLibre`).

## 6. Límites

- Las calles son cubos planos con colisión simple; no hay bordillo elevado, alcantarillas ni pendiente.
- El terreno es un plano de color; no hay vegetación ni acera de adoquín.
- La calle está ocupada en el cruce por la fila de edificios de cruce; no se probó caminar por ella.
- Iluminación de vista previa, no la de la build.
- Ubicación del casino y cuadrícula 21×21: pendientes (handoffs 09 y 11). Esta escena no las decide.

## 7. Actualización: casino y casas rediseñadas (verificado en Unity)

- **Casino colocado en la escena de calles**, por decisión delegada del usuario («donde tú quieras, un poquito alejado del centro»): lado este de la calle cruzada, centro en (16, 0, −20), a unos 26 m del centro, mirando a la calle (−X). La plaza de entrada (x 6…14, z −26…−14) queda libre. Es solo vista previa: no cambia `CityMap`, `CityLayout` ni el casino del barrio (ver handoff 11, sección 7).
- **Tres casas rediseñadas** (`PF_Casa_Colonial`, `PF_Casa_Moderna`, `PF_Casa_Cabana`, handoff 15) colocadas en la avenida y en el cruce.
- Pruebas en Unity 6000.0.84f1 (batch): `CALLES_BUILD_OK` y `TYCOON_CALLES_PREVIEW_OK` con **106 OK y 0 FALLO**. Entre otras: tamaño exacto de los tres prefabs de casa frente a Blender, casino centrado en (16, −20) con producto punto 1.000 hacia la calle, plaza de entrada libre y captura del casino.
- **Dos fallos previos de la verificación eran míos:** el criterio de asfalto buscaba un gris puro y la luz del cielo lo tiñe de azul; ahora se muestrea el color real del asfalto en un punto conocido de la avenida (21.7 % de la captura). Y, antes, un bloqueo de Unity por el editor del usuario abierto sobre el proyecto, que ya se cerró.
- **No verificado:** caminar por las calles; colisiones del porche y del balcón de las casas (el prefab solo tiene el volumen principal); iluminación de la build.
