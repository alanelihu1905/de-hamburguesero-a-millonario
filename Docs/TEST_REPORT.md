# Informe de pruebas — 2026-10-09

## Entorno

macOS 27 arm64; Unity Hub 3.22.2; Unity 6.0.84f1; Rosetta 2; Git LFS 3.8.0; Blender 5.2.0 LTS. Licencia Unity Personal activada en Hub por el usuario.

## Aprobado

- `Packages/manifest.json` pasó validación JSON.
- `dotnet run --project Tests/EconomySmoke/EconomySmoke.csproj`: ocho comprobaciones de saldo y transiciones.
- Unity importó paquetes, compiló scripts y generó `Assets/_Game/Scenes/BarrioResidencial.unity`; proceso batch salió con código 0.
- `Tycoon3D.Editor.PrototypePlaySmoke.Run` entró en Play Mode, encontró cámara y CharacterController; un raycast de 3 m alcanzó cada estación. Compró ingredientes, cocinó, vendió y confirmó una venta con $135. Proceso batch salió con código 0 y registró `TYCOON_SMOKE_OK`.
- Game View mostró la fachada, HUD, saldo inicial y cámara. Teclas W movieron al jugador hacia la puerta; R lo devolvió al punto inicial con $100 y cero ventas.
- Se añadió `com.unity.modules.audio` al manifest; tras actualizar paquetes desapareció el aviso de AudioListener desactivado.
- Git LFS inicializado localmente.
- Claude revisó el proyecto desde su app y creó `Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md`.
- Claude creó tres estaciones originales y corrigió dos medidas; Blender 5.2 regeneró fuentes, FBX, GLB y vistas. `stats.json`: huacal 628 triángulos y 0.90 × 0.55 × 0.50 m; plancha 332 y 1.10 × 0.60 × 0.75 m; venta 416 y 0.90 × 0.548 × 0.80 m.
- `CasaMareaModelIntegrator.Integrate` instanció tres FBX con materiales URP; las alturas en Unity son 0.50, 0.75 y 0.80 m y los colliders siguen las mallas. Salida `CASAMAREA_MODELS_OK`.
- Tras los cambios de QA, `PrototypePlaySmoke.Run` salió con código 0 y `TYCOON_SMOKE_OK`: tres modelos URP, raycast, prompts según estado y venta con $135. `EconomySmoke` volvió a pasar 8/8.
- `build_fachada.py` salió con Blender código 0; `stats_fachada.json` reporta fachada 1,376 tris, despacho 332 tris y puerta libre 1.99 × 2.29 m, 0 golpes de rayo. `CasaMareaFacadeIntegrator.Integrate` salió con `CASAMAREA_FACADE_OK`; Play Mode volvió a registrar `TYCOON_SMOKE_OK`.
- Antigravity.app entregó `Docs/Handoffs/Antigravity/02_QA_HITO1.md`; se corrigieron prompts estáticos, rebloqueo del cursor por clic y recarga de escena por índice. Se añadió retorno al origen si el jugador cae del borde provisional.

## Pendiente

- Pulsación E a las tres estaciones en Game View con puntero. El raycast geométrico y el método de interacción se probaron por separado en Play Mode.
- Build Windows. `PlaybackEngines` contiene solo `MacStandaloneSupport`; falta el módulo Windows.
- Build macOS completada: `TYCOON_MAC_BUILD_OK`, `Builds/Mac/De Hamburguesero a Millonario.app`, 107,987,931 bytes.
- Fachada exterior Casa Marea en elaboración por Claude.app.

## Incidencia resuelta

La apertura gráfica inicial entró en Safe Mode por `GenerateHLSL` en `com.unity.render-pipelines.universal-config` 17.0.3. Se incorporó el paquete local en `Packages/` y se retiró esa anotación; el archivo HLSL generado permanece. Tras ello, Unity salió de Safe Mode y la prueba de Play Mode pasó. Los avisos de carpetas `.meta` de paquetes sin directorio no bloquearon la prueba.
