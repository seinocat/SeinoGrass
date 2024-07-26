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
#if UNITY_EDITOR
        [CustomValueDrawer("DrawTexturePreview")]
        public Texture2D VoronoiDiagrams;
#endif
        
        public int SeedCount;
        public int2 Size;
        public Material VoronoiMaterial;
        public List<SeedPointData> SeedPointDatas;
        
        [NonSerialized]
        public NativeArray<Color32> VoronoiArray;

        private Vector4[] m_SeedArray;
        private Vector4[] m_ColorArray;
        private Texture2D m_RenderMap;
        
        private static readonly int SeedCountProperty = Shader.PropertyToID("_SeedCount");
        private static readonly int SeedArrayProperty = Shader.PropertyToID("_SeedArray");
        private static readonly int ColorArrayProperty = Shader.PropertyToID("_ColorArray");
        
        Random random = new(9800);

        private void Awake()
        {
            RandomSeed();
            Generate();
        }
        
        [Button("重新生成")]
        public void ReGenerate()
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
            List<float4> repeatColors = new List<float4>() { float4.zero };
            
            while (count < SeedCount) //防止颜色值重复
            {
                float4 color = float4.zero;
                while (repeatColors.Contains(color))
                {
                    byte r = (byte)random.NextInt(256);
                    byte g = (byte)random.NextInt(256);
                    byte b = (byte)random.NextInt(256);
                    byte a = (byte)random.NextInt(256);
                    color = new float4(r, g, b, a);
                }
                
                m_SeedArray[count] = new Vector4(random.NextFloat(), random.NextFloat());
                m_ColorArray[count] = color;
                SeedPointDatas.Add(new SeedPointData()
                {
                    Uv = new float2(m_SeedArray[count].x, m_SeedArray[count].y),
                    Color = new Color32((byte)color.x, (byte)color.y, (byte)color.z, (byte)color.w)
                });

                count++;
            }
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

            m_RenderMap = renderMap;
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


#if UNITY_EDITOR
        private Texture2D DrawTexturePreview()
        {
            Texture2D texture = m_RenderMap;
            if (texture != null)
            {
                Rect rect =  UnityEditor.EditorGUILayout.GetControlRect(false, GUILayout.Height(180));
                UnityEditor.EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit, 0, 0);
            }
            return texture;
        }
#endif
        
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