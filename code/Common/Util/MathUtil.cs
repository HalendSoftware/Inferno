using Sandbox;
using System;

/* 
 * Utility class to handle framerate independent + useful calculations
*/

namespace Sambit;

class MathUtil
{
	public static float FILerp(float fromF, float toF, float amount)
	{
		return fromF.LerpTo(toF, amount * RealTime.Delta);
	}

	public static Vector3 FILerp(Vector3 fromVec, Vector3 toVec, float amount)
	{
		return fromVec.LerpTo(toVec, amount * RealTime.Delta);
	}

	public static Angles FILerp(Angles fromAng, Angles toAng, float amount)
	{
		return Angles.Lerp(fromAng, toAng, amount * RealTime.Delta);
	}

	public static Vector3 RelativeAdd(Vector3 vec1, Vector3 vec2, Rotation rot)
	{
		vec1 += vec2.x * rot.Right;
		vec1 += vec2.y * rot.Up;
		vec1 += vec2.z * rot.Forward;

		return vec1;
	}

	public static float DistanceFrom(Vector3 v1, Vector3 v2)
	{
		Vector3 difference = new Vector3(
		v1.x - v2.x,
		v1.y - v2.y,
		v1.z - v2.z);

		float distance = (float)Math.Sqrt(
		Math.Pow(difference.x, 2f) +
		Math.Pow(difference.y, 2f) +
		Math.Pow(difference.z, 2f));

		return distance;

	}

	public static float InchesToMeters(float inches)
	{
		return (float)Math.Round(inches * 0.0254f, 0);
	}

	// Helpful bezier function. Use this if you gotta: https://www.desmos.com/calculator/cahqdxeshd
	public static float BezierY(float f, float a, float b, float c)
	{
		f = f * 3.2258f;
		return MathF.Pow((1.0f - f), 2.0f) * a + 2.0f * (1.0f - f) * f * b + MathF.Pow(f, 2.0f) * c;
	}

	public static Vector3 ToVector3(Angles angles)
	{
		return new Vector3(angles.pitch, angles.yaw, angles.roll);
	}

}

