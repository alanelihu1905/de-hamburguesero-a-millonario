# Diseño: los clientes de la fila piden una hamburguesa real (tarea C5)

La tarea C5 es grande, así que se divide en tres porciones que se pueden fusionar por separado. Este documento es el diseño corto que pide la propia tarea; las dos áreas que toca (A: `BurgerLoop`, C: clientes y escena) lo acuerdan aquí.

## Regla que no se rompe

**El cobro lo hace una sola vez, y solo `BurgerLoop.Sell()`.** Los clientes no mueven dinero: no suman ni restan pesos y no escriben en el libro común. Un cliente «paga» cuando el jugador completa la venta en el mostrador; el dinero y el libro ya los registra `BurgerLoop` como siempre. Por eso:

- no se puede cobrar dos veces (un clic repetido o recargar no duplica nada, porque `Sell()` solo cobra con una hamburguesa lista);
- el progreso guardado no cambia: ventas, saldo y paso de la hamburguesa viajan en el perfil igual que antes, sin versión nueva;
- una venta sin cliente esperando se cobra igual (venta de paso, mismo precio): el ciclo de hamburguesas y sus pruebas (`TYCOON_SMOKE_OK`, saldo $135) no cambian.

## Las tres porciones

1. **Lógica pura (hecha).** `CustomerOrderFlow` (C# puro, sin Unity): estados `Idle → Ordering → Leaving → Away → Returning → Idle`, paciencia de 45 s, tiempos de ausencia, contadores de pedidos servidos y abandonados. Se prueba con dotnet (`Tests/EconomySmoke`) con un reloj simulado. No sabe nada de dinero.
2. **Cliente en la escena (hecha).** `AnimatedCustomer` (solo los de la fila) usa el flujo: levanta la mano con el clip *Pedir* y un cartel «HAMBURGUESA $N» sobre su cabeza, espera, y al pagar camina hacia la calle, desaparece y vuelve a su lugar con un pedido nuevo. Si se agota la paciencia se va sin pagar y no pasa nada. Los clientes de ambiente y la cajera no cambian.
3. **Conexión con la venta (hecha).** `CustomerOrders` escucha la venta de `BurgerLoop` (el movimiento `Sale` del libro) y le da la venta al cliente que lleva más tiempo esperando. Se crea sola desde el perfil: no cambia la escena.

## Qué no incluye (a propósito)

- Propinas o precios distintos por cliente: cambiaría el dinero de la prueba de $135 y es una decisión de economía (área A). Queda como idea para una tarea nueva.
- Guardar los pedidos en curso: son efímeros. Al recargar la escena los clientes vuelven a su sitio y piden de nuevo; no se pierde ninguna venta porque nada de dinero depende de ellos.
- Que la venta *requiera* un cliente: cambiaría el juego. Hoy el cliente es la cara visible de la demanda, no una condición.

## Pruebas

- `dotnet run --project Tests/EconomySmoke`: el flujo de pedidos (paciencia, ausencia, regreso, no pagar fuera de un pedido, no pagar dos veces).
- `Tycoon3D.Editor.CustomerOrdersPlaySmoke.Run` (Play Mode, sin `-quit`): marcador `TYCOON_ORDERS_OK`. Con un pedido en curso, una venta paga exactamente $50 una vez, el cliente se va caminando y el ledger cuadra; sin pedido la venta paga igual; `TYCOON_SMOKE_OK` y `TYCOON_CASINO_SMOKE_OK` siguen dando los mismos saldos.
