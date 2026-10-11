# Informe de pruebas — 2026-10-10

## Entorno

macOS 27 arm64; Unity Hub 3.22.2; Unity 6.0.84f1; Rosetta 2; Git LFS 3.8.0; Blender 5.2.0 LTS. Licencia Unity Personal activada en Hub por el usuario.

## Aprobado

- `Packages/manifest.json` pasó validación JSON.
- `dotnet run --project Tests/EconomySmoke/EconomySmoke.csproj`: ocho comprobaciones de saldo y transiciones.
- Unity importó paquetes, compiló scripts y generó `Assets/_Game/Scenes/BarrioResidencial.unity`; proceso batch salió con código 0.
- `Tycoon3D.Editor.PrototypePlaySmoke.Run` entró en Play Mode, encontró cámara y CharacterController; un raycast de 3 m alcanzó cada estación. Compró ingredientes, cocinó, vendió y confirmó una venta con $135. Proceso batch salió con código 0 y registró `TYCOON_SMOKE_OK`.
- Game View mostró la fachada, HUD, saldo inicial y cámara. Teclas W movieron al jugador hacia la puerta; R lo devolvió al punto inicial con $100 y cero ventas.
- Se añadió `com.unity.modules.audio` al manifest; tras actualizar paquetes desapareció el aviso de AudioListener desactivado.
- Git LFS inicializado localmente.
- Claude revisó el proyecto desde su app y creó `Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md`.
- Claude creó tres estaciones originales y corrigió dos medidas; Blender 5.2 regeneró fuentes, FBX, GLB y vistas. `stats.json`: huacal 628 triángulos y 0.90 × 0.55 × 0.50 m; plancha 332 y 1.10 × 0.60 × 0.75 m; venta 416 y 0.90 × 0.548 × 0.80 m.
- `CasaMareaModelIntegrator.Integrate` instanció tres FBX con materiales URP; las alturas en Unity son 0.50, 0.75 y 0.80 m y los colliders siguen las mallas. Salida `CASAMAREA_MODELS_OK`.
- Tras los cambios de QA, `PrototypePlaySmoke.Run` salió con código 0 y `TYCOON_SMOKE_OK`: tres modelos URP, raycast, prompts según estado y venta con $135. `EconomySmoke` volvió a pasar 8/8.
- `build_fachada.py` salió con Blender código 0; `stats_fachada.json` reporta fachada 1,376 tris, despacho 332 tris y puerta libre 1.99 × 2.29 m, 0 golpes de rayo. `CasaMareaFacadeIntegrator.Integrate` salió con `CASAMAREA_FACADE_OK`; Play Mode volvió a registrar `TYCOON_SMOKE_OK`.
- Antigravity.app entregó `Docs/Handoffs/Antigravity/02_QA_HITO1.md`; se corrigieron prompts estáticos, rebloqueo del cursor por clic y recarga de escena por índice. Se añadió retorno al origen si el jugador cae del borde provisional.
- Claude entregó props, cocina, clientes y casas vecinas, además de 30 FBX con paleta UV compartida. `CasaMareaPaletteIntegration.IntegrateAll` registró `CASAMAREA_MODELS_OK`, `CASAMAREA_FACADE_OK` y `CASAMAREA_NEIGHBORHOOD_OK`. Unity midió casas 6.20 × 3.45 × 6.24 m y clientes 1.72–1.80 m de alto.
- Con la escena ampliada, `PrototypePlaySmoke.Run` volvió a registrar `TYCOON_SMOKE_OK`: venta completa y saldo $135.
- Build macOS regenerada con arte nuevo: `TYCOON_MAC_BUILD_OK`, 108,340,139 bytes. La app jugable se abrió y mostró la paleta turquesa, crema, caoba y chile. La captura batch de editor no reproduce estos colores correctamente.

## Pendiente

- Pulsación E a las tres estaciones en Game View con puntero. El raycast geométrico y el método de interacción se probaron por separado en Play Mode.
- Build Windows. `PlaybackEngines` contiene solo `MacStandaloneSupport`; falta el módulo Windows.
- Animaciones y expresiones de clientes en elaboración por Claude.app.

## Incidencia resuelta

La apertura gráfica inicial entró en Safe Mode por `GenerateHLSL` en `com.unity.render-pipelines.universal-config` 17.0.3. Se incorporó el paquete local en `Packages/` y se retiró esa anotación; el archivo HLSL generado permanece. Tras ello, Unity salió de Safe Mode y la prueba de Play Mode pasó. Los avisos de carpetas `.meta` de paquetes sin directorio no bloquearon la prueba.

## Casino FOX — 2026-10-10

Todo ejecutado con Unity 6.0.84f1 en modo batch y `dotnet` 8.0.419.

- `dotnet run --project Tests/CasinoSmoke/CasinoSmoke.csproj`: 334 comprobaciones aprobadas. Cubren la tabla de premios (suma 100, retorno esperado 247 por cada 100), la frecuencia exacta de cada nivel sobre las 100 tiradas, la forma de los rodillos (pérdida con tres símbolos distintos, par con dos iguales, trío con tres iguales), 400,000 giros simulados (retorno x2.475; aciertos 62.0 %), apuesta inicial y cambio de apuesta, ajuste de apuesta por saldo, giro de cortesía con saldo $0, saldo nunca negativo en 20,000 giros, integración con el `Wallet` real sin alterar el paso de la hamburguesa en curso (jackpot x40 = $1000) y una comparación de velocidad: llegar a $10,000 toma 849 acciones vendiendo hamburguesas y una mediana de 33 con el casino.
- `dotnet run --project Tests/EconomySmoke/EconomySmoke.csproj`: 8 comprobaciones aprobadas (sin cambios).
- `Tycoon3D.Editor.CasinoFoxBuilder.Build` (idempotente): compiló sin errores y registró `CASINO_FOX_OK`.
- `Tycoon3D.Editor.CasinoPlaySmoke.Run`: `TYCOON_CASINO_SMOKE_OK`. Verifica en Play Mode: casino con 4 tragamonedas y HUD presentes desde el inicio; suelo caminable continuo desde el punto de aparición hasta el fondo del casino; pasillo y puerta libres para una cápsula de 0.34 m de radio; raycast de 3 m a cada máquina y su prompt inicial; apuesta de $25 cobrada al girar (saldo $75) sin permitir un segundo giro simultáneo; jackpot FOX determinista que deja $1075; ciclo de hamburguesa completo después (saldo $1110, 1 venta); Q cambia la apuesta a $100; con saldo $0 el casino ofrece giro de cortesía y paga $75 con trío de chiles; tras recargar la escena (equivale a R) el casino y sus 4 máquinas siguen ahí, el saldo vuelve a $100 y vuelve a pagar un jackpot.
- `Tycoon3D.Editor.PrototypePlaySmoke.Run`: `TYCOON_SMOKE_OK` (tres modelos URP, raycast, prompts por estado, venta, $135) tras los cambios en `PlayerInteractor` y el casino.
- `Tycoon3D.Editor.PrototypeMacBuild.Build`: `TYCOON_MAC_BUILD_OK`, 108,442,859 bytes, con el casino incluido.
- Revisión visual con capturas de editor (`ArtSource/CasaMarea/Preview/Casino_0*.png`): fachada, interior y máquinas coherentes con la paleta tras corregir cuatro defectos (texto visible a través de las paredes, rótulo desbordado, emblema ilegible y manchas de luz rosadas). Los colores del editor se ven más pálidos que en la build.

### Pendiente del casino
- Pulsación real de E y Q con ratón en la app jugable, y ver el panel HUD IMGUI en la build (la captura Play Mode se descartó por colgarse en modo batch).
- Los rodillos arrancan con símbolos de color en ejecución; en las capturas del editor se ven blancos porque el color se aplica en `Update`.
- Sonidos, modelos de Blender para las máquinas y reajuste de la economía (el retorno x2.47 es deliberado).

## Casino FOX v2 (2026-10-10)
- `Tests/CasinoSmoke`: 667 comprobaciones aprobadas.
- `CasinoPlaySmoke.Run`: `TYCOON_CASINO_SMOKE_OK` (sin `-quit`).
- `PrototypePlaySmoke.Run`: `TYCOON_SMOKE_OK`, saldo $135.
- `PrototypeMacBuild.Build`: `TYCOON_MAC_BUILD_OK`, 109 647 155 bytes.
- Capturas reales de la app compilada: `ArtSource/CasaMarea/Preview/Casino2_*.png` (20). Legibles: rótulos, 6 máquinas, rodillos girando, lluvia de monedas, ruta con flechas, modelos CC0. **No muestran el HUD IMGUI** (limitación de captura).

## Casino FOX — HUD, RNG y pruebas en la app (2026-10-10, tanda 3)
**Verificado**
- `dotnet run --project Tests/CasinoSmoke`: **680 comprobaciones aprobadas** (13 nuevas: RNG constante 0/1/37/99 en las 3 tablas no cuelga el sorteo —límite de 2 s— y el par sigue siendo dos iguales y uno distinto).
- `SlotMachineLogic.ReelsFor` (caso Par): el `do…while` sin tope se sustituyó por 8 intentos y un fallback determinista. Antes un RNG que devolvía siempre el mismo valor colgaba el juego.
- `CasinoFoxBuilder.Build`: `CASINO_FOX_OK` (seis tragamonedas).
- `CasinoPlaySmoke.Run` (sin `-quit`): `TYCOON_CASINO_SMOKE_OK` — casino presente al inicio y tras recargar, apuesta, jackpot, cortesía y venta de hamburguesa.
- `PrototypePlaySmoke.Run` (sin `-quit`): `TYCOON_SMOKE_OK`, saldo $135.
- `PrototypeMacBuild.Build`: `TYCOON_MAC_BUILD_OK`, 109 649 207 bytes.
- **HUD, causa de la brecha:** `OnGUI` **no falla**. La app dibuja el HUD (diagnóstico `CasinoHud.RepaintCount`: miles de repaints, `panel=True`); lo que no lo recoge es `ScreenCapture` en URP/Metal. Por eso no se sustituyó por uGUI. `screencapture` del sistema no es utilizable aquí (sin permiso de grabación: «could not create image from display»); se usó una captura de la ventana en vivo con computer-use.
- **En la app compilada, con teclado real (modo `-casino-live`):** el HUD muestra «CASINO FOX · abierto siempre», distancia, saldo, apuesta, fichas [1]-[4], resultado («Girando… apuesta $25», «¡GANASTE +$50!»), tabla de premios y «E girar · 1-4 o rueda: apuesta · Q siguiente · F luces». `E` giró y cobró (saldo $100 → $75, −$25). `3` y `Q` cambiaron la apuesta ($500 → $2000); `1` volvió a $25.
- **Audio:** en la app, durante el giro hay 2 fuentes sonando en cada tipo de máquina (`CASINO_AUDIO … fuentes_sonando=2 listener=True pausado=False volumen=1`).
- Capturas: `ArtSource/CasaMarea/Preview/Casino3_*.png` (20, app compilada, sin HUD por la limitación de `ScreenCapture`) y `Casino3_HUD_*.jpg` (4, ventana en vivo con HUD; `…premio_zoom_layout_previo` es del diseño anterior).

**Defectos de HUD corregidos (vistos en la app real):** el panel tapaba la máquina (ahora compacto, anclado abajo); la tabla de premios se pisaba con el saldo y la línea de controles; la etiqueta «abierto siempre» se cortaba; el saldo quedó aplastado en una primera versión (ancho de fichas reducido).

**Pendiente / no verificado**
- Que el audio **se oiga** (solo se comprobó que las fuentes suenan, sin salida de audio real).
- Premio de jackpot y giro de cortesía en la app en vivo (sí en el smoke de Play Mode).
- Captura del HUD a 1600×900 y con la ventana a pantalla completa; las capturas en vivo se hicieron a 1280×720.
- `Q` solo se pulsó una vez junto a `3`; el ciclo completo de apuestas con `Q` está en el smoke, no en la app en vivo.

## Casino FOX — tanda 4: especificación, HUD en vivo, clientes animados (2026-10-10)
**Verificado (con marcador o prueba en la app)**
- `dotnet run --project Tests/CasinoSmoke`: **685 comprobaciones aprobadas** (5 nuevas del libro del casino: apuesta registrada al aceptar, premio liquidado una vez, segunda liquidación falla, resultado de la casa, cortesía contada aparte). `Tests/EconomySmoke`: 8 aprobadas.
- `CasinoFoxIntegration.IntegrateAllWithCasino`: `CASINO_FOX_OK` y `CLIENTES_ANIMADOS_OK` (3 clientes con Animator).
- `CasinoPlaySmoke.Run` (sin `-quit`): `TYCOON_CASINO_SMOKE_OK`. Añade: 3 `AnimatedCustomer` (2 en fila, 1 paseante), cada uno con Animator, avatar válido y clips Idle, Caminar y Pedir de duración mayor que 0.5 s.
- `PrototypePlaySmoke.Run` (sin `-quit`): `TYCOON_SMOKE_OK`, saldo $135.
- `PrototypeMacBuild.Build`: `TYCOON_MAC_BUILD_OK`, 110 264 643 bytes.
- **App compilada, ventana 1600×900** (computer-use sobre la ventana en vivo, modo `-casino-live`):
  - Jackpot (`-casino-roll 99`): «¡GANASTE +$1000!», «¡JACKPOT FOX x40!», saldo $1075, libro «1 rondas · apostado $25 · premios $1000». `Casino4_jackpot_en_vivo_1600x900.jpg`.
  - Giro de cortesía (`-casino-roll 0`, cuatro giros perdidos hasta saldo $0): «Giro de cortesía: la casa invita $25», giro jugado, saldo sigue en $0, libro «5 rondas · apostado $100 · premios $0 · cortesías 1». `Casino4_cortesia_*.jpg`.
- **Pantalla completa** (macOS, ventana 1382×868 con bandas): HUD completo y legible; tirada aleatoria con tres hamburguesas ×12, «¡GANASTE +$148!» con el contador subiendo, saldo $375. `Casino4_pantalla_completa_*.jpg`.
- **Audio:** 2 fuentes sonando durante el giro en los 3 tipos de máquina (`CASINO_AUDIO …`). No se escuchó (sin salida de audio verificable).
- **Paseante:** `CLIENTE_PASEANTE caminando=True` con la posición avanzando por la calle.
- Capturas de la app compilada (22): `ArtSource/CasaMarea/Preview/Casino5_*.png` (incluye `14_clientes_fila` y `15_paseante`). No muestran el HUD (limitación de `ScreenCapture`, ver tanda 3).

**Defectos encontrados y corregidos**
- Panel del HUD: la tabla de premios se pisaba con la línea de controles y el libro; panel más alto y filas reordenadas (se comprobó otra vez en la app).
- Clientes animados tumbados: el clip anima también el nodo raíz del modelo con −90° en X. Se importa con una máscara de avatar sin ese nodo (`Assets/_Game/Animation/Cliente_*_SinRaiz.mask`). Antes de eso, añadir o quitar la rotación del modelo no cambiaba nada.
- Modo manual: el controlador acumula giro de ratón al bloquear el cursor y el rayo de `E` caía entre máquinas; se añadió `FirstPersonController.FaceDirection` y el jugador aparece fuera del taburete (2.05 m), porque a 1.6 m el `CharacterController` lo desplazaba de lado.
- `Packages/manifest.json`: añadido `com.unity.modules.animation`.

**Pendiente / no verificado**
- **El saldo no se guarda**: al recargar la escena vuelve a $100 (el smoke lo exige). Es la brecha mayor frente a la especificación (§3.3, §17.8).
- Que el audio se oiga; mezcla por buses.
- Los clientes animados son de ambiente: no piden ni pagan ventas reales.
- Probado en una sola pantalla (Retina 2560×1664); no se midió rendimiento.
- Primer arranque tras un build: dos de cinco lanzamientos seguidos no avanzaron fotogramas hasta activar `runInBackground` en `AppShots`; si pasa en el juego normal, hay que revisar `Application.runInBackground`.

## Persistencia, fichas y cajera (2026-10-10, tanda 5)
**Diseño del guardado** (`Assets/_Game/Scripts/Core/Save/`, C# puro): texto versionado (`TYCOON3D-SAVE`, `version=1`, claves `k=v`, checksum FNV-1a). Clave desconocida se ignora; ausente toma el valor por defecto; fuera de rango o checksum distinto = dañado; versión mayor = solo lectura (no se sobrescribe). Escritura atómica (`profile.tmp` → reemplazo), copia `profile.bak` solo desde un principal válido, `profile.corrupt-N` para lo dañado. `GameProfile` guarda el estado vivo de monedero, hamburguesas y casino a la vez, una vez por fotograma, y al pausar o cerrar. Directorio: `-save-dir`, `TYCOON_SAVE_DIR` o `~/Library/Application Support/Tycoon3D/De Hamburguesero a Millonario/Tycoon3D/`; con `-casino-shots <carpeta>` usa `<carpeta>/save`.

**Verificado con marcador o prueba**
- `Tests/SaveSmoke`: **459 comprobaciones aprobadas** (códec, checksum, truncado, rangos, versión futura, migración, copia de seguridad, principal dañado, ambos dañados, `.tmp` a medias, directorio imposible, ronda pendiente liquidada una sola vez, fichas con conservación, simulación con reinicios a mitad de giro: valor total = inicial − apostado + premios).
- `Tests/CasinoSmoke`: 685. `Tests/EconomySmoke`: 8.
- `CasinoFoxIntegration.IntegrateAllWithCasino`: `CASINO_FOX_OK` y `CLIENTES_ANIMADOS_OK` (4 personajes, con la cajera).
- `CasinoPlaySmoke.Run` (sin `-quit`): `TYCOON_CASINO_SMOKE_OK`. Nuevo: perfil escrito en disco; ventanilla alcanzable por raycast; $75 → «Cambiar $75 por 3 fichas»; lote de 20 fichas = $500; la apuesta sale de las fichas, el premio vuelve en fichas (jackpot: 19 → 59); la cajera no opera mientras giran los rodillos; canje total $1975; segundo canje no paga; ronda aceptada sin liquidar → «releer el disco» + recarga de escena → $2950 (liquidada una vez, no $100), 1 venta conservada, libro conservado, aviso «giro pendiente»; principal dañado → `RecoveredFromBackup`.
- `PrototypePlaySmoke.Run` (sin `-quit`): `TYCOON_SMOKE_OK`, saldo $135 (ahora con perfil temporal propio: antes escribía en el perfil real).
- `PrototypeMacBuild.Build`: `TYCOON_MAC_BUILD_OK`, 110 291 303 bytes.

**Prueba real de varios lanzamientos de la app compilada** (`-save-dir <carpeta-temporal>`, teclas reales por computer-use, cierre con `kill -9`):
1. Perfil nuevo → jackpot (`-casino-roll 99`) → $1075. Cierre brusco. Disco: `pesos=1075`, `casinoRounds=1`.
2. Reabierta: `SAVE_LOAD Loaded pesos=1075`; HUD «Dinero: $1075», libro «1 rondas · apostado $25 · premios $1000». Tras **R**: sigue $1075 (no $100). `Casino6_relanzamiento_saldo_1075.jpg`, `Casino6_tras_R_saldo_1075.jpg`.
3. Frente a la cajera (`-casino-at-cashier`): E → «cambiaste $500 por 20 fichas. 1 ficha = $25.» ($575, 20 fichas); Q → «canjeaste 20 fichas por $500.» ($1075); E otra vez ($575, 20 fichas). Cierre brusco. `Casino6_cajera_*.jpg`.
4. Reabierta: `pesos=575 fichas=20`. Giro de $25 pagado con una ficha: saldo sigue $575, quedan 19 fichas. `Casino6_relanzamiento_20_fichas.jpg`, `Casino6_giro_pagado_con_ficha.jpg`.
5. Principal dañado a propósito: `SAVE_LOAD RecoveredFromBackup` (la copia traía una ronda pendiente) → `SAVE_PENDING_SETTLED premio=75`, 22 fichas, `profile.corrupt-1` conservado. Sin pérdida ni pago doble.
- Capturas de la app (24): `Casino7_*.png` (incluye `16_cajera`).

**Defectos corregidos esta tanda:** el smoke de hamburguesas escribía en el perfil real (ahora temporal); `File.Move` con sobrescritura no existe en el .NET de Unity (se usa `File.Replace`); rótulos de la recepción demasiado grandes de cerca; el mostrador se movió 0.45 m para dejar sitio a la cajera y se quitó el tablero que la tapaba.

**Pendiente / límites**
- El `kill -9` se hizo con el giro ya resuelto; la ronda aceptada y sin liquidar se probó en Play Mode, en dotnet y por el caso real del paso 5 (copia con ronda pendiente), no con un cierre exacto a mitad de giro.
- Una copia de seguridad es el guardado anterior: si el principal se daña se puede perder el último cambio (el paso 5 mostró que, con una ronda pendiente, el resultado queda coherente).
- Dos instancias de la app con el mismo perfil a la vez no están protegidas.
- La cajera no tiene turnos, salario ni caja con fondos; el audio sigue sin escucharse.
- Solo un perfil por equipo; no hay menú para borrarlo (carpeta indicada arriba).

## Libro común, perfil v2 (2026-10-10)
- `Tests/SaveSmoke`: **472 comprobaciones aprobadas**. Nuevas pruebas: migración de perfil v1 con pesos, fichas, ventas y ronda pendiente; persistencia de tipo, monto, cuentas e ID de ronda; copia profunda; recorte a 32 movimientos sin perder saldos; rechazo de entrada incompleta o historial excesivo; canjes registrados.
- `Tests/CasinoSmoke`: **685 comprobaciones aprobadas**. `Tests/EconomySmoke`: **9 comprobaciones aprobadas** (compras y ventas registradas).
- Unity 6.0.84f1 Play Mode, sin `-quit`: `TYCOON_CASINO_SMOKE_OK` y `TYCOON_SMOKE_OK`. El smoke del casino comprueba saldo del libro después de jackpot y venta, apuesta con fichas y liquidación de ronda pendiente.
- Sin build nueva de macOS ni escucha de audio en esta rama. El casino continúa disponible desde el primer momento.
