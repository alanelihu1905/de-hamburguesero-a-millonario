# QA de arranque: Windows

Objetivo: repetir diez comprobaciones en una build nueva. Fecha de la corrida de esta rama: 2026-10-10. La tabla distingue la evidencia disponible del resultado pendiente en Windows; un smoke de Unity no sustituye una prueba de la aplicación Windows.

## Preparación

1. En Unity Hub, instala Unity 6000.0.84f1 y **Windows Build Support** para esa misma versión. Abre el proyecto con Git LFS descargado.
2. En Unity, ejecuta `Tycoon3D.Editor.PrototypeWinBuild.Build` desde `-executeMethod`, o llama ese método desde el editor. El resultado debe estar en `Builds/Windows/De Hamburguesero a Millonario.exe` junto con su carpeta `*_Data` y `UnityPlayer.dll`. Conserva toda la carpeta. Busca `TYCOON_WIN_BUILD_OK` en el log. La ausencia del módulo debe dar el error explícito del script.
3. En Windows, crea una carpeta temporal nueva para el perfil. Desde PowerShell, inicia la aplicación así:

   ```powershell
   & '.\Builds\Windows\De Hamburguesero a Millonario.exe' -casino-live -casino-roll 99 -save-dir "$env:TEMP\TycoonQA-C1"
   ```

   Borra esa carpeta temporal **solo antes** de empezar la corrida; para comprobar persistencia conserva su contenido entre lanzamientos. `-casino-live` coloca al jugador frente a la primera máquina. `-casino-roll 99` fuerza el jackpot para repetirlo; `-casino-roll 0` fuerza una pérdida. El juego sigue siendo interactivo. `Player.log` queda bajo `%USERPROFILE%\AppData\LocalLow\Tycoon3D\De Hamburguesero a Millonario\`.

## Diez comprobaciones

| # | Acción y resultado esperado | Resultado actual de esta rama |
| --- | --- | --- |
| 1 | Iniciar perfil vacío. Ver barrio, casino abierto y HUD sin desbloqueo ni compra previa. Log: `CASINO_LIVE_READY` y `SAVE_LOAD` con $100 iniciales. | Windows pendiente: no hay build Windows. Casino inicial cubierto por smoke de Play Mode anterior. |
| 2 | Entrar al casino desde la calle. Contar seis máquinas, tres tipos y puerta/pasillo transitables; ninguna máquina atraviesa al jugador. | Windows pendiente. Seis máquinas cubiertas por smoke de Play Mode anterior. |
| 3 | Frente a máquina Clásica, ver prompt de `E`; pulsar `E` una vez. Deben girar los rodillos, terminar el giro y aparecer un premio. Log: `CASINO_LIVE_AIM ... interactuable=True`. | Windows pendiente. |
| 4 | Mientras gira, pulsar `E` otra vez. No debe cobrar segunda apuesta simultánea. Al acabar, pulsar `Q` y comprobar que cambia el importe de apuesta mostrado. | Windows pendiente. Bloqueo y cambio de apuesta cubiertos por smoke anterior. |
| 5 | Con `-casino-roll 99`, hacer un giro de $25 con saldo inicial $100. Ver jackpot de $1000 y saldo final $1075, o el equivalente en fichas si se canjearon antes. | Windows pendiente. Jackpot en pesos verificado antes en app macOS. |
| 6 | Salir, abrir de nuevo con la **misma** ruta `-save-dir` y sin borrar archivos. Ver $1075 y una ronda en el libro; pulsar `R`. Saldo y libro deben seguir iguales. | Windows pendiente. Relanzamiento y `R` verificados antes en app macOS. |
| 7 | Usar perfil temporal **nuevo** con `-casino-live -casino-roll 0 -save-dir <ruta-nueva>`. Apostar $25 cuatro veces; saldo $0. Pulsar `E` una quinta vez: giro de cortesía, sin saldo negativo. | Windows pendiente. Giro de cortesía verificado antes en app macOS. |
| 8 | Acercarse a la ventanilla de la cajera. Cambiar pesos por fichas con `E`, canjear todas con `Q`. Verificar que el valor total no crece por el canje y que un segundo canje no paga de nuevo. | Windows pendiente. Cajera cubierta por smoke y por app macOS anterior. |
| 9 | Regresar a la hamburguesería. Comprar ingredientes, cocinar y vender una hamburguesa. Debe aumentar ventas en 1 y saldo en $35; salir y abrir con la misma ruta de guardado para comprobar persistencia. | Windows pendiente. Venta cubierta por `TYCOON_SMOKE_OK` anterior; persistencia por app macOS anterior. |
| 10 | Repetir el inicio en 1280×720 y 1600×900. Comprobar que HUD, tabla de premios, controles y avisos son legibles y no se pisan; usar `Alt+Enter` para pantalla completa. | Windows pendiente. HUD revisado antes en app macOS. |

## Registro de esta rama

- Módulo Windows: pendiente de verificación final del editor. Inspección del directorio de Unity 6000.0.84f1: solo `MacStandaloneSupport` en `Unity.app/Contents/PlaybackEngines`.
- Build Windows: pendiente. El script exige soporte instalado; no se declara `TYCOON_WIN_BUILD_OK` hasta ver la línea real.
- Corrida de las diez comprobaciones: pendiente en Windows. Los resultados históricos macOS/Play Mode están en [`TEST_REPORT.md`](TEST_REPORT.md) y no se atribuyen a esta rama.

Cuando se disponga de Windows Build Support, ejecutar el build. Una persona con Windows ejecuta los diez pasos, añade fecha, equipo, versión de Unity, resultado `pasa/falla` por fila y fragmentos pertinentes de `Player.log` al PR. No subir el perfil ni la build.
