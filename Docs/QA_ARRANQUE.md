# QA de arranque: Windows

Objetivo: repetir diez comprobaciones en una build nueva. Primera corrida: 2026-10-11, en un equipo con Windows 11 (RTX 5070 Laptop), con **Unity 6000.3.25f1** (no la 6000.0.84f1 oficial). La tabla distingue lo que se **verificó en la build de Windows** de lo que solo cubre un smoke de Play Mode en el editor y de lo que sigue pendiente de una persona con teclado y ratón.

## Preparación

1. En Unity Hub, instala Unity 6000.0.84f1 y **Windows Build Support** para esa misma versión. Abre el proyecto con Git LFS descargado.
2. En Unity, ejecuta `Tycoon3D.Editor.PrototypeWinBuild.Build` con `-executeMethod` y **`-quit`**, o llama ese método desde el editor. El resultado queda en `Builds/Windows/De Hamburguesero a Millonario.exe` junto con su carpeta `*_Data` y `UnityPlayer.dll`; conserva toda la carpeta. Busca `TYCOON_WIN_BUILD_OK` en el log. Sin el módulo, el script da un error explícito.
3. En Windows, crea una carpeta temporal nueva para el perfil. Desde PowerShell, inicia la aplicación así:

   ```powershell
   & '.\Builds\Windows\De Hamburguesero a Millonario.exe' -casino-live -casino-roll 99 -save-dir "$env:TEMP\TycoonQA-C1"
   ```

   Borra esa carpeta temporal **solo antes** de empezar la corrida; para comprobar persistencia conserva su contenido entre lanzamientos. `-casino-live` coloca al jugador frente a la primera máquina. `-casino-roll 99` fuerza el jackpot para repetirlo; `-casino-roll 0` fuerza una pérdida. El juego sigue siendo interactivo. `Player.log` queda bajo `%USERPROFILE%\AppData\LocalLow\Tycoon3D\De Hamburguesero a Millonario\`. Si la ventana pierde el foco, la app se pausa: mantenla al frente.

## Diez comprobaciones

Estados: **Build** = verificado ejecutando la build de Windows; **Smoke** = cubierto por una prueba de Play Mode en el editor (con marcador), no por la build; **Pendiente** = falta una persona con teclado y ratón.

| # | Acción y resultado esperado | Resultado actual |
| --- | --- | --- |
| 1 | Iniciar perfil vacío. Ver barrio, casino abierto y HUD sin desbloqueo ni compra previa. Log: `CASINO_LIVE_READY` y `SAVE_LOAD` con $100 iniciales. | **Build (parcial):** `SAVE_LOAD Fresh` y casino abierto con HUD en las capturas `Cierre_*` de la build. **Pendiente:** `CASINO_LIVE_READY` con `-casino-live` y teclado. |
| 2 | Entrar al casino desde la calle. Contar seis máquinas, tres tipos y puerta/pasillo transitables; ninguna máquina atraviesa al jugador. | **Smoke** (`TYCOON_CASINO_SMOKE_OK`: seis máquinas, tres tipos, ruta libre para la cápsula). **Build:** capturas con las seis máquinas. **Pendiente:** caminar la ruta con teclado. |
| 3 | Frente a máquina Clásica, ver prompt de `E`; pulsar `E` una vez. Deben girar los rodillos, terminar el giro y aparecer un premio. Log: `CASINO_LIVE_AIM ... interactuable=True`. | **Smoke.** **Build:** giro y premio en las capturas del recorrido automático (`-casino-shots`). **Pendiente:** pulsación real de `E`. |
| 4 | Mientras gira, pulsar `E` otra vez. No debe cobrar segunda apuesta simultánea. Al acabar, pulsar `Q` y comprobar que cambia el importe de apuesta mostrado. | **Smoke** (bloqueo de segundo giro y cambio de apuesta). **Pendiente** en la build. |
| 5 | Con `-casino-roll 99`, hacer un giro de $25 con saldo inicial $100. Ver jackpot de $1000 y saldo final $1075, o el equivalente en fichas si se canjearon antes. | **Smoke** ($1075). **Build:** jackpot con premio y caja en capturas (con apuestas mayores). **Pendiente** con `-casino-roll 99` y teclado. |
| 6 | Salir, abrir de nuevo con la **misma** ruta `-save-dir` y sin borrar archivos. Ver $1075 y una ronda en el libro; pulsar `R`. Saldo y libro deben seguir iguales. | **Smoke** (guardar, recargar la escena y reabrir). **Build:** reabrir con el mismo perfil da `SAVE_LOAD Loaded` y la segunda copia no pisa el perfil (prueba de dos lanzamientos). **Pendiente:** `R` real. |
| 7 | Usar perfil temporal **nuevo** con `-casino-live -casino-roll 0 -save-dir <ruta-nueva>`. Apostar $25 cuatro veces; saldo $0. Pulsar `E` una quinta vez: giro de cortesía, sin saldo negativo. | **Smoke** (giro de cortesía con saldo $0). **Pendiente** en la build. |
| 8 | Acercarse a la ventanilla de la cajera. Cambiar pesos por fichas con `E`, canjear todas con `Q`. Verificar que el valor total no crece por el canje y que un segundo canje no paga de nuevo. | **Smoke** (`TYCOON_CASINO_SMOKE_OK` y `TYCOON_CASHIER_SHIFT_OK`: lote, canje, valor total, segundo canje, y turno 08:00–22:00). **Build:** el rótulo de turno y la cajera oculta de noche se ven en la captura de las 23:00. **Pendiente:** teclado. |
| 9 | Regresar a la hamburguesería. Comprar ingredientes, cocinar y vender una hamburguesa. Debe aumentar ventas en 1 y saldo en $35; salir y abrir con la misma ruta de guardado para comprobar persistencia. | **Smoke** (`TYCOON_SMOKE_OK` saldo $135; `TYCOON_ORDERS_OK`). **Pendiente** en la build (hay que caminar hasta las estaciones). |
| 10 | Repetir el inicio en 1280×720 y 1600×900. Comprobar que HUD, tabla de premios, controles y avisos son legibles y no se pisan; usar `Alt+Enter` para pantalla completa. | **Build (parcial):** capturas del HUD a 1024×768, 1280×720, 1280×800, 1600×900 y 2560×1080, y con el texto al 150 %: legibles y sin pisarse (ver `Cierre_*` en `ArtSource/CasaMarea/Preview/`). **Pendiente:** `Alt+Enter` y revisión a pantalla completa por una persona. |

## Registro de esta corrida

- Módulo Windows: instalado en 6000.3.25f1 (`WindowsStandaloneSupport`). En el editor oficial 6000.0.84f1 sigue sin comprobarse.
- Build Windows: `TYCOON_WIN_BUILD_OK`, 98 617 350 bytes.
- Corrida de las diez comprobaciones: **ninguna se completó con teclado y ratón reales en la build**; el resultado de cada fila está arriba. Las filas con «Pendiente» las debe cerrar una persona con Windows.

Cuando una persona ejecute los pasos pendientes, añade fecha, equipo, versión de Unity, resultado `pasa/falla` por fila y fragmentos pertinentes de `Player.log` al PR. No subir el perfil ni la build.
