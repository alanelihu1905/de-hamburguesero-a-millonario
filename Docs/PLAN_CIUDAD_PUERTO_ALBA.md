# Puerto Alba: plan de ciudad jugable

**Estado:** propuesta de diseño; ninguna zona nueva está implementada ni verificada. Base técnica: Unity 6 (6000.0.84f1), C#, URP y PC Windows. El prototipo comprobado contiene el barrio Casa Marea, venta de hamburguesas y Casino FOX. El casino permanece abierto desde el inicio, sin compra ni misión previa. El tercer negocio es un hito por definir y probar antes de escalar; este plan propone mensajería de utilería ficticia como opción, sin declararla existente.

## Objetivo y forma de la ciudad

Puerto Alba es una ciudad costera ficticia de **ocho zonas compactas y conectadas**. Cada zona añade una decisión económica, una rutina visible y al menos un destino útil. Primero se construye un circuito de tres negocios en Casa Marea; luego se abren conexiones. La expansión debe sentirse en rutas, empleados, propiedades y actividad, no solo en superficie vacía. Todo nombre, arte, diálogo y marca será original.

La ciudad usa calles legibles: una avenida central enlaza centro, mercado, Casa Marea y estación; un anillo secundario une muelle, talleres, paseo y colina. Cada acceso muestra nombre de zona y dos puntos de referencia. Caminar sigue siendo viable; transporte ahorra tiempo y sirve para llevar carga. Mapa y señales muestran solo destinos descubiertos. Ningún distrito bloquea la entrada al Casino FOX.

| Zona | Propósito jugable | Negocios comprables o arrendables | NPC y rutina visible | Encargos y conexión |
| --- | --- | --- | --- | --- |
| **1. Casa Marea** | Inicio, tutorial práctico y retorno seguro. | Puesto de hamburguesas; local pequeño para el tercer negocio tras validar su ciclo. Casino FOX accesible siempre; su propiedad puede ser una meta administrativa posterior, nunca un requisito para entrar. | Clientes hacen fila, paseante recorre la calle, cajera atiende. | Primera venta y primer encargo corto; conecta con mercado y centro. |
| **2. Mercado del Sol** | Abastecimiento y comparación de precios. | Almacén de ingredientes, puesto de bebidas y cocina de preparación. | Proveedores descargan a horas fijas; compradores recorren puestos; un repartidor sale con pedidos. | Recoger insumos, surtir locales y atender pedidos del mercado; conecta con Casa Marea y muelle. |
| **3. Muelle Farol** | Recepción de mercancía y reparto de mayor volumen. | Depósito, cafetería de trabajadores y local de paquetería ficticia. | Operarios cambian turno, caminan entre almacén y muelle; vehículos hacen paradas simuladas. | Lotes con ventana de entrega y capacidad de carga; conecta con mercado y talleres. |
| **4. Centro del Faro** | Administración, contratos y clientela de oficina. | Restaurante de mesas, oficina de gestión y quiosco. | Empleados llegan por la mañana, salen a comer y vuelven; peatones usan cruces. | Contratos de almuerzo, contratación y gestión de varias sucursales; conecta con Casa Marea y estación. |
| **5. Talleres Brisa** | Mejoras, mantenimiento y logística. | Taller de vehículos, obrador de equipamiento y pequeño almacén. | Técnicos trabajan por turnos; mensajeros salen con cargas; persianas cierran al acabar jornada. | Reparaciones ficticias, mejoras de capacidad y entregas de equipo; conecta con muelle y estación. |
| **6. Paseo Arena** | Demanda nocturna y entretenimiento. | Heladería, cafetería de paseo y sala de eventos. | Familias y turistas recorren el paseo de día; público de eventos llega de noche. | Picos de demanda, catering y montaje de eventos; conecta con centro y colina. |
| **7. Colina Cobalto** | Vivienda, propiedades y servicios de proximidad. | Local de barrio, vivienda operativa y cocina para reparto. | Residentes salen al trabajo, vuelven al anochecer y visitan tiendas cercanas. | Pedidos a domicilio, alquiler y mejoras de propiedad; conecta con paseo y estación. |
| **8. Estación Sur** | Intercambio de rutas y tráfico. | Terminal de mensajería, aparcamiento y tienda de conveniencia. | Viajeros llegan por horarios; conductores paran; personal limpia y abre locales. | Encargos entre zonas, compra de transporte y rutas delegadas; conecta con centro, talleres y colina. |

Los edificios sin función no reciben interior completo. Fachada, señal y ruta peatonal bastan hasta que un encargo o negocio necesite entrar. Los interiores activos deben tener acción, horario y salida clara.

## Sistemas compartidos

### Negocios, empleados y propiedades

- Cada negocio tiene identidad estable, propietario, zona, horario, estaciones, inventario, empleados, costes y libro de movimientos. Comprar o arrendar requiere precio claro y confirmación; la propiedad persiste al guardar y recargar.
- Propiedades admiten mejoras de capacidad, almacenamiento y apariencia. Una mejora cambia una operación medible; no vende solo un número abstracto. Al salir de una zona, la simulación remota usa datos y eventos, no GameObjects activos.
- Personal tiene turno, estación, tarea y motivo visible para estar inactivo. Un empleado no genera ventas sin insumos, tiempo ni clientes. El jugador puede volver a operar cualquier estación manualmente.
- La economía suma ventas, apuestas y premios del casino, salarios, compras, transporte, rentas y encargos en un libro común. Pesos y fichas mantienen reglas actuales. Los créditos de mensajería ficticia, si se adopta ese modo, quedan separados; no se convierten en pesos ni fichas sin decisión nueva. Guardado versionado y migración antes de agregar zonas o propiedades a perfiles existentes.
- El casino conserva entrada y giros de cortesía desde el primer minuto. Adquirir su gestión podría desbloquear estadísticas o mejoras, pero **jamás** abrir la puerta o habilitar las máquinas.

### Población, horarios y tráfico

- Un reloj común mueve turnos y demanda por zona. Rutinas: casa, trayecto, trabajo, pausa, compra y regreso. Cada NPC necesita destino, estado y reacción simple a un bloqueo. Al salir del sector, se conserva el estado lógico y se despawnea la representación.
- Peatones próximos usan rutas caminables y cruces; los lejanos se representan con conteos de demanda. Vehículos de ambiente siguen carriles y paradas predeterminadas. Solo los próximos al jugador usan colisión y animación completas.
- Primer transporte útil: bicicleta o furgoneta ligera de reparto, con carga, aparcamiento y recuperación si queda atascado. Viajar no teletransporta mercancía; la entrega delegada consume tiempo y capacidad. Conducción arcade, con límites y señales legibles, sin simulación de tráfico real.
- La cámara, el HUD y el mapa muestran pedido activo, negocio, hora, destino y ruta. Se requiere lectura clara a 1280×720 y 1920×1080; no saturar el HUD del casino.

### Encargos y policía arcade

- Encargos de comida, suministro, montaje de eventos y mensajería de **cajas de utilería luminosa**: aceptar, recoger, llevar, entregar, cobrar o fallar. Cada paso se valida una vez para impedir cobro duplicado. Carga, tiempo, destino y pago aparecen antes de aceptar.
- La línea de mensajería especial es ficción arcade. Su alerta 0–100 cambia por eventos del juego, se comunica con avisos y baja con tiempo. Patrullas ficticias tienen estados patrulla, observación, persecución y regreso. Detección usa distancia y línea de visión dentro del juego; pérdida de encargo y reaparición segura sustituyen castigos permanentes.
- La policía nunca cierra el casino, borra un negocio ni bloquea el progreso principal. La entrega tiene alternativa sin combate. Armas de ficción son **opcionales** y quedan fuera de los primeros hitos; no se necesita combate para terminar encargos. No hay daño a civiles ni instrucciones sobre delitos reales.
- Antes de añadir patrullas físicas, validar el estado de alerta y un encargo completo en Casa Marea. Para persecuciones en la ciudad, probar rutas que no atraviesen paredes, recuperación de atascos y pausa correcta al descargar sectores.

## Arquitectura de mundo y rendimiento

- Una escena persistente mantiene perfil, reloj, economía, mapa y servicios. Ocho escenas de zona se cargan aditivamente según posición. Cada zona tiene un identificador estable; las referencias entre zonas son datos, no objetos de escena. Una sola área integra y sube las escenas generadas según `Docs/ARRANQUE_AGENTES.md`.
- El sector activo y vecinos cercanos muestran geometría y NPC completos. Sectores lejanos usan silueta, señal y simulación de datos. Cargar un interior solo al entrar; al salir, conservar su inventario y trabajadores en el estado lógico.
- Medir antes de fijar presupuesto final. Metas iniciales propuestas para PC Windows de referencia: 60 FPS a 1080p en zonas activas, percentil 95 de fotograma ≤16.7 ms en recorrido de 10 minutos, sin pausa visible mayor de 250 ms al cruzar frontera, memoria estable tras diez viajes de ida y vuelta. Registrar GPU, CPU, RAM, resolución y percentiles; ajustar metas al hardware objetivo acordado.
- Usar URP, SRP Batcher, instancing y LOD en activos repetidos; limitar luces y sombras por sector. Pool de peatones y vehículos. Evitar `FindObjectsByType` por fotograma, simulación remota en `Update` por NPC y cargas sin límite. Perfilar primero en build Windows, después en Editor.
- Pruebas de streaming: cruzar cada frontera; guardar en transición; cerrar y reabrir en otra zona; terminar un encargo mientras su origen está descargado; verificar NPC, tráfico, audio, colisiones y propiedad sin duplicados ni objetos perdidos.

## Fases y puertas de salida

| Fase | Entrega jugable | Criterio para pasar |
| --- | --- | --- |
| **0. Base de tres negocios** | Cerrar venta de hamburguesas, Casino FOX y tercer ciclo pequeño en Casa Marea; libro común, guardado y pruebas. | Tres ciclos completos y repetibles, casino abierto desde perfil nuevo, guardado/recarga sin duplicar dinero. Si el tercero aún no se aprueba, no se declara terminado. |
| **1. Dos zonas útiles** | Casa Marea y Mercado del Sol, proveedor, un encargo de suministro y mapa mínimo. | Ida y vuelta a pie, compra/entrega real, NPC con rutina simple, datos persistentes. |
| **2. Cuatro zonas conectadas** | Añadir Centro del Faro y Estación Sur; primer empleado, propiedad, bicicleta o furgoneta y tráfico cercano. | Negocio remoto consume insumos y salarios; ruta en transporte funciona; guardar lejos de Casa Marea y volver. |
| **3. Ocho zonas completas** | Añadir Muelle Farol, Talleres Brisa, Paseo Arena y Colina Cobalto; streaming, horarios y contratos entre zonas. | Cada zona tiene al menos un negocio operable, rutina NPC y encargo; fronteras y memoria pasan medición. |
| **4. Alerta y persecución arcade** | Encargo especial, alerta visible y patrulla física después de validar la economía del modo. | Ruta sin combate completada; persecución y fallo recuperables; policía no atraviesa edificios ni bloquea negocio/casino. |
| **5. Pulido y entrega** | Audio por zonas, accesibilidad, rendimiento, guardado migrado y build Windows. | Lista de terminado completa, regresiones de hamburguesas y casino aprobadas, recorrido manual de ciudad documentado. |

## Definición de terminado de Puerto Alba

1. Ocho zonas conectadas y distinguibles. Cada una tiene destino útil, rutina NPC, negocio operable y encargo reproducible; no cuenta una fachada sola.
2. Tres negocios iniciales y sus ampliaciones tienen transacciones trazables. Comprar propiedad, contratar personal, vender, apostar, pagar y entregar no duplican ni pierden valor al guardar, viajar o recargar.
3. Casino FOX abre con perfil nuevo y tras cualquier viaje o recarga. Seis máquinas, cajera y giro de cortesía siguen accesibles; una patrulla nunca lo clausura.
4. Peatones y tráfico completan rutas sin atasco permanente. Los servicios remotos operan mediante estado lógico y horarios, sin NPC activos en ocho escenas a la vez.
5. Un encargo por categoría pasa aceptación, entrega, fallo y reanudación. Alerta y persecución son legibles y recuperables; terminar el juego no requiere armas.
6. Build Windows en Unity 6, smokes de Play Mode con marcadores, pruebas C# puras y recorrido manual de las ocho zonas con log de FPS, memoria y fallos. Ningún resultado se declara verificado sin evidencia de esa build.

**Límites actuales:** el prototipo aún no tiene ocho zonas, propiedades, tráfico ni policía. Windows Build Support falta en el equipo macOS inspeccionado; la build Windows debe verificarse en un entorno con ese módulo. Este documento define alcance y orden, no confirma implementación.
