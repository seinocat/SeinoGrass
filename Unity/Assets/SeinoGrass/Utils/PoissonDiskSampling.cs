using System.Collections.Generic;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace SeinoGrass.Utils
{
    public class PoissonDiskSampling
    {
        public const float TwoPi = 2 * math.PI;
        public static readonly float SqrtTwo = math.sqrt(2);
        
        /// <summary>
        /// 泊松圆盘采样，在指定区域均匀布点
        /// </summary>
        /// <param name="corner0">对角线起点</param>
        /// <param name="corner1">对角线终点</param>
        /// <param name="radius">最小半径</param>
        /// <param name="k">迭代次数(默认30)</param>
        /// <returns>点列表，每个点之间的距离大于radius</returns>
        public static List<float2> Sample(float2 corner0, float2 corner1, float radius, float k = 30)
        {
            Random random = new Random(9527);
            List<float2> points = new List<float2>();
            List<float2> activePoints = new List<float2>();
            
            float2 diagonal = math.abs(corner0 - corner1);
            float cellsize = radius / SqrtTwo;
            int width = (int)(diagonal.x / cellsize + 1);
            int height = (int)(diagonal.y / cellsize + 1);
            
            float2?[,] grids = new float2?[width, height];
            float2 p0 = new float2(corner0.x + random.NextFloat(diagonal.x), corner0.y + random.NextFloat(diagonal.y));
            AddPoint(ref grids, cellsize, corner0, p0);
            
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
                    if (!IsValidPoint(ref grids, cellsize, width, height, radius, corner0, corner1, new_p))
                        continue;
                    
                    points.Add(new_p);
                    activePoints.Add(new_p);
                    AddPoint(ref grids, cellsize, corner0, new_p);
                    found = true;
                    break;
                }

                if (!found)
                    activePoints.RemoveAt(index);
            }
            
            return points;
        }

        private static void AddPoint(ref float2?[,] grids, float cellsize, float2 p0, float2 p)
        {
            int x = (int)((p.x - p0.x) / cellsize);
            int y = (int)((p.y - p0.y) / cellsize);
            grids[x, y] = p;
        }

        private static float2 GetRandomPoint(float2 p, float radius, ref Random random)
        {
            float radians = TwoPi * random.NextFloat();
            float new_radius = radius * (1 + random.NextFloat());
            float x = p.x + new_radius * math.cos(radians);
            float y = p.y + new_radius * math.sin(radians);
            return new float2(x, y);
        }

        private static bool IsValidPoint(ref float2?[,] grids, float cellsize, int width, int height, float radius, float2 p0, float2 p1, float2 p)
        {
            if (p.x < p0.x || p.x >= p1.x || p.y < p0.y || p.y >= p1.y)
                return false;
            
            int xindex = (int)((p.x - p0.x) / cellsize);
            int yindex = (int)((p.y - p0.y) / cellsize);
            
            int i0 = math.max(0, xindex - 2);
            int i1 = math.min(xindex + 2, width);
            int j0 = math.max(0, yindex - 2);
            int j1 = math.min(yindex + 2, height);

            for (int i = i0; i < i1; i++)
                for (int j = j0; j < j1; j++)
                    if (grids[i, j].HasValue && math.distance(grids[i, j].Value, p) < radius)
                        return false;

            return true;
        } 
    }
}