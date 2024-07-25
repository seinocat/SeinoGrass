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
        public List<SeedPointData> SeedPointDatas;

        private Vector4[] m_SeedArray;
        private Vector4[] m_ColorArray;
        [NonSerialized]
        public NativeArray<Color32> VoronoiArray;

        private static readonly int SeedCountProperty = Shader.PropertyToID("_SeedCount");
        private static readonly int SeedArrayProperty = Shader.PropertyToID("_SeedArray");
        private static readonly int ColorArrayProperty = Shader.PropertyToID("_ColorArray");
        Random random = new(9800);

        private void Awake()
        {
            RandomSeed();
            Generate();
        }
        
        private void RandomSeed()
        {
            int count = 0;
            m_SeedArray = new Vector4[512];
            m_ColorArray = new Vector4[512];
            SeedPointDatas = new List<SeedPointData>();
            while (count < SeedCount)
            {
                m_SeedArray[count] = new Vector4(random.NextFloat(), random.NextFloat());
                byte r = (byte)random.NextInt(256);
                byte g = (byte)random.NextInt(256);
                byte b = (byte)random.NextInt(256);
                byte a = (byte)random.NextInt(256);
                m_ColorArray[count] = new float4(r,g,b,a);
                SeedPointDatas.Add(new SeedPointData()
                {
                    Uv = new float2(m_SeedArray[count].x, m_SeedArray[count].y),
                    Color = new Color32(r,g,b,a)
                });

                count++;
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
            VoronoiArray = new NativeArray<Color32>(SeedCount, Allocator.Persistent);
            
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
            VoronoiArray = renderMap.GetPixelData<Color32>(0);

            for (int i = 0; i < VoronoiArray.Length; i++)
            {
                for (int j = 0; j < SeedPointDatas.Count; j++)
                {
                    if (SeedPointDatas[j].Color.Equals(VoronoiArray[i]))
                    {
                        SeedPointDatas[j].AddAnchor(i, Size);
                        break;
                    }
                }
            }
        }
    }


    public class SeedPointData
    {
        public float2 Uv;
        public Color32 Color;
        public List<int> Anchors = new ();
        public int2 ChunkSize;//预估宽高
        public int2 WidthAnchor;
        public int2 HeightAnchor;

        public void AddAnchor(int anchor, int2 size)
        {
            int2 uv = new int2(anchor % size.x, anchor / size.x);
            if (uv.x < WidthAnchor.x) WidthAnchor.x = uv.x;
            if (uv.x > WidthAnchor.y) WidthAnchor.y = uv.x;
            if (uv.y < HeightAnchor.x) HeightAnchor.x = uv.y;
            if (uv.y > HeightAnchor.y) HeightAnchor.y = uv.y;

            ChunkSize = new int2(WidthAnchor.y - WidthAnchor.x, HeightAnchor.y - HeightAnchor.x);
            
            Anchors.Add(anchor);
        }
    }
}