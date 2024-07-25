using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SeinoGrass.Utils
{
    /// <summary>
    /// 维诺泊松采样
    /// </summary>
    public class VoronoiPoissonSampling
    {
        public const float TwoPi = 2 * math.PI;
        public static readonly float SqrtTwo = math.sqrt(2);

        struct SampleSetting
        {
            public float2?[,] grids;
            public int2 size;
            public int cellx;
            public int celly;
            public float cellsize;
            public float radius;
            public float2 corner0;
            public float2 corner1;
            public NativeArray<Color32> colors;
            public Color32 color;
        }

        /// <summary>
        /// 维诺-泊松圆盘采样，在指定色块区域内均匀布点
        /// </summary>
        /// <param name="corner0">对角线起点</param>
        /// <param name="corner1">对角线终点</param>
        /// <param name="p0">初始点坐标</param>
        /// <param name="radius">最小半径</param>
        /// <param name="colorArray">维诺图数据</param>
        /// <param name="color">目标区域颜色值</param>
        /// <param name="k">迭代次数(默认30)</param>
        /// <returns>点列表，每个点之间的距离大于radius</returns>
        public static List<float2> Sample(float2 corner0, float2 corner1, float2 p0, float radius, NativeArray<Color32> colorArray, Color32 color, float k = 30)
        {
            Random random = new Random(9527);
            List<float2> points = new List<float2>();
            List<float2> activePoints = new List<float2>();
            
            float2 diagonal = math.abs(corner0 - corner1);
            float cellsize = radius / SqrtTwo;//格子大小
            int cellx = (int)(diagonal.x / cellsize + 1);//x格子数量
            int celly = (int)(diagonal.y / cellsize + 1);//y格子数量
            
            float2?[,] grids = new float2?[cellx, celly];

            SampleSetting setting = new SampleSetting
            {
                grids = grids,
                radius = radius,
                cellsize = cellsize,
                cellx = cellx,
                celly = celly,
                corner0 = corner0,
                corner1 = corner1,
                colors = colorArray,
                color = color,
                size = new int2((int)diagonal.x, (int)diagonal.y)
            };
            
            AddPoint(ref setting, p0);
            
            points.Add(p0);
            activePoints.Add(p0);

            while (activePoints.Count > 0)
            {
                bool found = false;
                int index = random.NextInt(activePoints.Count);
                float2 p = activePoints[index];
                
                for (int i = 0; i < k; i++)
                {
                    float2 new_p = GetRandomPoint(p, radius, ref random);
                    if (!IsValidPoint(ref setting , new_p))
                        continue;
                    
                    points.Add(new_p);
                    activePoints.Add(new_p);
                    AddPoint(ref setting, new_p);
                    found = true;
                    break;
                }

                if (!found)
                    activePoints.RemoveAt(index);
            }
            
            return points;
        }

        private static void AddPoint(ref SampleSetting setting, float2 p)
        {
            int x = (int)((p.x - setting.corner0.x) / setting.cellsize);
            int y = (int)((p.y - setting.corner0.y) / setting.cellsize);
            setting.grids[x, y] = p;
        }

        private static float2 GetRandomPoint(float2 p, float radius, ref Random random)
        {
            float radians = TwoPi * random.NextFloat();
            float new_radius = radius * (1 + random.NextFloat());
            float x = p.x + new_radius * math.cos(radians);
            float y = p.y + new_radius * math.sin(radians);
            return new float2(x, y);
        }

        private static bool IsValidPoint(ref SampleSetting setting, float2 p)
        {
            //判断是否在区域内
            if (p.x <= setting.corner0.x || p.x >= setting.corner1.x || p.y <= setting.corner0.y || p.y >= setting.corner1.y)
                return false;
            
            //判断是否在色块范围内
            float coordite_u = p.x / setting.size.x;
            float coordite_v = p.y / setting.size.y;
            Color32 color = SeinoGrassUtils.GetBilinearPixel(setting.colors, coordite_u, coordite_v, setting.size.x, setting.size.y);
            if (!color.Equals(setting.color))
                return false;
            
            int u = (int)((p.x - setting.corner0.x) / setting.cellsize);
            int v = (int)((p.y - setting.corner0.y) / setting.cellsize);
            
            int i0 = math.max(0, u - 2);
            int i1 = math.min(u + 2, setting.cellx);
            int j0 = math.max(0, v - 2);
            int j1 = math.min(v + 2, setting.celly);

            for (int i = i0; i < i1; i++)
                for (int j = j0; j < j1; j++)
                    if (setting.grids[i, j].HasValue && math.distance(setting.grids[i, j].Value, p) < setting.radius)
                        return false;

            return true;
        } 
    }
}