namespace Sandbox.Util;

public class LengthUtil
{
    const float MetersPerUnit = 0.0254f;

    public static float MetersToHammer(float meters)
    {
        return meters / MetersPerUnit;
    }

    public static float HammerToMeters(float hammer)
    {
        return hammer * MetersPerUnit;
    }
}