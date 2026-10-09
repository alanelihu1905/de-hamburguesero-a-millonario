# Arquitectura inicial

## Componentes existentes

- `FirstPersonController`: movimiento con `CharacterController`, mirada de ratón y gravedad.
- `PlayerInteractor`: raycast desde cámara; invoca `IInteractable` al pulsar E.
- `Wallet`: saldo y operaciones de cobro/gasto con validación de importes.
- `BurgerLoop`: estados `NeedIngredients → NeedCooking → ReadyToSell → NeedIngredients`.
- `BurgerStation`: adapta las tres estaciones físicas al bucle de negocio.
- `PrototypeHud`: información provisional y reinicio de escena.
- `PrototypeSceneBuilder`: herramienta de editor que genera escena, materiales y configuración inicial si no existen.

## Límite de autoridad

El `BurgerLoop` vive en una sesión local durante esta fase. Al integrar Netcode, un servicio del anfitrión será el único escritor de saldo, inventario compartido y ventas. Las estaciones enviarán comandos validados con identidad de jugador, estado esperado y protección contra repetición. Las vistas recibirán instantáneas o eventos de estado. Ninguna cifra del cliente podrá usarse como fuente de autoridad.

## Próximos módulos

Inventario y recetas como datos configurables; pedidos/NPC; misiones; calendario; guardado con versión de esquema; red social; impresión; casino; robótica. Cada módulo tendrá contratos y pruebas propias antes de acoplarse al mundo abierto.

## Estado de renderizado

`manifest.json` solicita URP 17.0.3. El generador intenta crear un asset URP con Universal Renderer y asignarlo. Esto debe verificarse en un editor real. Si el paquete/API difiere, ajustar el generador después de inspeccionar la consola; no asumir que está validado.

