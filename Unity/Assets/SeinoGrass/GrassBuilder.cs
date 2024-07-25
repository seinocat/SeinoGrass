using System.Collections.Generic;
using SeinoGrass.Core;
using SeinoGrass.Utils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace SeinoGrass
{
    public class GrassBuilder : MonoBehaviour
    {
        public GameObject Grass;
        public float Radius = 0.2f;
        public float2 Corner0;
        public float2 Corner1;
        public VoronoiDiagramsRender VoronoiRender;

        [Button("生成")]
        public void Build()
        {
            Grass.SetActive(false);
            List<float2> points = PoissonDiskSampling.Sample(Corner0, Corner1, Radius);
            GameObject root = new GameObject("GrassRoot");
            for (int i = 0; i < points.Count; i++)
            {
                float2 point = points[i];
                var go = Instantiate(Grass, new float3(point.x, 0, point.y), Quaternion.identity);
                go.SetActive(true);
                go.transform.SetParent(root.transform);
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(Corner0.x, 0, Corner0.y), new Vector3(Corner0.x, 0, Corner1.y));
            Gizmos.DrawLine(new Vector3(Corner0.x, 0, Corner1.y), new Vector3(Corner1.x, 0, Corner1.y));
            Gizmos.DrawLine(new Vector3(Corner1.x, 0, Corner1.y), new Vector3(Corner1.x, 0, Corner0.y));
            Gizmos.DrawLine(new Vector3(Corner0.x, 0, Corner0.y), new Vector3(Corner1.x, 0, Corner0.y));
        }
    }
}