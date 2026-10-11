# Estado temporal — 2026-10-10

Proyecto Unity 6.0.84f1 con URP y licencia Personal activada. Escena `Assets/_Game/Scenes/BarrioResidencial.unity` jugable. Movimiento en primera persona, interacción por raycast, saldo y ciclo comprar ingredientes → cocinar → vender implementados. Las tres estaciones y la fachada Casa Marea están integradas con FBX, orientación, materiales URP y colliders de interacción. Prueba batch de Play Mode aprobada: cámara, CharacterController, modelos, materiales, raycast, prompts por estado, una venta y saldo final $135. Game View confirmó movimiento y reinicio con R. Ocho pruebas de lógica de economía aprobadas con stubs.

Claude.app entregó plano, guía visual, estaciones, fachada, props, cocina, tres clientes estáticos y dos casas vecinas en `ArtSource/CasaMarea/`. También produjo 30 FBX con una paleta compartida de 26 colores. Los modelos quedaron integrados en la escena mediante `CasaMareaPaletteIntegration.IntegrateAll`; casas, clientes y cocina tienen tamaño vertical correcto. La compilación macOS jugable confirmó visualmente los colores de la paleta. La captura batch `Unity_Barrio_Integrado.png` muestra colores incorrectos y no sirve como referencia de arte. Claude trabaja ahora en rig, animaciones, expresiones y poses de los tres clientes; no editar sus rutas de arte en paralelo. Antigravity.app entregó `Docs/Handoffs/Antigravity/02_QA_HITO1.md`; sus tres defectos se corrigieron.

El smoke de Play Mode volvió a pasar tras integrar la paleta y el barrio: una venta y saldo $135. La build macOS se regeneró con éxito (108,340,139 bytes) y se abrió en la app. Pendiente: pulsación E manual a las tres estaciones, integrar animaciones de Claude cuando estén listas y build Windows (falta módulo). No hay multijugador ni comportamiento NPC todavía. Ver `Docs/TEST_REPORT.md`.

**Casino FOX (2026-10-10, requisito prioritario del usuario).** Implementado por Claude.app. El casino está siempre abierto desde el inicio, sin desbloqueo, en una plaza nueva al este de la calle (edificio de 8 × 6.5 m centrado en x = 14.5, z = 1, que sustituye a la casa vecina B), con letrero vertical visible desde el punto de aparición. Cuatro tragamonedas: E gira, Q cambia la apuesta ($25, $100, $500, $2000). Tabla fija de 100 resultados con retorno esperado x2.47 y 62 % de aciertos; con menos de $25 el giro es de cortesía, así que nunca se bloquea el juego. Solo pesos virtuales. La lógica es pura y con azar inyectable (`SlotMachineLogic`, `CasinoSession`) para validarla en el anfitrión. Tras R la escena se recarga y el casino sigue ahí. Ver `Docs/Handoffs/Claude/06_CASINO_FOX.md` y `Docs/DECISIONS.md`.

Pruebas del casino: `dotnet run --project Tests/CasinoSmoke` con 334 comprobaciones; `Tycoon3D.Editor.CasinoPlaySmoke.Run` con `TYCOON_CASINO_SMOKE_OK`; `PrototypePlaySmoke` y `EconomySmoke` siguen aprobados (venta con saldo $135). Build macOS regenerada con el casino (108,442,859 bytes). Sin verificar: pulsación real de E y Q con ratón y el HUD del casino en la app jugable (el panel IMGUI no se pudo capturar en modo batch).

Claude.app entregó el rig de los tres clientes en `Assets/_Game/Models/CasaMarea/Animados/`: 16 huesos, clips Idle (2.0 s), Caminar (1.07 s, 1.42 m/s, sin movimiento de raíz) y Pedir (3.0 s), y 6 blend shapes de expresión; 708, 752 y 684 triángulos y 1.80, 1.75 y 1.72 m de alto. Ejecutado en Blender y verificado leyendo los FBX; la integración en Unity (Avatar, Animator, orientación −90° en X como el resto del arte) está pendiente. Ver `ArtSource/CasaMarea/README_ANIMADOS.md` y `README_POSES.md`. No editar sus rutas de arte en paralelo.

## Casino FOX v2 (2026-10-10)
Seis máquinas, rodillos animados, sonido, HUD IMGUI, 22 modelos CC0, barrio mejorado. Smokes (casino y hamburguesas) y build macOS verificados. Pendiente: verificar visualmente el HUD IMGUI en la app. Handoff: `Docs/Handoffs/Claude/07_HANDOFF_CASINO_V2.md`.

## Tanda 4 (2026-10-10)
Verificado en la app: HUD a 1600×900 y pantalla completa, jackpot y giro de cortesía. Tres clientes animados integrados (2 en la fila, 1 paseante). Libro del casino. Sorteo sin bucles abiertos. Smokes de casino y hamburguesas con marcador, build macOS 110 264 643 bytes. **Brecha principal: el saldo no se guarda.** Mapa de requisitos: `Docs/REQUISITOS_ESPECIFICACION.md`.

## Tanda 5 (2026-10-10): persistencia, fichas, cajera
El saldo, las ventas, el libro del casino, las fichas y una ronda pendiente sobreviven a cerrar y reabrir la app y a R. Cajera funcional en el casino. 5 lanzamientos reales probados. Smokes con marcador y build macOS 110 291 303 bytes. Siguiente: caja con fondos, libro de movimientos común, preparar el repositorio para tres colaboradores (ver `CONTRIBUTING.md` cuando exista).

## Preparación para GitHub (2026-10-10, solo local; nada publicado)
Repositorio público compartido para tres personas con sus agentes. Preparado: `README.md` de arranque, `CONTRIBUTING.md`, `Docs/COLABORACION.md` (áreas, tablero, decisiones), plantillas de PR e issues, flujo de pruebas `dotnet`, 15 issues iniciales en `tools/github/issues.tsv` (script en simulación), `.gitignore` y `.gitattributes` reforzados. Pendiente de confirmar con el usuario: propietario, nombre, usuarios de las dos personas, licencia y si se publica la especificación. Sin commits ni remotos todavía.

## Puerto Alba · Fase 1 · modelo de zonas (rama `ciudad/fase-1-modelo`, 2026-10-11)
**Hecho y verificado**
- `Assets/_Game/Scripts/City/CityMap.cs`: ocho zonas con nombre, propósito y lugares para negocios; nueve enlaces (avenida central y anillo secundario del plan); ruta mínima por cruces; validador con datos rotos.
- `Tests/CitySmoke` (dotnet): **44 comprobaciones aprobadas**. Cubre validez del mapa, grafo simétrico, alcanzabilidad, casino en la zona de inicio, rutas conocidas y 10 fallos detectados a propósito (zona aislada, enlace a sí misma, duplicado, menos de 8 zonas, zona inexistente, sin nombre, sin negocios, zona de inicio cortada, sin ruta, casino fuera de inicio).
- Unity 6000.0.84f1: `CityMap.cs` compila sin errores (`-batchmode -quit`, 0 `error CS`).

**No hecho (siguientes bloques)**
- Escena de ciudad y streaming por zonas; navegación de primera persona y cámara física; HUD, mapa y ajustes de ciudad; NPC por zona; transporte; casino en esta rama (queda para la rama de prioridad 2).
- Ningún smoke de Play Mode nuevo: este bloque no tiene escena ni comportamiento en juego que probar todavía.
- El barrio actual y el casino no se han modificado en este bloque.

## Propuesta visual Puerto Alba y handoff para Claude (2026-10-11)
- Antigravity preparó la propuesta de diseño visual, física de primera persona, HUD multi-resolución (720p, 900p, ultrawide 21:9) y guía técnica de Unity 6 URP en `Docs/Handoffs/Claude/08_PROPUESTA_VISUAL_PUERTO_ALBA.md`.
- No se editó código en este paso; la entrega queda preparada para la dirección visual de Claude y la integración modular de Codex.

## Puerto Alba · Fase 1 · escena mínima y apoyo del jugador (rama `ciudad/fase-1-modelo`, 2026-10-11)
**Hecho y verificado**
- `Assets/_Game/Scripts/City/CityLayout.cs`: geometría de dos zonas (Casa Marea y Mercado del Sol) con una calle entre ellas fuera de cualquier zona. Validación de cajas y pruebas en `Tests/CitySmoke`: **53 comprobaciones aprobadas**.
- `Assets/_Game/Scripts/City/CityZoneTracker.cs`: informa de la zona en la que está el jugador; no mueve nada ni toca la economía.
- `Assets/Editor/PuertoAlbaSceneBuilder.cs` genera `Assets/_Game/Scenes/PuertoAlba.unity` (escena nueva; no cambia el barrio ni la lista de escenas de compilación).
- `Assets/Editor/PuertoAlbaPlaySmoke.cs`: Play Mode con marcador real `TYCOON_PUERTO_ALBA_SMOKE_OK` (Unity 6000.0.84f1, batch, sin `-quit`). Comprueba que el jugador queda apoyado sobre el suelo (hueco ≤ 0,12 m) y que el seguidor de zona cambia Casa Marea → calle → Mercado del Sol → Casa Marea.
- **Cambio en `FirstPersonController`:** la gravedad ya se aplica aunque el cursor esté libre (antes, con Esc, el jugador se quedaba flotando). Solo el giro y el desplazamiento horizontal siguen dependiendo del cursor bloqueado.
- Regresión: `TYCOON_SMOKE_OK` (hamburguesas) y `TYCOON_CASINO_SMOKE_OK` (casino) pasan tras ese cambio.

**No verificado**
- La caída por gravedad en batch no es fiable: el controlador se integra con pasos de microsegundos (`Time.deltaTime` ~2e-5 s), así que el smoke comprueba el **asentamiento** con `CharacterController.Move`, no la velocidad de caída. Falta comprobarla en una ventana real de la app.
- Ninguna escena de ciudad se ha probado en build macOS ni Windows.

**No hecho (siguientes bloques)**
- Navegación en primera persona completa (aceleración, pasos, interacción E/Q en la calle), HUD y minimapa propios, ajustes de sensibilidad y FOV, streaming de zonas, NPC por zona, transporte y la rama de casino.

## Puerto Alba · kit visual modular (rama `ciudad/fase-1-modelo`, 2026-10-11)
**Hecho y verificado en Blender 5.2 LTS**
- `ArtSource/PuertoAlba/build_kit_ciudad.py` genera siete piezas: fachada residencial, puerta, tienda de barrio, marquesina, surtidor, caseta y letrero de gasolinera. Termina con código 0; dimensiones y pivote comprobados dentro del script; todas por debajo de su presupuesto de triángulos (732 en total).
- Vistas en `ArtSource/PuertoAlba/Preview/`. Variación de color y piezas preferentes por zona en `zonas_variacion.json` (ocho zonas, ids de `CityMap.cs`).
- Detalle, licencia propia y decisiones abiertas en `Docs/Handoffs/Claude/09_KIT_VISUAL_CIUDAD.md` y `ArtSource/PuertoAlba/LICENCIA.md`.

**No verificado**
- Importación en Unity 6000.0.84f1 (batch, `PuertoAlbaKitImportCheck`): siete FBX importados con `.meta`, escala 1, base en y = 0, frente en +Z por medida. Informe en `ArtSource/PuertoAlba/unity_import_check.txt`. No es una revisión visual en escena.
- Colocación en la cuadrícula 21×21: no hay `CityGridLayout` en esta rama; el tamaño de celda está por confirmar.
- Ubicación del casino: el plan y `CityMap.cs` lo ponen en Casa Marea; la propuesta 08 lo pone en la zona 8. Decisión pendiente de Claude o Codex.

## Puerto Alba · HUD y ajustes de la ciudad (rama `ciudad/fase-1-modelo`, 2026-10-11)
**Verificado**
- `Tests/CitySmoke`: 183 comprobaciones aprobadas. Incluye disposición del HUD en 1280×720, 1600×900 y 2560×1080 con escala de texto de 0,85× a 1,5×, mapeo del minimapa y límites de ajustes.
- Unity 6000.0.84f1 batch, secuencia limpia: `PuertoAlbaSceneBuilder.Build` con `PUERTO_ALBA_SCENE_OK` (código 0) y `PuertoAlbaPlaySmoke.Run` con `TYCOON_PUERTO_ALBA_SMOKE_OK` (código 0). El smoke comprueba el banner de zona en cada paso y que sensibilidad y FOV se limitan y llegan al jugador y a la cámara.
- Regresión: `TYCOON_SMOKE_OK` (barrio) y `TYCOON_CASINO_SMOKE_OK` (casino) pasan tras el cambio en `FirstPersonController`.

**No verificado**
- Aspecto visual del HUD en pantalla real (solo pruebas de disposición y de lógica).
- Escala de texto y minimapa en las resoluciones objetivo fuera de las pruebas geométricas.
- Caída por gravedad en ventana real (pendiente desde la fase 1).
