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

## Puerto Alba — cuadrícula urbana (2026-10-11, rama `codex/city-graph`)

- Cuadrícula determinista de 21 × 21 cuadras, radio de diez, huella 1,776 m; zonas usan el `CityMap` canónico.
- Casino visual en `(0,0)`, zona inicial Casa Marea; dos gasolineras, mercado, muelle, parque, restaurante, patio industrial y puertas escasas para visitantes de prueba.
- Escena de blockout separada; el casino jugable con máquinas sigue en `BarrioResidencial.unity`. No hay interiores ni arte final en la cuadrícula.
- `dotnet run --project Tests/CitySmoke`: 67 comprobaciones aprobadas. Tres avisos de nulabilidad preexistentes en `CityMap.cs`.
- Antigravity creó la escena en Unity y el log registró `TYCOON_CITY_GRID_BUILT blocks=441 diameter=1776m`. Su smoke de Play Mode no llegó al marcador requerido; interacción E y vista en Play Mode siguen sin verificar.

## Puerto Alba — expansión visual de ciudad (2026-10-11)

- Sigue siendo 21 × 21 cuadras / diez cuadras de radio, ahora con identidad visual por barrio: centro financiero con dos torres y plaza, apartamentos, mercado, terminal de autobuses, parque, puerto con grúa y carga, bodegas y zona industrial.
- Dos avenidas marcadas cruzan la ciudad; incluye cruces peatonales, alumbrado, palmeras y 16 vehículos que recorren circuitos urbanos.
- Fachadas reciben variación reproducible y escaparates; la densidad de detalles se mantuvo acotada para el blockout.
- `dotnet run --project Tests/CitySmoke`: 72 comprobaciones aprobadas. Humo de Unity ampliado para exigir torres, terminal, carga y los 16 vehículos; el marcador sigue pendiente de ejecución en Play Mode.
- La ciudad aún usa geometría procedural sencilla. No declarar iluminación final, arte final ni escala de rendimiento confirmados hasta verla y medirla en Unity.
## Puerto Alba — cuadrícula urbana (2026-10-11)

- Ciudad determinista 21 × 21: diez cuadras desde centro y huella de 1,776 m por lado.
- Ocho zonas, casino central siempre presente, dos gasolineras, puntos de interés escasos y puertas de visitante deterministas.
- Blockout de primitivas y menú de editor para escena separada `PuertoAlba_Grid`.
- `dotnet run --project Tests/CitySmoke`: pasan 66 comprobaciones.
- Unity Play Mode aún pendiente. Render, colisión e interacción de puertas requieren marcador real.

## Codex (área C) — cierre del smoke de Puerto Alba (2026-10-11)

- `CityGridPlaySmoke` ahora conserva/restaura las opciones de Play Mode, desactiva Domain Reload durante el smoke y cierra Unity con código de salida al correr en batch; el uso desde Editor normal solo sale de Play Mode.
- `DoorVisitorSpawner` aplica el azul al visitante con `MaterialPropertyBlock`, sin instanciar material.
- Escena reproducible: `Assets/_Game/Scenes/PuertoAlba_Grid.unity`.
- Unity 6000.0.84f1, sin `-quit`: `TYCOON_PUERTO_ALBA_GRID_SMOKE_OK blocks=441 radiusBlocks=10 zones=8 casino=1 gasStations=2 towers=2 transit=1 cargo=3 traffic=16 doors=75 objects=2374`.
- `dotnet run --project Tests/CitySmoke`: 72 comprobaciones aprobadas. La ciudad aún genera 2,374 objetos en `Start`; optimización de agrupado/batching queda pendiente. No es arte final.

## Codex (área C) — consulta espacial de cuadras (2026-10-11)

- `CityGridLayout.TryGetBlockAtWorldPosition` resuelve la cuadra por coordenadas X/Z del mundo, limita la consulta al borde de la cuadrícula y rechaza valores no finitos.
- `Tests/CitySmoke` cubre casino central, gasolinera, bordes, exterior e infinitos/NaN: 77 comprobaciones aprobadas.
- Prueba pura de datos; no verifica Unity Play Mode ni cambia geometría.

## Codex (área C) — conversión de centros de cuadra (2026-10-11)

- `CityGridLayout.TryGetBlockCenterWorldPosition` convierte coordenadas de cuadrícula al centro X/Z del mundo y rechaza cuadras fuera de la ciudad.
- `Tests/CitySmoke` comprueba ida y vuelta en las 441 cuadras: `dotnet run --project Tests/CitySmoke` aprobó 960 comprobaciones.
- No modifica la escena ni demuestra la integración en Unity Play Mode. Siguen tres avisos de nulabilidad preexistentes en `CityMap.cs`.

## Codex (área C) — lógica base de ruleta europea (2026-10-11)

- Se añadió `RouletteLogic` para ruleta virtual de un cero: apuestas a pleno, color, paridad y mitades; incluye retorno bruto y neto con pago estándar.
- `Tests/CasinoSmoke` verifica el cero verde, límites, pago de pleno 35:1, pago de color 1:1, errores de rango y muestra: 698 comprobaciones aprobadas.
- Lógica pura con RNG inyectable; no está conectada al HUD ni probada en Unity Play Mode.

## Codex — movimiento y HUD (2026-10-11)

- Control FPS suavizado: aceleración/frenado, correr, salto calibrado por altura, balanceo sutil de cámara y FOV al correr; diagonales no aumentan velocidad.
- HUD transparente: salud y efectivo persistentes, aviso de interacción contextual, teléfono con bolsa, paquetes de contrabando y nivel de atención. Eliminada lista fija de teclas. Salud/carga viven en `PlayerStatus`.
- Panel del Casino FOX usa menos opacidad y ocupa menos espacio.
- `dotnet run --project Tests/PlayerSmoke`: 9 comprobaciones de normalización, aceleración, frenado, salto y balanceo aprobadas.
- Unity 6000.0.84f1: `Run HUD Play Smoke` aprobó `TYCOON_HUD_SMOKE_OK` (HUD, teléfono, salud, carga y atención); el smoke amplio se detuvo por la escala medida de un modelo Casa Marea.
- Unity reporta `SAVE_FAIL Perfil de una versión futura: no se sobrescribe`; guardado no se reemplazó. Revisar compatibilidad del perfil aparte.
