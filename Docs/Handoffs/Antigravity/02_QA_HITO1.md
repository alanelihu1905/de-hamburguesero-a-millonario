# Informe de QA Independiente — Hito 1 (Prototipo Jugable)

- **Fecha:** 2026-10-09
- **Rol:** QA Independiente (Google Antigravity)
- **Entorno Evaluado:** macOS arm64, Unity 6 (6000.0.84f1), .NET 8.0, Universal Render Pipeline (URP 17.0.3)
- **Documentos de Referencia:**
  - [`README.md`](../../../README.md)
  - [`Docs/Handoffs/Antigravity/01_QA_PROTOTIPO.md`](../../../Docs/Handoffs/Antigravity/01_QA_PROTOTIPO.md)
  - [`Docs/TEST_REPORT.md`](../../../Docs/TEST_REPORT.md)
  - [`Docs/PROJECT_STATE.md`](../../../Docs/PROJECT_STATE.md)
  - [`Docs/DECISIONS.md`](../../../Docs/DECISIONS.md)

---

## 1. Resumen Ejecutivo del Hito

El objetivo del primer hito era establecer la base técnica y el ciclo esencial de juego:
1. Apertura limpia de la escena prototipo [`BarrioResidencial.unity`](../../../Assets/_Game/Scenes/BarrioResidencial.unity).
2. Movimiento en primera persona (WASD y ratón) con colisiones sólidas.
3. Detección por raycast (3 m) y retículo de interacción.
4. Ciclo transaccional completo con la tecla **E**: Comprar ingredientes ($15) → Cocinar hamburguesa → Vender hamburguesa ($50).
5. Gestión consistente del saldo inicial ($100 → $135 → $170) y control de ventas.
6. Reinicio limpio del estado de la escena con la tecla **R**.
7. Ausencia de errores de compilación o fallos bloqueantes en el flujo principal.

**Dictamen General:** **APROBADO CON OBSERVACIONES (Pass with Minor Defects & Risks)**.
El núcleo de la mecánica, la arquitectura desacoplada de componentes, las pruebas unitarias de economía y la prueba automatizada Play Mode demuestran que el bucle económico y funcional opera de acuerdo con las especificaciones. No se detectaron fallos críticos bloqueantes (*crash*, excepciones no controladas ni corrupción de saldo) en el ciclo nominal.

---

## 2. Matriz de Criterios de Aceptación

| Criterio | Estado | Tipo de Evidencia | Observación |
| :--- | :---: | :--- | :--- |
| **1. Abrir y Cargar Escena** | **APROBADO** | Batch smoke + análisis de escena | Escena [`BarrioResidencial.unity`](../../../Assets/_Game/Scenes/BarrioResidencial.unity) configurada en [`EditorBuildSettings`](../../../Assets/Editor/PrototypeSceneBuilder.cs#L135). Manifest JSON válido. Incidencia de `GenerateHLSL` resuelta previamente. |
| **2. Caminar (WASD + Ratón + Colisiones)** | **APROBADO** | Análisis de código + Play Mode smoke | [`FirstPersonController.cs`](../../../Assets/_Game/Scripts/Player/FirstPersonController.cs) gestiona `CharacterController` con velocidad 4.5 m/s, gravedad -20 m/s² y rotación axial con límites de pitch (-85° a +85°). Paredes y suelo poseen `BoxCollider`. |
| **3. Raycast (Alcance 3m + Retículo)** | **APROBADO** | Play Mode smoke + análisis de código | [`PlayerInteractor.cs`](../../../Assets/_Game/Scripts/Interaction/PlayerInteractor.cs) dispara `Physics.Raycast` de 3 m desde la cámara ignorando triggers. El smoke test confirmó impacto en las 3 estaciones. Retículo visual en [`PrototypeHud.cs`](../../../Assets/_Game/Scripts/UI/PrototypeHud.cs#L46). |
| **4. Tecla E (Comprar, Cocinar, Vender)** | **APROBADO** | Smoke test Play Mode + Unit tests | [`BurgerStation.cs`](../../../Assets/_Game/Scripts/Restaurant/BurgerStation.cs) delega a [`BurgerLoop.cs`](../../../Assets/_Game/Scripts/Restaurant/BurgerLoop.cs). Máquina de estados (`NeedIngredients` → `NeedCooking` → `ReadyToSell`) respeta el orden estricto y bloquea acciones fuera de turno. |
| **5. Saldo y Monedero** | **APROBADO** | `dotnet run` (8/8 checks) + Play Mode | Saldo inicial $100. Compra descuenta $15 ($85). Cocina mantiene $85. Venta añade $50 ($135, 1 venta). Segundo ciclo alcanza $170 y 2 ventas. Rechaza saldo negativo y gastos duplicados sin fondos. |
| **6. Tecla R (Reiniciar Escena)** | **APROBADO** | Análisis de código + Play Mode | [`PrototypeHud.cs`](../../../Assets/_Game/Scripts/UI/PrototypeHud.cs#L26) recarga la escena activa. La sesión local no es persistente (`DontDestroyOnLoad`), por lo que [`Wallet`](../../../Assets/_Game/Scripts/Economy/Wallet.cs) vuelve a $100 y [`BurgerLoop`](../../../Assets/_Game/Scripts/Restaurant/BurgerLoop.cs) a 0 ventas. |
| **7. Ausencia de Errores Bloqueantes** | **APROBADO** | Compilación C# + Ejecución batch | No hay errores de compilación ni excepciones no capturadas en el ciclo nominal. |

---

## 3. Defectos Observados (Bugs Reproducibles)

### [DEF-01] Prompts de interacción en pantalla son estáticos y no reactivos al estado del ciclo
- **Prioridad:** Media (P3 - Usabilidad / UX)
- **Componentes involucrados:** [`BurgerStation.cs`](../../../Assets/_Game/Scripts/Restaurant/BurgerStation.cs#L13-L24), [`PlayerInteractor.cs`](../../../Assets/_Game/Scripts/Interaction/PlayerInteractor.cs#L30), [`PrototypeHud.cs`](../../../Assets/_Game/Scripts/UI/PrototypeHud.cs#L44)
- **Pasos para reproducir:**
  1. Iniciar la escena con saldo $100 (estado inicial: `NeedIngredients`).
  2. Acercarse a la **Parrilla** y apuntar el retículo hacia ella.
  3. Observar el texto de interacción central inferior.
  4. Presionar la tecla **E**.
- **Resultado observado:**
  - El retículo muestra `E - Cocinar hamburguesa` a pesar de que el jugador aún no ha comprado ingredientes.
  - Al presionar **E**, la parrilla no cocina y la caja superior del HUD emite el mensaje `Primero compra ingredientes`.
  - Lo mismo ocurre si se mira al **Punto de venta** antes de cocinar (`E - Vender hamburguesa ($50)` permanece visible).
- **Impacto:** Aunque [`BurgerLoop`](../../../Assets/_Game/Scripts/Restaurant/BurgerLoop.cs) protege adecuadamente la lógica de negocio y no permite transacciones inválidas, la interfaz promete una acción que el sistema inmediatamente rechaza, generando confusión en el usuario.
- **Sugerencia de corrección:** Que [`BurgerStation.Prompt`](../../../Assets/_Game/Scripts/Restaurant/BurgerStation.cs#L13) consulte el estado actual de `burgerLoop.Step` y muestre un texto atenuado o indicativo (ej. `"Parrilla (Requiere ingredientes)"` o no mostrar la indicación de acción cuando no está habilitada).

---

### [DEF-02] Liberación del cursor con tecla Escape no permite rebloqueo mediante clic de ratón
- **Prioridad:** Media (P3 - Usabilidad / Control)
- **Componentes involucrados:** [`FirstPersonController.cs`](../../../Assets/_Game/Scripts/Player/FirstPersonController.cs#L27-L32)
- **Pasos para reproducir:**
  1. En Play Mode, pulsar **Escape** para liberar el cursor y moverlo libremente por la pantalla.
  2. Hacer clic izquierdo sobre la ventana de juego (Game View) para retomar el control.
- **Resultado observado:**
  - El cursor permanece libre y visible; el movimiento de la cámara y del jugador continúa completamente congelado (debido a la guarda `if (Cursor.lockState != CursorLockMode.Locked) return;` en la línea 34).
  - El jugador está obligado a recordar presionar nuevamente la tecla **Escape** para volver a fijar el ratón.
- **Impacto:** En videojuegos en primera persona en PC, la convención estándar es que hacer clic en el viewport rebloquea el cursor inmediatamente.
- **Sugerencia de corrección:** Añadir comprobación en `Update()`: si el cursor está desbloqueado y se recibe `Input.GetMouseButtonDown(0)`, fijar `Cursor.lockState = CursorLockMode.Locked` y `Cursor.visible = false`.

---

### [DEF-03] Recarga de escena con `buildIndex` vulnerable a escenas no registradas
- **Prioridad:** Baja (P4 - Robustez)
- **Componentes involucrados:** [`PrototypeHud.cs`](../../../Assets/_Game/Scripts/UI/PrototypeHud.cs#L26)
- **Código actual:**
  ```csharp
  if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
  ```
- **Riesgo / Comportamiento:** Si un diseñador o desarrollador abre la escena desde la carpeta de assets sin que esté registrada en los Build Settings de la sesión, `GetActiveScene().buildIndex` devuelve `-1`, lo que provoca que `SceneManager.LoadScene(-1)` falle lanzando `ArgumentException`.
- **Sugerencia de corrección:** Usar `SceneManager.LoadScene(SceneManager.GetActiveScene().name)` o el path activo.

---

## 4. Riesgos No Verificados

Los siguientes aspectos representan riesgos técnicos y arquitectónicos que **no han sido verificados experimentalmente** debido a restricciones de plataforma o etapas pendientes de integración:

### [RSK-01] Ausencia de módulo y prueba de Build Standalone para PC Windows
- **Prioridad:** Alta (P2)
- **Detalle:** La regla permanente del proyecto estipula que PC Windows es la base técnica obligatoria. El entorno de desarrollo actual opera sobre macOS arm64. No se ha confirmado la instalación del módulo `Windows Build Support (Mono/IL2CPP)` ni se ha compilado un binario ejecutable (`.exe`).
- **Consecuencia potencial:** Incompatibilidades en shaders URP, diferencias en manejo de rutas o problemas en compilación nativa Windows al momento de empaquetar el juego.

---

### [RSK-02] Integración pendiente de los modelos 3D de Claude (Casa Marea)
- **Prioridad:** Alta (P2)
- **Detalle:** Claude ha completado los modelos de las 3 estaciones en formato Blender y exportado a FBX/GLB en [`Assets/_Game/Models/CasaMarea/`](../../../Assets/_Game/Models/CasaMarea/). Sin embargo, dichos modelos carecen de archivos `.meta` y aún no han sido importados ni ensamblados en la jerarquía de la escena.
- **Consecuencia potencial:** Cuando Codex sustituya los cubos provisionales por las mallas definitivas, los colliders o las escalas locales podrían alterar las distancias de raycast (3 m) o desalinear la colisión de interacción.

---

### [RSK-03] Coexistencia de Legacy Input Manager y New Input System
- **Prioridad:** Media (P3)
- **Detalle:** La inicialización de Unity registró en log: `Input System module state changed to: Initialized`, pero los scripts ([`FirstPersonController.cs`](../../../Assets/_Game/Scripts/Player/FirstPersonController.cs) y [`PlayerInteractor.cs`](../../../Assets/_Game/Scripts/Interaction/PlayerInteractor.cs)) consumen la API clásica `UnityEngine.Input`.
- **Consecuencia potencial:** Si `ProjectSettings/InputManager.asset` o el Player Setting de Active Input Handling se modifica a "Input System Package (New)", todos los scripts que llaman a `Input.GetAxisRaw` o `Input.GetKeyDown` dejarán de recibir eventos de entrada.

---

### [RSK-04] Ausencia de límites perimetrales en la calle peatonal (Caída al infinito)
- **Prioridad:** Baja (P4)
- **Detalle:** La plataforma exterior `Calle peatonal` mide 18 x 10 m y carece de colisionadores invisibles en sus bordes exteriores.
- **Consecuencia potencial:** El jugador puede caminar hacia los extremos del barrio provisional y precipitarse al vacío indefinidamente por efecto de la aceleración gravitacional (`gravity = -20f`), sin existir volumen de respawn ni muerte por caída.

---

## 5. Recomendaciones para el Siguiente Hito (Codex + Claude)

1. **A Codex (Integración técnica):**
   - Importar los modelos FBX generados por Claude en [`Assets/_Game/Models/CasaMarea/`](../../../Assets/_Game/Models/CasaMarea/).
   - Conservar los `BoxCollider` actuales como volúmenes de interacción estables o ajustar colliders compuestos precisos para evitar fallos en el raycast de [`PlayerInteractor.cs`](../../../Assets/_Game/Scripts/Interaction/PlayerInteractor.cs).
   - Resolver los defectos menores [DEF-01] (prompts contextuales) y [DEF-02] (rebloqueo con clic).
2. **A Claude (Dirección artística):**
   - Las métricas de polígonos registradas en `stats.json` (Huacal: 628 tris, Plancha: 332 tris, Punto de venta: 416 tris) se encuentran holgadamente dentro de los topes presupuestados. Proceder con el modelado de la arquitectura de la casa colonial.
3. **Al Equipo de Infraestructura:**
   - Habilitar el módulo de compilación para Windows Standalone en Unity Hub para cumplir la validación de build requerida en [`AGENTS.md`](../../../AGENTS.md).
