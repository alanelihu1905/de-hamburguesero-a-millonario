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
3. Controles: **WASD** mover · **ratón** mirar · **E** interactuar · **Q** siguiente apuesta / canjear fichas · **1-4** apuesta · **F** luces fijas · **R** volver al inicio (no borra el progreso) · **Esc** libera el cursor.

El progreso (dinero, ventas, fichas, libro del casino) se guarda solo. Para empezar de cero, borra la carpeta `~/Library/Application Support/Tycoon3D/De Hamburguesero a Millonario/Tycoon3D/` (macOS) o ejecuta con `-save-dir <carpeta-vacía>`.

## Pruebas

Rápidas, sin Unity (las mismas que corre GitHub en cada PR):

```bash
dotnet run --project Tests/SaveSmoke      # guardado, recuperación, fichas, rondas pendientes
dotnet run --project Tests/CasinoSmoke    # tablas de premios, libro del casino
dotnet run --project Tests/EconomySmoke   # monedero y ciclo de hamburguesas
```

Con Unity (de línea de comandos; **sin** `-quit`, porque Unity saldría antes de ejecutar nada y devolvería 0). Busca el marcador en el log:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -nographics -projectPath . -executeMethod Tycoon3D.Editor.CasinoPlaySmoke.Run -logFile smoke.log
grep TYCOON_CASINO_SMOKE_OK smoke.log        # debe aparecer
$UNITY -batchmode -nographics -projectPath . -executeMethod Tycoon3D.Editor.PrototypePlaySmoke.Run -logFile burger.log
grep TYCOON_SMOKE_OK burger.log
```

Build macOS: `Tools > Tycoon3D > ...` o `-executeMethod Tycoon3D.Editor.PrototypeMacBuild.Build` (con `-quit`). Los builds van a `Builds/` y no se suben.

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
