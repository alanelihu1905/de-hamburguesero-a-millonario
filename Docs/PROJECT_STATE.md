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

## Preparación para GitHub (2026-10-10, histórico)
Se prepararon `README.md`, `CONTRIBUTING.md`, `Docs/COLABORACION.md`, plantillas, flujo `dotnet`, 15 issues y reglas de Git LFS. El estado publicado figura abajo. Siguen pendientes los usuarios de los dos colaboradores y la licencia.

## Colaboración pública y libro común (2026-10-10)
Repositorio público: `https://github.com/alanelihu1905/de-hamburguesero-a-millonario`. GitHub CI pasó; 15 issues abiertas. Los usuarios de los dos colaboradores aún faltan. La automatización que asignaba tareas a Claude quedó pausada por instrucción del usuario; Codex implementa y dirige ahora.

Rama `codex/libro-movimientos`: libro común de 32 movimientos con migración v1→v2, saldo auditable y premio de ronda pendiente registrado una sola vez. `SaveSmoke` 474, `CasinoSmoke` 685 y `EconomySmoke` 9 comprobaciones. Play Mode: `TYCOON_CASINO_SMOKE_OK` y `TYCOON_SMOKE_OK`. Pendiente: publicar PR y fusionar tras revisión; build macOS actual aún no regenerada.


## Cierre de las tareas iniciales (2026-10-11, rama local `integracion`; nada publicado)

Sobre `main` se integraron las tres ramas abiertas (libro común #16, audio por buses #17 y build Windows) y se implementaron las demás tareas de `tools/github/issues.tsv`:

- **A2** caja del casino con fondo, tope por ronda y premios por cobrar; **A3** precios en `StreamingAssets/economy.json`; **A4** candado de perfil contra dos copias.
- **B2** ajustes de accesibilidad (Esc); **B3** cajera con turnos y fichas sobre el mostrador; **B4** HUD en otras proporciones; **B5** celebración del jackpot.
- **C2** ciclo de día y noche con reloj guardado; **C3** medición de rendimiento `-perf-walk`; **C4** variedad de clientes con la paleta; **C5** clientes que piden y pagan (diseño en `Docs/DISENO_CLIENTES_PIDEN.md`); **C1** primera build de Windows y lista de QA ejecutada.
- Formato de guardado v5 (v3 caja, v4 hora, v5 ajustes), cada versión con su migración y su prueba.

Todo se probó con `dotnet` y con un smoke de Play Mode con marcador por función; lo medido en la build de Windows y lo que **no** se pudo verificar (sobre todo el oído y la versión exacta del editor) está en `Docs/TEST_REPORT.md`. La escena se regeneró con Unity 6000.3.25f1, no con 6000.0.84f1: ver `Docs/DECISIONS.md`.
