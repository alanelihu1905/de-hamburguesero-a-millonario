# Decisiones

## 2026-10-09

- Repositorio inicialmente vacío: se prepara un proyecto Unity desde cero en la raíz del repositorio.
- Unity 6.0 LTS con URP 17 como objetivo inicial. La versión exacta se ajustará al editor instalado y al resultado de resolución de paquetes.
- Escena generada por herramienta de editor para evitar mantener YAML de escena escrito a mano. El generador no sobrescribe una escena existente.
- Primer bucle de venta deliberadamente pequeño para comprobar movimiento, interacción, transacción y reinicio antes de añadir NPC y ciudad.
- Sistema de entrada clásico de Unity para este prototipo; se evaluará migración al Input System al integrar controles y multijugador.
- Claude Code está disponible por CLI para encargos de solo texto; se conservarán sus entregas en Git. Para Antigravity no se confirmó un canal automatizado, así que su encargo de QA queda como handoff.
- Claude Code sí respondió mediante CLI de solo texto. Se incorpora su paleta Casa Marea al prototipo y su revisión geográfica del Golfo. Las propuestas de geometría y HUD quedan para el siguiente hito.
- A petición del usuario, las futuras comunicaciones con Claude se hacen en la app de Claude, no por CLI. La app revisó el proyecto y creó `Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md` sin editar otros archivos.
- Se adopta para el primer hito la casa pequeña con fachada colonial propuesta por Claude. Patio y aljibe quedan para expansión. Se integran ahora colores, iluminación básica, posición de estaciones y toma inicial; las siluetas, UI final y texturas quedan para fases siguientes.
- Unity 6.0.84f1 abrió primero en Safe Mode por un error `GenerateHLSL` del paquete `com.unity.render-pipelines.universal-config` incluido con el editor. Se incluye copia local del paquete con la anotación de generación retirada; el archivo HLSL generado sigue incluido. Unity volvió a compilar y Play Mode pasó una prueba de venta.
- Claude.app modela las tres estaciones en Blender en rutas aisladas `ArtSource/CasaMarea/` y `Assets/_Game/Models/CasaMarea/`; Codex conserva integración y pruebas de Unity.
- Los FBX de Blender se instancian bajo un grupo sin escala en la escena para preservar dimensiones; se orientan de Z arriba a Y arriba, se asignan materiales URP y se ajustan los colliders de las estaciones. La integración es reproducible desde `CasaMareaModelIntegrator`.
- Antigravity.app realiza auditoría independiente y conserva su informe en `Docs/Handoffs/Antigravity/02_QA_HITO1.md`. Los defectos confirmados se corrigen antes de ampliar mecánicas.
- La fachada se integra como grupo de arte separado; se ocultan solo cubos provisionales que ocupaban el frente. La puerta queda sin geometría para conservar el recorrido a la cocina.
- 2026-10-10, requisito prioritario del usuario: el Casino FOX está siempre abierto desde el inicio, sin desbloqueo ni requisitos, sigue presente tras reiniciar con R y permite conseguir pesos virtuales mucho más rápido que vendiendo hamburguesas. Por indicación directa del usuario, Claude.app implementa mecánica, escena, arte, HUD, pruebas y build de esta función (excepción puntual al reparto de AGENTS.md); Codex conserva el resto.
- Mecánica del casino: tragamonedas de tres rodillos; E gira, Q cambia la apuesta ($25, $100, $500, $2000). Tabla fija de 100 resultados: 38 sin premio, 30 par x2, 18 tres chiles x3, 9 tres mangos x5, 4 tres hamburguesas x12, 1 jackpot FOX x40. Retorno esperado x2.47 por diseño (es un salto de progreso, no una apuesta equilibrada; se reajustará al diseñar la economía real). Si el saldo es menor de $25, el giro es de cortesía (la casa invita $25), así nunca se bloquea el juego.
- Solo pesos virtuales: no hay dinero real ni compras. La lógica es pura y sin Unity (`SlotMachineLogic`, `CasinoSession`), con generador de azar inyectable, para que el anfitrión valide cada ronda antes de añadir Netcode/Relay. El premio se acredita al revelar el giro; la apuesta se cobra al iniciarlo.
- El casino ocupa el solar de la casa vecina derecha, sobre una plaza nueva que prolonga la calle hacia el este (centro x=14.5, z=1), visible y alcanzable caminando desde el inicio. La casa vecina B y su cubo provisional se retiran; `CasinoFoxBuilder` es idempotente y debe ejecutarse después de la integración de arte de Casa Marea. La tecla Q usa una interfaz opcional `ISecondaryInteractable`; el ciclo comprar-cocinar-vender no cambia.

## 2026-10-10 — Casino FOX v2 (6 máquinas, 3 tipos, modelos CC0)
- Seis tragamonedas desde el inicio, de tres tipos con tablas propias (Clásica x2.47, Mango Rápido x2.19, Gran Jackpot x2.29); lógica pura en C# validable por anfitrión. El casino sigue siempre abierto y sobrevive a la recarga.
- Modelos externos solo bajo CC0 (Kenney Furniture Kit 2.0 y Food Kit 2.0); registro en `Docs/ASSETS_EXTERNOS.md` y `Assets/_Game/Models/External/MANIFEST.json`. No se usó nada de Schedule I; solo se tomó como referencia de tipo de experiencia.
- Los smokes de Play Mode no se lanzan con `-quit` (falso verde).

## 2026-10-10 — Especificación «Schedule I y Tycoon» como referencia
- Se usa como referencia de producto, no como autoridad. Mapa en `Docs/REQUISITOS_ESPECIFICACION.md`.
- **Excepción deliberada:** el casino abre desde el inicio y siempre (pedido directo del usuario), aunque §15.1/§21.2 lo sitúan tras una inversión y fuera del ciclo inicial.
- Se apuesta en pesos virtuales directos; fichas con tasa, caja con fondos y personal quedan pendientes.
- El libro del casino (rondas, apostado, premios, resultado de la casa, cortesías) vive en `CasinoSession`, sin Unity, para validarlo en el anfitrión.
- El sorteo de `SlotMachineLogic` no usa bucles abiertos: un RNG degenerado nunca puede colgar el juego.
- Los clientes animados se integran como ambiente (`AnimatedCustomer`, Animator con Idle/Caminar/Pedir). No inician ventas: ligarlos a `BurgerLoop` es la siguiente decisión.

## 2026-10-10 — Guardado y fichas
- Perfil en texto versionado con checksum, escritura atómica, copia de seguridad y recuperación; una versión futura no se sobrescribe. El estado vivo se guarda a la vez (una vez por fotograma) para que dinero, fichas y ronda pendiente no se separen. Detalle en `Docs/TEST_REPORT.md`.
- R recarga la escena pero **no** reinicia el progreso (ya no regala $100).
- Fichas: 1 ficha = $25, canje sin comisión, opcional y sin bloqueo (el casino sigue abierto sin ellas). La apuesta se paga con fichas si alcanzan y, si no, con pesos; el premio vuelve en la misma moneda. Giro de cortesía solo sin pesos ni fichas.
- Cajera: interacción mínima y visible (E cambia un lote de 20 fichas, Q canjea todo). Sin turnos ni salario hasta que exista un libro de movimientos común.
- Los smokes de Play Mode usan un perfil temporal propio y nunca el real.

## 2026-10-10 — Libro común de movimientos
- El perfil sube a `version=2`. Guarda los últimos 32 movimientos con ID, tipo, monto en pesos, origen, destino e ID de ronda. Al recortar, conserva saldos anteriores como punto inicial para seguir auditando pesos y fichas.
- Comprar o canjear fichas mueve valor entre monedero y fichas sin crear ni destruir dinero. Ingredientes, ventas, apuestas y premios cruzan el límite del jugador. Un giro de cortesía registra monto cero.
- Un perfil v1 migra sus saldos de pesos y fichas al punto inicial. El historial anterior no se inventa. Una ronda pendiente se liquida una sola vez y su premio entra al libro.
