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
    public class PTS2Texture : EditorWindow
    {
        [SerializeField]
        private int maxCount = 2048 * 2048;
        [SerializeField]
        float scale = 1;
        [SerializeField]
        private bool isCentering;
        [SerializeField]
        private bool isNormalizedColor;
        private const int maxTextureSize = 2048;
        Queue queue;

        bool isRunning;

        [MenuItem("Custom/PTS2Texture")]
        static void Init()
        {
            EditorWindow.GetWindow<PTS2Texture>(true, "PTS2Texture");
        }

        void OnGUI()
        {
            isNormalizedColor = EditorGUILayout.ToggleLeft("IsNormalizedColor", isNormalizedColor);
            isCentering = EditorGUILayout.ToggleLeft("IsCentering", isCentering);
            maxCount = EditorGUILayout.IntField("MaxCount", maxCount);
            scale = EditorGUILayout.FloatField("Scale", scale);
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
                EditorUtility.DisplayProgressBar("PTS2Texture", "converting...(" + Mathf.RoundToInt(percent) + "%)", progress);
            }
        }

        async void Convert()
        {
            queue = new Queue();
            queue = Queue.Synchronized(queue);
            isRunning = true;
            var ptsFilePath = EditorUtility.OpenFilePanel("ptsFile", Application.dataPath, "pts");
            if (string.IsNullOrEmpty(ptsFilePath)) return;
            var saveDirectory = EditorUtility.SaveFolderPanel("saveDirectory", Application.dataPath, "");
            if (string.IsNullOrEmpty(saveDirectory)) return;
            if (!saveDirectory.Contains(Application.dataPath))
            {
                Debug.LogError("choose folder in this project.");
                return;
            }
            await Convert(ptsFilePath, saveDirectory, onProgress);
            EditorUtility.ClearProgressBar();
            queue = null;

        }

        async Task Convert(string ptsFilePath, string directory, Action<float> onProgress = null)
        {
            int count = 0;
            maxCount = maxTextureSize * maxTextureSize;
            var fileNamePrefix = Path.GetFileNameWithoutExtension(ptsFilePath);
            var assetFolderPath = FileUtil.GetProjectRelativePath(directory);
            var size = 0;
            var min = Vector3.one * float.MaxValue;
            var max = Vector3.one * float.MinValue;
            var colors = new List<Vector3>();
            var positions = new List<Vector3>();

            await Task.Run(
                () =>
                {
                    using (var streamReader = new StreamReader(ptsFilePath))
                    {
                        var lineCount = 0;
                        var line = streamReader.ReadLine();
                        var pointNUM = int.Parse(line);
                        var totalCount = Mathf.Min(pointNUM, maxCount);
                        var rate = (float)pointNUM / totalCount;

                        var next = 0f;
                        size = Mathf.Min(maxTextureSize, Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(totalCount))));
                        while (!streamReader.EndOfStream)
                        {
                            line = streamReader.ReadLine();

                            if (lineCount >= next || rate <= 1f)
                            {
                                next += rate;
                                var values = line.Split(' ');
                                var x = float.Parse(values[0]);
                                var y = float.Parse(values[1]);
                                var z = float.Parse(values[2]);
                                var r = float.Parse(values[4]);
                                var g = float.Parse(values[5]);
                                var b = float.Parse(values[6]);
                                var position = new Vector3(x, z, y);
                                colors.Add(new Vector3(r, g, b));
                                positions.Add(position);

                                if (isCentering)
                                {
                                    min.x = Mathf.Min(min.x, position.x);
                                    min.y = Mathf.Min(min.y, position.y);
                                    min.z = Mathf.Min(min.z, position.z);
                                    max.x = Mathf.Max(max.x, position.x);
                                    max.y = Mathf.Max(max.y, position.y);
                                    max.z = Mathf.Max(max.z, position.z);
                                }

                                count++;
                                var progress = Mathf.Min((float)count / (float)totalCount, 1f);
                                onProgress?.Invoke(progress);
                                if (count >= totalCount)
                                {
                                    break;
                                }
                            }
                            lineCount++;
                            if (!isRunning) break;
                        }
                    }
                }
                );

            if (!isRunning) return;
            var colorBuffer = new ComputeBuffer(colors.Count, Marshal.SizeOf(typeof(Vector3)));
            var positionBuffer = new ComputeBuffer(positions.Count, Marshal.SizeOf(typeof(Vector3)));
            colorBuffer.SetData(colors.ToArray());
            positionBuffer.SetData(positions.ToArray());
            var computeShader = (ComputeShader)Resources.Load("PointBuffer2Texture");
            var format = RenderTextureFormat.ARGBHalf;
            var colorRenderTexture = new RenderTexture(size, size, 24, format);
            var positionRenderTexture = new RenderTexture(size, size, 24, format);
            colorRenderTexture.enableRandomWrite = true;
            colorRenderTexture.Create();
            positionRenderTexture.enableRandomWrite = true;
            positionRenderTexture.Create();
            computeShader.SetTexture(0, "ColorTex", colorRenderTexture);
            computeShader.SetTexture(0, "PositionTex", positionRenderTexture);
            computeShader.SetVector("_Center", isCentering ? (min + max) / 2f : Vector3.zero);
            computeShader.SetFloat("_ColorCoefficient", isNormalizedColor ? 1f : 1f / 255f);
            computeShader.SetFloat("_Scale", scale);
            computeShader.SetBuffer(0, "ColorBuffer", colorBuffer);
            computeShader.SetBuffer(0, "PositionBuffer", positionBuffer);
            computeShader.Dispatch(0, Mathf.CeilToInt((float)size / 8) + 1, Mathf.CeilToInt((float)size / 8) + 1, 1);

            var colorTex = TextureUtils.CreateTexture2DFromRenderTexture(colorRenderTexture);
            var positionTex = TextureUtils.CreateTexture2DFromRenderTexture(positionRenderTexture);
            Graphics.CopyTexture(colorRenderTexture, colorTex);
            Graphics.CopyTexture(positionRenderTexture, positionTex);
            SaveTexture(assetFolderPath, fileNamePrefix + "Color", colorTex);
            SaveTexture(assetFolderPath, fileNamePrefix + "Position", positionTex);

            colorBuffer.Dispose();
            positionBuffer.Dispose();
            colorRenderTexture.Release();
            positionRenderTexture.Release();
        }

        private void SaveTexture(string folder, string textureName, Texture2D texture)
        {
            var path = folder + "/" + textureName + ".asset";
            AssetDatabase.CreateAsset(texture, path);
        }

        private void OnDestroy()
        {
            isRunning = false;
        }
    }
}
#endif
