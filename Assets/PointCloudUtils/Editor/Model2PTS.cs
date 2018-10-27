#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;

namespace PointCloudUtils
{
    public class Model2PTS : EditorWindow
    {
        private int maxCount = 2048 * 2048;
        private Mesh mesh;
        private Texture2D texture;
        Queue queue;

        bool isRunning;

        [MenuItem("Custom/Model2PTS")]
        static void Init()
        {
            EditorWindow.GetWindow<Model2PTS>(true, "Model2PTS");
        }

        void OnGUI()
        {

            maxCount = EditorGUILayout.IntField("MaxCount", maxCount);
            mesh = EditorGUILayout.ObjectField("Mesh", mesh, typeof(Mesh), true) as Mesh;
            texture = EditorGUILayout.ObjectField("Texture", texture, typeof(Texture2D), true) as Texture2D;

            if (GUILayout.Button("Convert")) Convert();

        }

        void onProgress(float progress)
        {
            queue.Enqueue(progress);
        }

        void Update()
        {
            if (queue == null) return;
            if (queue.Count > 0)
            {
                float progress = 0f;
                while (queue.Count > 0)
                {
                    progress = (float)queue.Dequeue();
                }
                var percent = Mathf.Min(progress * 100f, 100f);
                EditorUtility.DisplayProgressBar("Model2PTS", "converting...(" + Mathf.RoundToInt(percent) + "%)", progress);
            }
        }

        async void Convert()
        {
            if (mesh == null || texture == null) return;
            isRunning = true;
            var saveFilePath = EditorUtility.SaveFilePanel("saveFile", Application.dataPath, "", "pts");
            if (string.IsNullOrEmpty(saveFilePath)) return;
            queue = new Queue();
            queue = Queue.Synchronized(queue);
            await Convert(saveFilePath, onProgress);
            EditorUtility.ClearProgressBar();
            queue = null;
        }

        async Task Convert(string saveFilePath, Action<float> onProgress = null)
        {
            var colors = texture.GetPixels();
            var trianglesLength = mesh.triangles.Length;
            var triangles = mesh.triangles;
            var uv = mesh.uv;
            var vertices = mesh.vertices;
            var textureWidth = texture.width;
            var textureHeight = texture.height;
            var isReduction = maxCount < trianglesLength / 3;
            var pointCount = maxCount * 3;
            var step = (float)trianglesLength / pointCount;
            //Debug.Log("step : " + step);
            await Task.Run(
                () =>
                {

                    var random = new System.Random();


                    using (var streamWriter = new StreamWriter(saveFilePath))
                    {
                        streamWriter.WriteLine(maxCount);
                        if (isReduction)
                        {
                            var next = 0f;
                            for (var i = 0; i < trianglesLength; i += 3)
                            {
                                if (i / 3 >= next)
                                {
                                    var t1 = triangles[i];
                                    var t2 = triangles[i + 1];
                                    var t3 = triangles[i + 2];

                                    var uv1 = uv[t1];
                                    var uv2 = uv[t2];
                                    var uv3 = uv[t3];

                                    var v1 = vertices[t1];
                                    var v2 = vertices[t2];
                                    var v3 = vertices[t3];

                                    var r1 = (float)random.NextDouble();
                                    var r2 = (float)random.NextDouble() * (1f - r1);
                                    var r3 = 1f - r1 - r2;

                                    var pos = v1 * r1 + v2 * r2 + v3 * r3;
                                    var texCoord = uv1 * r1 + uv2 * r2 + uv3 * r3;
                                    var colorIndex = (int)(texCoord.x * textureWidth) + (int)(texCoord.y * textureHeight) * textureWidth;
                                    var col = colors[colorIndex];
                                    var str = pos.x + " " + pos.z + " " + pos.y + " 128 " + Mathf.RoundToInt(col.r * 255) + " " + Mathf.RoundToInt(col.g * 255) + " " + Mathf.RoundToInt(col.b * 255);
                                    streamWriter.WriteLine(str);
                                    var progress = (float)i / maxCount;
                                    onProgress?.Invoke(progress);
                                    next += step;
                                }
                            }
                        }
                        else
                        {
                            var index = 0;
                            for (var i = 0; i < pointCount; i += 3)
                            {
                                var t1 = triangles[index];
                                var t2 = triangles[index + 1];
                                var t3 = triangles[index + 2];

                                var uv1 = uv[t1];
                                var uv2 = uv[t2];
                                var uv3 = uv[t3];

                                var v1 = vertices[t1];
                                var v2 = vertices[t2];
                                var v3 = vertices[t3];

                                var r1 = (float)random.NextDouble();
                                var r2 = (float)random.NextDouble() * (1f - r1);
                                var r3 = 1f - r1 - r2;

                                var pos = v1 * r1 + v2 * r2 + v3 * r3;
                                var texCoord = uv1 * r1 + uv2 * r2 + uv3 * r3;
                                var colorIndex = (int)(texCoord.x * textureWidth) + (int)(texCoord.y * textureHeight) * textureWidth;
                                var col = colors[colorIndex];
                                var str = pos.x + " " + pos.z + " " + pos.y + " 128 " + Mathf.RoundToInt(col.r * 255) + " " + Mathf.RoundToInt(col.g * 255) + " " + Mathf.RoundToInt(col.b * 255);
                                streamWriter.WriteLine(str);
                                var progress = (float)i / maxCount;
                                onProgress?.Invoke(progress);
                                index += 3;
                                if (index >= trianglesLength)
                                {
                                    index = 0;
                                }
                            }
                        }

                    }
                }
                );
        }

        private void OnDestroy()
        {
            isRunning = false;
        }
    }
}
#endif
