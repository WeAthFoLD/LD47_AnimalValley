using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtil {
    public const int PPU = 48;

    public static float PixelSnap(float x, float scale = 1.0f) {
        float mod = scale / PPU;
        return x - x % mod;
    }

    public static int CeilDiv(int a, int b)
    {
        if (a % b == 0)
            return a / b;
        return 1 + (a / b);
    }

    public static Vector2 PixelSnap(Vector2 v, float scale = 1.0f) {
        return new Vector2(PixelSnap(v.x, scale), PixelSnap(v.y, scale));
    }

    public static Vector3 PixelSnap(Vector3 v, float scale = 1.0f) {
        return new Vector3(PixelSnap(v.x, scale), PixelSnap(v.y, scale), v.z);
    }

    public static Vector2 Shake(float freq, Vector2 range, Vector2 offset, int octave, float time) {
        float noiseX = 0, noiseY = 0;
        float strength = 1.0f;
        for (int i = 0; i < octave; ++i) {
            noiseX += strength * range.x * 2 * (Mathf.PerlinNoise(offset.x, time * freq) - .5f);
            noiseY += strength * range.y * 2 * (Mathf.PerlinNoise(offset.y, time * freq) - .5f);
            strength /= 2.0f;
            freq *= 2.0f;
        }

        return new Vector2(noiseX, noiseY);
    }

    public static void Swap<T>(ref T lhs, ref T rhs) {
        var temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    // http://www.roguebasin.com/index.php?title=Bresenham%27s_Line_Algorithm

    /// <summary>
    /// The plot function delegate
    /// </summary>
    /// <param name="x">The x co-ord being plotted</param>
    /// <param name="y">The y co-ord being plotted</param>
    /// <returns>True to continue, false to stop the algorithm</returns>
    public delegate bool PlotFunction(int x, int y);

    /// <summary>
    /// Plot the line from (x0, y0) to (x1, y10
    /// </summary>
    /// <param name="x0">The start x</param>
    /// <param name="y0">The start y</param>
    /// <param name="x1">The end x</param>
    /// <param name="y1">The end y</param>
    /// <param name="plot">The plotting function (if this returns false, the algorithm stops early)</param>
    public static void Line(int x0, int y0, int x1, int y1, PlotFunction plot)
    {
        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
        if (steep) { Swap<int>(ref x0, ref y0); Swap<int>(ref x1, ref y1); }
        if (x0 > x1) { Swap<int>(ref x0, ref x1); Swap<int>(ref y0, ref y1); }
        int dX = (x1 - x0), dY = Mathf.Abs(y1 - y0), err = (dX / 2), ystep = (y0 < y1 ? 1 : -1), y = y0;

        for (int x = x0; x <= x1; ++x)
        {
            if (!(steep ? plot(y, x) : plot(x, y))) return;
            err = err - dY;
            if (err < 0) { y += ystep;  err += dX; }
        }
    }

}

