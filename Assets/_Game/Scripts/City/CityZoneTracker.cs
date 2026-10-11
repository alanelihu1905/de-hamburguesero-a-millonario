using UnityEngine;

namespace Tycoon3D.City
{
    /// <summary>
    /// Sigue al jugador y dice en qué zona de Puerto Alba está. Solo lee la posición y avisa al cambiar de zona:
    /// no mueve nada ni cambia la economía.
    /// </summary>
    public sealed class CityZoneTracker : MonoBehaviour
    {
        [SerializeField] private Transform target;

        /// <summary>Zona actual; null si el jugador está en la calle o fuera de las zonas construidas.</summary>
        public ZoneId? CurrentZone { get; private set; }
        public bool Known { get; private set; }

        public event System.Action<ZoneId?> ZoneChanged;

        public void Configure(Transform player) => target = player;

        private void Update()
        {
            if (target == null) return;
            Vector3 p = target.position;
            ZoneId? zone = CityLayout.Locate(p.x, p.z);
            if (Known && zone == CurrentZone) return;
            Known = true;
            CurrentZone = zone;
            Debug.Log("CIUDAD_ZONA " + (zone.HasValue ? CityMap.Get(zone.Value).Name : "calle"));
            ZoneChanged?.Invoke(zone);
        }
    }
}
