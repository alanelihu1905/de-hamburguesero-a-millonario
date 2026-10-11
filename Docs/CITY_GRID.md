# Puerto Alba: cuadrícula urbana

Puerto Alba usa una cuadrícula determinista de **21 × 21 cuadras**: diez cuadras desde Casa Marea en cada dirección. Cada cuadra ocupa 84 m. Centros extremos quedan a 1,680 m; calles perimetrales amplían la huella total a **1,776 × 1,776 m**.

## Barrios y lectura de ciudad

- Ocho zonas usan los `ZoneId` de `CityMap` y reciben cuadras por proximidad a sus centros de actividad.
- Casino ocupa siempre la cuadra inicial Casa Marea `(0, 0)`, junto al spawn; no depende de un desbloqueo.
- Las seis máquinas jugables siguen en `BarrioResidencial.unity`. Esta escena solo construye exterior de blockout; no contiene todavía máquinas ni interior.
- Dos gasolineras permanecen ubicadas en sectores distintos.
- Ocho zonas ocupan sectores propios y reciben rótulos de orientación.
- Centro del Faro tiene dos torres de 58 m y 43 m, ventanas, plaza y monumento.
- Colina Cobalto mezcla vivienda de varias alturas; Mercado del Sol suma fachadas comerciales y toldos.
- Muelle Farol tiene bodegas, grúa, contenedores y muelle; Talleres Brisa suma naves, puertas industriales, tuberías y chimeneas.
- Estación Sur tiene terminal, andenes y bahías de autobús. Paseo Arena incluye parque, caminos, palmeras, banca y quiosco.
- Dos avenidas anchas cruzan la cuadrícula; tienen marcas viales, cruces peatonales, faroles, palmeras y 16 vehículos en circulación.
- Casino permanece abierto desde la cuadra inicial; dos gasolineras y fachadas de barrio conectan los puntos de interés.
- Un subconjunto fijo de edificios sencillos tiene puerta visible: `E` toca y crea un visitante de prueba. `E` otra vez lo retira.
- La escena genera el terreno con colisión. Edificios principales y locales tienen colisión; los adornos son geometría sencilla sin collider.

## Abrir blockout en Unity

1. Abre proyecto con Unity `6000.0.84f1`.
2. Ejecuta `Tools > Tycoon3D > Create Puerto Alba 21x21 Grid Scene`.
3. Abre `Assets/_Game/Scenes/PuertoAlba_Grid.unity` y presiona Play.

La escena nueva no reemplaza `BarrioResidencial.unity` ni altera Build Settings. El blockout genera primitivas en Play Mode; no es contenido artístico final. `CityGridLayout` usa las zonas actuales y conserva coordenadas de cuadra para sustituir geometría por prefabs y preparar streaming futuro.

## Verificación

`dotnet run --project Tests/CitySmoke` comprueba grafo y ocupación de zonas, casino inicial, 441 cuadras, radio diez, zonas, puntos de interés y puertas deterministas (72 comprobaciones aprobadas en esta revisión).

El aspecto actual sigue siendo un blockout procedural de primitivas; la variedad de siluetas, señales, calles y vida urbana fija la escala y composición para sustituir después por arte modular. La escena y su interacción requieren marcador Play Mode de Unity antes de declararse verificadas. El humo .NET no ejecuta Unity ni comprueba render, colisión o uso de `E` dentro del editor.
