# Decisiones

## 2026-10-09

- Repositorio inicialmente vacío: se prepara un proyecto Unity desde cero en la raíz del repositorio.
- Unity 6.0 LTS con URP 17 como objetivo inicial. La versión exacta se ajustará al editor instalado y al resultado de resolución de paquetes.
- Escena generada por herramienta de editor para evitar mantener YAML de escena escrito a mano. El generador no sobrescribe una escena existente.
- Primer bucle de venta deliberadamente pequeño para comprobar movimiento, interacción, transacción y reinicio antes de añadir NPC y ciudad.
- Sistema de entrada clásico de Unity para este prototipo; se evaluará migración al Input System al integrar controles y multijugador.
- Claude Code está disponible por CLI para encargos de solo texto; se conservarán sus entregas en Git. Para Antigravity no se confirmó un canal automatizado, así que su encargo de QA queda como handoff.
- Claude Code sí respondió mediante CLI de solo texto. Se incorpora su paleta Casa Marea al prototipo y su revisión geográfica del Golfo. Las propuestas de geometría y HUD quedan para el siguiente hito.
- A petición del usuario, las futuras comunicaciones con Claude se hacen en la app de Claude, no por CLI. La app revisó el proyecto y creó `Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md` sin editar otros archivos.
- Se adopta para el primer hito la casa pequeña con fachada colonial propuesta por Claude. Patio y aljibe quedan para expansión. Se integran ahora colores, iluminación básica, posición de estaciones y toma inicial; las siluetas, UI final y texturas quedan para fases siguientes.
- Unity 6.0.84f1 abrió primero en Safe Mode por un error `GenerateHLSL` del paquete `com.unity.render-pipelines.universal-config` incluido con el editor. Se incluye copia local del paquete con la anotación de generación retirada; el archivo HLSL generado sigue incluido. Unity volvió a compilar y Play Mode pasó una prueba de venta.
- Claude.app modela las tres estaciones en Blender en rutas aisladas `ArtSource/CasaMarea/` y `Assets/_Game/Models/CasaMarea/`; Codex conserva integración y pruebas de Unity.
- Los FBX de Blender se instancian bajo un grupo sin escala en la escena para preservar dimensiones; se orientan de Z arriba a Y arriba, se asignan materiales URP y se ajustan los colliders de las estaciones. La integración es reproducible desde `CasaMareaModelIntegrator`.
- Antigravity.app realiza auditoría independiente y conserva su informe en `Docs/Handoffs/Antigravity/02_QA_HITO1.md`. Los defectos confirmados se corrigen antes de ampliar mecánicas.
- La fachada se integra como grupo de arte separado; se ocultan solo cubos provisionales que ocupaban el frente. La puerta queda sin geometría para conservar el recorrido a la cocina.
