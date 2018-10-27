using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PTS2PointCloudVoxelView : MonoBehaviour
{
    [SerializeField]
    string fileName;
    [SerializeField]
    float pointSize;
    [SerializeField]
    Shader shader;
    [SerializeField]
    Mesh mesh;

    Material material;

    ComputeBuffer computeBuffer;

    Queue queue;

    [SerializeField]
    int maxCount;
    [SerializeField]
    bool isCentering;
    [SerializeField]
    bool isNormalizedColor;
    [SerializeField]
    bool isAutoLoad = true;
    [SerializeField]
    Vector3 areaScale;

    public event Action<float> LoadingProgressEvent;
#if UNITY_EDITOR
    [SerializeField]
    bool DisplayProgressBar = true;
#endif


    ComputeBuffer argsBuffer;
    bool isRunning;
    bool hasInitialized;

    
    // Use this for initialization
    protected void Start()
    {
        Init();
    }

    public void Init()
    {
        Release();
        if (isAutoLoad)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path)) return;
            Load(path);
        }
        
    }

    async void Load(string path)
    {
        queue = new Queue();
        queue = Queue.Synchronized(queue);
        material = new Material(shader);
        isRunning = true;
        await Load(path, onProgress);
        hasInitialized = true;
        HideProgress();
        queue = null;
    }

    void onProgress(float progress)
    {
        queue.Enqueue(progress);
    }

    private void Update()
    {
        DisplayProgress();
        Refresh();
    }

    void DisplayProgress()
    {
        if (queue == null) return;
        if (queue.Count > 0)
        {
            float progress = 0f;
            while (queue.Count > 0)
            {
                progress = (float)queue.Dequeue();
            }
            LoadingProgressEvent?.Invoke(progress);
#if UNITY_EDITOR
            if (DisplayProgressBar)
            {
                var progressInfo = "Loading...";
                var percent = Mathf.Min(progress * 100f, 100f);
                EditorUtility.DisplayProgressBar("PTS2PointCloud", progressInfo + "(" + Mathf.RoundToInt(percent) + "%)", progress);

            }
#endif
        }
    }

    void HideProgress()
    {
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
    }

    public async Task Load(string path, Action<float> onProgress)
    {
        var points = new List<PointCloudPoint>();
        var min = Vector3.one * float.MaxValue;
        var max = Vector3.one * float.MinValue;
        await Task.Run(() =>
        {
            using (var streamReader = new StreamReader(path))
            {
                var line = streamReader.ReadLine();
                var pointNUM = int.Parse(line);
                var totalCount = Mathf.Min(pointNUM, maxCount);
                var rate = (float)pointNUM / totalCount;
                var count = 0;
                var lineCount = 0;
                var next = 0f;
                while (!streamReader.EndOfStream)
                {
                    line = streamReader.ReadLine();
                    if (lineCount >= next || rate <= 1f)
                    {
                        next += rate;
                        var point = GetPointCloudPoint(line);
                        points.Add(point);
                        min.x = Mathf.Min(min.x, point.position.x);
                        min.y = Mathf.Min(min.y, point.position.y);
                        min.z = Mathf.Min(min.z, point.position.z);

                        max.x = Mathf.Max(max.x, point.position.x);
                        max.y = Mathf.Max(max.y, point.position.y);
                        max.z = Mathf.Max(max.z, point.position.z);

                        onProgress?.Invoke((float)points.Count / (float)totalCount);

                        count++;
                        if (count >= maxCount)
                        {
                            break;
                        }
                    }
                    lineCount++;
                    if (!isRunning) break;
                }
            }
        });
        if (!isRunning) return;
        
        computeBuffer = new ComputeBuffer(points.Count, Marshal.SizeOf(typeof(PointCloudPoint)));
        computeBuffer.SetData(points.ToArray());
        material.SetFloat("_ColorCoefficient", isNormalizedColor ? 1 : 1f / 255f);
        material.SetBuffer("PointCloudPoints", computeBuffer);
        var center = (max + min) / 2f;
        print(max);
        print(min);
        print(center);
        material.SetVector("_Center", isCentering ? center : Vector3.zero);
        var args = new uint[5];
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)computeBuffer.count;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    private PointCloudPoint GetPointCloudPoint(string line)
    {
        var values = line.Split(' ');
        var x = float.Parse(values[0]);
        var y = float.Parse(values[1]);
        var z = float.Parse(values[2]);
        var r = float.Parse(values[4]);
        var g = float.Parse(values[5]);
        var b = float.Parse(values[6]);
        var point = new PointCloudPoint(new Vector3(x, z, y), new Color(r, g, b, 1f));
        return point;

    }
    

    protected virtual void Refresh()
    {
        if (!hasInitialized) return;
        material.SetMatrix("_TRS", Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale));
        material.SetFloat("_PointSize", pointSize);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(this.transform.position, areaScale), argsBuffer);
    }

    protected void Release()
    {
        if (argsBuffer != null) argsBuffer.Release();
        if (computeBuffer != null) computeBuffer.Release();
        isRunning = false;
    }

    protected virtual void OnDestroy()
    {
        Release();
    }
}
