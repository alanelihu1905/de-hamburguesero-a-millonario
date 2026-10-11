# Colaboración: tres personas, tres agentes, tres áreas

Plan para el repositorio compartido en GitHub. Las normas de trabajo están en [`CONTRIBUTING.md`](../CONTRIBUTING.md). Este documento fija el reparto, el tablero y el orden de arranque.

## Decisión de herramientas (la solución más simple)

**Un repositorio de GitHub + Issues + GitHub Projects + plantillas + una acción gratuita de pruebas.** No hace falta nada más para tres personas.

| Necesidad | Solución | Notas verificadas en la documentación oficial de GitHub |
| --- | --- | --- |
| Código, historial, revisión | Repositorio + *pull requests* | Squash and merge, una rama por issue |
| Reparto y avance visible | **GitHub Projects** (vista Tablero) | Tabla, tablero y calendario sobre issues y PRs; se enlaza con el repositorio y se actualiza solo al abrir o cerrar issues y PRs |
| Tareas pequeñas | **Issues** con plantilla y etiquetas por área | Formularios de issue en `.github/ISSUE_TEMPLATE` |
| Archivos pesados | **Git LFS** (ya configurado en `.gitattributes`) | Gratis hasta 10 GiB de almacenamiento y 10 GiB de ancho de banda por cuenta personal; al superarlo, se bloquea el uso de LFS hasta el mes siguiente. Tamaño máximo por archivo: 2 GB |
| Pruebas automáticas | **GitHub Actions** con `dotnet` (sin Unity) | Corre `Tests/*` en cada PR; no necesita licencia de Unity |
| Proteger `main` | Regla de rama (*branch protection*/ruleset) | Exigir PR, una aprobación y que pasen las pruebas. **Comprobar en Ajustes del repositorio qué reglas ofrece el plan gratuito para repositorios públicos** antes de contar con ellas |

Se descartó por ahora: CI con Unity en GitHub Actions (exige activar una licencia de Unity como secreto del repositorio, algo delicado en un repositorio público), tableros externos (Trello, Linear) y repositorios separados por área (rompen la integración de escenas).

### Aviso: repositorio público
Todo lo que se sube lo puede leer y copiar cualquiera, incluido el **historial**. Por eso: ningún secreto, perfil de guardado, build ni ruta personal. Auditoría previa a publicar (hecha el 2026-10-10): sin coincidencias de claves o tokens en 270 archivos nuevos; rutas locales sustituidas por relativas; los commits usan el correo `noreply` de GitHub (no un correo personal ni el nombre del equipo). Los modelos de Kenney son CC0; el resto del arte es propio.

## Las tres áreas

| | **A. Economía y guardado** | **B. Casino y UI** | **C. Arte, mundo y QA** |
| --- | --- | --- | --- |
| Persona / agente | _por asignar_ | _por asignar_ | _por asignar_ |
| Meta | Que el dinero y el progreso sean correctos, guardados y listos para validar en un anfitrión | Que el casino y el HUD se sientan bien, sean accesibles y paguen sin errores | Que el barrio se vea cuidado, tenga vida y que todo se pruebe de verdad |
| Carpetas | `Core`, `Economy`, `Restaurant`, `Tests/` | `Casino`, `UI`, `Art/Casino`, `Editor/Casino*` | `ArtSource`, `Models`, `Materials`, `Animation`, `Editor/CasaMarea*`, evidencia |
| Etiqueta | `area:economia` | `area:casino-ui` | `area:arte-qa` |
| Rama | `economia/N-...` | `casino/N-...` | `arte/N-...` |

Las áreas A y B comparten una frontera: **A** posee `CasinoSession` y sus pruebas (reglas de dinero); **B** posee `CasinoService`, HUD, sonido y escena del casino. Un cambio de reglas de pago lo hace A; un cambio de cómo se muestra lo hace B.

## Tablero (GitHub Projects)

Columnas: **Pendiente → Listo → En curso → En revisión → Hecho**. Campos: *Área* (A/B/C), *Tamaño* (S ≤ 2 h de agente, M ≤ medio día, L solo si se divide), *Prioridad* (P1/P2/P3). Vistas: *Tablero* (por estado), *Por persona* (agrupada por asignado) y *Por área*. Regla: máximo **2 issues en curso por persona**; un issue grande se divide antes de empezar.

Automatizaciones recomendadas: al abrir un issue entra en *Pendiente*; al abrir un PR que lo enlaza pasa a *En revisión*; al fusionar pasa a *Hecho*.

## Orden de arranque (primera semana)

1. **Antes de nada** (una persona, 30 min): clonar con LFS, abrir en Unity, correr las tres pruebas `dotnet` y el smoke de Play Mode. Cada persona deja un comentario «funciona en mi equipo» en el issue #1.
2. Cada agente toma el primer issue `P1` de su área. Los issues de la primera tanda están pensados para **no tocar los mismos archivos**.
3. Revisión cruzada: A revisa a B, B revisa a C, C revisa a A.
4. Al final de cada tanda, C ejecuta la cadena completa (integrar escena, smokes con marcador, build macOS, capturas) sobre `main` y actualiza `Docs/TEST_REPORT.md`.

## Issues iniciales

Están en [`tools/github/issues.tsv`](../tools/github/issues.tsv) (título, área, prioridad, tamaño, cuerpo) y se pueden crear con `tools/github/crear_issues.sh` cuando el repositorio exista y `gh` esté autenticado. Resumen:

- **A — Economía y guardado:** libro de movimientos común; caja del casino con fondos y tope; protección contra dos instancias; precios y recetas como datos; reporte de ventas, dinero y resultado.
- **B — Casino y UI:** audio en buses y verificación audible; ajustes de accesibilidad (tamaño de texto y teclas); turnos y fichas físicas de la cajera; HUD en otras proporciones de pantalla; celebración de jackpot.
- **C — Arte, mundo y QA:** clientes que piden una hamburguesa real; ciclo día/noche; build Windows y lista de QA; medición de rendimiento; variaciones de clientes y vida en la calle.

## Qué falta decidir con el usuario
- Dueño del repositorio (cuenta personal u organización), nombre y visibilidad (**público**, ya indicado).
- Usuarios de GitHub de las dos personas invitadas y qué área toma cada una.
- Si la especificación `Especificacion_Completa_Schedule_I_y_Tycoon.md` se publica en el repositorio (contiene el nombre del autor) o queda fuera.
- Licencia del código (por ejemplo MIT) y del arte propio; **hasta decidirlo, el repositorio público sin licencia significa «todos los derechos reservados»**.
