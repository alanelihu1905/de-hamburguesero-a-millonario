# Propuesta: modo de tráfico ficticio (nombre provisional)

**Estado:** idea abierta, nombre provisional; nada implementado ni probado. Se mantiene Unity 6, C#, URP, el barrio pequeño y el ciclo de hamburguesas. El Casino FOX sigue abierto desde el primer minuto, sin desbloqueo.

## Fantasía y límites

El jugador distribuye **cajas de utilería luminosa** para comerciantes inventados. Los objetos, marcas, diálogos y personajes serán originales. El conflicto es de ficción arcade, sin reproducir sustancias, métodos de tráfico reales ni recursos de otro juego. La cámara, la lectura visual y el humor siguen el barrio Casa Marea.

## Ciclo jugable de diez minutos

| Minuto | Decisión del jugador | Respuesta del juego |
| --- | --- | --- |
| 0–2 | Acepta uno de dos encargos en el tablón de la plaza; recoge una caja. | Aparecen destino, tiempo y pago en una tarjeta clara. |
| 2–4 | Elige ruta corta visible o ruta larga tranquila. | La ruta corta aumenta alerta; la larga consume tiempo. |
| 4–6 | Entrega al contacto correcto con una interacción. | Cobra créditos clandestinos y desbloquea un segundo encargo. |
| 6–8 | Atiende la alerta: espera, cambia de ruta o acepta una persecución arcade. | Una patrulla ficticia se acerca si la alerta supera el umbral. Sin arresto instantáneo. |
| 8–10 | Completa entrega final o vuelve al punto seguro. | Resumen de entregas, tiempo y alerta; la próxima partida ofrece otro destino. |

El jugador puede abandonar el modo en cualquier momento y seguir vendiendo hamburguesas o jugando en el casino. Una entrega fallida solo pierde esa recompensa y reinicia el encargo; conserva progreso de los otros sistemas.

## Progresión y economía

- Tres rangos: **Mensajero**, **Operador**, **Socio**. Suben por entregas completadas, no por gasto.
- Los encargos pagan **créditos clandestinos**. Estos créditos compran capacidad de carga, información de ruta y aspectos cosméticos del modo. No se cambian por pesos ni fichas.
- El casino usa sus pesos y fichas actuales; permanece disponible y no financia mejoras clandestinas. Así el casino conserva su vía rápida de dinero sin romper esta progresión.
- Cada entrega registra identidad de misión, pago y estado final. El saldo nunca baja por una entrega fallida.

## Alerta, patrulla y combate opcional

- Alerta de 0 a 100. Crece por acciones visibles del encargo; baja con tiempo fuera de misión. La interfaz indica causa y nivel. Los umbrales disparan **observación** (30), **seguimiento** (60) y **persecución** (85).
- En persecución, el jugador puede llegar a una zona segura o dejar expirar el encargo. No hay muerte, pérdida del perfil ni bloqueo del casino.
- El combate es opcional. La misión siempre tiene una solución sin disparar. Armas, munición y enemigos armados quedan fuera del primer hito, pero forman parte de una fase posterior.
- La IA usa estados legibles: patrulla, sospecha, seguimiento y regreso. No recibe información invisible de la posición del jugador.

### Fase posterior: armas y persecuciones PvE

Añadir armas ficticias de varias clases, incluido disparo a distancia, con munición limitada, recarga, daño y señales visuales claras. El jugador podrá comprar o encontrar equipo dentro del modo; la progresión no depende de combatir. La patrulla PvE reaccionará con cobertura simple, persecución y retirada según alerta, distancia y daño recibido. Al agotar salud, el jugador reaparece en un punto seguro con el encargo actual fallido; conserva perfil, créditos ya ganados, negocio y acceso al casino. Probar aciertos, consumo de munición, daño, IA y respawn con resultados deterministas antes de ampliar el barrio. Sin PvP ni daño a civiles.

## Primer hito implementable

**Una entrega completa en el barrio actual, sin ampliar la ciudad.** Crear datos configurables para un encargo, una caja visible, un contacto, un destino, temporizador, recompensa en créditos y barra de alerta. Implementar estados puros `Disponible`, `Aceptado`, `Entregado`, `Fallido`; guardar solo al completar la arquitectura acordada con el área A. Sin patrulla física ni combate en este hito: al llegar a 85 de alerta, mostrar aviso y aplicar fallo tras una cuenta atrás recuperable.

**Pruebas propuestas, aún no ejecutadas:**

1. C# puro: aceptar una vez; impedir doble cobro; entrega al contacto equivocado no paga; tiempo agotado falla; alerta se limita a 0–100; créditos no alteran pesos ni fichas.
2. Unity Play Mode: caja y contacto accesibles por raycast; una entrega paga una vez; el casino está abierto antes y después; el ciclo de hamburguesas aún completa una venta. Registrar un marcador nuevo solo cuando la prueba exista y pase.
3. QA manual: una partida de diez minutos, ruta corta y larga, aviso de alerta legible, salida voluntaria del modo y retorno al casino. Anotar FPS y resolución usada.

**Dependencias de integración:** área A define formato de guardado y libro de movimientos; área C genera y sube `BarrioResidencial.unity` después del código. Este documento no cambia reglas existentes.
