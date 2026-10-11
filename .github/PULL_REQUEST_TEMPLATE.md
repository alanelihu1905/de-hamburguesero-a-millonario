## Qué cambia y por qué
<!-- En lenguaje llano. Un PR, un issue. -->

Closes #

## Área
- [ ] A · Economía y guardado   - [ ] B · Casino y UI   - [ ] C · Arte, mundo y QA

## Pruebas que ejecuté
<!-- Pega las líneas reales del log. Si no corrió, escribe «no ejecutado» y por qué. -->
- [ ] `dotnet run --project Tests/SaveSmoke` →
- [ ] `dotnet run --project Tests/CasinoSmoke` →
- [ ] `dotnet run --project Tests/EconomySmoke` →
- [ ] Smoke de casino en Unity (sin `-quit`): `TYCOON_CASINO_SMOKE_OK` →
- [ ] Smoke de hamburguesas en Unity (sin `-quit`): `TYCOON_SMOKE_OK` →
- [ ] Build macOS: `TYCOON_MAC_BUILD_OK` →

## Evidencia visual
<!-- Capturas de la app compilada si cambia algo que se ve. Bajo 2 MB cada una. -->

## No pude verificar
<!-- Lo que sigue sin probarse, sin adornos. -->

## Lista de control
- [ ] Solo toco mi área (o lo acordé en el issue y etiqueté a la otra persona)
- [ ] Los `.meta` van con sus archivos; no edité la escena a mano
- [ ] Sin secretos, perfiles de guardado, `Library/`, `Builds/` ni rutas personales
- [ ] Archivos pesados por Git LFS; nuevos assets de terceros anotados en `Docs/ASSETS_EXTERNOS.md`
- [ ] Si toco el formato de guardado: subí la versión y añadí migración y prueba
- [ ] Actualicé `Docs/PROJECT_STATE.md` / `DECISIONS.md` / `TEST_REPORT.md` (solo añadiendo al final)

## Relevo
<!-- Qué quedó hecho, qué falta y cómo reproducir las pruebas, para quien siga después. -->
