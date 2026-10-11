# 07 — Handoff: Casino FOX v2 (varias máquinas, rodillos reales, modelos CC0)

**Autor:** Claude.app · **Fecha:** 2026-10-10 · **Si la sesión se agota:** abrir una conversación nueva de Claude en la app, leer este archivo, `06_CASINO_FOX.md` y `Docs/DECISIONS.md`, y continuar por «Pendiente».

## Encargo
El usuario pidió un casino con varias maquinitas y la sensación de un casino de juego (referencia: Schedule I en Steam), **sin copiar modelos, marca ni interfaz**: sala reconocible, tragamonedas distintas y usables desde el inicio, rodillos que giran de verdad, apuesta fácil de cambiar, luces/sonido/resultado legibles, premios rápidos; mejora del barrio (arte, luz, señalética, props, HUD), prioridad a defectos visibles y accesibilidad, y modelos 3D pequeños con licencia permisiva y manifiesto. Conservar Unity 6/URP y el ciclo de hamburguesas.

## Investigación (resumen propio)
Guías de texto indican: casino con recepción, unas cinco tragamonedas en hilera, blackjack y un juego de cartas, apuestas pequeñas ($5–$100), símbolos de fruta, campana y siete, premios por tres iguales. Una captura muestra un edificio estilizado con rótulo «CASINO» grande. Se tomó solo el *tipo* de experiencia (hilera de máquinas, apuestas pequeñas y rápidas, rótulo grande); el diseño, los símbolos, los nombres y la interfaz son propios. No se descargó nada de Schedule I.

## Hecho y verificado
- Modelos externos: Kenney Furniture Kit y Food Kit (CC0), 24 FBX en `Assets/_Game/Models/External/` con licencias y `MANIFEST.json` (URL, autor, SHA-256).
- Arte y sonido propios generados por `ArtSource/CasaMarea/casino_assets/generar_assets.py`: símbolos (`slot_symbols.png`), piso, adoquín y 10 sonidos WAV en `Assets/_Game/Art/Casino/`.
- Lógica: `SlotPaytable` con 3 tablas (Zorro Clásico x2.47, Mango Rápido x2.19, Gran Jackpot x2.29), `CasinoSession.SetBetIndex`. `dotnet run --project Tests/CasinoSmoke` → **667 comprobaciones aprobadas**.
- Scripts runtime nuevos en `Assets/_Game/Scripts/Casino/`: `ReelDrum` (tambor pentagonal que gira y frena), `SlotMachineStation` (3 tipos), `CasinoService` (atajos 1-4, rueda, F luces fijas), `CasinoSounds`, `CasinoHud` (símbolos, fichas, resultado grande, tabla); y `Assets/_Game/Scripts/Dev/AppShots.cs` (capturas desde la app con `-casino-shots <carpeta>`).

## Estado de la integración en la escena (actualizar al terminar)
Ver la sección «Resultado final» al final de este archivo; si no existe, la construcción de la escena (`Assets/Editor/CasinoFoxBuilder.cs`, v2 con ayudantes en `CasinoArtHelpers.cs`) **no se completó**.

## Pendiente (en orden)
1. Si el archivo `Assets/Editor/CasinoFoxBuilder.cs` sigue siendo el de la v1 (4 máquinas), reescribirlo con la v2: edificio 11 × 8.4 m centrado en (16, 1.6), 6 máquinas (Clásica, Mango, Jackpot, Jackpot, Mango, Clásica) con `ReelDrum`, recepción, zona de estar, señalética, props Kenney, plaza adoquinada, árbol mejorado.
2. Ejecutar: `Unity -batchmode -nographics -quit -projectPath . -executeMethod Tycoon3D.Editor.CasinoFoxBuilder.Build` (Unity y Blender solo corren fuera del sandbox).
3. Actualizar `Assets/Editor/CasinoPlaySmoke.cs` (6 máquinas, 3 tipos, rodillos que acaban en el símbolo del resultado, sonidos completos) y correrlo junto con `PrototypePlaySmoke.Run` y `PrototypeMacBuild.Build`.
4. Capturas reales de la app: `"Builds/Mac/De Hamburguesero a Millonario.app/Contents/MacOS/De Hamburguesero a Millonario" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -casino-shots <carpeta>`; revisar y corregir defectos; copiar los PNG a `ArtSource/CasaMarea/Preview/`.
5. Escribir `Docs/ASSETS_EXTERNOS.md` con la lista exacta de modelos usados (el builder imprime `EXTERNAL_USED nombre xN`) y borrar de `External/` los FBX que no se usen.
6. Documentar en `Docs/DECISIONS.md`, `PROJECT_STATE.md` y `TEST_REPORT.md`.

## Notas técnicas
- Unity y Blender se ejecutan con permiso fuera del sandbox; ninguna ejecución debe esperar capturas más de 60 s (una captura en Play Mode por lote se colgó).
- `CasinoFoxBuilder` es idempotente y debe correrse después de `CasaMareaPaletteIntegration.IntegrateAll`.
- Aviso de seguridad visual: todos los destellos ≤ 3 Hz; la tecla F deja las luces fijas y se guarda en `PlayerPrefs`.

## Resultado final (2026-10-10)
**Estado: completo y verificado, con una limitación de captura (ver abajo).**

- Escena: `CasinoFoxBuilder.Build` → `CASINO_FOX_OK`, 22 modelos CC0 distintos (42 instancias; lista exacta en `Docs/ASSETS_EXTERNOS.md`). 6 máquinas desde el inicio (Clásica, Mango, Jackpot, Jackpot, Mango, Clásica), 3 rodillos `ReelDrum` cada una.
- `dotnet run --project Tests/CasinoSmoke` → 667 comprobaciones aprobadas.
- `CasinoPlaySmoke.Run` → `TYCOON_CASINO_SMOKE_OK` (casino presente al inicio y tras recargar la escena, 6 máquinas, apuesta, jackpot, giro de cortesía y venta de hamburguesa).
- `PrototypePlaySmoke.Run` → `TYCOON_SMOKE_OK` (venta, saldo $135).
- `PrototypeMacBuild.Build` → `TYCOON_MAC_BUILD_OK`, 109 647 155 bytes.
- Capturas de la app compilada (20 archivos): `ArtSource/CasaMarea/Preview/Casino2_*.png`.

### Correcciones hechas en la recta final
1. `SlotMachineStation.OnRevealed` fija siempre los rodillos al símbolo del resultado (el smoke fallaba porque quedaban a medio giro).
2. Los textos 3D salían ~2.4× más grandes en la app que en el editor: `CasinoFoxBuilder.TextScale = .42`; subtítulo de la fachada subido para no quedar bajo el toldo; rótulos de las máquinas en ciruela (contraste).
3. Plantas de la entrada movidas contra la pared para no tapar las máquinas.
4. `AppShots`: `Application.runInBackground = true` (sin foco la app se pausaba), captura con `ScreenCapture.CaptureScreenshot` y RNG de capturas que varía tras la primera tirada (un valor constante provocaba un bucle infinito en `SlotMachineLogic` línea ~116; solo afecta a ese RNG de prueba, no al juego).

### Lecciones
- **No lanzar los smokes de Play Mode con `-quit`**: Unity sale antes y devuelve 0 sin ejecutar nada. Comprobar siempre el marcador `TYCOON_*_OK` en el log.
- Unity y Blender solo corren fuera del sandbox.

### Limitaciones abiertas
- Las capturas de la app **no muestran el HUD IMGUI** (ni la etiqueta «CASINO FOX · abierto siempre» ni el panel): `ScreenCapture` no lo recoge en esta configuración URP/Metal. El HUD no se ha verificado visualmente en esta tanda; solo por lógica y smoke. Pendiente: capturar con una captura del sistema (p. ej. `screencapture` mientras la app corre en primer plano) o pasar el HUD a uGUI.
- Pulsación real de E/Q con ratón y el sonido solo se verificaron por código/smoke, no a oído.
- Los modelos externos se ven (taburetes, lámparas, plantas, mesas, alfombra, bocinas, cócteles) pero no se hizo una segunda pasada de ajuste fino de escala.

## Actualización tanda 3 (HUD, RNG, pruebas en app)
- El HUD IMGUI funciona; `ScreenCapture` es lo que no lo captura. Se rediseñó el panel (`CasinoHud.cs`) y se verificó en vivo con `-casino-live` + computer-use (`app_screenshot`). Detalle y pendientes en `Docs/TEST_REPORT.md` («tanda 3»).
- `SlotMachineLogic`: sin bucles abiertos con RNG degenerado (prueba en `Tests/CasinoSmoke`, 680 comprobaciones).
- `AppShots` ahora registra `hudRepaints`, `CASINO_AUDIO` y admite `-casino-live` (deja al jugador frente a la primera máquina para pruebas manuales).

## Actualización tanda 4 (especificación, verificación en vivo, clientes animados)
- Leer primero `Docs/REQUISITOS_ESPECIFICACION.md` y la sección «tanda 4» de `Docs/TEST_REPORT.md`.
- Modos de la app compilada para pruebas: `-casino-shots <carpeta>` (22 capturas sin HUD), `-casino-live` (jugador frente a la primera máquina), `-casino-roll N` (fija la tirada de premio; 99 = jackpot, 0 = pérdida). Para ver el HUD hay que capturar la ventana en vivo con computer-use (`app_screenshot`); `screencapture` y `ScreenCapture` no sirven.
- Clientes animados: `Assets/Editor/ClientesAnimadosIntegrator.cs` (importa FBX, máscara sin raíz, controladores en `Assets/_Game/Animation/`, instancia en la escena); runtime `Assets/_Game/Scripts/Restaurant/AnimatedCustomer.cs`. Forma parte de `CasinoFoxIntegration.IntegrateAllWithCasino`.
- Siguiente paso recomendado: guardar el saldo y el libro del casino; después ligar los clientes de la fila a `BurgerLoop`.
- Recordatorio: no lanzar los smokes de Play Mode con `-quit`; comprobar el marcador `TYCOON_*_OK`.

## Actualización tanda 5 (persistencia, fichas, cajera)
- Código: `Assets/_Game/Scripts/Core/Save/` (SaveData, SaveCodec, SaveRepository), `Core/GameProfile.cs`, `Casino/CasinoSession.cs` (fichas, Snapshot/Restore), `Casino/CasinoState.cs`, `Casino/CasinoProfileMapping.cs`, `Casino/CashierDesk.cs`, `AnimatedCustomer` (modo Cashier). Escena: `CasinoFoxBuilder.BuildCashier` y `ClientesAnimadosIntegrator` (cajera).
- Pruebas: `Tests/SaveSmoke` (459), `Tests/CasinoSmoke` (685), `Tests/EconomySmoke` (8); Play Mode `CasinoPlaySmoke`, `PrototypePlaySmoke` (sin `-quit`; usan perfil temporal).
- Modos de la app compilada: `-save-dir <dir>`, `-casino-live`, `-casino-at-cashier`, `-casino-roll N`, `-casino-shots <dir>`.
- Perfil real: `~/Library/Application Support/Tycoon3D/De Hamburguesero a Millonario/Tycoon3D/profile.sav` (+ `.bak`, `.corrupt-N`). Borrar esa carpeta reinicia el progreso.
- Pendiente: caja del casino con fondos, libro de movimientos común, turnos de la cajera, audio audible.
