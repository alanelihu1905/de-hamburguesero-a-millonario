# Plan maestro ampliado — De Hamburguesero a Millonario

**Fuente base:** documento de diseño entregado por Alan el 11 de octubre de 2026.  
Las decisiones de este archivo que no aparecen en el PDF están marcadas como **propuesta ampliada**. No son funciones implementadas todavía.

## 1. Decisiones consolidadas

- Unity 6, C#, URP, primera persona y barrio pequeño antes de ampliar el mapa.
- Puerto Alba conserva cuatro zonas: Las Acacias, Calle Mercado, Distrito Norte y Avenida Central.
- El casino está visible y jugable desde el inicio, con las tragamonedas y apuestas actuales. La campaña solo reserva propiedad y expansión para después; nunca bloquea el acceso.
- La campaña principal dura 4–5 horas y el modo libre continúa después.
- El dinero del negocio, las fichas del casino y los créditos de encargos se mantienen separados.
- No hay dinero real, personajes reales ni instrucciones de delito real.

## 2. Capa nueva: encargos de riesgo ficticios

**Propuesta ampliada.** Desde el Capítulo 3 aparece un tablón de encargos para transportar cajas de utilería y mercancía ficticia entre comerciantes inventados. El jugador puede ignorarlo y continuar con hamburguesas y casino.

El nombre del modo y su mercancía definitiva siguen **provisionales**. Ver [propuesta jugable](MODO_ENCARGOS_CLANDESTINOS.md).

### Ciclo de diez minutos

1. Elegir uno de dos encargos con pago, distancia, tiempo y alerta esperada.
2. Recoger una caja visible en un punto seguro.
3. Elegir ruta corta con más alerta o ruta larga con menos alerta.
4. Entregar al contacto correcto mediante interacción.
5. Reaccionar a una alerta: esconderse, cambiar de ruta o aceptar persecución arcade.
6. Cobrar créditos de encargos y consultar el resumen.

Estados puros: `Disponible`, `Aceptado`, `Transportando`, `Entregado`, `Fallido`, `Interceptado`.

### Alerta y patrulla

- Alerta 0–100 con causa visible en HUD.
- 30: observación; 60: seguimiento; 85: persecución.
- La patrulla usa waypoints, distancia y línea de visión. No conoce la posición del jugador sin evidencia.
- Llegar a zona segura o esperar fuera de misión permite recuperarse.
- Fallar reinicia solo el encargo; no borra perfil, casino ni negocios.

### Armas opcionales

**Propuesta ampliada para una fase posterior.** Las armas son herramientas arcade de ficción y no son necesarias para completar una entrega.

- Primer hito: sin armas ni combate.
- Fase posterior: varias clases de armas ficticias, disparo a distancia, munición, recarga, daño PvE, IA con reacción visible y respawn arcade. La herramienta no letal puede ser una opción, no el único tipo.
- Toda misión debe conservar una solución de sigilo, ruta alternativa o zona segura.
- No incluir armas reales detalladas, instrucciones de fabricación ni daño a civiles.

Si se agota la salud, el jugador reaparece en un punto seguro y falla solo el encargo actual. Conserva negocios, créditos ganados y casino. Antes de ampliar el mapa, pruebas deterministas deben cubrir aciertos, consumo de munición, daño, estados de IA y respawn.

## 3. Economía ampliada

### Monedas separadas

- **Pesos:** ingredientes, salarios, propiedades y casino personal.
- **Fichas:** juego del casino; valor separado y auditable.
- **Créditos de encargos:** rutas, capacidad de carga e indumentaria del modo; nunca se convierten a pesos.

### Variables persistentes

- Patrimonio neto.
- Reputación del barrio.
- Confianza del equipo.
- Índice de Impulso.
- Deuda personal.
- Presupuesto semanal protegido.
- Alerta actual y rango de encargos.

El área A debe versionar `SaveData` y registrar movimientos antes de añadir pagos persistentes de encargos.

## 4. Campaña ampliada por capítulos

### Capítulo 1 — Cincuenta dólares

Puesto, compra, cocina, entrega y primera venta. Sin encargos de riesgo. El casino ya permite jugar con sus tragamonedas y apuestas actuales.

### Capítulo 2 — Dinero fácil

Nico, automatización básica, proveedores y primera visita narrativa al casino. Se desbloquea la tentación, no todavía la propiedad.

### Capítulo 3 — Todo lo que sube

Restaurante, Lupita, reputación, inspección y presupuesto. Se desbloquea el tablón de encargos de bajo riesgo y el primer contrato de entrega ficticia.

### Capítulo 4 — La apuesta más peligrosa

Vehículo, Tommy, rutas, sucursal y encargos de riesgo medio. Aparecen observación, seguimiento y persecuciones sin combate obligatorio.

### Capítulo 5 — El dueño de la casa

Compra y reforma de Gran Fortuna, gerente Irene, casino administrable, juego responsable y contratos de alto valor. Las armas opcionales solo se habilitan como contenido de modo libre después del primer final.

## 5. Casino consolidado

- Desde el primer minuto: tragamonedas y reglas visibles.
- Después: ruleta, blackjack, dados, VIP y administración del local.
- La primera apuesta tutorial puede estar calibrada solo si se identifica claramente como tutorial.
- El casino personal nunca puede apostar el presupuesto semanal protegido.
- Como dueño, el jugador puede activar límites, autoexclusión, avisos de tiempo y capacitación.
- Reputación, liquidez, salarios, mantenimiento y seguridad afectan el casino administrado.

## 6. Finales y modo libre

Se conservan los cuatro finales del PDF. Los encargos añaden una variable secundaria: reputación de operador. No reemplaza patrimonio, Impulso, confianza ni deuda.

En modo libre se pueden abrir restaurantes, ampliar el casino, completar recetas, hacer contratos, comprar propiedades y activar la fase opcional de armas arcade.

## 7. Primer hito jugable

Implementar una entrega completa en el barrio actual, sin ampliar la ciudad:

1. Un encargo configurable.
2. Caja recogible.
3. Contacto y zona destino.
4. Temporizador.
5. Créditos separados.
6. Barra de alerta con decaimiento.
7. Fallo recuperable al superar 85.
8. Casino disponible antes y después.
9. Guardado diferido hasta cerrar contrato, después de acordar versión con A.

Fuera de este hito: armas, NavMesh, múltiples patrullas, inventario grande y expansión de mapa.

## 8. Escala a la ciudad completa

**Propuesta ampliada posterior al barrio jugable.** Abrir Puerto Alba por etapas: Las Acacias, Calle Mercado, Distrito Norte y Avenida Central. Cada zona contiene un nodo de recogida, varios contactos, un punto seguro y rutas visibles. Los contratos entre zonas requieren planear tiempo, carga y alerta; un trayecto largo paga más créditos, pero no fuerza combate ni bloquea el casino.

- **Contratos por distrito:** cada distrito ofrece encargos de dificultad y recompensas propias. Las rutas cruzadas conectan dos distritos y cambian destino, horario ficticio y presión de patrulla; no se generan calles nuevas hasta validar las existentes.
- **Reputación local:** sube por entregas cumplidas en un distrito; abre contactos y encargos de ese distrito. Fallos repetidos la reducen con un suelo que evita bloquear todas las misiones.
- **Reputación global:** resume fiabilidad entre distritos; permite contratos de ciudad y mejoras de capacidad. Nunca reemplaza la reputación del restaurante ni cierra el casino.
- **Restaurantes y casino:** las sucursales funcionan como destinos narrativos o puntos de información de encargos. El casino aporta contactos y actividad de barrio; sus tragamonedas siguen disponibles desde el inicio. Pesos, fichas y créditos de encargos permanecen separados, sin conversión automática.
- **Integración técnica:** representar distritos y conexiones como datos configurables, con identificadores estables para contratos y guardado. Medir tiempo de viaje y FPS antes de abrir el siguiente distrito. Mantener un solo barrio cargado si la ciudad completa excede el presupuesto de rendimiento.

Orden: una entrega estable en Las Acacias, dos rutas dentro del barrio, primer contrato entre Las Acacias y Calle Mercado, después los otros distritos. Cada ampliación repite pruebas de pago único, regreso seguro, persistencia y casino abierto.

## 9. Tareas para agentes

### Claude — dirección visual y contenido

- Diseñar caja, tablón, contacto, señales de ruta y zona segura con la paleta Casa Marea.
- Preparar una propuesta visual de HUD de encargo, alerta y resumen final.
- Crear referencias originales para patrulla arcade y herramienta no letal; no integrar todavía armas en escena.
- Entregar archivos en `ArtSource/` y documentación de integración; no editar scripts de economía.

### Codex — sistemas y economía

- Crear `DeliveryContract` y máquina de estados pura.
- Crear alerta limitada 0–100 y créditos separados.
- Integrar `IInteractable` existente para caja, destino y zona segura.
- Añadir persistencia solo después de definir migración `SaveData`.
- Añadir pruebas de doble entrega, contacto equivocado, tiempo agotado, alerta y separación de monedas.

### QA / Antigravity

- Verificar raycast, prompts, rutas y zonas seguras en Play Mode.
- Comprobar que el casino y las hamburguesas siguen funcionando durante un encargo.
- Medir FPS y registrar resolución, marcador y evidencia visual.
- Revisar que una captura o intercepción nunca pague dos veces ni corrompa el perfil.

### Integración del director

- Una rama por área y un issue por tarea.
- Ningún PR se fusiona sin marcador real de prueba.
- Claude no modifica la escena mientras QA la esté verificando.
- El director integra escenas, revisa conflictos y mantiene `PROJECT_STATE.md`.

## 10. Criterios de terminado del primer hito

- `DeliverySmoke` pasa todas las reglas de estado.
- Unity Play Mode muestra caja, contacto, destino, alerta y resumen.
- `TYCOON_CASINO_SMOKE_OK` y `TYCOON_SMOKE_OK` siguen pasando.
- Una entrega paga exactamente una vez.
- El fallo conserva pesos, fichas, ventas y casino.
- No se afirma que patrulla o armas estén terminadas hasta tener pruebas específicas.
