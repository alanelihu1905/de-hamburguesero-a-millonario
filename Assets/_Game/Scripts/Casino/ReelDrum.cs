using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Un rodillo físico: tambor pentagonal con una cara por símbolo. Gira de verdad, frena con una curva suave y se
    /// detiene exactamente en el símbolo que decidió la lógica (la animación es solo cosmética).
    /// Eje del tambor: X local; la ventana mira hacia -Z local.
    /// Geometría: la cara k tiene normal (0, sen(72k°), -cos(72k°)); con Quaternion.AngleAxis(a, Vector3.right) la normal
    /// queda en (0, sen(72k°+a), -cos(72k°+a)), así que la cara k mira al frente cuando a = -72k° (módulo 360).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ReelDrum : MonoBehaviour
    {
        public const int Faces = 5;
        public const float FaceDegrees = 360f / Faces;
        [SerializeField] private float radius = .14f;
        [SerializeField] private float length = .20f;

        // cara k = símbolo k (el mismo orden que SlotSymbol y que las celdas del atlas)
        private float angle;
        private bool spinning;
        private float startAngle, travel, startTime, duration, restAngle;

        public bool Spinning => spinning;
        public float Angle => angle;
        public float Radius => radius;

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null) filter.sharedMesh = BuildMesh(radius, length);
            transform.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
        }

        /// <summary>Tambor pentagonal: cada cara muestra la parte central (cuadrada) de su celda del atlas de símbolos.</summary>
        public static Mesh BuildMesh(float radius, float length)
        {
            var vertices = new Vector3[Faces * 4];
            var normals = new Vector3[Faces * 4];
            var uvs = new Vector2[Faces * 4];
            var triangles = new int[Faces * 6];
            float half = Mathf.PI / Faces;
            float halfSlot = radius * Mathf.Tan(half);
            for (int k = 0; k < Faces; k++)
            {
                float theta = k * FaceDegrees * Mathf.Deg2Rad;
                var normal = new Vector3(0, Mathf.Sin(theta), -Mathf.Cos(theta));
                var along = new Vector3(0, Mathf.Cos(theta), Mathf.Sin(theta));       // hacia arriba cuando la cara está al frente
                Vector3 center = normal * radius;
                Vector3 axis = Vector3.right * (length * .5f);
                int i = k * 4;
                vertices[i] = center - along * halfSlot - axis;
                vertices[i + 1] = center + along * halfSlot - axis;
                vertices[i + 2] = center + along * halfSlot + axis;
                vertices[i + 3] = center - along * halfSlot + axis;
                for (int n = 0; n < 4; n++) normals[i + n] = normal;
                float u0 = k / (float)Faces, u1 = (k + 1) / (float)Faces;
                // el símbolo ocupa la mitad central de la celda (128 x 128 dentro de 128 x 256)
                uvs[i] = new Vector2(u0, .25f); uvs[i + 1] = new Vector2(u0, .75f);
                uvs[i + 2] = new Vector2(u1, .75f); uvs[i + 3] = new Vector2(u1, .25f);
                int t = k * 6;
                triangles[t] = i; triangles[t + 1] = i + 1; triangles[t + 2] = i + 2;
                triangles[t + 3] = i; triangles[t + 4] = i + 2; triangles[t + 5] = i + 3;
            }
            var mesh = new Mesh { name = "Tambor de rodillo" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Ángulo (módulo 360) con el que el símbolo queda al frente.</summary>
        public static float RestAngle(SlotSymbol symbol) => Mod(-(int)symbol * FaceDegrees, 360f);

        /// <summary>Símbolo que se ve de frente ahora (cara con la normal más alineada con -Z).</summary>
        public SlotSymbol SymbolAtFront() => SymbolAtFront(angle);

        public static SlotSymbol SymbolAtFront(float angleDegrees)
        {
            int best = 0;
            float bestDot = -2f;
            for (int k = 0; k < Faces; k++)
            {
                float theta = (k * FaceDegrees + angleDegrees) * Mathf.Deg2Rad;
                float dot = Mathf.Cos(theta);                                         // componente -Z de la normal girada
                if (dot > bestDot) { bestDot = dot; best = k; }
            }
            return (SlotSymbol)best;
        }

        public void SnapTo(SlotSymbol symbol)
        {
            spinning = false;
            angle = RestAngle(symbol);
            transform.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
        }

        /// <summary>Empieza a girar y se detiene tras <paramref name="seconds"/> mostrando <paramref name="target"/>.</summary>
        public void StartSpin(SlotSymbol target, float seconds, int extraTurns)
        {
            restAngle = RestAngle(target);
            float delta = Mod(angle - restAngle, 360f);                               // las caras bajan: el ángulo disminuye
            travel = -(delta + 360f * Mathf.Max(1, extraTurns));
            startAngle = angle;
            startTime = Time.time;
            duration = Mathf.Max(.2f, seconds);
            spinning = true;
        }

        private static float Mod(float v, float m) => ((v % m) + m) % m;

        private void Update()
        {
            if (!spinning) return;
            float u = Mathf.Clamp01((Time.time - startTime) / duration);
            const float u0 = .70f;                                                   // velocidad constante y luego frenada suave
            float v0 = 2f / (1f + u0);
            float f = u <= u0 ? v0 * u : v0 * u0 + v0 * (u - u0) - v0 * (u - u0) * (u - u0) / (2f * (1f - u0));
            angle = startAngle + travel * f;
            if (u >= 1f)
            {
                spinning = false;
                angle = restAngle;
            }
            transform.localRotation = Quaternion.AngleAxis(angle, Vector3.right);
        }
    }
}
