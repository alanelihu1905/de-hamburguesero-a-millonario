# Handoff Claude 15 — Rediseño de las casas de Puerto Alba

**Fecha:** 2026-10-11
**Autor:** Claude Code (Sonnet 5.5), rama `ciudad/fase-1-modelo`
**Estado:** modelos y vistas **verificados en Blender**. La importación en Unity y la escena de calles con las casas nuevas **no están verificadas**: Unity estaba bloqueado por otro proceso (ver sección 5).

## 1. Qué cambió

La casa anterior (`SM_Casa_Dos_Plantas`) es una caja con tejado plano y un balcón de panel sólido (`Preview/Casas/SM_Casa_Dos_Plantas_anterior_tres_cuartos.png`). Se añaden tres tipos de casa con volumen, tejado, acceso y detalle:

| Pieza | Tipo | Detalles | Tris (tope) | Dimensiones X × Y × Z (m) |
| --- | --- | --- | --- | --- |
| `SM_Casa_Colonial` | dos plantas, tejado a cuatro aguas | porche con dos columnas, balcón con balaustres y barandilla, chimenea de ladrillo, cornisas, escalones, ventanas con marco, dintel, alféizar y postigos, puerta con marco y tirador, ventanas laterales | 1178 (2500) | 8.60 × 10.50 × 9.62 |
| `SM_Casa_Moderna` | azotea plana, planta alta en voladizo | garaje con puerta seccional, entrada con visera de madera, columna del voladizo, ventanal con travesaños, equipo de azotea, muro bajo con hueco y camino | 436 (2500) | 7.20 × 10.73 × 7.30 |
| `SM_Casa_Cabana` | dos aguas con hastial al frente | hastial de muro con buhardilla redonda, porche con barandal y escalones, chimenea, puerta con montante de vidrio, ventanas con postigos | 756 (2500) | 5.60 × 9.575 × 6.50 |

- Estilo low-poly propio, sin texturas. 1 u = 1 m, origen en el centro de la huella en X y base en z = 0, frente hacia −Y (en Unity, +Z).
- Los nombres de material (`M_Muro_Base`, `M_Tejado`, `M_Cornisa`, `M_Zocalo`, `M_Vidrio`, `M_Toldo`) son los del kit: Unity los sustituye por los de cada zona. Se añaden `M_Ladrillo` y `M_Hierba` (neutros).
- La escala se comprueba con **rangos de diseño**, no con una medida única, porque cada casa tiene salientes (porche, escalones, tejado). Los rangos están en `build_casas.py` (`PIEZAS`).

## 2. Archivos

| Ruta | Contenido |
| --- | --- |
| `ArtSource/PuertoAlba/build_casas.py` | Generador (Blender) de las tres casas, con comprobación de rangos, pivote y presupuesto |
| `ArtSource/PuertoAlba/stats_casas.json` | Triángulos, dimensiones y caja medidos por Blender |
| `Assets/_Game/Models/PuertoAlba/Casas/SM_Casa_*.fbx` | Tres FBX. **Sin `.meta` todavía**: Unity los genera al importar |
| `ArtSource/PuertoAlba/Preview/Casas/*_tres_cuartos.png` | Vistas de las tres casas y de la casa anterior para comparar |
| `ArtSource/PuertoAlba/validar_calles.py` | Validador sin Unity del trazado de calles |
| `ArtSource/PuertoAlba/build_kit_ciudad.py`, `build_zonas_hitos.py` | Ahora guardan también la caja medida de cada pieza (`bbox_min_XYZ_m`, `bbox_max_XYZ_m`) |
| `ArtSource/PuertoAlba/stats_kit.json`, `stats_zonas_hitos.json` | Regenerados con esas cajas |

## 3. Pruebas ejecutadas

- `build_casas.py`: `LISTO_CASAS 3 piezas` (código 0); rangos de escala, base en z = 0 y presupuesto comprobados dentro del script.
- Vistas revisadas a mano: se vieron las tres casas y la anterior. Corregí dos defectos que vi ahí: el hastial de la cabaña quedaba tapado por el tejado (ahora el tejado son solo las dos vertientes y el muro triangular queda a la vista) y los laterales del balcón colonial eran paneles sólidos (ahora son balaustres).
- `build_kit_ciudad.py` y `build_zonas_hitos.py`: regenerados (código 0) con las cajas medidas.
- `validar_kit.py`: `FALLOS (0)`.
- `validar_calles.py`: **296 comprobaciones OK, 0 FALLO**, sobre la tabla de calles **con las casas nuevas** (la del `PuertoAlbaCallesBuilder.cs` sin commitear, sección 4). Lee la tabla de calles del constructor de C#, reproduce la colocación con las cajas medidas por Blender y comprueba que ningún edificio invade la avenida ni el cruce, que no hay solapes entre piezas y que la plaza de entrada del casino queda libre.
  - **Prueba de mutación:** con el apoyo contra la acera desactivado, el validador detecta justo las mismas seis invasiones que Unity encontró en su primera ejecución (norte 1, norte 3, sur 2, sur 3, sur 6 y sur 7), más la cabaña. Eso indica que reproduce la geometría de Unity; no sustituye a Unity.

## 4. Lo que no está commiteado

El cambio en `Assets/Editor/PuertoAlbaCallesBuilder.cs` (casas nuevas en la tabla de calles, prefabs `PF_Casa_Colonial`, `PF_Casa_Moderna`, `PF_Casa_Cabana`, casino colocado en el lado este de la calle cruzada) está escrito pero **no compilado ni ejecutado en Unity**. No lo commiteé para no subir un script de Editor sin compilar.

## 5. Bloqueo y pasos siguientes

**Bloqueo:** el proceso Unity 24848 mantiene abierto el proyecto desde las 06:52 y Unity no admite dos instancias. No lo cerré porque no sé si es el editor del usuario.

Cuando se libere:
1. `PuertoAlbaCallesBuilder.Build` (importa los FBX de `Casas/`, genera sus `.meta`, crea los tres prefabs y reconstruye la escena de calles).
2. `PuertoAlbaCallesBuilder.Verify` (tamaño de los prefabs, invasión de la vía, frente a la calle, plaza del casino y capturas).
3. Revisar las capturas de calle y del casino; commitear el C# y los `.meta` solo si todo pasa.

## 6. Límites

- Las casas son geometría low-poly sin texturas; el detalle es de volumen (postigos, balaustres, escalones), no de superficie.
- Las colisiones previstas son cajas del volumen principal; el porche y el balcón no las tienen.
- No hay prueba en Unity de importación, escala ni colisiones de las casas nuevas.
- No se han colocado en la ciudad real (`PuertoAlba.unity`), solo en la escena de calles de vista previa, y todavía sin verificar.
