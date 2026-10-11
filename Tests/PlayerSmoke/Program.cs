using System;
using Tycoon3D.Player;

int checks = 0;

PlayerMotionMath.NormalizeInput(1f, 1f, out float diagonalX, out float diagonalZ);
Check(Math.Abs(diagonalX * diagonalX + diagonalZ * diagonalZ - 1f) < .0001f,
    "la diagonal queda normalizada y no corre más rápido");
Check(Math.Abs(diagonalX - diagonalZ) < .0001f, "la diagonal conserva la proporción de entrada");
PlayerMotionMath.NormalizeInput(float.NaN, float.PositiveInfinity, out float invalidX, out float invalidZ);
Check(invalidX == 0f && invalidZ == 0f, "entrada no finita no mueve al jugador");

PlayerMotionMath.MovePlanar(0f, 0f, 4.5f, 0f, 1.8f, out float accelX, out float accelZ);
Check(Math.Abs(accelX - 1.8f) < .0001f && accelZ == 0f, "aceleración limita el cambio de velocidad por frame");
PlayerMotionMath.MovePlanar(accelX, 0f, 0f, 0f, 1f, out float brakeX, out _);
Check(Math.Abs(brakeX - .8f) < .0001f, "soltar dirección frena gradualmente");
PlayerMotionMath.MovePlanar(1f, 2f, -1f, -2f, 10f, out float snappedX, out float snappedZ);
Check(snappedX == -1f && snappedZ == -2f, "movimiento llega al objetivo sin sobrepasarlo");

float jumpSpeed = PlayerMotionMath.JumpSpeed(1.05f, 20f);
float jumpHeight = jumpSpeed * jumpSpeed / (2f * 20f);
Check(Math.Abs(jumpHeight - 1.05f) < .0001f, "velocidad inicial produce la altura de salto configurada");
Check(Math.Abs(PlayerMotionMath.BobOffset((float)(Math.PI / 2), .04f) - .04f) < .0001f,
    "bamboleo de cámara respeta amplitud máxima");
Check(PlayerMotionMath.BobOffset(0f, .04f) == 0f, "la cámara parte en posición neutral");

Console.WriteLine("PlayerSmoke: {0} comprobaciones aprobadas.", checks);

void Check(bool result, string label)
{
    checks++;
    if (!result) throw new Exception("Falló: " + label);
}
