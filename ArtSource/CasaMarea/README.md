# Casa Marea — estaciones del hito 1 (low-poly)

Tres modelos originales para el ciclo comprar → cocinar → vender, hechos con `build_estaciones.py` (Blender 5.2 LTS). Especificación: `Docs/Handoffs/Claude/04_CASA_MAREA_PLANO_Y_LOOK.md`, sección 3. Son geometría con colores planos, sin texturas ni UV pintadas.

> **Tanda de props del barrio** (banco, maceta, farol, cubeta, cartel): ver [`README_PROPS.md`](README_PROPS.md). Script `build_props_barrio.py`, ejecutado y verificado.

> **Interior de la cocina** (refrigerador, hamburguesa modular, tabla, campana, repisa, lámpara, mostrador): ver [`README_INTERIOR.md`](README_INTERIOR.md). Script `build_interior_cocina.py`, ejecutado y verificado.

> **Props, tanda 3** (banco de callejón, cajas de pescado, bolardo, barril, farol de pared, maceta de patio): [`README_PROPS_2.md`](README_PROPS_2.md). Script `build_props_barrio_2.py`, ejecutado y verificado.

> **Paleta única** (25 materiales → 2): [`README_PALETA.md`](README_PALETA.md). Paleta generada y verificada en `Palette/`; `tools/apply_palette.py` ejecutado y verificado: 30 FBX en `Paleta/` sin regresiones y con una sola textura compartida.

> **Clientes y casas vecinas** (tres clientes estáticos, dos variantes de casa vecina): [`README_CLIENTES_VECINAS.md`](README_CLIENTES_VECINAS.md). Script `build_clientes_vecinas.py`, ejecutado y verificado.

> **Clientes con rig, animaciones y expresiones** (Pescador, Vecina y Joven: 16 huesos, Idle, Caminar y Pedir, 6 blend shapes): [`README_ANIMADOS.md`](README_ANIMADOS.md); guía de poses en [`README_POSES.md`](README_POSES.md). Script `build_clientes_animados.py`, ejecutado y verificado en Blender; sin verificar en Unity.

## Archivos

| Qué | Dónde |
| --- | --- |
| Fuentes `.blend` | `ArtSource/CasaMarea/Huacal_Ingredientes.blend`, `Plancha_Tubo.blend`, `PuntoVenta_Feston.blend`, `Lineup_Estaciones.blend` |
| Script generador | `ArtSource/CasaMarea/build_estaciones.py` |
| **FBX (uso directo en Unity)** | `Assets/_Game/Models/CasaMarea/SM_Huacal_Ingredientes.fbx`, `SM_Plancha_Tubo.fbx`, `SM_PuntoVenta_Feston.fbx` |
| GLB (conservado; Unity no lo importa sin el paquete glTFast) | mismos nombres con `.glb` |
| Capturas | `ArtSource/CasaMarea/Preview/` (frontal y tres cuartos por estación, más `Lineup_3m_*`) |
| Cifras medidas por el script | `ArtSource/CasaMarea/stats.json` |

## Cifras verificadas

Medidas en metros, ancho × fondo × alto, tomadas de `stats.json` tras regenerar con el script corregido y contrastadas con los accesores de cada `.glb` (leídos aparte, sin Blender). Ambas fuentes coinciden.

| Estación | Triángulos (tope) | Medido | Objetivo | Materiales |
| --- | --- | --- | --- | --- |
| Huacal de ingredientes | 628 (900) | 0.900 × 0.550 × 0.500 | 0.90 × 0.55 × 0.50 | 6 |
| Plancha con tubo | 332 (1 100) | 1.100 × 0.600 × 0.750 | 1.10 × 0.60 × 0.75 | 4 |
| Punto de venta con festón | 416 (1 000) | 0.900 × 0.548 × 0.800 | 0.90 × 0.55 × 0.80 | 5 |

Total de las tres: 1 376 triángulos. Origen en el centro de la base (z = 0) y frente hacia −Y en Blender. La desviación del punto de venta en fondo (−0.002 m) es irrelevante.

Las dos desviaciones de la primera versión (huacal 0.471 m de alto, plancha 0.62 m de fondo) quedaron corregidas.

## Materiales

Color plano, sin texturas. Colores de la paleta Casa Marea más tres de comida.

| Material | Color | Usado en |
| --- | --- | --- |
| `M_Chile` | `#E4572E` | huacal (tablillas), perillas y banda de la plancha |
| `M_Caoba` | `#8A5A3C` | huacal (patines, borde, lecho), campanilla, caja de dinero |
| `M_Crema` | `#F7EEDD` | pictograma del huacal, festón, tickets, filete |
| `M_Turquesa` | `#2BA6A0` | punto de venta (base y retablo) |
| `M_Mango` | `#F4C95D` | campanilla y pestillo; emisión apagada |
| `M_Metal_Plancha` | `#3E4A50` | cuerpo y tubo de la plancha, riel |
| `M_Plancha_Superficie` | `#2B3438` | superficie de cocción y sombrerete |
| `M_Brasa` | `#5A2A1E` | ranura frontal de la plancha; emisión apagada |
| `M_Pan` / `M_Lechuga` / `M_Jitomate` | `#D99A4E` / `#6FA83F` / `#C0261F` | contenido del huacal |

11 materiales distintos. El documento 04 fijó un tope de 12 para toda la escena, así que estos tres modelos usan casi todo el presupuesto. Los tres de comida se pueden fundir en un atlas más adelante.

## Uso en Unity

1. Usar los `.fbx` de `Assets/_Game/Models/CasaMarea/`. El `.glb` no se importa sin el paquete glTFast.
2. Escala 1 u = 1 m. Poner el collider y `BurgerStation` en la raíz, con las medidas de la tabla.
3. **Emisión:** `M_Brasa` y `M_Mango` llevan el color de emisión definido, pero con intensidad 0. El GLB no exporta emisión con intensidad 0. En Unity hay que activar la emisión de estos dos materiales para animar la brasa de la plancha y el destello de la campanilla (estados del documento 04, sección 3.4).
4. Posiciones propuestas en el mostrador (documento 04): x = −3.3 (huacal), 0 (plancha) y +3.3 (venta), z = 1.75, **base en y = 0.85**.
5. **Orientación sin verificar en Unity:** el frente mira a −Y en Blender y se exportó con el eje Y arriba. Si el frente queda hacia +Z en la escena, girar el prefab 180° en Y para que mire al jugador.

## Silueta a 3 m

`Preview/Lineup_3m_silueta.png` muestra las tres estaciones juntas, a 3 m, con una cámara de FOV 60° y los ojos a 0.80 m sobre la base. Se distinguen por el contorno: el huacal es bajo y ancho con bultos, la plancha es una losa con un solo tubo, y el punto de venta es alto con festón. `Lineup_3m_grises.png` es la misma vista en grises.

Limitación: el huacal y la plancha son ambos bajos y anchos, y se diferencian sobre todo por el tubo y los bultos. A 3 m de frente, sin ver el tubo, pesa más el color que la forma. El festón del punto de venta tiene poca altura (7.5 cm) y se lee como una cresta, no como una forma ondulada marcada.

## Límites de esta entrega

- **Sin verificar en Unity:** la importación, la orientación, la escala y la lectura dentro de la escena.
- Previsualizaciones con luz de estudio; no representan la iluminación final URP.
- Geometría con formas simplificadas, sin texturas pintadas a mano, sin UV desplegadas ni mapas normales. No hay animaciones.
- No incluye la hamburguesa terminada sobre la plancha ni el humo del documento 04; son activos aparte.
- El pictograma del huacal es geometría, no un dibujo.

## Regenerar

Desde la raíz del proyecto, en el host (Blender necesita GPU; no corre dentro del sandbox de Claude):

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_estaciones.py
```

Sobrescribe los `.blend`, `.fbx`, `.glb`, las capturas y `stats.json`. Si ya hay un `.meta` de Unity, conserva los GUID.

---

# Fachada exterior (`build_fachada.py`)

**Estado:** ejecutado en el host (Blender salió con código 0). Los dos FBX, el `.blend`, `stats_fachada.json` y las vistas existen. Contenido de los FBX comprobado por nombres de objeto y de material; cifras de `stats_fachada.json`, medidas por el script. El script se volvió a ejecutar con las tres correcciones de «Defectos encontrados» y quedaron verificadas.

## Qué genera

| Archivo | Contenido |
| --- | --- |
| `Assets/_Game/Models/CasaMarea/SM_Fachada_CasaMarea.fbx` | fachada sur con portón libre, ventanas falsas con reja y postigos, zócalo, pilastras, cornisa, azotea con pretil, chimenea, rótulo, banderín de la esquina SE y banqueta. Incluye el objeto `SM_Letrero_Texto` (2 triángulos) |
| `Assets/_Game/Models/CasaMarea/SM_Despacho_Este.fbx` | muro este con la ventanilla de despacho, marco, repisa exterior y toldo con festón |
| `ArtSource/CasaMarea/Fachada_CasaMarea.blend` | fuente, con calle y vecinas solo para las vistas |
| `ArtSource/CasaMarea/Preview/` | `Fachada_postal`, `Fachada_tres_cuartos`, `Fachada_porton_detalle`, `Despacho_callejon`, `Despacho_toldo_alto` |
| `ArtSource/CasaMarea/stats_fachada.json` | triángulos, dimensiones, materiales y comprobación de la puerta |

Para regenerar, en el host y desde la raíz del proyecto:

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/CasaMarea/build_fachada.py
```

## Diseño

- Sigue el documento 04, secciones 2.3 y 2.5, con las cotas de esa tabla.
- Los modelos están construidos **en las coordenadas de la escena** (Blender x, y, z = escena x, z, y), así que el prefab se coloca en (0, 0, 0). Sustituye a las piezas provisionales `Frente izquierda`, `Frente derecha`, `Dintel puerta`, `Letrero provisional`, `Pared derecha`, `Techo` y a la calle frontal; Codex decide cuáles ocultar.
- **Puerta libre:** el vano mide 2.0 m de ancho por 2.3 m de alto y no tiene geometría. Las hojas del portón están abiertas hacia la banqueta. El script lo comprueba con rayos y escribe la altura y el ancho libres en `stats_fachada.json`. El resultado de la comprobación está en «Cifras verificadas».
- Paleta: arena `#E8C4A0` en muros, turquesa `#2BA6A0` en zócalo, postigos y rótulo, crema `#F7EEDD` en molduras, caoba `#8A5A3C` en el portón, chile y mango solo como acentos. Es la regla 60/30/10 del documento 04.
- Materiales: 9 en la fachada y el despacho (`M_Cal_Arena`, `M_Turquesa`, `M_Crema`, `M_Caoba`, `M_Herreria`, `M_Chile`, `M_Mango`, `M_Piedra_Caliza`, `M_Metal_Plancha`) más `M_Letrero` para el plano del texto. Los colores son planos, sin texturas.
- Triángulos y dimensiones: ver «Cifras verificadas».

## Cifras verificadas

Salen de `stats_fachada.json`. Los triángulos coinciden con una cuenta independiente hecha pieza por pieza (1 376 y 332).

| Modelo | Triángulos (presupuesto doc. 04) | Extensión X × Y × Z (Blender, m) | Materiales |
| --- | --- | --- | --- |
| `SM_Fachada_CasaMarea` | 1 376 (2 500 + 300) | 18.0 × 7.4 × 4.45 (x −9…9, y −3.2…4.2, z −0.15…4.3) | 9 |
| `SM_Letrero_Texto` | 2 | 1.9 × 0.0 × 0.34 | 1 (`M_Letrero`) |
| `SM_Despacho_Este` | 332 (700) | 1.81 × 6.2 × 3.1 (x 4.9…6.71, y −2.1…4.1) | 5 |

- La extensión de la fachada la domina la banqueta (18 m, x ±9). La casa en sí ocupa x ±5.2.
- Ambos modelos usan el sistema de coordenadas de la escena (Blender x, y, z = escena x, z, y), por lo que el prefab va en (0, 0, 0).
- El FBX de la fachada contiene dos objetos, `SM_Fachada_CasaMarea` y `SM_Letrero_Texto`, con sus materiales (comprobado leyendo los nombres dentro del archivo).

### Puerta libre

Con el muestreo fino (1 cm en altura, 0.5 cm en ancho), el script midió: **0 golpes** de rayo, altura libre **2.29 m** y ancho libre **1.99 m**. Coincide con el vano de construcción (2.0 × 2.3 m) dentro de la resolución del muestreo. Las hojas abiertas quedan fuera del vano (x ≥ 1.09 m).

### Vistas revisadas

- `Fachada_postal`: la fachada se lee de inmediato y el portón queda abierto. Rótulo, rejas, postigos y banderín coinciden con el plano.
- `Fachada_tres_cuartos`: confirma el volumen, la chimenea sobre la azotea y el banderín en la esquina.
- `Fachada_porton_detalle`: el vano está despejado y el rótulo libra el dintel.
- `Despacho_callejon`: ventanilla, marco, repisa y postes correctos. El callejón queda en sombra, como se esperaba del sol del suroeste.

## Defectos encontrados (los tres corregidos y verificados tras regenerar)

1. **Pilastras 2 cm por delante de la cornisa.** Medían 3.0 m y la cornisa empieza en 2.9 m. Ahora miden 2.9 m; en `Fachada_porton_detalle` ya no aparecen las muescas sobre el rótulo.
2. **Vista del callejón engañosa.** La cámara veía el toldo de canto. La vista nueva `Despacho_toldo_alto` lo muestra desde arriba: las 8 franjas turquesa y crema, el festón triangular, el riel y los dos postes están bien.
3. **Puerta con muestreo grueso:** resuelto; ver «Puerta libre».
4. **Contraste bajo en la vista previa del callejón** (sombra de la casa más relleno lila). Sigue igual: es de la luz de la vista previa, no del modelo; se evaluará con la iluminación URP en Unity.

Los triángulos no cambiaron con la regeneración (1 376 y 332), ya que las correcciones no añaden ni quitan piezas.

## Decisiones que se apartan del documento 04

1. **Rótulo** adelantado a y = −2.20 (marco de 0.20 m de fondo) en lugar de −2.15, para que las pilastras no lo tapen. Su alto es 0.60 m, centrado en z = 2.62, con lo que no baja del dintel (2.3 m).
2. **El nombre del negocio no está modelado.** El centro del rótulo queda libre y `SM_Letrero_Texto` es el plano donde Codex puede aplicar la textura cuando exista el nombre (`[NOMBRE_NEGOCIO]`). Las letras no llevan geometría.
3. **Banderín sin flecha.** La placa es perpendicular a la fachada y por eso no puede mostrar una flecha hacia +X; solo lleva el icono de hamburguesa en ambas caras, y la esquina del callejón sirve de indicación.
4. **Cierre de esquina:** los muros frontales llegan hasta x = ±5.1 y las pilastras de esquina están en x = ±4.95. La escena provisional deja un hueco de 10 cm en esa esquina.
5. **Arco rebajado:** no se modela; el documento 04 lo reserva para el modelo final.

## Límites previstos

- Sin colliders: Codex los añade (la puerta debe quedar libre; las hojas del portón y la placa del banderín pueden llevar collider).
- Sin UV pensadas para pintar ni texturas; el humo de la chimenea y la animación del portón son aparte.
- Orientación en Unity sin verificar, igual que en las estaciones.
- Blender deja copias `*.blend1` junto a las fuentes. Conviene ignorarlas en Git; es decisión de Codex, no la toqué.
