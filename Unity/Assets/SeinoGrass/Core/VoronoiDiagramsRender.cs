using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = Unity.Mathematics.Random;

namespace SeinoGrass.Core
{
    public class VoronoiDiagramsRender : MonoBehaviour
    {
        [PreviewField]
        public Texture2D VoronoiDiagrams;
        public int SeedCount;
        public int2 Size;
        public Material VoronoiMaterial;
        
        private List<Vector4> m_SeedArray;
        private List<Vector4> m_ColorArray;
        private NativeArray<Color32> m_VoronoiArray;

        private static readonly int SeedCountProperty = Shader.PropertyToID("_SeedCount");
        private static readonly int SeedArrayProperty = Shader.PropertyToID("_SeedArray");
        private static readonly int ColorArrayProperty = Shader.PropertyToID("_ColorArray");
        

        private void Awake()
        {
            RandomSeed();
            Generate();
        }
        
        private void RandomSeed()
        {
            Random random = new Random(9800);
            int count = SeedCount;
            m_SeedArray = new List<Vector4>();
            m_ColorArray = new List<Vector4>();
            while (count-- > 0)
            {
                m_SeedArray.Add(new Vector4(random.NextFloat(), random.NextFloat()));
                m_ColorArray.Add(new float4(random.NextInt(256), random.NextInt(256), random.NextInt(256), random.NextInt(256)));
            }
        }

        [Button("重新生成")]
        public void ReGenerate()
        {
            RandomSeed();
            Generate();
        }

        private void Generate()
        {
            m_VoronoiArray = new NativeArray<Color32>(SeedCount, Allocator.Persistent);
            
            VoronoiMaterial.SetInt(SeedCountProperty, SeedCount);
            VoronoiMaterial.SetVectorArray(SeedArrayProperty, m_SeedArray);
            VoronoiMaterial.SetVectorArray(ColorArrayProperty, m_ColorArray);

            RenderTexture rt = new RenderTexture(Size.x, Size.y, 0, GraphicsFormat.R8G8B8A8_UNorm);
            rt.name = "Voronoi";
            rt.Create();
            
            Graphics.Blit(null, rt, VoronoiMaterial);
            Texture2D renderMap = new Texture2D(Size.x, Size.y, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            
            RenderTexture.active = rt;
            renderMap.ReadPixels(new Rect(0, 0, Size.x, Size.y), 0, 0);
            renderMap.Apply();
            RenderTexture.active = null;

            VoronoiDiagrams = renderMap;
            m_VoronoiArray = renderMap.GetPixelData<Color32>(0);
        }
    }
}