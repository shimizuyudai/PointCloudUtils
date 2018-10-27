using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class VTFPointCloudVoxelView : MonoBehaviour
{
    [SerializeField]
    Texture2D pointTexture, colorTexture, positionTexture;
    [SerializeField]
    Shader shader;
    
    [SerializeField]
    public float pointSize;
    Material material;
    ComputeBuffer argsBuffer;

    [SerializeField]
    Vector3 areaScale;

    [SerializeField]
    Mesh mesh;
    // Use this for initialization
    protected void Start()
    {
        Init();
    }

    public void Init()
    {
        material = new Material(shader);
        var count = colorTexture.width * colorTexture.height;
        
        Release();
        var args = new uint[5];
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        material.SetTexture("_ColorTex", colorTexture);
        material.SetTexture("_PositionTex", positionTexture);
    }

    protected void Release()
    {
        if (argsBuffer != null) argsBuffer.Release();
    }

    // Update is called once per frame
    protected void Update()
    {
        Refresh();
    }

    protected virtual void Refresh()
    {
        material.SetMatrix("_TRS", Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale));
        material.SetFloat("_PointSize", pointSize);
        material.SetVector("_Center", Vector3.zero);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(this.transform.position, areaScale), argsBuffer);
    }

    protected virtual void OnDestroy()
    {
        Release();
    }
}
