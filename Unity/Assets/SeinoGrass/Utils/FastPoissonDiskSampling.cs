using System.Collections.Generic;
using Unity.Mathematics;

namespace SeinoGrass.Utils
{
    
    public class FastPoissonDiskSampler
    {
        public const float Pi = math.PI;
        public const float HalfPi = math.PI / 2;
        public const float TwoPi = math.PI * 2;
        public const int DefaultPointsPerIteration = 30;

        static readonly float SquareRootTwo = math.sqrt(2);

        struct Settings
        {
            public float2 TopLeft, LowerRight, Center;
            public float2 Dimensions;
            public float? RejectionSqDistance;
            public float MinimumDistance;
            public float CellSize;
            public int GridWidth, GridHeight;
        }

        struct State
        {
            public float2?[,] Grid;
            public List<float2> ActivePoints, Points;
        }

        public static List<float2> SampleCircle(float2 center, float radius, float minimumDistance, ref Random random)
        {
            return SampleCircle(center, radius, minimumDistance, DefaultPointsPerIteration, ref random);
        }

        public static List<float2> SampleCircle(float2 center, float radius, float minimumDistance, int pointsPerIteration, ref Random random)
        {
            return Sample(center - new float2(radius, radius), center + new float2(radius, radius), radius, minimumDistance, pointsPerIteration, ref random);
        }

        public static List<float2> SampleRectangle(float2 topLeft, float2 lowerRight, float minimumDistance, ref Random random)
        {
            return SampleRectangle(topLeft, lowerRight, minimumDistance, DefaultPointsPerIteration, ref random);
        }

        public static List<float2> SampleRectangle(float2 topLeft, float2 lowerRight, float minimumDistance, int pointsPerIteration, ref Random random)
        {
            return Sample(topLeft, lowerRight, null, minimumDistance, pointsPerIteration, ref random);
        }
     
        static List<float2> Sample(float2 topLeft, float2 lowerRight, float? rejectionDistance, float minimumDistance, int pointsPerIteration, ref Random random)
        {
            var settings = new Settings
            {
                TopLeft = topLeft,
                LowerRight = lowerRight,
                Dimensions = lowerRight - topLeft,
                Center = (topLeft + lowerRight) / 2,
                CellSize = minimumDistance / SquareRootTwo,
                MinimumDistance = minimumDistance,
                RejectionSqDistance = rejectionDistance == null ? null : rejectionDistance * rejectionDistance
            };
            settings.GridWidth = (int)(settings.Dimensions.x / settings.CellSize) + 1;
            settings.GridHeight = (int)(settings.Dimensions.y / settings.CellSize) + 1;

            var state = new State
            {
                Grid = new float2?[settings.GridWidth, settings.GridHeight],
                ActivePoints = new List<float2>(),
                Points = new List<float2>()
            };

            AddFirstPoint(ref settings, ref state, ref random);

            while (state.ActivePoints.Count != 0)
            {
                var listIndex = random.NextInt(state.ActivePoints.Count);

                var point = state.ActivePoints[listIndex];
                var found = false;

                for (var k = 0; k < pointsPerIteration; k++)
                    found |= AddNextPoint(point, ref settings, ref state, ref random);

                if (!found)
                    state.ActivePoints.RemoveAt(listIndex);
            }

            return state.Points;
        }

        static void AddFirstPoint(ref Settings settings, ref State state, ref Random random)
        {
            bool added = false;
            while (!added)
            {
                float d = random.NextFloat();
                float xr = settings.TopLeft.x + settings.Dimensions.x * d;

                d = random.NextFloat();
                float yr = settings.TopLeft.y + settings.Dimensions.y * d;

                var p = new float2((float)xr, (float)yr);
                if (settings.RejectionSqDistance != null && DistanceSquared(settings.Center, p) > settings.RejectionSqDistance)
                    continue;
                added = true;

                float2 index = Denormalize(p, settings.TopLeft, settings.CellSize);

                state.Grid[(int)index.x, (int)index.y] = p;

                state.ActivePoints.Add(p);
                state.Points.Add(p);
            }
        }

        static bool AddNextPoint(float2 point, ref Settings settings, ref State state, ref Random random)
        {
            bool found = false;
            float2 q = GenerateRandomAround(point, settings.MinimumDistance, ref random);

            if (q.x >= settings.TopLeft.x && q.x < settings.LowerRight.x &&
                q.y > settings.TopLeft.y && q.y < settings.LowerRight.y &&
                (settings.RejectionSqDistance == null || DistanceSquared(settings.Center, q) <= settings.RejectionSqDistance))
            {
                float2 qIndex = Denormalize(q, settings.TopLeft, settings.CellSize);
                bool tooClose = false;

                for (int i = (int)math.max(0, qIndex.x - 2); i < math.min(settings.GridWidth, qIndex.x + 3) && !tooClose; i++)
                    for (int j = (int)math.max(0, qIndex.y - 2); j < math.min(settings.GridHeight, qIndex.y + 3) && !tooClose; j++)
                        if (state.Grid[i, j].HasValue && math.length(state.Grid[i, j].Value - q) < settings.MinimumDistance)
                            tooClose = true;

                if (!tooClose)
                {
                    found = true;
                    state.ActivePoints.Add(q);
                    state.Points.Add(q);
                    state.Grid[(int)qIndex.x, (int)qIndex.y] = q;
                }
            }
            return found;
        }

        static float2 GenerateRandomAround(float2 center, float minimumDistance, ref Random random)
        {
            float d = random.NextFloat();
            float radius = minimumDistance + minimumDistance * d;

            d = random.NextFloat();
            float angle = TwoPi * d;

            float newX = radius * math.cos(angle);
            float newY = radius * math.sin(angle);

            return new float2((float)(center.x + newX), (float)(center.y + newY));
        }

        static float2 Denormalize(float2 point, float2 origin, double cellSize)
        {
            return new float2((int)((point.x - origin.x) / cellSize), (int)((point.y - origin.y) / cellSize));
        }

        static float DistanceSquared(float2 value1, float2 value2)
        {
            float x = value1.x - value2.x;
            float y = value1.y - value2.y;

            return (x * x) + (y * y);
        }
    }

}