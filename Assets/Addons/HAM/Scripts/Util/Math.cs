using UnityEngine;
using System;

public class HamMath
{
    public static float Sinerp(float v)
    {
        return Mathf.Sin(v * Mathf.PI * 0.5f);
    }

    public static float Berp(float v)
    {
        v = Mathf.Clamp01(v);
        return (Mathf.Sin(v * Mathf.PI * (0.2f + 2.5f * v * v * v)) * Mathf.Pow(1f - v, 2.2f) + v) * (1f + (1.2f * (1f - v)));
    }

    public static float Bounce(float x)
    {
        return Mathf.Abs(Mathf.Sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
    }

    public static float EaseInBounce(float x)
    {
        if (x < (1f / 2.75f))
        {
            return 7.5625f * x * x;
        }
        else if (x < (2f / 2.75f))
        {
            return 7.5625f * (x -= (1.5f / 2.75f)) * x + 0.75f;
        }
        else if (x < (2.5f / 2.75f))
        {
            return 7.5625f * (x -= (2.25f / 2.75f)) * x + 0.9375f;
        }
        else
        {
            return 7.5625f * (x -= (2.625f / 2.75f)) * x + 0.984375f;
        }
    }
}