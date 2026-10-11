# Grafo de Puerto Alba

`Tycoon3D.World.CityGraph` define ocho zonas, sus nombres para interfaz, puntos de interés, función económica y conexiones de calle. No depende de Unity, NavMesh ni de la escena, así los encargos, la brújula y el mapa pueden compartir IDs estables.

## Zonas

| ID | Nombre visible | Punto de interés | Función |
| --- | --- | --- | --- |
| `LasAcacias` | Las Acacias | Barrio Burger | Puesto inicial y viviendas |
| `Mercado` | Calle Mercado | Mercado de Rosa | Proveedores, ingredientes y almacenes |
| `Muelle` | Muelle | Terminal del Puerto Alba | Carga, pesca, turismo y entregas |
| `Restaurantes` | Distrito Gastronómico | Calle de los Sabores | Restaurantes y competencia culinaria |
| `Industrial` | Zona Industrial | Centro de Distribución | Producción y logística |
| `Parque` | Parque del Río | Anfiteatro del Río | Eventos, clientes y rutas tranquilas |
| `CentroFinanciero` | Centro Financiero | Torre Puerto Alba | Oficinas, contratos y propiedades |
| `AvenidaCentral` | Avenida Central | Casino Gran Fortuna | Ocio nocturno y casino siempre abierto |

## Uso desde código

- `Zones`: datos ordenados y estables para crear tarjetas o leyenda del mapa.
- `TryGetZone(id, out info)`: nombre, descripción, punto de interés y función.
- `RoadsFrom(id)`: calles vecinas con distancia de diseño en metros.
- `FindRoute(from, to)`: ruta de menor distancia; desempates estables.
- `RouteDistanceMeters(route)`: longitud acumulada, o cero si la secuencia contiene un tramo inexistente.

Las distancias son aproximaciones para diseño del mapa, no mediciones de una escena construida. El primer destino de balance es que el camino corto entre Las Acacias y Avenida Central mida 1,790 m. Al construir zonas, el integrador debe calibrar cada tramo contra la geometría real. El grafo describe conectividad entre zonas; no sustituye la navegación peatonal/vehicular dentro de una zona.

La distancia también puede alimentar contratos: duración estimada = distancia / velocidad de referencia del modo + factor de tráfico. Mantener la simulación del tráfico fuera de `CityGraph`.
