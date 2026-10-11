# 06 — Casino FOX: mecánica, arte y HUD

**Autor:** Claude.app. **Fecha:** 2026-10-10. **Origen:** requisito prioritario del usuario: casino siempre disponible desde el inicio, sin desbloqueo, que siga tras reiniciar con R y que dé pesos virtuales mucho más rápido. Solo pesos virtuales; sin dinero real ni compras.

## Mecánica

Una tragamonedas de tres rodillos, una sola acción por giro.

| Control | Efecto |
| --- | --- |
| **E** frente a una máquina | gira; cobra la apuesta al empezar y paga el premio al revelar (≈ 1.4 s después) |
| **Q** frente a una máquina | cambia la apuesta: $25 → $100 → $500 → $2000 → $25 … |

- **Sin desbloqueo.** No hay requisitos de nivel, saldo ni progreso: el casino existe desde el primer fotograma y la primera apuesta es de $25.
- **Giro de cortesía.** Con menos de $25 de saldo, el giro lo paga la casa ($25); si gana, el premio se acredita. Así el jugador nunca se queda sin poder jugar.
- **Apuesta ajustada.** Si el saldo no alcanza la apuesta elegida, baja sola a la mayor apuesta que alcanza y el HUD lo avisa.
- **Un giro a la vez** para todo el casino.

### Tabla de premios (100 resultados)

| Resultado | Resultados de 100 | Multiplicador |
| --- | --- | --- |
| Sin premio | 38 | x0 |
| Par | 30 | x2 |
| Tres chiles | 18 | x3 |
| Tres mangos | 9 | x5 |
| Tres hamburguesas | 4 | x12 |
| **Jackpot FOX** (tres zorros) | 1 | x40 |

Retorno esperado: **x2.47** por cada peso apostado; el 62 % de los giros gana. Es un salto de progreso a propósito, no una apuesta equilibrada. Medido con 400 000 giros simulados: x2.475 y 62.0 % de aciertos. Para llegar a $10,000 desde $100 hacen falta unas 849 acciones vendiendo hamburguesas y una mediana de unas 33 con el casino (unas 26 veces menos).

Se reajustará al diseñar la economía real. Se afina cambiando solo `Weights` y `Multipliers` en `SlotMachineLogic`.

## Dónde está

Plaza nueva que prolonga la calle hacia el este; edificio de 8 × 6.5 m centrado en x = 14.5, z = 1, con la entrada de 3.2 m hacia la calle. Sustituye al solar de la casa vecina B. Desde el punto de aparición se ve el letrero vertical y el rótulo, y se llega caminando unos 15 m. Cuatro tragamonedas en el muro del fondo.

## Arte

Identidad propia, distinta de Casa Marea pero con su paleta: **ciruela** (`#3B2F4A`) de base, **mango** (`#F4C95D`) en el neón y los focos, **chile** (`#E4572E`) en el zorro y las máquinas, crema y turquesa como apoyo.

- **Fachada:** muros de ciruela, rótulo con «CASINO FOX» y emblema de zorro hecho con triángulos (cabeza invertida, dos orejas con interior ciruela, hocico crema, ojos y nariz ciruela), neón mango arriba y chile abajo, once focos de marquesina y jambas luminosas.
- **Letrero vertical:** poste de acero con tablero «FOX · CASINO · abierto» a ambos lados, visible al caminar por la calle.
- **Interior:** piso ciruela claro, alfombra chile con filetes de neón, cuatro luces cálidas y cartel «TRAGAMONEDAS FOX».
- **Máquina:** base ciruela, cuerpo chile, pantalla oscura con tres rodillos emisivos, cabecera con marquesina que se enciende al girar y parpadea al ganar, bandeja de acero, palanca con bola mango y banco.
- **Símbolos** (color en el HUD y en los rodillos): chile `#E4572E`, mango `#F4C95D`, concha `#2BA6A0`, hamburguesa `#D99A4E`, FOX crema `#F7EEDD`.

Todo es geometría primitiva de Unity (más los triángulos extruidos del zorro) con materiales URP/Lit en `Assets/_Game/Materials/Casino/`. Los textos son `TextMesh` con un shader propio, `Tycoon3D/FoxText` (`Assets/_Game/Shaders/FoxText.shader`), que respeta la profundidad: el shader por defecto de `TextMesh` dibujaba el texto por encima de las paredes y los rótulos del interior se veían desde la calle. No hay modelos de Blender ni texturas; el emblema y los rótulos son provisionales y se pueden sustituir por modelos propios más adelante.

## HUD

- **Etiqueta siempre visible** arriba a la derecha: «CASINO FOX · abierto siempre», distancia y dirección («N m al este >>») y «pesos virtuales».
- **Panel central**, cerca de una máquina (menos de 4.5 m) o durante los 4 s tras girar: tres rodillos con color y nombre del símbolo, apuesta y saldo, mensaje del resultado y tabla de premios.
- La ayuda inferior del HUD principal añade «Q apuesta (casino)».

## Pendiente de arte

- Modelos de Blender para las máquinas y la fachada (con paleta única).
- Sonidos de giro y premio.
- Variaciones de símbolos como textura en los rodillos.
