# Handoff Claude 09 — Kit visual modular de la ciudad (Puerto Alba)

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Encargo:** kit low-poly propio para fachadas residenciales, tienda de barrio y gasolinera, con variación de materiales y siluetas para las ocho zonas. Casino central, permanente y visible.
**Base técnica:** Unity 6 (6000.0.84f1), URP, C#, PC Windows. Blender 5.2 LTS para generar los modelos.

## 0. Estado (qué está probado y qué no)

| Punto | Estado |
| --- | --- |
| Generación en Blender 5.2 (`build_kit_ciudad.py`) | **Verificado:** el script termina con código 0 y muestra `LISTO`. |
| Dimensiones y pivote de las nueve piezas | **Verificado en Blender:** cada medida coincide con la tabla de la sección 2 (comprobación dentro del script). |
| Presupuesto de triángulos | **Verificado en Blender:** todas las piezas quedan por debajo de su tope (`stats_kit.json`). |
| Vistas de previsualización | **Verificado visualmente:** `Preview/Kit_residencial_tienda.png` y `Preview/Kit_gasolinera.png`. |
| Importación en Unity 6000.0.84f1 | **Verificado en batch:** `Assets/Editor/PuertoAlbaKitImportCheck.cs` importa los siete FBX sin errores y genera sus `.meta`. Informe: `ArtSource/PuertoAlba/unity_import_check.txt`; marcador `TYCOON_KIT_IMPORT_OK`. |
| Orientación del frente en Unity | **Verificado por medida:** en Unity, las salientes (toldo, visera, pistola) quedan en +Z, que es el frente de Unity. Es una medición de límites, no una revisión visual en escena. |
| Escala y base en Unity | **Verificado por medida:** escala de importador 1; tamaños y base en y = 0 coinciden con Blender (sección 2). |
| Colocación en la ciudad (cuadrícula 21×21) | **No hecho.** No he encontrado `CityGridLayout` ni `CityGridBlockout` en esta rama ni en el árbol de trabajo; no los he tocado. Ver sección 6. |
| Zonas aplicadas en Unity | **No hecho.** `zonas_variacion.json` describe la variación; nada la aplica todavía. |

## 1. Carpetas y archivos

| Ruta | Contenido |
| --- | --- |
| `ArtSource/PuertoAlba/build_kit_ciudad.py` | Generador. Regenera todo desde cero. |
| `ArtSource/PuertoAlba/Kit_Ciudad.blend` | Fuente con el kit alineado para las vistas. |
| `ArtSource/PuertoAlba/stats_kit.json` | Triángulos, dimensiones, pivote y materiales medidos por Blender. |
| `ArtSource/PuertoAlba/Preview/*.png` | Vistas frontales del kit. |
| `ArtSource/PuertoAlba/validar_kit.py` | Validador sin Blender ni Unity: FBX reales, metas, presupuesto, medidas, referencias y materiales. |
| `ArtSource/PuertoAlba/render_variantes_zonas.py` | Vistas por zona (hoja de contacto). |
| `ArtSource/PuertoAlba/zonas_variacion.json` | Colores y piezas preferentes por zona (ocho zonas). |
| `ArtSource/PuertoAlba/LICENCIA.md` | Licencia propia: obra del proyecto, sin material de terceros. |
| `Assets/_Game/Models/PuertoAlba/SM_*.fbx` (+ `.meta`) | Siete piezas importables, con el pivote en el origen. |
| `Assets/Editor/PuertoAlbaKitImportCheck.cs` | Chequeo de importación en batch (escala, base, frente). |
| `ArtSource/PuertoAlba/unity_import_check.txt` | Informe del último chequeo en Unity. |

Para regenerar, desde la raíz del proyecto, en el host (Blender necesita GPU):

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/PuertoAlba/build_kit_ciudad.py
```

Convenciones: 1 u = 1 m, Z arriba, origen en el centro de la huella sobre el suelo (z = 0), frente hacia −Y. Los FBX no llevan texturas incrustadas; las piezas usan materiales de color plano.

## 2. Kit y medidas

Medidas en metros, X × Y × Z, según `stats_kit.json`. Las medidas incluyen salientes (toldos, viseras, pistola del surtidor).

| Pieza | Descripción | Dimensiones | Triángulos (tope) |
| --- | --- | --- | --- |
| `SM_Fachada_Residencial` | Tramo de 4 m, una planta, dos ventanas con postigos, zócalo y cornisa | 4.20 × 0.52 × 3.38 | 204 (400) |
| `SM_Puerta_Residencial` | Puerta de 1.1 m con marco, capialzado y visera | 1.70 × 0.58 × 2.65 | 108 (300) |
| `SM_Tienda_Barrio` | Tienda de 6 × 4 m: escaparate, puerta lateral, toldo, rótulo y losa de azotea | 6.20 × 4.95 × 4.45 | 144 (700) |
| `SM_Gasolinera_Marquesina` | Techo de 12 × 8 m a 5 m, cuatro pilares y banda luminosa apagada | 12.00 × 8.00 × 5.50 | 72 (300) |
| `SM_Gasolinera_Surtidor` | Surtidor con pantalla, manguera y pistola | 0.90 × 0.65 × 1.55 | 72 (300) |
| `SM_Gasolinera_Caseta` | Caseta de cajas de 3 × 2.5 m con escaparate y techo | 3.30 × 2.80 × 3.00 | 84 (300) |
| `SM_Gasolinera_Letrero` | Letrero de poste, cara inferior a 3.0 m | 2.40 × 0.20 × 4.20 | 48 (200) |
| `SM_Casa_Dos_Plantas` | Casa de dos plantas: puerta y dos ventanas por planta, balcón con barandilla y tejado | 5.30 × 7.05 × 6.45 | 468 (500) |
| `SM_Hito_Casino` | Hito propio del casino, visible desde la calle: basamento, dos torres con remate y rótulo ámbar entre ellas | 12.00 × 4.43 × 10.30 | 120 (500) |

Presupuesto: el kit completo suma 1320 triángulos (nueve piezas). Cada pieza tiene su tope en `build_kit_ciudad.py`.

La banda luminosa de la marquesina (`M_Marquesina_Luz`) tiene emisión con intensidad 0, como los materiales de la casa de hamburguesas. Unity la encenderá en el estado de noche.

## 3. Materiales

Paleta base neutra en `build_kit_ciudad.py` (`MATS`): muro crema, zócalo azul grisáceo, cornisa terracota, marco blanco, madera, puerta verde azulada, vidrio turquesa, toldo rojo, metal oscuro, surtidor rojo, pantalla, marquesina blanca, luz, rótulo ciruela con texto ámbar y tejado.

Las ocho zonas reemplazan esos colores según `zonas_variacion.json`. La idea es que cada barrio se lea de lejos por color, sin cambiar la geometría.

## 4. Siluetas y variación por zona

Los ids son los de `ZoneId` en `CityMap.cs` y del plan `Docs/PLAN_CIUDAD_PUERTO_ALBA.md`.

| Zona (id) | Silueta dominante | Color dominante | Piezas preferentes |
| --- | --- | --- | --- |
| Casa Marea (`CasaMarea`) | Fachada de dos plantas, puerta con visera | Crema calizo y terracota | Fachada, puerta |
| Mercado del Sol (`MercadoDelSol`) | Tiendas bajas con toldo | Mostaza, mango y verde bosque | Tienda |
| Muelle Farol (`MuelleFarol`) | Caseta de servicio y tienda sobria | Azul marino y gris | Caseta, tienda |
| Centro del Faro (`CentroDelFaro`) | Fachada alta y rótulo | Cobalto y blanco titanio | Fachada, letrero |
| Talleres Brisa (`TalleresBrisa`) | Surtidor, caseta y letrero | Gris acero y rojo óxido | Surtidor, caseta, letrero |
| Paseo Arena (`PaseoArena`) | Fachada baja y tienda con toldo | Esmeralda y buganvilia | Fachada, tienda |
| Colina Cobalto (`ColinaCobalto`) | Viviendas con puerta y tienda de barrio | Azul cobalto y crema | Fachada, puerta, tienda |
| Estación Sur (`EstacionSur`) | Marquesina con surtidores y letrero | Gris carbón y amarillo | Marquesina, surtidor, caseta, letrero |

Esta fachada residencial es genérica. La fachada propia de Casa Marea ya existe en `Assets/_Game/Models/CasaMarea/SM_Fachada_CasaMarea.fbx` y no la reemplaza este kit.

## 4 bis. Vista previa de la variación por zona (verificado en Blender)

`ArtSource/PuertoAlba/render_variantes_zonas.py` cambia solo los colores de los materiales con `zonas_variacion.json` y renderiza cada zona en dos vistas. La geometría no cambia.

- `ArtSource/PuertoAlba/Preview/Zonas/hoja_zonas.png`: hoja de contacto, una fila por zona en el orden del JSON (Casa Marea, Mercado del Sol, Muelle Farol, Centro del Faro, Talleres Brisa, Paseo Arena, Colina Cobalto, Estación Sur). Columna izquierda, piezas residenciales; derecha, gasolinera.
- Vistas individuales: `Preview/Zonas/<id>_residencial_tienda.png` y `<id>_gasolinera.png`.
- Verificado: el script termina con `LISTO_VARIANTES 8 zonas` y las ocho zonas muestran colores distintos.
- No verificado: que Unity aplique estos colores (ver sección 9).

## 4 ter. Piezas nuevas, materiales por zona y validación (esta tanda)

- **Casas:** `SM_Casa_Dos_Plantas` cubre el encargo de «casas» junto con la fachada de tramo.
- **Casino:** `SM_Hito_Casino` es un hito propio, de geometría simple y sin texturas. **No reproduce** el edificio del casino de `Assets/_Game/Models/CasaMarea` ni `Assets/_Game/Art/Casino`, y no sustituye al casino real. Su función es marcar la posición del casino en la propuesta: visible, permanente, sin tapar nada del kit.
- **Materiales por zona:** `Assets/_Game/Models/PuertoAlba/Zonas/Zona_<id>_<campo>.mat`, 48 materiales (8 zonas × 6 campos: muro, zócalo, acento, tejado, toldo, vidrio). Los crea `PuertoAlbaKitImportCheck` a partir de `zonas_variacion.json`. Cada uno tiene solo el color base; ninguna escena los usa todavía.
- **Validación sin Unity ni Blender:** `ArtSource/PuertoAlba/validar_kit.py`. Comprueba FBX binarios reales (no punteros de Git LFS), `.meta` con GUID único, presupuesto de triángulos, medidas de Blender frente a las de Unity con los ejes intercambiados, referencias del JSON contra `ZoneId` de `CityMap.cs`, y los 48 materiales.
- **Resultado:** `OK (242)`, `FALLOS (0)`, código de salida 0. Log de Unity: `TYCOON_KIT_IMPORT_OK: 9 FBX importados y 48 materiales de zona creados`.
- **Vista previa:** `Preview/Zonas/hoja_zonas.png` ahora tiene tres columnas por zona: residencial y tienda, gasolinera, y casa con hito de casino.

**NO VERIFICADO (marcador explícito):**
- Aspecto de las piezas dentro de una escena de Unity. La importación y las medidas sí están verificadas; la mirada no.
- Colocación en la cuadrícula 21×21: no hay `CityGridLayout` en esta rama.
- Aplicación de los materiales por zona a instancias en escena.
- Ubicación definitiva del casino (sigue abierta entre el plan, que lo pone en Casa Marea, y la propuesta 08, que lo pone en la zona 8).

## 4 quater. Prefabs y escena de vista previa en Unity (verificado en batch)

**Qué existe**
- Prefabs ensamblados en `Assets/_Game/Models/PuertoAlba/Prefabs/` con colisión en el raíz: `PF_Casa_Dos_Plantas`, `PF_Tienda_Barrio`, `PF_Gasolinera_Completa` (canopy, dos surtidores, caseta y letrero, con colisión de pilares, surtidores, caseta y postes), `PF_Hito_Casino` (basamento y dos torres).
- Escena de vista previa `Assets/_Game/Scenes/PuertoAlba_KitPreview.unity`. No está en la lista de escenas de build. Contiene luz, suelo de 200×80 m, la fila frontal (hito centrado en x = 0, z = 0; casa, tienda y gasolinera), la fila trasera con muestras de las ocho zonas (casa y tienda, con los materiales `Zona_<id>_*`) y dos cámaras de captura marcadas como «no build».
- Todos los frentes miran a −Z, hacia la calle y la cámara; la calle está en z entre −30 y 0.

**Marcadores (Unity 6000.0.84f1, batch)**
- `KIT_PREVIEW_BUILD_OK: prefabs y escena Assets/_Game/Scenes/PuertoAlba_KitPreview.unity` (código 0).
- `TYCOON_KIT_PREVIEW_OK: prefabs, escena, muestras de zona y capturas comprobados.` (código 0), con 117 comprobaciones OK y 0 FALLO en `ArtSource/PuertoAlba/unity_preview_check.txt`.
- Comprobaciones: cada prefab tiene malla y colisión, base en y = 0 y tamaño igual al de Blender (con ejes intercambiados); la escena abre, no está en la build, tiene el hito centrado, las casas y el hito son instancias de prefab y la cámara existe; cada muestra de zona usa solo sus materiales, y entre las ocho zonas aparecen los 48 materiales.

**Capturas (renders reales, no mockups)**
- `ArtSource/PuertoAlba/Preview/Unity/Kit_hito_casino_unity.png`: hito centrado, con la casa de dos plantas a la izquierda y la tienda a la derecha. Se renderiza dos veces antes de leer el buffer, porque el primer fotograma salía casi blanco por carga de shaders.
- `ArtSource/PuertoAlba/Preview/Unity/Kit_vista_general_unity.png`: fila completa con las ocho muestras de zona, el hito, la tienda y la gasolinera.

**NO VERIFICADO (marcador explícito):**
- Iluminación y materiales en build (solo se ha visto la escena de vista previa).
- Colisiones probadas caminando: solo existen; no hay prueba de movimiento del jugador.
- Colocación en la cuadrícula 21×21: no hay `CityGridLayout` en esta rama, y esta escena no es integración.
- Ubicación final del casino: sigue abierta entre el plan (Casa Marea) y la propuesta 08 (zona 8). El hito es un marcador, no el edificio real.
- Materiales de zona aplicados en la ciudad real.

## 5. Casino central, permanente y visible

- El kit **no contiene piezas del casino** y ninguna pieza supera 5.5 m de altura, para no competir con su letrero.
- Regla para la colocación: ninguna pieza del kit se coloca sobre el terreno del casino ni delante de su fachada.
- **Decisión abierta:** el plan (`PLAN_CIUDAD_PUERTO_ALBA.md`) y `CityMap.cs` ponen el casino en Casa Marea (`CasinoZone = CasaMarea`). La propuesta visual `08_PROPUESTA_VISUAL_PUERTO_ALBA.md` lo pone en la zona 8 (Distrito del Ocio). No he elegido entre ambas. Claude o Codex deben confirmar cuál es la ubicación del casino antes de colocar el kit alrededor.

## 6. Cuadrícula 21×21 (pendiente)

El encargo menciona una cuadrícula de 21×21. En esta rama no encuentro `CityGridLayout` ni `CityGridBlockout`, así que **no conozco el tamaño de celda**. El kit se diseñó con módulos de 4 m (fachada), 6 m (tienda) y 12 m (marquesina). Si la celda es de 4 m, encajan las fachadas (una celda), la tienda (1.5 celdas de ancho, por confirmar) y la marquesina (tres celdas por dos). Hay que confirmarlo con la implementación de la cuadrícula antes de colocar piezas.

## 7. Decisiones que tomé y por qué

- **Piezas separadas, no un edificio completo.** Así una zona puede combinar fachadas, tiendas y puertas sin modelar cada manzana.
- **Pivote en la base y centrado en X.** No centré Y, porque el toldo y la visera sobresalen hacia el frente; forzar el centro en Y dejaría la base descentrada.
- **Sin alféizar en la puerta.** El primer intento dejaba un listón 0.07 m bajo el suelo; lo quité para que la base quede en z = 0.
- **Vistas separadas por grupo.** Las piezas fuera del grupo quedan ocultas en el render; antes se superponían en el origen.

## 8. Límites conocidos

- Las piezas son geometría básica: cubos y planos con color. Sin detalle de molduras ni texturas.
- La fachada residencial es un solo tipo de tramo. La variedad debe venir de combinar tramos, colores y piezas preferentes; no hay tramos alternativos todavía.
- La banda de la marquesina y los rótulos son planos; su texto lo pondrá Unity.
- Sin colisionadores ni LOD. Los colisionadores los definirá la integración en Unity.
- No hay animación: el surtidor es estático.

## 9. Lo que sigue (para Claude y Codex)

1. Confirmar la ubicación del casino (sección 5) y el tamaño de celda de la cuadrícula (sección 6).
2. Revisión visual en escena del kit en Unity (el chequeo actual mide límites, no mira la escena).
3. Aplicar `zonas_variacion.json` mediante instancias de material, en una pieza de prueba antes de todo el kit.

Ningún archivo de `CityGrid*` ni escena compartida se ha modificado.
