# Puerto Alba: cuadrícula urbana

Puerto Alba usa una cuadrícula determinista de **21 × 21 cuadras**: diez cuadras desde el centro en cada dirección. Cada cuadra ocupa 84 m. Centros extremos quedan a 1,680 m; calles perimetrales amplían la huella total a **1,776 × 1,776 m**.

## Contenido de bloque inicial

- Ocho zonas reciben cuadras por proximidad a sus centros de actividad.
- Casino ocupa siempre la cuadra `(0, 0)`. Abre al iniciar la escena y no depende de una misión.
- Dos gasolineras permanecen ubicadas en sectores distintos.
- Mercado, muelle, restaurante, patio industrial y parque reciben cuadras propias.
- Cuadras restantes usan fachadas simples. Un subconjunto fijo tiene puerta visible: `E` toca y crea un visitante de prueba. `E` otra vez lo retira.
- Calles forman una retícula uniforme. Ciudad tiene un suelo con colisión y fachadas con colisión.

## Abrir blockout en Unity

1. Abre proyecto con Unity `6000.0.84f1`.
2. Ejecuta `Tools > Tycoon3D > Create Puerto Alba 21x21 Grid Scene`.
3. Abre `Assets/_Game/Scenes/PuertoAlba_Grid.unity` y presiona Play.

La escena nueva no reemplaza `BarrioResidencial.unity` ni altera Build Settings. El blockout genera primitivas en Play Mode; no es contenido artístico final. `CityGridLayout` conserva datos independientes para sustituir geometría por prefabs y para streaming futuro.

## Verificación

`dotnet run --project Tests/CitySmoke` comprueba 441 cuadras, radio de diez, ocho zonas, casino central, dos gasolineras y puertas NPC deterministas.

La escena y su interacción todavía requieren marcador Play Mode de Unity antes de declararse verificadas. El humo .NET no ejecuta Unity ni comprueba render, colisión o uso de `E` dentro del editor.
