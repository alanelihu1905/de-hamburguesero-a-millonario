# Casa Marea — clientes con rig, animaciones y expresiones (`build_clientes_animados.py`)

Los tres clientes (Pescador, Vecina, Joven) con esqueleto de 16 huesos, tres clips y seis expresiones. Mismo diseño y paleta que los clientes estáticos de [`README_CLIENTES_VECINAS.md`](README_CLIENTES_VECINAS.md). Guía de dirección de poses: [`README_POSES.md`](README_POSES.md).

## Estado: ejecutado en Blender 5.2 y verificado por dentro; sin verificar en Unity

- Blender ejecutó `build_clientes_animados.py` (la segunda vez, con llaves intermedias en Caminar) y escribió 12 FBX, `Clientes_Animados.blend`, las vistas y `stats_clientes_animados.json`.
- Los 12 FBX se inspeccionaron leyendo su estructura (`tools/inspeccionar_fbx.py`, sin Blender): huesos, piel, blend shapes, curvas y duración de cada clip coinciden con lo esperado.
- La lógica de las animaciones se comprobó antes con cinemática directa en Python (`tools/precheck_animacion.py`) y la geometría con pesos (`tools/precheck_animados.py`); ambas coincidieron con lo medido en Blender.
- **No está verificado en Unity:** importación, nombre de los clips, orientación, bucle y escala frente al jugador de 1.8 m.

## Archivos

| Archivo | Contenido |
| --- | --- |
| `Assets/_Game/Models/CasaMarea/Animados/SM_Cliente_<Nombre>.fbx` | malla + armadura + 6 blend shapes, sin animación (3 archivos) |
| `…/SM_Cliente_<Nombre>@Idle.fbx`, `@Caminar.fbx`, `@Pedir.fbx` | solo armadura + un clip (9 archivos) |
| `ArtSource/CasaMarea/Clientes_Animados.blend` | fuente con los tres personajes y sus acciones |
| `ArtSource/CasaMarea/rigdata.py` | esqueleto y clips en Python puro (fuente única de las poses) |
| `ArtSource/CasaMarea/Preview/Animados_*.png`, `Expresiones_*.png` | hojas de fotogramas y de expresiones |
| `ArtSource/CasaMarea/stats_clientes_animados.json` | cifras medidas por Blender |

Para regenerar (en el host, desde la raíz del proyecto): `/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_clientes_animados.py`.

## Geometría (medida)

| Cliente | Triángulos (tope 1 000) | Dimensiones X × Y × Z en reposo | Vértices sin peso |
| --- | --- | --- | --- |
| `SM_Cliente_Pescador` | 708 | 0.731 × 0.54 × **1.80** (con sombrero) | 0 |
| `SM_Cliente_Vecina` | 752 | 0.731 × 0.54 × **1.75** | 0 |
| `SM_Cliente_Joven` | 684 | 0.731 × 0.438 × **1.72** | 0 |

Origen en el centro de los pies (z = 0), frente a −Y. Con las mismas opciones de eje que el resto del arte, la raíz llega a Unity con −90° en X; hay que compensarlo igual que en `CasaMareaNeighborhoodIntegrator` (`Quaternion.Euler(0, 180 + giro, 0) * Quaternion.Euler(-90, 0, 0)`). Los tres miden entre 1.72 y 1.80 m: el Pescador, con sombrero, es el que llega a 1.80 m.

Cada pieza de la malla pertenece a un solo hueso con peso 1.0 (apariencia de «piezas rígidas», coherente con el estilo). Las articulaciones usan esferas pequeñas (hombro, codo, rodilla) para tapar los huecos al doblar.

## Esqueleto (16 huesos, idéntico en los tres)

```
root                              (en los pies; sin vértices; sin movimiento de raíz)
└─ pelvis
   ├─ columna ── cabeza
   │  ├─ brazo_D ── antebrazo_D ── mano_D
   │  └─ brazo_I ── antebrazo_I ── mano_I
   ├─ muslo_D ── pantorrilla_D ── pie_D
   └─ muslo_I ── pantorrilla_I ── pie_I
```

«D» es la derecha del personaje (x < 0) y «I» la izquierda (x > 0). Como los tres comparten nombres y posiciones, los clips sirven para los tres. Los 15 huesos con piezas tienen vértices; `root` no. Medidas en `rigdata.py`: columna de la pelvis (z 0.84) a la cabeza (z 1.70), brazos de 0.66 m, muslos de 0.40 m y pantorrillas de 0.36 m.

## Clips (30 fps, medidos en los FBX)

| Clip | Fotogramas | Duración | Bucle | Llaves | Descripción |
| --- | --- | --- | --- | --- | --- |
| **Idle** | 61 (1–61) | 2.000 s | sí | 1, 16, 31, 46, 61 | de pie; respira y se balancea; los pies no se mueven |
| **Caminar** | 33 (1–33) | 1.067 s | sí | cada 2 fotogramas (17 llaves) | ciclo de paso en el sitio; **1.42 m/s** y **0.76 m** de paso |
| **Pedir** | 91 (1–91) | 3.000 s | no | 1, 11, 25, 37, 49, 61, 75, 91 | se inclina, levanta la mano derecha con dos gestos, cabecea; la izquierda al vientre |

- Los fotogramas van de 1 a N+1 porque el último repite el primero (para bucle sin salto, Unity debe cortar el clip en la penúltima o marcar *Loop Pose*).
- **Caminar** sin movimiento de raíz: Unity debe desplazar el objeto a unos 1.4 m/s para que los pies no patinen. La pelvis sube y baja (−4.4 cm en el contacto, ±0.5 cm en el apoyo) para que la suela toque el suelo.
- Cada FBX de clip contiene 17 nodos animados (la armadura y los 16 huesos) con 153 curvas. El nombre interno del *take* es «Scene»; con la convención `Modelo@Clip.fbx` Unity debe nombrar el clip a partir del archivo (sin verificar).

### Contacto con el suelo (medido en las mallas deformadas, todos los fotogramas)

| Clip | Suela más baja | Suela más alta en el punto más bajo de cada fotograma |
| --- | --- | --- |
| Idle | 0.0 mm | 0.0 mm |
| Caminar | −2.6 mm | +1.9 mm |
| Pedir | 0.0 mm | 0.0 mm |

Antes de añadir llaves intermedias, Caminar llegaba a −15 mm. Idle y Pedir no mueven los pies. Altura máxima durante Pedir: 1.82 m (Pescador), por el brazo alzado.

## Expresiones (blend shapes, medidas)

Seis formas de ojos, cejas y boca; todas mueven **solo piezas de la cara** en los tres clientes (comprobado vértice a vértice) y ninguna desplaza más de 3 cm.

| Forma | Vértices movidos | Desplazamiento máximo |
| --- | --- | --- |
| Feliz | 56 | 2.0 cm |
| Triste | 56 | 1.6 cm |
| Sorpresa | 56 | 3.0 cm |
| Enojado | 56 | 2.6 cm |
| Hablar | 24 | 1.8 cm |
| Parpadeo | 16 | 1.6 cm |

En el FBX del modelo hay un `BlendShape` con 6 canales (`Feliz`, `Triste`, `Sorpresa`, `Enojado`, `Hablar`, `Parpadeo`). La cara se compone de dos ojos, dos cejas y tres piezas de boca (centro y dos comisuras). Detalle de uso y tiempos en `README_POSES.md`.

## Uso en Unity (propuesta)

1. Importar `SM_Cliente_<Nombre>.fbx` con *Rig → Animation Type: Generic* y *Root node: root* (o «Armadura_Cliente_…» según cómo aparezca). Crear un Avatar con el Pescador y usarlo en los otros dos con *Copy From Other Avatar*, para compartir clips.
2. Importar los `@Idle`, `@Caminar` y `@Pedir` con el mismo tipo de rig y el mismo Avatar. Marcar *Loop Time* en Idle y Caminar; Pedir sin bucle.
3. Controlador sencillo: **Idle** como estado inicial; **Caminar** cuando el cliente se desplaza; **Pedir** al llegar a la ventanilla y volver a Idle.
4. Expresiones: ajustar `SkinnedMeshRenderer.SetBlendShapeWeight` (0–100) desde código; no hay clips de expresión.
5. Colliders: no incluidos; una cápsula de 0.35 m de radio y 1.8 m de alto.
6. Materiales: usan los mismos colores de la paleta; aún no pasan por la paleta única de `Paleta/` (la armadura y los pesos requieren un paso adicional en `apply_palette.py`).

## Límites

- **Sin verificar en Unity** (ver arriba).
- Estilo de piezas rígidas: las articulaciones se ven bien a 3 m, pero de cerca (menos de 1 m) se aprecian cortes entre piezas al doblar.
- La falda del vestido de la Vecina va ligada a la columna y no se mueve con las piernas; durante Caminar las piernas la atraviesan ligeramente.
- Una sola variación de cada clip; sin transiciones propias (las hace el *Animator*).
- Dedos, pies y cara sin hueso propio (la cara se mueve solo con blend shapes). Si se quiere el rig *Humanoid* de Unity, falta el hueso de cuello y el de hombro; hoy sirve como *Generic*.
- Las poses por hacer (recibir, impaciente, saludo, jackpot, decepción) están descritas en `README_POSES.md` pero todavía no tienen clip.
