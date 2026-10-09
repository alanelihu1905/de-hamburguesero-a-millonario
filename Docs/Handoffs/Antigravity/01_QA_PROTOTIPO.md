# Encargo QA 01 — Primer prototipo

**Destinatario:** Google Antigravity. **Estado:** preparado; no enviado ni ejecutado.

Abrir el proyecto en Unity 6 con URP y registrar versión exacta, resolución de paquetes y mensajes de consola. Confirmar que se genera `BarrioResidencial.unity` y que está en Build Settings.

## Casos de prueba

1. Play Mode: aparecer frente a la casa, entrar con WASD y mirar con ratón; no atravesar paredes ni suelo.
2. Mirar cada estación a menos de 3 m: aparece su texto y E ejecuta solo la acción apuntada.
3. Saldo inicial $100. Comprar ingredientes resta $15; cocción no cambia saldo; venta suma $50 y aumenta ventas a 1. Saldo esperado: $135.
4. Intentar vender sin cocinar y comprar ingredientes dos veces: no duplicar dinero ni producto.
5. Repetir ciclo y comprobar saldo $170 tras segunda venta.
6. Pulsar R: escena recarga y vuelve a saldo $100 y cero ventas sin error bloqueante.
7. Verificar que URP está activo y que materiales no aparecen magenta.
8. Revisar consola y, si existe módulo Windows, realizar build de prueba.

Reportar para cada caso: aprobado/fallido/no ejecutado, pasos, resultado observado, consola y captura cuando ayude. No modificar los scripts de la mecánica simultáneamente con Codex; registrar incidencias en un informe separado.

