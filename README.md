# De Hamburguesero a Millonario: Tycoon 3D

Simulador de negocios en primera persona hecho en **Unity 6 (6000.0.84f1), URP, C#**, para PC. Empiezas con $100 y una parrilla; vendes hamburguesas, juegas en el **Casino FOX** (siempre abierto, seis tragamonedas, cajera y fichas) y creces. Es un proyecto original: no usa assets ni marcas de ningún otro juego.

> Estado: prototipo jugable en un barrio pequeño. Las funciones nuevas se añaden de a una y solo se dan por hechas cuando tienen una prueba con marcador. Ver [`Docs/PROJECT_STATE.md`](Docs/PROJECT_STATE.md).

## Empezar (5 minutos)

Requisitos: **Unity Hub** con el editor **6000.0.84f1**, **Git** y **Git LFS** (`git lfs install` una vez por equipo). Para las pruebas rápidas, **.NET 8 SDK**.

```bash
git clone <URL-del-repositorio>
cd <carpeta>
git lfs pull            # modelos, imágenes y audio viven en Git LFS
```

1. En Unity Hub: *Add project from disk* y elige la carpeta clonada. La primera apertura tarda varios minutos (genera `Library/`, que no se sube).
2. Abre `Assets/_Game/Scenes/BarrioResidencial.unity` y pulsa **Play**.
3. Controles: **WASD** mover · **ratón** mirar · **E** interactuar · **Q** siguiente apuesta / canjear fichas · **1-4** apuesta · **F** luces fijas · **R** volver al inicio (no borra el progreso) · **Esc** libera el cursor y abre los **ajustes** (tamaño del texto y reasignar E, Q y F; WASD, R y Esc no se pueden reasignar).

El progreso (dinero, ventas, fichas, libro del casino, caja del casino, hora del juego y ajustes) se guarda solo. Para empezar de cero, borra la carpeta del perfil o ejecuta con `-save-dir <carpeta-vacía>`:

| Sistema | Carpeta del perfil |
| --- | --- |
| Windows | `%USERPROFILE%\AppData\LocalLow\Tycoon3D\De Hamburguesero a Millonario\Tycoon3D\` |
| macOS | `~/Library/Application Support/Tycoon3D/De Hamburguesero a Millonario/Tycoon3D/` |

Si abres dos copias del juego con el mismo perfil, la segunda avisa en pantalla y juega **sin guardar** (la primera conserva el candado `profile.lock`).

### Datos editables

Los precios de las hamburguesas y el lote de fichas de la cajera salen de [`Assets/StreamingAssets/economy.json`](Assets/StreamingAssets/economy.json) (en una build, junto al ejecutable, en `*_Data/StreamingAssets/`). Se editan con cualquier editor de texto, sin recompilar; si el archivo falta o es inválido se usan los valores de siempre y queda una línea `ECONOMY_CONFIG` en el log. También se puede apuntar a otro archivo con `-economy-config <ruta>`.

### Argumentos de línea de comandos (pruebas y capturas)

| Argumento | Para qué |
| --- | --- |
| `-save-dir <carpeta>` | Perfil de guardado aparte (nunca uses el de otra persona) |
| `-casino-live`, `-casino-roll <0-99>`, `-casino-at-cashier` | Empezar frente a una máquina o la cajera; forzar la tirada (99 = jackpot, 0 = pérdida) |
| `-casino-shots <carpeta>` | Recorre puntos de vista, juega una ronda de cada tipo, guarda PNG y cierra |
| `-clock-hour <h>`, `-clock-speed <segundos por día>`, `-clock-freeze` | Fijar la hora del juego, su velocidad o detenerla |
| `-perf-walk`, `-perf-laps <n>`, `-perf-out <archivo>`, `-perf-label <texto>` | Medición de rendimiento reproducible (ver `Docs/TEST_REPORT.md`) |
| `-quality-level <n>`, `-no-daynight`, `-no-variety` | Medir en otra calidad o sin el ciclo de día y noche / la variedad de clientes |
| `-economy-config <archivo>` | Usar otro `economy.json` |

## Pruebas

Rápidas, sin Unity (las mismas que corre GitHub en cada PR):

```bash
dotnet run --project Tests/SaveSmoke      # guardado, recuperación, fichas, rondas pendientes
dotnet run --project Tests/CasinoSmoke    # tablas de premios, libro del casino
dotnet run --project Tests/EconomySmoke   # monedero y ciclo de hamburguesas
```

Con Unity (de línea de comandos; **sin** `-quit`, porque Unity saldría antes de ejecutar nada y devolvería 0). Busca el marcador en el log:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity      # en Windows: "C:\Program Files\Unity\Hub\Editor\6000.0.84f1\Editor\Unity.exe"
$UNITY -batchmode -nographics -projectPath . -executeMethod Tycoon3D.Editor.CasinoPlaySmoke.Run -logFile smoke.log
grep TYCOON_CASINO_SMOKE_OK smoke.log        # debe aparecer
$UNITY -batchmode -nographics -projectPath . -executeMethod Tycoon3D.Editor.PrototypePlaySmoke.Run -logFile burger.log
grep TYCOON_SMOKE_OK burger.log
```

Todas las pruebas de Play Mode (cada una con su marcador, **sin** `-quit`):

| Método (`Tycoon3D.Editor.…Run`) | Marcador | Qué comprueba |
| --- | --- | --- |
| `PrototypePlaySmoke` | `TYCOON_SMOKE_OK` | Ciclo de hamburguesas, saldo $135 |
| `CasinoPlaySmoke` | `TYCOON_CASINO_SMOKE_OK` | Casino, fichas, cajera, guardado y recarga |
| `EconomyConfigPlaySmoke` | `TYCOON_ECONOMY_CONFIG_OK` | Precios desde `economy.json` |
| `DayNightPlaySmoke` | `TYCOON_DAYNIGHT_OK` | Ciclo de día y noche y hora guardada |
| `SettingsPlaySmoke` | `TYCOON_SETTINGS_OK` | Ajustes de teclas y tamaño del texto |
| `CashierShiftPlaySmoke` | `TYCOON_CASHIER_SHIFT_OK` | Turno de la cajera y fichas sobre el mostrador |
| `JackpotPlaySmoke` | `TYCOON_JACKPOT_OK` | Celebración del jackpot |
| `CustomerOrdersPlaySmoke` | `TYCOON_ORDERS_OK` | Clientes que piden y pagan |
| `CustomerVarietyPlaySmoke` | `TYCOON_VARIETY_OK` | Variantes de ropa y paseantes nuevos |
| `PerfWalkPlaySmoke` (con `-perf-walk -perf-laps 1 -perf-out <archivo>`) | `TYCOON_PERF_WALK_OK` | Recorrido de medición |

Los constructores de la escena (`CasinoFoxIntegration.IntegrateAllWithCasino`, `PrototypeMacBuild.Build`, `PrototypeWinBuild.Build`) sí se lanzan **con** `-quit`: no terminan solos.

Builds: `Tools > Tycoon3D > ...` o `-executeMethod Tycoon3D.Editor.PrototypeMacBuild.Build` / `PrototypeWinBuild.Build` (con `-quit`; el de Windows necesita el módulo *Windows Build Support* del editor). Los builds van a `Builds/` y no se suben. La lista de QA de arranque para una build de Windows está en [`Docs/QA_ARRANQUE.md`](Docs/QA_ARRANQUE.md).

## Estructura

| Carpeta | Qué hay |
| --- | --- |
| `Assets/_Game/Scripts/` | Juego: `Core` (perfil y guardado), `Economy`, `Restaurant`, `Casino`, `Interaction`, `Player`, `UI`, `Dev` |
| `Assets/Editor/` | Constructores reproducibles de la escena y pruebas de Play Mode |
| `Assets/_Game/Models`, `Materials`, `Animation`, `Art` | Arte integrado (FBX, materiales URP, animaciones, texturas y audio del casino) |
| `ArtSource/` | Fuentes Blender y scripts que generan el arte; capturas de evidencia |
| `Tests/` | Pruebas con `dotnet`, sin Unity |
| `Docs/` | Estado, decisiones, informe de pruebas, requisitos y relevos |

## Colaborar

Trabajamos tres personas, cada una con su agente, en tres áreas. Lee **[CONTRIBUTING.md](CONTRIBUTING.md)** y **[Docs/COLABORACION.md](Docs/COLABORACION.md)** antes de tu primer cambio. Reglas permanentes del proyecto: [`AGENTS.md`](AGENTS.md).

## Licencias y créditos

El código y el arte propios son del proyecto. Los modelos externos pequeños (Kenney, CC0) están registrados en [`Docs/ASSETS_EXTERNOS.md`](Docs/ASSETS_EXTERNOS.md). Cualquier asset nuevo de terceros necesita licencia que permita su uso y un renglón en ese manifiesto antes de entrar.
