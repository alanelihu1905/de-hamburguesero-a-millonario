# Reglas permanentes del proyecto

- Unity 6, C#, URP y PC Windows son la base técnica. Conservar esta arquitectura salvo decisión documentada.
- Codex integra código y escenas. Claude dirige el diseño visual. Antigravity revisa y prueba; no atribuirles resultados sin evidencia.
- Completar un barrio pequeño y el ciclo de venta antes de ampliar la ciudad.
- Guardar lógica en componentes pequeños; separar datos configurables al crecer cada sistema.
- Preparar toda lógica económica para validación por anfitrión antes de incorporar Netcode for GameObjects y Relay.
- No agregar apuestas de dinero real ni personajes reales a tramas polémicas.
- Registrar decisiones duraderas en `Docs/DECISIONS.md` y progreso temporal en `Docs/PROJECT_STATE.md`.
- No declarar una función verificada sin prueba en Unity. No guardar secretos en Git ni en documentos.

