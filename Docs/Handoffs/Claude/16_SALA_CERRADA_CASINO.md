# Handoff Claude 16 — Sala cerrada del Casino FOX (DEF-01 y DEF-03 del QA)

**Fecha:** 2026-10-11
**Autor:** Claude Code (Sonnet 5.5), rama `ciudad/fase-1-modelo`
**Origen:** `Docs/Handoffs/Antigravity/04_QA_CASINO_Y_PUERTO_ALBA.md`, DEF-01 (ausencia de envolvente arquitectónica) y DEF-03 (sin taburetes ni mostrador de caja).

> **ARTE, NO MECÁNICA.** No hay apuestas, cobros, RNG ni lógica de juego. El mostrador de cambio es una forma estática con su letrero; ningún rodillo gira. Tampoco cambia la ubicación urbana del casino (handoff 11, pendiente) ni las escenas `PuertoAlba_CasinoIntegracionPreview` y `PuertoAlba_CallesPreview`.

## 1. Qué se resolvió

| Defecto | Solución |
| --- | --- |
| DEF-01 | Kit modular que cierra la sala: muros de 4 m con zócalo de madera y rieles dorados, 4 columnas de esquina, techo con vigas oscuras y rosetones dorados, y dos puertas de vaivén con letrero **ENTRADA** (verde) y **SALIDA** (rojo) en bloques, sin TMP ni fuentes. |
| DEF-03 | Mostrador de cambio con ventanilla de vidrio, ranura y letrero **CAMBIO**, en la esquina de la entrada; **6 taburetes** de 0.69 m, uno delante de cada tragamonedas. |

**Decisión de diseño:** el QA habla de encerrar 35 × 35 m. Esa losa era solo el suelo de la vista previa. La sala cerrada mide **20 × 20 m**, que es lo que ocupa el mobiliario (bancos a x = ±6, ruleta y blackjack a z = 4, jackpot a z = 9); no hay 35 m útiles que llenar. Si se quiere otra planta, cambia la tabla de módulos en `PuertoAlbaCasinoSalaBuilder.cs`.

## 2. Piezas (Blender → FBX)

Tris = triángulos (tope). Dimensiones X × Y × Z en Blender, metros. Todas con base en z = 0, centradas en X, cara interior hacia −Y (en Unity, +Z).

| Pieza | Tris (tope) | Dimensiones |
| --- | --- | --- |
| `SM_Sala_Muro` | 72 (400) | 4.00 × 0.40 × 5.00 |
| `SM_Sala_Esquina` | 48 (300) | 0.90 × 0.90 × 5.00 |
| `SM_Sala_Puerta_Entrada` | 1092 (1500) | 4.00 × 1.53 × 5.00 |
| `SM_Sala_Puerta_Salida` | 852 (1500) | 4.00 × 1.53 × 5.00 |
| `SM_Sala_Techo` | 72 (400) | 4.00 × 4.00 × 0.40 |
| `SM_Mostrador_Cambio` | 924 (1500) | 3.20 × 1.00 × 2.46 |
| `SM_Taburete` | 236 (400) | 0.43 × 0.44 × 0.69 |

- **Techo:** su origen z = 0 es la cara inferior de las vigas; en la sala se coloca a 4.6 m, así que el techo queda a 5.0 m, igual que los muros.
- **Puertas:** abertura de 3.0 × 3.2 m, marco dorado, y dos hojas de vaivén abiertas 80° hacia el interior. **Las hojas no tienen colisión**, para no estrechar el paso.
- Los materiales `M_Casino_Rojo`, `M_Casino_Oro`, `M_Fieltro` y `M_Madera` son los del kit del casino: Unity los sustituye por la variante rojo o dorado.

## 3. Archivos

| Ruta | Contenido |
| --- | --- |
| `ArtSource/PuertoAlba/Casino/build_casino_sala.py` | Generador de Blender (rangos de medida, pivote y presupuesto comprobados dentro del script) |
| `ArtSource/PuertoAlba/Casino/stats_sala.json` | Triángulos, dimensiones y cajas medidos por Blender |
| `ArtSource/PuertoAlba/Preview/Casino_sala/*_tres_cuartos.png` | Vistas de las siete piezas |
| `Assets/_Game/Models/PuertoAlba/Casino/Sala/SM_*.fbx` (+ `.meta`) | Siete FBX |
| `Assets/_Game/Models/PuertoAlba/Casino/Sala/PF_*.prefab` (+ `.meta`) | Siete prefabs con colisión simple |
| `Assets/Editor/PuertoAlbaCasinoSalaBuilder.cs` | Constructor de prefabs y de la escena, y verificación (`Build`, `Verify`) |
| `Assets/Editor/PuertoAlbaCasinoSalaPlaySmoke.cs` | Prueba de Play Mode con física real |
| `Assets/_Game/Scenes/PuertoAlba_CasinoSalaPreview.unity` | Escena de vista previa, fuera de la build |
| `ArtSource/PuertoAlba/Casino/unity_sala_check.txt`, `unity_sala_play_check.txt` | Informes de las pruebas |
| `ArtSource/PuertoAlba/Preview/Unity/Sala_*.png` | Capturas reales de Unity |

## 4. Pruebas y marcadores (Unity 6000.0.84f1, batch)

- Blender: `LISTO_SALA 7 piezas` (código 0).
- `CASINO_SALA_BUILD_OK` (código 0).
- `TYCOON_CASINO_SALA_PREVIEW_OK`: **65 OK, 0 FALLO**, consola sin errores. Entre otras comprobaciones:
  - triángulos de Unity iguales a los de Blender en las siete piezas, y tamaño de cada prefab igual al medido;
  - **recinto cerrado**: 0 huecos inesperados en el contorno (rayos cada 0.5 m) y techo cerrado en una rejilla de 49 puntos;
  - las aberturas de las puertas existen, y un cuerpo de 1.80 m × 0.60 m pasa por ambas con `CapsuleCast`; un muro lo bloquea en x = 0;
  - pasillo libre de x −4.5 a 4.5 y de z −8 a 8.8; letrero JACKPOT dentro del encuadre y sin nada que lo tape;
  - 6 taburetes de 0.69 m a 0.6–1.2 m de su tragamonedas; mostrador de cambio a menos de 8 m de la entrada y de frente a la sala;
  - mobiliario sin solapes con muros, puertas, esquinas ni techo (0);
  - letreros **ENTRADA** y **SALIDA** legibles en la captura (≈ 20 % de píxeles de letra sobre su tablero).
- `TYCOON_CASINO_SALA_PLAY_OK`: **8 OK, 0 FALLO, 0 errores de consola**, con un `CharacterController` de 1.80 m × 0.60 m: entra por ENTRADA, recorre el pasillo hasta el jackpot, sale por SALIDA, y lo detienen el muro frontal, un taburete con su tragamonedas y el mostrador de cambio.

## 5. Capturas (revisadas)

- `Sala_frontal_unity.png`: desde la entrada, JACKPOT al fondo con el pasillo libre, bancos rojo y dorado con sus taburetes, y el techo con vigas.
- `Sala_general_unity.png`: vista desde arriba con el techo oculto: recinto cerrado y las dos puertas en el muro frontal.
- `Sala_entrada_unity.png`, `Sala_salida_unity.png`: cada letrero sobre su puerta.
- `Sala_mostrador_unity.png`: mostrador de cambio en la esquina de la entrada.

## 6. Defectos que se corrigieron en esta tanda (míos)

1. **Letreros ilegibles en la primera verificación:** la sala estaba demasiado oscura (las letras blancas salían grises) y mi criterio buscaba blanco absoluto. Subí la luz ambiente plana y las luces de las puertas, y el criterio mide contraste (claro y poco saturado).
2. **Las puertas abrían al vacío (hallazgo del Play Mode):** sin suelo fuera de las puertas, el cuerpo de prueba chocaba con el borde de la losa y no podía entrar. Se añadió una acera exterior (`Acera exterior (no build)`, z −19…−10). Es el mismo defecto que el QA describe como «mira directamente al vacío».
3. Un error de compilación mío (`Transform` frente a `GameObject`) que corregí antes de la primera ejecución completa.

## 7. Límites

- **Arte, no mecánica**: nada gira, cobra ni se apuesta; el mostrador no cambia fichas.
- **Hojas de las puertas estáticas**: están abiertas 80° y sin animación; no se abren ni se cierran por sí solas.
- **Iluminación provisional**: luz ambiente plana y luces puntuales «no build», sin hornear ni sondas. El rosetón central del techo se ve sobreexpuesto bajo la luz puntual de la puerta.
- **Colisión de las puertas**: solo jambas y dintel; las hojas, los marcos dorados y las vigas del techo no tienen colisión. El techo es una losa fina.
- **No integrado**: la sala no está conectada con la calle de Puerto Alba ni con el casino del barrio; no hay teletransporte ni transición. La ubicación urbana del casino sigue pendiente (handoff 11).
- **No tocado**: DEF-02 (colisión de las mesas bajas ante `stepOffset`) y DEF-04 (ubicación). Los taburetes son un modelo propio; no se usó el taburete de Kenney.
- **Play Mode en batch**: solo prueba contactos y recorrido, no manejo con teclado/ratón ni rendimiento.

## 8. Cómo se reproduce

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/Casino/build_casino_sala.py
U=/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity
$U -batchmode -nographics -projectPath . -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoSalaBuilder.Build
$U -batchmode -projectPath . -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoSalaBuilder.Verify
$U -batchmode -nographics -projectPath . -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoSalaPlaySmoke.Run
```

Los dos primeros, sin `-quit`, y el último nunca con `-quit` (terminaría antes de entrar a Play Mode). Marcadores esperados: `CASINO_SALA_BUILD_OK`, `TYCOON_CASINO_SALA_PREVIEW_OK`, `TYCOON_CASINO_SALA_PLAY_OK`.
