using System;
using System.Collections.Generic;
using SeinoGrass.Core;
using SeinoGrass.Utils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SeinoGrass
{
    public class GrassBuilder : MonoBehaviour
    {
        public GameObject Grass;
        public float Radius = 0.2f;
        public float2 Corner0;
        public float2 Corner1;
        public VoronoiDiagramsRender VoronoiRender;
        Random random = new Random(4712);
        
        
        public Mesh RenderMesh;
        public Material GrassMaterial;
        public ComputeShader ComputeShader;
        
        private ComputeBuffer m_TRSBuffer;
        private ComputeBuffer m_VisibleBuffer;
        private static readonly int TRSBufferProperty = Shader.PropertyToID("_TRSBuffer");
        private static readonly int VisibleBufferProperty = Shader.PropertyToID("_VisibleBuffer");
        private List<float2> points = new();

        private void Update()
        {
            DrawInstance();
        }

        [Button("泊松圆盘生成")]
        public void Build()
        {
            Grass.SetActive(false);
            List<float2> samples = PoissonDiskSampling.Sample(Corner0, Corner1, Radius);
            points.AddRange(samples);
        }
        
        [Button("维诺-泊松圆盘生成")]
        public void VoronoiBuild()
        {
            Grass.SetActive(false);
            for (int j = 0; j < VoronoiRender.SeedPointDatas.Count; j++)
            {
                var Seed = VoronoiRender.SeedPointDatas[j];
                float2 p0 = Corner0 + new float2(Seed.Uv.x * (Corner1.x - Corner0.x), Seed.Uv.y * (Corner1.y - Corner0.y));
                List<float2> samples = VoronoiPoissonSampling.Sample(Corner0, Corner1, p0, Radius, VoronoiRender.VoronoiArray, Seed.Color);
                points.AddRange(samples);
            }
        }
        
        private void DrawInstance()
        {
            int instanceCount = points.Count;
            if (instanceCount == 0)
                return;

            ComputeBuffer argsBuff = new ComputeBuffer(instanceCount, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            uint[] args = new uint[5];
            args[0] = RenderMesh.GetIndexCount(0);
            args[1] = (uint)instanceCount;
            args[2] = RenderMesh.GetIndexStart(0);
            args[3] = RenderMesh.GetBaseVertex(0);
            args[4] = 0;
            argsBuff.SetData(args);

            List<Matrix4x4> trsMatrixs = new List<Matrix4x4>();
            //设置坐标
            for (int i = 0; i < instanceCount; i++)
            {
                Vector3 pos = new Vector3(points[i].x, 0, points[i].y);
                var matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                trsMatrixs.Add(matrix);
            }
            m_TRSBuffer?.Release();
            m_TRSBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 16);
            m_TRSBuffer.SetData(trsMatrixs);

            m_VisibleBuffer?.Release();
            m_VisibleBuffer = new ComputeBuffer(instanceCount, sizeof(uint), ComputeBufferType.Append);
            
            GrassMaterial.SetBuffer(TRSBufferProperty, m_TRSBuffer);
            
            //视锥剔除
            Camera mainCamera = Camera.main;
            ComputeShader.SetBuffer(0, VisibleBufferProperty, m_VisibleBuffer);
            ComputeShader.SetBuffer(0, TRSBufferProperty, m_TRSBuffer);
            ComputeShader.SetVector("_BoundMin", new Vector3(-0.5f,0,-0.5f));
            ComputeShader.SetVector("_BoundMax", new Vector3(0.5f,0.5f,0.5f));
            ComputeShader.SetMatrix("_VpMatrix", mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix);

            m_VisibleBuffer.SetCounterValue(0);
            ComputeShader.Dispatch(0, Mathf.CeilToInt(instanceCount/640f), 1, 1);
            ComputeBuffer.CopyCount(m_VisibleBuffer, argsBuff, sizeof(uint));
            
            Graphics.DrawMeshInstancedIndirect(RenderMesh, 0, GrassMaterial, new Bounds(Vector3.zero, Vector3.one * 100), argsBuff);
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