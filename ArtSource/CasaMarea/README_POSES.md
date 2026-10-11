# Casa Marea — guía de poses y expresiones del primer NPC

Guía para animar y dirigir al primer cliente del barrio (el **Pescador**; la Vecina y el Joven comparten esqueleto y clips). Complementa `README_ANIMADOS.md`. Los ángulos están en grados, en ejes del mundo y con el convenio de `rigdata.py`:

- El personaje mira a −Y. «D» = derecha del personaje (x < 0); «I» = izquierda (x > 0).
- Para huesos que cuelgan (brazos, piernas): rx **negativo = hacia delante**, positivo = hacia atrás. Rodillas: solo rx ≥ 0 (se doblan hacia atrás). Codos: solo rx ≤ 0 (se doblan hacia delante).
- Para huesos que suben (columna, cabeza): rx **positivo = inclinarse hacia delante**.
- ry abre o cierra un brazo (positivo = el brazo derecho se separa del cuerpo); rz gira sobre el eje vertical (positivo = la parte derecha avanza).

## 1. Estados del cliente en el ciclo de venta

Qué debe hacer el primer NPC según lo que ocurre en el juego. Los tres primeros clips existen; el resto son poses previstas (todavía sin clip).

| Momento del ciclo | Pose o clip | Estado | Expresión |
| --- | --- | --- | --- |
| Llega al callejón por la calle | **Caminar** (1.4 m/s) | clip hecho | Neutro |
| Espera en la cola | **Idle** | clip hecho | Neutro; **Parpadeo** cada 3–5 s |
| Llega a la ventanilla | Frenar en 1 paso y mirar al frente | por hacer | Neutro |
| Pide | **Pedir** | clip hecho | **Hablar** durante los gestos; **Feliz** al terminar |
| Recibe la hamburguesa | Recibir (ambos brazos adelante) | por hacer | **Feliz** |
| Se va satisfecho | Caminar + saludo con la mano | por hacer | **Feliz** |
| Se impacienta si tarda | Impaciente (peso de un pie a otro, brazos cruzados) | por hacer | **Enojado** leve |
| Se va decepcionado | Caminar con cabeza baja | por hacer | **Triste** |
| Jackpot en el casino cercano | Brazos arriba | por hacer | **Sorpresa** y luego **Feliz** |

## 2. Poses clave de los clips existentes

Valores de `rigdata.py` (todos los demás huesos en 0). `D` y `I` intercambian en los fotogramas simétricos del paso.

| Clip y fotograma | Pose |
| --- | --- |
| Idle, f15 (inhala) | columna 1°, cabeza −1°, brazo_I 3° / brazo_D −3°, antebrazo_I −7° / antebrazo_D −4° |
| Caminar f0 (contacto, pierna D adelante) | muslo_D −24°, pantorrilla_D 5°, pie_D 11°; muslo_I 24°, pantorrilla_I 20°, pie_I −18°; brazo_D 23°, brazo_I −23°; pelvis giro 4°, columna giro −3° |
| Caminar f8 (apoyo, pierna I en vuelo) | muslo_I −17°, pantorrilla_I 54°; pierna D casi recta; pelvis desplazada 2 cm hacia el lado de apoyo |
| Pedir f24 (primer gesto) | brazo_D −74° (con 7° de abertura), antebrazo_D −32°; brazo_I −17°, antebrazo_I −57° (mano al vientre); columna 5°, cabeza 7° |
| Pedir f36 y f60 (gestos) | antebrazo_D −52°, cabeza 10° |

## 3. Poses por hacer (valores de partida)

Aproximadas con el mismo convenio; ajustar en pantalla al animarlas.

| Pose | Valores de partida |
| --- | --- |
| Recibir | brazo_D −55°, brazo_I −55°, antebrazo D/I −40°, columna 5°, cabeza 8° |
| Impaciente | brazos cruzados: brazo D/I −25° con ry 20°, antebrazo D/I −110°; peso a la pierna D: pelvis x −3 cm, muslo_I −6°, pantorrilla_I 12° |
| Saludo al irse | brazo_D −150°, antebrazo_D −40° con balanceo de ±20° a 2 Hz |
| Brazos arriba (jackpot) | brazo D/I −165° con ry ±20°, antebrazo D/I −10°, columna −4°, cabeza −10° |
| Cabeza baja (decepción) | columna 8°, cabeza 20°, brazo D/I 8° |

## 4. Expresiones (blend shapes)

Seis formas que solo mueven ojos, cejas y boca; en Unity son *blend shapes* con peso 0 a 100. Se combinan: la mayoría se usan con un solo peso a la vez y **Parpadeo** se suma a cualquiera.

| Forma | Ojos | Cejas | Boca | Uso |
| --- | --- | --- | --- | --- |
| Feliz | entrecerrados (alto ×0.55) | suben 6 mm | más ancha y comisuras arriba 16 mm | recibir, despedirse, ganar |
| Triste | bajan 4 mm | extremo interior arriba (inclinación 0.5) | comisuras abajo 16 mm | decepción |
| Sorpresa | grandes (×1.3 ancho, ×1.6 alto) | suben 30 mm | abierta (alto ×4), estrecha | jackpot, precio inesperado |
| Enojado | alto ×0.7 | extremo interior abajo (−0.8) y bajan 8 mm | comisuras abajo 10 mm | impaciencia |
| Hablar | — | — | abierta (alto ×3) | acompaña a «Pedir»; oscilar entre 0 y 60 |
| Parpadeo | alto ×0.1 | — | — | cada 3–5 s, 120 ms |

Pautas de uso:

1. **Parpadeo**: peso 0 → 100 → 0 en 120 ms; intervalo aleatorio entre 3 y 5 s. No parpadear durante Sorpresa.
2. **Hablar**: oscilar entre 0 y 60 a 6 Hz mientras dura el diálogo; con Feliz o Triste a 40 de peso para que la boca no pierda el gesto.
3. **Transiciones**: 150 ms entre expresiones; nunca saltar de golpe.
4. Los desplazamientos máximos son de 3 cm; a 3 m se leen como cambios de cara, no de forma.

## 5. Dirección de arte de las poses

- **Silueta primero.** A 3 m, el pescador se reconoce por el sombrero, la vecina por el vestido y el chongo, el joven por la gorra. Cualquier pose nueva debe conservar esa lectura: nada de brazos pegados al cuerpo, que fundirían la silueta del vestido o la camisa.
- **Gestos grandes, pocos fotogramas.** Estilo de personaje muy simple: anticipación (retroceso de 3–5°), gesto claro y retorno. El clip «Pedir» ya sigue este patrón (fotogramas 0, 10, 24, 36, 60, 74, 90).
- **Pies planos.** La pelvis baja lo justo para que la suela siga en z = 0; sin deslizamiento en el sitio (el ciclo de caminar mide 1.42 m/s y 0.76 m de paso, y Unity debe mover el objeto a esa velocidad).
- **Color de apoyo.** Los gestos de pedir se leen mejor con el brazo derecho contra el fondo claro de la fachada; al colocar a la cola, orientar al cliente de modo que su lado derecho dé a la ventanilla.
- **Coherencia con el HUD.** Cuando un cliente espera y se impacienta, la barra de pedido del HUD puede mostrar un icono de cara que replica la expresión (Neutro, Enojado, Triste).
