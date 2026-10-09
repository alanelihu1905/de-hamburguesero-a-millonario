# De Hamburguesero a Millonario: Tycoon 3D

Primer prototipo de un simulador narrativo en Unity 6. Esta entrega contiene una escena residencial provisional que se genera al abrir el proyecto en Unity, control en primera persona y un ciclo mínimo de venta de hamburguesas.

## Abrir y jugar

1. Abrir Unity Hub con la licencia Unity Personal ya activada en este equipo. Unity Hub y el editor 6.0.84f1 están instalados.
2. En Unity Hub, abrir esta carpeta como proyecto: la carpeta del proyecto.
3. Abrir `Assets/_Game/Scenes/BarrioResidencial.unity`. La escena ya existe. El generador la crea una sola vez si falta, desde **Tools > Tycoon3D > Crear escena prototipo si falta**.
4. Abrir la escena, pulsar **Play** y recorrer la casa. Frente a cada estación, pulsar **E**: ingredientes ($15), parrilla y mostrador (+$50). **R** reinicia la escena; **Esc** libera el cursor.

El HUD y parte del barrio siguen en iteración; la fachada, despacho y tres estaciones ya usan modelos originales de Claude. Hay build macOS en `Builds/Mac/`; falta el módulo de build Windows. No hay multijugador. La prueba automática de Play Mode comprobó modelos, raycast, prompts, una venta y saldo final. Queda pendiente comprobar manualmente la tecla E en Game View.

## Estructura

- `Assets/_Game/Scripts/`: mecánicas de juego.
- `Assets/Editor/PrototypeSceneBuilder.cs`: creación reproducible de la escena y configuración inicial URP.
- `Assets/Editor/CasaMareaModelIntegrator.cs`: integración reproducible de los FBX de estaciones en la escena.
- `ArtSource/CasaMarea/`: fuentes Blender, renders y métricas de Claude.
- `Docs/`: diseño, arquitectura, decisiones, tareas, pruebas y encargos.
- `Packages/manifest.json`: dependencias iniciales. Unity generará `packages-lock.json` al resolverlas.
