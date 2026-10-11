# Arranque para los tres agentes

Tres encargos independientes, uno por área, sacados de las 15 issues de [`tools/github/issues.tsv`](../tools/github/issues.tsv). Cada persona trabaja con su agente; **nadie está asignado todavía** (los usuarios de GitHub llegan después). Normas generales: [`CONTRIBUTING.md`](../CONTRIBUTING.md). Reparto y tablero: [`COLABORACION.md`](COLABORACION.md). Reglas permanentes: [`AGENTS.md`](../AGENTS.md).

Códigos de las 15 issues (se identifican por su **título** en `tools/github/issues.tsv`; los números de GitHub se asignan al crearlas):

| Código | Título | Prioridad | Tamaño |
| --- | --- | --- | --- |
| **Arranque** | Primera ejecución en cada equipo: clonar con LFS, abrir en Unity y correr las pruebas (previa para todos; la crea el área A; será la primera issue, #1) | P1 | S |
| **A1** | Libro de movimientos común (hamburguesas y casino) | P1 | M |
| **A2** | Caja del casino con fondos y tope de premio por ronda | P2 | M |
| **A3** | Precios y recetas como datos editables | P2 | M |
| **A4** | Proteger el perfil contra dos instancias de la app | P3 | S |
| **B1** | Audio por buses y verificación audible | P1 | M |
| **B2** | Ajustes de accesibilidad: tamaño de texto y reasignación de teclas | P2 | M |
| **B3** | Turnos y fichas físicas de la cajera | P3 | M |
| **B4** | HUD en otras proporciones de pantalla | P2 | S |
| **B5** | Celebración de jackpot | P3 | S |
| **C1** | Build Windows y lista de QA de arranque | P1 | M |
| **C2** | Ciclo día y noche con lectura clara | P2 | M |
| **C3** | Medición de rendimiento reproducible | P3 | S |
| **C4** | Más variedad de clientes y vida en la calle | P3 | M |
| **C5** | Clientes de la fila que piden una hamburguesa real | P1 | L (se divide antes de empezar) |

## Regla de oro: una sola persona escribe la escena

La escena `Assets/_Game/Scenes/BarrioResidencial.unity` **se genera**, no se edita a mano, y solo la **escribe el área C** (`Assets/Editor/CasinoFoxIntegration.IntegrateAllWithCasino`, que encadena la paleta, el casino y los clientes animados).

- **A y B no suben cambios del `.unity`.** Cambian código, constructores (`Assets/Editor/...`) y datos; para probar, regeneran la escena **en su equipo** y descartan el cambio (`git restore Assets/_Game/Scenes`).
- Si tu cambio exige que la escena cambie (un objeto nuevo, un componente), déjalo en el constructor y avisa en el PR con la etiqueta `toca-escena`.
- **C regenera y sube la escena** en un PR propio («Regenera la escena») **después** de fusionar los PRs de A y B, nunca antes. Solo puede haber **un PR abierto** que incluya el `.unity`.
- Prefabs y materiales nuevos: los crea quien los necesita, en carpetas de su área, con su `.meta`.
- Si por error dos ramas tocan la escena: no se resuelve el conflicto a mano; se toma la de `main` y se regenera.

## Orden de integración

1. **Arranque (todos)**: cada persona clona con `git lfs pull`, abre el proyecto, corre las pruebas y comenta «funciona» con el sistema operativo.
2. **Ola 1, en paralelo y sin tocar los mismos archivos:** **A1** (libro de movimientos), **B1** (audio por buses), **C1** (build Windows y lista de QA).
3. **Orden de fusión de la ola 1:** A1 → B1 → C1. A1 va primero porque cambia `CasinoSession` y el formato de guardado, y B1 debe rebasar sobre él.
4. **Después de fusionar A1 y B1:** C ejecuta la cadena completa (integrar escena, los dos smokes con marcador, build macOS, capturas), sube la escena regenerada y actualiza `Docs/TEST_REPORT.md`.
5. **Ola 2** (cuando la ola 1 esté en `main`): A2, A3, B2, B3, C2. Revisión cruzada: **A revisa a B, B revisa a C, C revisa a A.**
6. **Ola 3:** A4, B4, B5, C3, C4. **C5 (clientes que piden una hamburguesa real)** es grande: se divide en el propio issue antes de empezar, toca `BurgerLoop` y `AnimatedCustomer` (de A) y la escena (de C), así que **no** se arranca hasta que A1 y C1 estén en `main` y A y C acuerden el diseño por comentario.

## Encargo A: Economía y guardado

**Primera issue lista para asignar:** *A1 · Libro de movimientos común (hamburguesas y casino)*, prioridad P1, tamaño M.

- **Objetivo:** un único libro de movimientos en C# puro (tipo, monto, origen y destino) que alimente ventas, compras, giros, canjes y premios, conserve el valor total y se guarde en el perfil.
- **Puede tocar:** `Assets/_Game/Scripts/Core/` (incluido `Core/Save/`), `Economy/`, `Restaurant/BurgerLoop.cs`, `Tests/`.
- **Coordina antes de tocar:** `Casino/CasinoSession.cs` y `CasinoState.cs` (los usa B: avisa en el issue B1/B2 si cambia una firma); el formato de guardado (`SaveData`, `SaveCodec`): **solo A lo cambia**, y B o C piden claves nuevas por issue; `Packages/manifest.json`.
- **Criterios de aceptación:**
  - [ ] El libro registra cada venta, compra, giro, canje y premio; el valor total (pesos + fichas) se conserva en todas las operaciones.
  - [ ] Se guarda en el perfil con la **versión del formato subida** y una migración desde la anterior.
  - [ ] Un perfil guardado antes del cambio se carga sin perder nada.
  - [ ] Los smokes de Play Mode siguen pasando con marcador.
- **Pruebas mínimas:** `dotnet run --project Tests/SaveSmoke` (con pruebas nuevas de conservación y de migración), `Tests/CasinoSmoke`, `Tests/EconomySmoke`; en Unity, `TYCOON_CASINO_SMOKE_OK` y `TYCOON_SMOKE_OK` (sin `-quit`).
- **No toca:** la escena, `Casino/CasinoService.cs`, `Casino/CasinoHud.cs`, `ArtSource/`.

## Encargo B: Casino y UI

**Primera issue lista para asignar:** *B1 · Audio por buses y verificación audible*, prioridad P1, tamaño M.

- **Objetivo:** separar música, efectos y ambiente en un mixer de Unity y comprobar a oído que el giro, la cajera y el premio se oyen sin tapar avisos.
- **Puede tocar:** `Assets/_Game/Scripts/Casino/CasinoSounds.cs`, `CasinoService.cs`, `CasinoHud.cs`, `SlotMachineStation.cs`, `Assets/_Game/Scripts/UI/`, `Assets/_Game/Art/Casino/` (audio y mixer nuevos) y `Assets/Editor/CasinoFoxBuilder.cs` solo para enlazar fuentes al mixer.
- **Coordina antes de tocar:** `CasinoSession.cs` (de A: si necesitas otra regla de pago, abre un issue a A); cualquier clave nueva de guardado (ajustes de sonido) se pide a A; la **escena**: no se sube (ver regla de oro), avisa con `toca-escena`.
- **Criterios de aceptación:**
  - [ ] Existe un mixer con tres buses (música, efectos, ambiente) y todas las fuentes del casino los usan.
  - [ ] Los volúmenes por defecto están documentados en el PR.
  - [ ] Una persona escuchó la app compilada y escribió qué oyó (giro, parada de rodillos, premio, jackpot, cajera, ambiente).
  - [ ] Ningún cambio de reglas de pago ni de formato de guardado.
- **Pruebas mínimas:** `dotnet run --project Tests/CasinoSmoke`; en Unity, `TYCOON_CASINO_SMOKE_OK` (sin `-quit`); app compilada: el diagnóstico `CASINO_AUDIO` muestra fuentes sonando durante el giro; captura del mixer.
- **No toca:** `Core/Save`, `Economy/`, `BurgerLoop`, `ArtSource/`.

## Encargo C: Arte, mundo y QA

**Primera issue lista para asignar:** *C1 · Build Windows y lista de QA de arranque*, prioridad P1, tamaño M.

- **Objetivo:** instalar el módulo de build Windows, generar el build o documentar con precisión la limitación, y escribir una lista de QA de 10 comprobaciones que cualquiera pueda repetir.
- **Puede tocar:** `ArtSource/`, `Assets/_Game/Models/`, `Materials/`, `Animation/`, `Assets/Editor/CasaMarea*`, `ClientesAnimadosIntegrator.cs`, `PrototypeMacBuild.cs` (y un `PrototypeWinBuild.cs` nuevo), `Docs/TEST_REPORT.md` y la evidencia en `ArtSource/CasaMarea/Preview/`.
- **Coordina antes de tocar:** **es la única área que sube la escena**; `Restaurant/AnimatedCustomer.cs` y `BurgerLoop.cs` son de A (pide los cambios por issue); `ProjectSettings/` y `Packages/manifest.json` (avisa en el PR: afectan a todos).
- **Criterios de aceptación:**
  - [ ] Build Windows con marcador `TYCOON_WIN_BUILD_OK`, o el motivo exacto de que no se pueda (módulo, licencia, plataforma) y los pasos para resolverlo.
  - [ ] Lista de QA en `Docs/` con 10 comprobaciones concretas y el resultado actual de cada una.
  - [ ] La lista usa solo los modos de prueba ya existentes: `-casino-live`, `-casino-roll`, `-save-dir`.
  - [ ] Ningún cambio de jugabilidad.
- **Pruebas mínimas:** `TYCOON_MAC_BUILD_OK` sigue apareciendo; los dos smokes de Play Mode con marcador; la lista de QA ejecutada una vez, con evidencia.
- **No toca:** `Core/`, `Economy/`, `Casino/` (salvo lectura).

## Cómo publicar el estado

Mismo flujo para los tres agentes:

1. **Al empezar:** comenta en el issue «Tomo esto», con la rama (`area/N-descripcion`) y los archivos que prevés tocar. Pásalo a *En curso* en el tablero (máximo 2 por persona).
2. **Abre un PR en borrador (Draft) en cuanto haya el primer commit**, con `Closes #N`. La plantilla [`PULL_REQUEST_TEMPLATE.md`](../.github/PULL_REQUEST_TEMPLATE.md) se rellena de verdad: pega las **líneas reales de marcador** de las pruebas, no un resumen.
3. **Durante el trabajo:** un comentario breve en el issue cuando cambie el plan, aparezca un bloqueo o se necesite a otra área (menciona el área con la etiqueta `area:...`, y a la persona cuando exista).
4. **Antes de pedir revisión:** marca el PR como *Ready for review*, rellena «No pude verificar» y «Relevo», y comprueba que la acción de `dotnet` pasa.
5. **Al fusionar:** *Squash and merge*, el issue se cierra solo y el tablero lo pasa a *Hecho*. La persona de C anota el resultado en `Docs/TEST_REPORT.md` tras la integración.

Un agente nunca declara algo verificado sin su marcador, y si no pudo ejecutar una prueba lo escribe tal cual.

## Qué falta antes de repartir

- Asignar personas a las áreas (usuarios de GitHub aún por llegar) y crear los issues (`tools/github/crear_issues.sh`, hoy en simulación).
- Que el repositorio exista y la acción de `dotnet` haya corrido al menos una vez en GitHub.
