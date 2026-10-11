# Mapa de requisitos: especificación «Schedule I y Tycoon» frente al juego actual

Fuente: `Especificacion_Completa_Schedule_I_y_Tycoon.md` (archivo del usuario; no está en el repositorio) (v1.0, 10-oct-2026, 1 131 líneas). Se usa como **referencia de producto**. Sus reglas son propuestas de diseño, no hechos de Schedule I (así lo dice su §1.2). Si choca con un pedido directo del usuario, manda el usuario. No se descargó ni copió nada de Schedule I.

Estado: **Hecho** = existe y se probó con marcador; **Parcial** = existe parte; **Falta** = no existe.

## Conflictos con pedidos directos (gana el usuario)
| Spec | Pedido del usuario | Decisión |
| --- | --- | --- |
| §15.1 y §21.2: el casino se abre tras inversión y queda «fuera del ciclo inicial» | Casino siempre abierto desde el inicio, sin desbloqueo, para ganar pesos virtuales rápido | **Casino abierto siempre.** Se documenta como excepción deliberada. |
| §15.2: fichas con tasa de conversión | Apuestas fáciles de cambiar, premios rápidos, sin bloqueos | **Fichas opcionales**: 1 ficha = $25, canje sin comisión en la ventanilla. Se puede jugar solo con pesos; las fichas nunca son requisito. |
| §15.4: la caja debe tener fondos para pagar | Progreso acelerado | La casa paga sin tope de caja; el libro registra el neto. Caja con fondos: pendiente. |
| §15.3: «un solo juego completo» | Varias maquinitas distintas | 6 máquinas de 3 tipos, cada una con su tabla de pagos. |

## Casino (prioridad de esta tanda)
| Requisito (§) | Estado | Evidencia |
| --- | --- | --- |
| Resultado decidido por la autoridad, no por el cliente (§15.3) | Hecho | Lógica pura `SlotMachineLogic`/`CasinoSession`; la animación solo muestra el resultado. 685 comprobaciones dotnet. |
| Tabla de pagos visible (§15.3) | Hecho | Panel muestra tres iguales y par por máquina. |
| Apuesta registrada al aceptar y liquidada una sola vez (§15.5, §19.7) | Hecho | `CasinoSession.Begin/Complete`, libro (`RoundsStarted`, `TotalWagered`, `TotalWon`, `HouseNet`, `FreeSpins`), segunda liquidación falla. Pruebas «libro:». |
| Moneda ficticia, sin dinero real (§15.2) | Hecho | Pesos virtuales; sin apuestas reales (AGENTS.md). |
| Sorteo robusto ante RNG degenerado | Hecho | Sin bucles abiertos; prueba con RNG constante (0/1/37/99) en 3 tablas. |
| Estados de HUD visibles: apuesta, saldo, premio, bloqueos (§16.1, §16.3) | Hecho | Verificado en la app a 1280×720, 1600×900 y pantalla completa. |
| Jackpot y giro de cortesía en la app viva | Hecho | `Casino4_*` (ver TEST_REPORT). |
| Accesibilidad: no solo color, destellos bajos (§16.8) | Hecho | Símbolos por forma, ≤3 Hz, tecla F. Tamaño de texto escalado por pantalla. |
| Audio comunica resultado (§16.7) | Parcial | Fuentes de audio sonando en el giro; no se escuchó. Buses de audio separados: falta. |
| Fichas: compra y canje en una transacción, sin duplicar (§15.2) | Hecho | `CasinoSession.BuyChips/CashOutChips`; el valor total (pesos + fichas) se conserva; pruebas de conservación con 700 operaciones aleatorias. |
| Personal de caja con interacción visible (§15.4) | Hecho (mínimo) | Cajera animada tras el mostrador: E cambia un lote de 20 fichas ($500), Q canjea todas; gesto al atender, aviso en pantalla. Sin turnos ni salario. |
| Caja con fondos y tope de premio, horarios, aforo (§15.1, §15.4) | Falta | La casa paga sin tope; el libro registra el neto. |
| Otros juegos / mesas (§15.6) | Falta | Fuera de alcance de esta tanda. |

## Economía de avance rápido y ciclo de hamburguesas
| Requisito (§) | Estado | Evidencia |
| --- | --- | --- |
| Primera venta: comprar, cocinar, vender (§19.11, §20.1) | Hecho | `TYCOON_SMOKE_OK`, saldo $135. |
| Compras/cobros únicos, sin duplicar por clics repetidos (§23) | Parcial | `Wallet.TrySpend/Earn` y `CasinoSession` no duplican; sin identificadores de solicitud. |
| Libro de movimientos (§14.2) | Parcial | Solo el libro del casino; falta el de hamburguesas. |
| Guardado y carga (§3.3, §17.8) | **Hecho** | Perfil versionado con checksum, copia de seguridad y recuperación; saldo, hamburguesas, libro, fichas y ronda pendiente sobreviven a cerrar/reabrir y a R. 459 comprobaciones dotnet, smoke de Play Mode y 5 lanzamientos reales. |
| Migraciones de guardado (§17.10) | Parcial | Mecanismo de migración por versión probado con una migración de prueba; solo existe la versión 1. |
| Guardado en producción: ronda aceptada durante el cierre (§20.7) | Hecho | La ronda pendiente se liquida una sola vez al volver (Play Mode y copia de seguridad real). |
| Inventario, lotes, recetas como datos (§7, §9) | Falta | El ciclo actual es una cadena fija de 3 estaciones. |
| Reloj único, día/noche, horarios (§6) | Falta | |

## Clientes, NPC y empleados
| Requisito (§) | Estado | Evidencia |
| --- | --- | --- |
| Los NPC circulan y se ven (§19.8, §5.5) | Hecho | 3 clientes animados: 2 en la fila ante el despacho, 1 paseante. `CLIENTE_PASEANTE caminando=True`. |
| Animaciones representan estados: caminar, esperar, pedir (§16.6) | Hecho | Animator con Idle/Caminar/Pedir. |
| Clientes con pedidos reales ligados a la venta (§10) | Falta | Los clientes animados son de ambiente; no inician ni cobran ventas. |
| Empleados, tareas, turnos (§11) | Falta | |

## Experiencia visual original (§16.4, §16.5)
| Requisito | Estado | Evidencia |
| --- | --- | --- |
| Escena de referencia pequeña con escala y materiales coherentes | Hecho | Barrio Casa Marea + casino, paleta única de 26 colores. |
| Iluminación que permite leer estaciones y rutas | Parcial | Casino legible; falta ciclo día/noche. |
| Señalética y rutas | Hecho | Carteles «CASINO FOX», flechas en el suelo, rótulos por máquina. |
| Modelos externos con licencia clara y manifiesto | Hecho | Kenney CC0, 22 modelos, `Docs/ASSETS_EXTERNOS.md` y `MANIFEST.json`. Esta tanda no añadió modelos nuevos. |

## Fuera de alcance (no se tocó)
Cooperativo/Netcode/Relay (§17), propiedades y construcción (§12), vehículos (§13), teléfono y mapa (§16.2). La lógica económica sigue separada de Unity para validarla en el anfitrión (AGENTS.md).

## Siguiente tanda sugerida (por impacto en lo que pidió el usuario)
1. Caja del casino con fondos y tope de premio por ronda (sin frenar el avance rápido: el tope solo cambia el reparto).
2. Libro de movimientos común (hamburguesas + casino) y reporte (§14.2, §14.8).
3. Clientes animados que pidan una hamburguesa real (ligar `AnimatedCustomer` con `BurgerLoop`).
4. Libro de movimientos para hamburguesas y reporte distinguiendo ventas, dinero y resultado (§14.8).
5. Buses de audio (música, efectos, ambiente) y verificación auditiva.
