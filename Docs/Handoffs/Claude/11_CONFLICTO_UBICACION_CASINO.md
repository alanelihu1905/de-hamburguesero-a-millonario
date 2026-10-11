# Handoff 11 — Conflicto de ubicación del Casino FOX: Casa Marea o zona 8

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Estado:** propuesta con evidencia y opciones. **No cambia** `CityMap.cs`, `CityLayout.cs`, `CasinoFoxBuilder.cs` ni ninguna escena compartida. Requiere decisión de Alan (y de Claude/Codex por el diseño visual).

## 1. El conflicto en una frase

Los documentos y el código ponen el casino en **Casa Marea** (zona de inicio). La propuesta visual 08 lo pone en la **zona 8 (Distrito del Ocio)**, aunque su mapa lo dibuja en el **eje central** de la ciudad. Además, el casino ya implementado en el prototipo del barrio ocupa un hueco que **cae sobre la calle** de la geometría de Puerto Alba.

## 2. Evidencia

| # | Fuente | Qué dice | Línea / dato |
|---|---|---|---|
| E1 | `Assets/_Game/Scripts/City/CityMap.cs` | `CasinoZone = ZoneId.CasaMarea`, igual que `Start` | l. 24–25 |
| E2 | `CityMap.cs` (validación) | Si el casino no está en la zona de inicio, el mapa es inválido: «no puede exigir recorrido ni compra previa» | l. 137–138 |
| E3 | `Docs/PLAN_CIUDAD_PUERTO_ALBA.md` | Casa Marea: «Casino FOX accesible siempre» | tabla, fila 1 |
| E4 | `Docs/DECISIONS.md` (2026-10-10) | Casino en «la casa vecina derecha», sobre una plaza nueva al este de la calle, centro **x = 14.5, z = 1**, «visible y alcanzable caminando desde el inicio» | l. 22 |
| E5 | `DECISIONS.md` (excepción) | El casino abre desde el inicio **por pedido directo del usuario**, aunque §15.1/§21.2 lo sitúan más tarde | l. 31 |
| E6 | `Docs/PROJECT_STATE.md` | Implementado en el barrio: edificio de **8 × 6.5 m** centrado en **x = 14.5, z = 1** | l. 9 |
| E7 | `Assets/_Game/Scripts/City/CityLayout.cs` | Caja de Casa Marea **x −10…10, z −10…10**; la **calle** es **x 10…22, z −2…2** | l. 24–31 |
| E8 | `Docs/Handoffs/Claude/08_PROPUESTA_VISUAL_PUERTO_ALBA.md` | Zona 8 «Distrito del Ocio / Casino FOX» (l. 60); el mapa dibuja «★ CASINO FOX (Eje) ★» en el centro (l. 38); vistas despejadas hacia el casino desde la ciudad (l. 70) | |
| E9 | `CityMap.cs` | La zona 8 de la propuesta **no existe** en `ZoneId` (solo hay 8 zonas: CasaMarea, MercadoDelSol, MuelleFarol, CentroDelFaro, TalleresBrisa, PaseoArena, ColinaCobalto, EstacionSur) | l. 8 |

### Cruce de geometría (E4 × E7)

- Huella del casino según E4 y E6: x ∈ [10.5, 18.5], z ∈ [−2.25, 4.25].
- Calle de la ciudad (E7): x ∈ [10, 22], z ∈ [−2, 2].
- **Solapamiento:** x ∈ [10.5, 18.5], z ∈ [−2, 2], unos 8 × 4 m. El casino no encaja en la calle sin tapar el paso.
- Nota: el barrio y la ciudad son escenas distintas. E4/E6 describen el **prototipo del barrio**; E7 es la **geometría de Puerto Alba**. La coincidencia de coordenadas es la razón del conflicto, no una prueba de que el casino ya esté en la ciudad.

## 3. Opciones

### Opción A — Casino en Casa Marea, en el eje este de la calle (recomendada)
- **Qué:** conservar la zona de inicio y su regla (E1, E2, E3, E5). Ubicar el casino en x ≈ 14.5, z ≈ 1 (E4) **ampliando la caja de Casa Marea** hasta x = 22 para que incluya la plaza del eje. La calle deja de ser una caja aparte entre Casa Marea y el mercado.
- **Cumple:** regla de inicio (E2), posición de decisiones (E4), eje central de la propuesta 08 (E8), sin zona nueva (E9).
- **Coste:** cambiar `CityLayout.Built`/`Street` (geometría de la fase 1, en esta rama) y los tests de `Tests/CitySmoke` que dependen de la calle (la prueba «la calle separa las dos zonas»). Casa Marea pasa de 20×20 m a unos 32×20 m.
- **Riesgo:** la zona de inicio crece y la pantalla de «zona actual» cambia de texto en el eje.

### Opción B — Casino en zona 8 («Distrito del Ocio»), fuera de Casa Marea
- **Qué:** crear o reutilizar una zona de ocio y apuntar `CasinoZone` a ella.
- **Coste:** rompe E2 (la validación exige que el casino esté en la zona de inicio) y la excepción E5. Hay que cambiar la regla en `CityMap.cs`, el test `Tests/CitySmoke`, y la decisión de DECISIONS.md. Además, el casino pasa de «visible desde el inicio» a «hay que caminar hasta él», salvo que se añada un hito visible.
- **Riesgo:** alto. Cambia una decisión que el usuario tomó por escrito.

### Opción C — Hito visual en el eje y casino jugable en Casa Marea
- **Qué:** dejar la lógica del casino en Casa Marea (E1) y colocar **solo el hito** `SM_Hito_Casino` en el eje central, como marca visual de la propuesta 08.
- **Coste:** dos sitios que parecen casino; hay que dejar claro al jugador cuál es el de verdad. Sin cambios en `CityMap`.
- **Riesgo:** confusión. Se puede mitigar con el mismo rótulo en el hito y una señal que indique «Casino FOX — entrada en Casa Marea».

## 4. Recomendación

**Opción A**: cumple las reglas escritas (E2, E5), respeta la decisión de ubicación (E4), y sigue el eje central de la propuesta 08 (E8) sin crear zonas nuevas. Es la opción con menos cambio de reglas, pero toca la geometría de Casa Marea y los tests de ciudad.

## 5. Lo que necesito de ti (una decisión)

1. Elige **A**, **B** o **C**.
2. Si eliges **A**, confirma que se puede ampliar Casa Marea hasta x = 22 (cambia la calle de la fase 1).
3. Si eliges **B**, confirma que se puede cambiar la regla de «casino en zona de inicio» (E2) y la excepción E5.

Hasta entonces no muevo nada: ni el casino del barrio, ni `CityLayout`, ni `CityMap`. La escena de integración de este handoff (`PuertoAlba_CasinoIntegracionPreview.unity`) es un **preview aislado** y no decide la ubicación.

## 6. Lo que no está verificado

- Que el casino del barrio (E6) realmente esté en x = 14.5, z = 1 en la escena actual. Solo consta en los documentos; no comprobé la escena `BarrioResidencial.unity`.
- Que la propuesta 08 sea la visión vigente: es un documento de Claude sin fecha de aprobación en `DECISIONS.md`.
- Impacto de la opción A sobre el HUD de zona y los smokes de Play Mode: no ejecutados, porque no hay cambio de código.

## 7. Actualización: ubicación elegida para la vista previa

El usuario delegó la ubicación («pon el casino donde tú quieras, un poquito alejado del centro»). En la **escena de calles de vista previa** (`PuertoAlba_CallesPreview.unity`) el casino (hito) está en el lado este de la calle cruzada, centrado en (16, 0, −20), a unos 26 m del centro de las calles, con la entrada hacia la calle y su plaza despejada. Verificado en Unity (handoff 14, sección 7).

**Esto no resuelve el conflicto del juego real.** Esa posición queda fuera de la caja de Casa Marea (x −10…10, z −10…10), así que sigue incumpliendo la regla de `CityMap.cs` (el casino debe estar en la zona de inicio). Para llevarla al juego haría falta cambiar `CityMap`/`CityLayout` y el casino del barrio, y eso no está hecho ni aprobado. La opción A de la sección 3 (ampliar Casa Marea) sigue siendo la recomendación si se quiere esa posición.
