using System;

namespace Tycoon3D.Player
{
    /// <summary>Matemática pequeña del movimiento FPS, independiente de Unity para poder probar aceleración y salto.</summary>
    public static class PlayerMotionMath
    {
        public static void NormalizeInput(float horizontal, float vertical, out float x, out float z)
        {
            x = ClampAxis(horizontal);
            z = ClampAxis(vertical);
            float length = (float)Math.Sqrt(x * x + z * z);
            if (length > 1f) { x /= length; z /= length; }
        }

        public static void MovePlanar(float currentX, float currentZ, float targetX, float targetZ,
            float maxDelta, out float resultX, out float resultZ)
        {
            if (float.IsNaN(maxDelta) || float.IsInfinity(maxDelta) || maxDelta < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxDelta));
            float dx = targetX - currentX, dz = targetZ - currentZ;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);
            if (distance <= maxDelta || distance <= 0.000001f)
            {
                resultX = targetX;
                resultZ = targetZ;
                return;
            }
            float ratio = maxDelta / distance;
            resultX = currentX + dx * ratio;
            resultZ = currentZ + dz * ratio;
        }

        public static float JumpSpeed(float height, float gravityMagnitude)
        {
            if (height < 0f) throw new ArgumentOutOfRangeException(nameof(height));
            if (gravityMagnitude <= 0f) throw new ArgumentOutOfRangeException(nameof(gravityMagnitude));
            return (float)Math.Sqrt(2f * height * gravityMagnitude);
        }

        public static float BobOffset(float phase, float amplitude) => (float)Math.Sin(phase) * amplitude;

        private static float ClampAxis(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return Math.Max(-1f, Math.Min(1f, value));
        }
    }
}
