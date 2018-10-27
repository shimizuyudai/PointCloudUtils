using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;

namespace PointCloudUtils
{
    public class Texture2PointCloud : MonoBehaviour
    {
        [SerializeField]
        Texture2D pointTexture, colorTexture, positionTexture;
        [SerializeField]
        ComputeShader computeShader;
        [SerializeField]
        Shader shader;
        [SerializeField]
        float pointSize;

        ComputeBuffer computeBuffer;
        Material material;
        int count;

        // Use this for initialization
        void Start()
        {
            if (colorTexture == null || positionTexture == null) return;

            material = new Material(shader);
            count = colorTexture.width * colorTexture.height;
            var points = new PointCloudPoint[count];
            Parallel.For(0, count, i =>
            {
                points[i] = new PointCloudPoint(Vector3.zero, Color.black);
            });
            computeShader = Instantiate(computeShader) as ComputeShader;
            computeBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(PointCloudPoint)));
            computeBuffer.SetData(points);
            computeShader.SetTexture(0, "ColorTex", colorTexture);
            computeShader.SetTexture(0, "PositionTex", positionTexture);
            computeShader.SetVector("_Scale", this.transform.localScale);
            computeShader.SetVector("_Offset", this.transform.position);
            computeShader.SetVector("_Center", Vector3.zero);
            computeShader.SetBuffer(0, "_PointCloudPoints", computeBuffer);
            computeShader.Dispatch(0, Mathf.CeilToInt(colorTexture.width / 8), Mathf.CeilToInt(colorTexture.height / 8), 1);
            material.SetBuffer("PointCloudPoints", computeBuffer);


        }

        // Update is called once per frame
        void Update()
        {
            computeShader.SetVector("_Scale", this.transform.localScale);
            computeShader.SetVector("_Offset", this.transform.position);
            computeShader.SetVector("_Center", Vector3.zero);
            computeShader.SetVector("_Angles", this.transform.localEulerAngles);
            computeShader.SetMatrix("_TRS", Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.lossyScale));
            computeShader.Dispatch(0, Mathf.CeilToInt(colorTexture.width / 8), Mathf.CeilToInt(colorTexture.height / 8), 1);
        }

        void OnRenderObject()
        {
            material.SetTexture("_MainTex", pointTexture);
            material.SetFloat("_PointSize", pointSize);
            material.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, count);
        }

        private void OnDestroy()
        {
            if (computeBuffer != null) computeBuffer.Release();
        }
    }
}
