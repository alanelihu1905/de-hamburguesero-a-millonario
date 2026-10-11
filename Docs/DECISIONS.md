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


## 2026-10-11 — Cierre de las tareas iniciales (A, B y C)

Decisiones de diseño tomadas al implementar las 15 tareas de `tools/github/issues.tsv`. Todas siguen las reglas de `AGENTS.md`: lógica económica en C# puro, sin dinero real, nada se da por verificado sin su marcador.

- **Caja del casino (A2).** Fondo inicial $20 000, tope de $5 000 por ronda y reserva mínima de $1 000 que la casa repone (y anota en el libro como `Refill`). Un premio que no cabe se paga hasta el tope y el resto queda «por cobrar»: se entrega en cuotas, una por ronda, aunque la ronda no tenga premio. Nunca se pierde ni bloquea el juego y las tablas de premios no cambian (retornos x2.47 / x2.19 / x2.29). Con la reserva, cada ronda paga al menos $1 000 de lo debido, así que el avance rápido sigue. El HUD muestra la caja y lo que queda por cobrar.
- **Precios como datos (A3).** `StreamingAssets/economy.json`, JSON plano y versionado, con costo de ingredientes, precio de venta y lote de fichas. Un archivo ausente, roto, de versión futura o fuera de rango deja los valores de siempre y avisa en el log. El valor de la ficha ($25) **sigue fijo en código**: el libro y el guardado dependen de que cada movimiento de fichas sea múltiplo de 25.
- **Perfil con dos copias (A4).** Candado `profile.lock` con acceso exclusivo (lo suelta el sistema si la app se cae). La segunda copia lee lo último guardado, no toca el disco, avisa en pantalla y juega sin guardar. Elegido sobre «usar otro perfil» para no dividir el progreso.
- **Formato de guardado v3 a v5.** v3 (caja), v4 (hora del juego), v5 (ajustes). Cada versión trae su migración desde la anterior y su prueba; una clave ausente toma el valor de siempre.
- **Reloj del juego (C2).** Un día dura 8 minutos reales y empieza a las 10:00. Se guarda en el perfil. `DayNightCycle` mueve sol y luna, ambiente, niebla y cielo a partir de lo que trae la escena; la noche no baja de ~35 % del ambiente del día y las luces de los interiores no se tocan, para que el casino y la cocina sigan legibles. No cambia la escena.
- **Ajustes de accesibilidad (B2).** Tamaño del texto de 70 a 220 % y las teclas de interactuar, acción secundaria y luces fijas (E, Q, F por defecto), reasignables. WASD, R y Esc quedan reservadas; una tecla repetida se rechaza. Esc abre el menú.
- **HUD en otras proporciones (B4).** `HudLayout` (C# puro) reparte panel, etiqueta y aviso; la escala pedida se recorta para que todo quepa. En pantallas casi cuadradas el aviso baja bajo la etiqueta. Es la única fuente de las posiciones del HUD del casino.
- **Cajera con turnos (B3).** Trabaja de 08:00 a 22:00 del reloj del juego. Fuera de turno no cambia pesos por fichas, pero **el casino sigue abierto**: se juega con pesos y las fichas que ya se tienen se apuestan y se canjean en la ventanilla automática. El horario nunca bloquea ni deja valor atrapado.
- **Celebración del jackpot (B5).** Lluvia de monedas desde las seis máquinas, bombillas festejando, sonido propio y marco dorado en el panel durante 5 s. Ninguna luz parpadea más de 2.5 veces por segundo (tope 3) y con «luces fijas» ninguna parpadea.
- **Clientes que piden (C5).** El cliente es la cara de la demanda, no una condición: el cobro lo hace solo `BurgerLoop.Sell`, una vez, al mismo precio, así que no hay cobro doble y el guardado no cambia. Sin propinas ni precios por cliente (sería cambiar la economía). Diseño en `Docs/DISENO_CLIENTES_PIDEN.md`.
- **Variedad de clientes (C4).** Las variantes de ropa son la textura de paleta con celdas de ropa cambiadas por otros colores de la misma paleta; no hay colores ni mallas nuevas. La cajera y dos paseantes nuevos las usan.
- **Medición de rendimiento (C3).** `-perf-walk` recorre la ruta y escribe un informe con equipo y escena. Las cifras son de una máquina y no prometen nada para otras.
- **Escena regenerada con otra versión del editor.** El editor disponible al integrar fue Unity 6000.3.25f1 (el proyecto pide 6000.0.84f1). La escena y los activos generados se regeneraron con esa versión en un commit aparte para poder repetirlo: el área C debe volver a ejecutar `CasinoFoxIntegration.IntegrateAllWithCasino` con 6000.0.84f1 antes de fusionar.
