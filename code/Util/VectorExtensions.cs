namespace Sandbox.Util;

public static class VectorExtensions
{
    public static float GetAngleFromPoints(this Vector3 origin, Vector3 to, Vector3 lookDir)
    {
        var direction = (to - origin).Normal;
        return Vector3.GetAngle(direction, lookDir);
    }
}