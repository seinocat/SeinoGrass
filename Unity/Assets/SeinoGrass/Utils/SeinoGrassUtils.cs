using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SeinoGrass.Utils
{
    public class SeinoGrassUtils
    {
        public static float2 GetUv(float x, float y, int2 size)
        {
            float u = (x % size.x) / size.x;
            float v = (y % size.y) / size.y;
            return new float2(u, v);
        }
        
        public static int GetAnchor(float x, float y, int2 size)
        {
            int2 uv = new int2((int)(x / size.x), (int)(y / size.y));
            return uv.x + uv.y * size.x;
        }
        
        /// <summary>
        /// 双线性采样
        /// </summary>
        public static Color32 GetBilinearPixel(NativeArray<Color32> pixelData, float u, float v, int width, int height)
        {
            u *= width;
            v *= height;
            width = Mathf.Max(width, 2);
            height = Mathf.Max(height, 2);
            int x = Mathf.Clamp(Mathf.FloorToInt(u), 0, width - 2);
            int y = Mathf.Clamp(Mathf.FloorToInt(v), 0, height - 2);
            float s = u - x;
            float t = v - y;
            Color32 c1 = pixelData[x + (y * width)];
            Color32 c2 = pixelData[x + 1 + (y * width)];
            Color32 c3 = pixelData[x + ((y + 1) * width)];
            Color32 c4 = pixelData[x + 1 + ((y + 1) * width)];
            Color32 a = Color32.Lerp(c1, c2, s);
            Color32 b = Color32.Lerp(c3, c4, s);
            return Color32.Lerp(a, b, t);
        }
    }
}