using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VTFPointCloudView : MonoBehaviour
{
    [SerializeField]
    public Texture2D pointTexture, colorTexture, positionTexture;
    [SerializeField]
    Shader shader;
    [SerializeField]
    float pointSize;
    Material material;
    int count;
    // Use this for initialization
    void Start()
    {
        material = new Material(shader);
        count = colorTexture.width * colorTexture.height;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnRenderObject()
    {
        material.SetTexture("_MainTex", pointTexture);
        material.SetTexture("_ColorTex", colorTexture);
        material.SetTexture("_PositionTex", positionTexture);
        material.SetMatrix("_TRS", Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale));
        material.SetFloat("_PointSize", pointSize);
        material.SetVector("_Center", Vector3.zero);
        material.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, count);
    }
}
