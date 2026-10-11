# Cómo contribuir

Somos tres personas, cada una con su agente de IA. Para que el trabajo se vea bien repartido en Git y no se pisen los cambios, seguimos estas normas. Las reglas permanentes del proyecto están en [`AGENTS.md`](AGENTS.md); aquí está el flujo. El reparto de áreas y el tablero están en [`Docs/COLABORACION.md`](Docs/COLABORACION.md).

## Flujo en cuatro pasos

1. **Elige un issue** de tu área en el tablero (columna *Listo*), asígnatelo y pásalo a *En curso*. Si no existe, créalo antes con la plantilla *Tarea*.
2. **Rama desde `main`**: `area/numero-descripcion-corta`, por ejemplo `economia/12-libro-de-movimientos`. Una rama, un issue.
3. **Pull request pequeño** (idealmente menos de 400 líneas sin contar `.meta`, escenas ni imágenes) con la plantilla rellena. Enlaza el issue con `Closes #12`.
4. **Revisión de otra persona** (puede ser el agente de otra área, pero debe leer el diff y las pruebas). Se integra con *Squash and merge*; `main` siempre debe abrir en Unity y pasar las pruebas.

Nunca se hace push directo a `main` ni `git push --force` sobre ramas compartidas.

## Áreas y dueños de carpetas

| Área | Carpetas principales |
| --- | --- |
| **Economía y guardado** | `Assets/_Game/Scripts/Core`, `Economy`, `Restaurant`, `Tests/` |
| **Casino y UI** | `Assets/_Game/Scripts/Casino`, `UI`, `Assets/_Game/Art/Casino`, `Assets/Editor/Casino*` |
| **Arte, mundo y QA** | `ArtSource/`, `Assets/_Game/Models`, `Materials`, `Animation`, `Assets/Editor/CasaMarea*`, `ClientesAnimadosIntegrator.cs`, evidencia y `Docs/TEST_REPORT.md` |

Puedes tocar archivos de otra área solo si el issue lo pide o lo acuerdas en un comentario del PR; en ese caso, etiqueta a esa persona como revisora.

## Archivos que se pisan fácil (reglas especiales)

- **Escena `BarrioResidencial.unity` y prefabs**: no se editan a mano en dos ramas a la vez. La escena se **genera** con los constructores de `Assets/Editor/` (`CasinoFoxIntegration.IntegrateAllWithCasino`). Si hay conflicto en la escena, **no lo resuelvas a mano**: toma la versión de `main`, repite el constructor y vuelve a guardar.
- **`.meta`**: se suben siempre junto a su archivo y nunca se editan a mano. Un archivo sin su `.meta` rompe referencias.
- **`Docs/PROJECT_STATE.md`, `DECISIONS.md`, `TEST_REPORT.md`**: solo se **añade** al final, bajo un encabezado con fecha y el nombre de tu área. No reescribas lo ajeno.
- **`Packages/manifest.json`**: cambiar un paquete exige decirlo en el PR y que otra persona lo revise.

## Qué nunca se sube

`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, perfiles de guardado (`profile.*`), `.env`, claves, tokens y cualquier archivo con datos personales. `.gitignore` ya cubre lo habitual; si dudas, no lo subas. Si un secreto llega a subirse, avisa de inmediato: hay que **rotarlo** (borrarlo del historial no basta en un repositorio público).

## Archivos pesados (Git LFS)

`.blend`, `.fbx`, `.glb`, `.png`, `.jpg`, `.wav`, `.mp3`, `.ogg`, `.mp4` y `.psd` van por **Git LFS** (ver `.gitattributes`). Antes de tu primer commit: `git lfs install`. No subas capturas enormes: la evidencia va en `ArtSource/CasaMarea/Preview/` y bajo 2 MB cuando se pueda. La cuota gratuita de LFS es compartida, así que no versiones builds ni vídeos largos.

## Qué debe traer un PR

- **Qué cambia y por qué**, en lenguaje llano, y el issue que cierra.
- **Pruebas que se ejecutaron**: las de `dotnet` aplicables y, si tocas lógica o escena, el smoke de Play Mode **con su marcador** (`TYCOON_CASINO_SMOKE_OK`, `TYCOON_SMOKE_OK`, `TYCOON_MAC_BUILD_OK`). Pega la línea del log, no un resumen.
- **Evidencia visual** si cambia algo que se ve (captura de la app, no solo del editor).
- **Lo que no se pudo verificar**, dicho sin adornos.
- Si tocas el guardado (`Core/Save`): subir la versión del formato y añadir su migración y su prueba, o explicar por qué no hace falta.

## Normas para agentes

1. Lee `AGENTS.md`, este archivo y el issue antes de editar. Trabaja solo en tu área y en tu issue; no «aproveches» para reordenar código ajeno.
2. **No declares algo verificado sin una prueba**: no digas «funciona» sin el marcador, ni atribuyas resultados a otras herramientas sin evidencia.
3. No lances los smokes de Play Mode con `-quit` (Unity sale antes y devuelve 0). Comprueba el marcador en el log.
4. Las pruebas usan un perfil de guardado temporal (`GameProfile.DirectoryOverride` o `-save-dir`). Nunca escribas en el perfil real de nadie.
5. La lógica económica va en C# puro y pequeño, sin Unity, para validarla después en el anfitrión (multijugador). Ver `AGENTS.md`.
6. No añadas dinero real, apuestas con dinero real ni personajes reales en tramas polémicas. No copies assets, modelos, marcas ni interfaces de otros juegos. Todo asset de terceros: licencia clara y renglón en `Docs/ASSETS_EXTERNOS.md`.
7. Commits pequeños y descriptivos, en español, en imperativo («Guarda el libro del casino»). Si tu agente añade coautoría, que sea el que realmente escribió el cambio.
8. Si te bloqueas o algo contradice una instrucción, deja un comentario en el issue con lo que viste, y espera; no improvises un cambio de arquitectura. Cambiar Unity 6, URP o PC Windows exige una decisión registrada en `Docs/DECISIONS.md`.
9. Al terminar, deja un **relevo** corto en el PR: qué quedó hecho, qué falta, cómo reproducir las pruebas.

## Conflictos y dudas

Si dos ramas tocan lo mismo, la que llegó primero a `main` manda; la otra se actualiza (`git fetch && git rebase origin/main`) y repite sus pruebas. Para decisiones de diseño sin dueño claro, abre un issue con la etiqueta `decisión` y resuélvelo ahí.
