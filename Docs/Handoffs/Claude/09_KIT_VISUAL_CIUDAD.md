# Handoff Claude 09 — Kit visual modular de la ciudad (Puerto Alba)

**Fecha:** 2026-10-11
**Autor:** Claude Code (Haiku 5.5), rama `ciudad/fase-1-modelo`
**Encargo:** kit low-poly propio para fachadas residenciales, tienda de barrio y gasolinera, con variación de materiales y siluetas para las ocho zonas. Casino central, permanente y visible.
**Base técnica:** Unity 6 (6000.0.84f1), URP, C#, PC Windows. Blender 5.2 LTS para generar los modelos.

## 0. Estado (qué está probado y qué no)

| Punto | Estado |
| --- | --- |
| Generación en Blender 5.2 (`build_kit_ciudad.py`) | **Verificado:** el script termina con código 0 y muestra `LISTO`. |
| Dimensiones y pivote de las siete piezas | **Verificado en Blender:** cada medida coincide con la tabla de la sección 2 (comprobación dentro del script). |
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

Presupuesto: el kit completo suma 732 triángulos. Cada pieza tiene su tope en `build_kit_ciudad.py`.

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
