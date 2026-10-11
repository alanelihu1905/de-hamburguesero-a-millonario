# Assets externos (modelos 3D de terceros)

Manifiesto de todo el material de terceros incorporado al proyecto. Criterio: solo licencias que permiten uso comercial e integración sin condiciones (CC0). **No se descargó ni se usó ningún asset de Schedule I.** Versión máquina: `Assets/_Game/Models/External/MANIFEST.json` (incluye SHA-256 de cada zip y de cada archivo).

Fecha de descarga: 2026-10-10. Los dos archivos se descargaron desde kenney.nl, que es el sitio del propio autor.

## Paquetes

| Paquete | Autor | Licencia | Página | Descarga directa | SHA-256 del zip |
| --- | --- | --- | --- | --- | --- |
| Furniture Kit 2.0 | Kenney (www.kenney.nl) | CC0 1.0 Universal (dominio público) | https://kenney.nl/assets/furniture-kit | https://kenney.nl/media/pages/assets/furniture-kit/440e0608a4-1677580847/kenney_furniture-kit.zip (5 130 729 bytes) | `e67652d0932cee41683f74711c03d3e192a2af9979ef8e6b237711f5482d46b0` |
| Food Kit 2.0 | Kenney (www.kenney.nl) | CC0 1.0 Universal (dominio público) | https://kenney.nl/assets/food-kit | https://kenney.nl/media/pages/assets/food-kit/83086fa91c-1719418518/kenney_food-kit.zip (4 606 270 bytes) | `cdad90853682499b94c9fda2f87678b24bfd8f3264e0ed323f6b6a27fd7c6f6f` |

Licencia: https://creativecommons.org/publicdomain/zero/1.0/ (copia del `License.txt` de cada paquete dentro de su carpeta). La atribución no es obligatoria; se registra aquí por transparencia.

## Modelos incorporados y usados (22 modelos distintos, 42 instancias en el casino)

Ruta: `Assets/_Game/Models/External/<paquete>/<modelo>.fbx`.

**Kenney_FurnitureKit** (12 modelos, 27 instancias): `lampSquareTable` ×2 (lámparas de mesa), `lampWall` ×6 (lámparas de pared), `plantSmall1` ×1, `plantSmall2` ×1, `pottedPlant` ×2 (plantas), `rugRound` ×1 (alfombra redonda), `sideTable` ×1 (mesa auxiliar), `speakerSmall` ×2 (bocinas), `stoolBar` ×6 (taburetes de las máquinas), `stoolBarSquare` ×2 (taburetes de la recepción), `tableRound` ×2 (mesas de la zona de estar), `trashcan` ×1 (bote).

**Kenney_FoodKit** (10 modelos, 15 instancias, más `Textures/colormap.png`): `bottle-ketchup` ×1, `burger-cheese` ×1, `cocktail` ×3, `cup` ×2, `cupcake` ×1, `glass` ×1, `mug` ×2, `plate` ×1, `soda-can` ×2, `soda-glass` ×1.

(Se copió también `kitchenBar` del Furniture Kit para valorarlo y se eliminó del proyecto por no usarse.)

## Adaptación a este juego (URP, escala, orientación)

- Cada FBX se instancia con `CasinoArt.Place` (`Assets/Editor/CasinoArtHelpers.cs`): elige la orientación que lo deja de pie (o plano en alfombras y platos), lo escala a una altura o ancho objetivo en metros, lo apoya sobre la superficie y le quita los colliders del FBX.
- Materiales: los del FBX se sustituyen por materiales URP/Lit propios en `Assets/_Game/Materials/Casino/` (`Kenney_FurnitureKit_*`, `Kenney_FoodKit_*`), con el color del material original o la textura `colormap.png` del Food Kit.
- Escala objetivo en metros (altura salvo indicación): taburetes 0.72–0.74; mesas 0.74; lámparas de mesa 0.42; lámparas de pared 0.34; bocinas 0.5; botes 0.55; plantas 0.55 a 1.15; alfombra 3.4 de ancho; cóctel 0.17; taza 0.08; vaso 0.13; lata 0.12; hamburguesa 0.18 de ancho; plato 0.26 de ancho.
- No se modificó la geometría original; solo se escaló y se reasignó el material.

## Material propio (no es de terceros)

Los símbolos de los rodillos, el piso, el adoquín y los 10 sonidos del casino los genera `ArtSource/CasaMarea/casino_assets/generar_assets.py` (Python puro). La tipografía de los carteles es la fuente integrada de Unity (`LegacyRuntime.ttf`).
