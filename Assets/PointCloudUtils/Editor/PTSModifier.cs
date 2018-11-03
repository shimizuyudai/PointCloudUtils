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
using System.Collections.Concurrent;
using System.Linq;

namespace PointCloudUtils
{
    public class PTSModifier : EditorWindow
    {
        private float amountRate;
        private float noiseScale;
        Queue queue;

        bool isRunning;
        bool modifyAmount, normalizePosition, normalizeColor;

        Vector3 scale;

        [MenuItem("Custom/PTSModifier")]
        static void Init()
        {
            EditorWindow.GetWindow<PTSModifier>(true, "PTSModifier");
        }

        void OnGUI()
        {
            modifyAmount = EditorGUILayout.ToggleLeft("ModifyAmount", modifyAmount);
            if (modifyAmount)
            {
                amountRate = EditorGUILayout.FloatField("AmountRate", amountRate);
                noiseScale = EditorGUILayout.FloatField("NoiseScale", noiseScale);
            }

            normalizePosition = EditorGUILayout.ToggleLeft("NormalizeScale", normalizePosition);
            normalizeColor = EditorGUILayout.ToggleLeft("NormalizeColor", normalizeColor);

            if (GUILayout.Button("Modify")) Modify();

        }

        void onProgress(ProgressInfo info)
        {
            queue.Enqueue(info);
        }

        void Update()
        {
            if (queue == null) return;
            if (queue.Count > 0)
            {
                ProgressInfo info = null;
                while (queue.Count > 0)
                {
                    info = queue.Dequeue() as ProgressInfo;
                }
                if (info == null) return;

                EditorUtility.DisplayProgressBar("Complement", info.message, info.progress);
            }
        }

        public class ProgressInfo
        {
            public float progress;
            public string message;
        }

        async void Modify()
        {
            isRunning = true;
            var fromFile = EditorUtility.OpenFilePanel("from", Application.streamingAssetsPath, "pts");
            if (string.IsNullOrEmpty(fromFile)) return;
            var directory = Path.GetDirectoryName(fromFile);
            var filenName = "Modified" + Path.GetFileNameWithoutExtension(fromFile);
            var toFile = EditorUtility.SaveFilePanel("to", directory, filenName, "pts");
            if (string.IsNullOrEmpty(toFile)) return;
            queue = new Queue();
            queue = Queue.Synchronized(queue);
            await Modify(fromFile, toFile, onProgress);
            EditorUtility.ClearProgressBar();
            queue = null;
        }

        private static float Map(float value, float start1, float stop1, float start2, float stop2)
        {
            return ((value - start1) / (stop1 - start1)) * (stop2 - start2) + start2;
        }

        async Task Modify(string fromPath, string toPath, Action<ProgressInfo> onProgress = null)
        {
            await Task.Run(
                 () =>
                 {
                     using (var streamWriter = new StreamWriter(toPath))
                     {
                         var streamReader = new StreamReader(fromPath);
                         var min = Vector3.one * float.MaxValue;
                         var max = Vector3.one * float.MinValue;

                         var line = streamReader.ReadLine();
                         var pointNUM = int.Parse(line);
                         var totalCount = modifyAmount ? pointNUM * (int)this.amountRate : pointNUM;
                         var rate = (float)pointNUM / totalCount;

                         var next = 0f;
                         var count = 0;
                         var lineCount = 0;

                         var points = new List<PTSPoint>();
                         streamWriter.WriteLine(totalCount);
                         while (!streamReader.EndOfStream)
                         {
                             line = streamReader.ReadLine();
                             if (lineCount >= next || !modifyAmount)
                             {
                                 next += rate;
                                 var values = line.Split(' ');
                                 var x = float.Parse(values[0]);
                                 var y = float.Parse(values[1]);
                                 var z = float.Parse(values[2]);
                                 var i = float.Parse(values[3]);
                                 var r = float.Parse(values[4]);
                                 var g = float.Parse(values[5]);
                                 var b = float.Parse(values[6]);


                                 var position = new Vector3(x, y, z);


                                 if (!normalizePosition)
                                 {
                                     streamWriter.WriteLine(line);
                                     if (modifyAmount)
                                     {
                                         var color = new Vector3(r, g, b);
                                         if (normalizeColor) color /= 255f;
                                         var point = new PTSPoint(position, i, color);
                                         points.Add(point);
                                     }
                                 }
                                 else
                                 {
                                     min.x = Mathf.Min(min.x, position.x);
                                     min.y = Mathf.Min(min.y, position.y);
                                     min.z = Mathf.Min(min.z, position.z);
                                     max.x = Mathf.Max(max.x, position.x);
                                     max.y = Mathf.Max(max.y, position.y);
                                     max.z = Mathf.Max(max.z, position.z);
                                 }

                                 count++;
                                 var progress = Mathf.Min((float)count / (float)pointNUM, 1f);
                                 var percent = Mathf.Min(progress * 100f, 100f);
                                 var message = "processing...(" + Mathf.RoundToInt(percent) + "%)";

                                 var info = new ProgressInfo { message = message, progress = progress };
                                 onProgress?.Invoke(info);
                                 if (count >= totalCount)
                                 {
                                     break;
                                 }
                             }
                             lineCount++;
                             if (!isRunning) break;
                         }
                         var box = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));

                         var scaleRate = Vector3.one;
                         var maxLength = Mathf.Max(box.x, box.y, box.z);
                         scaleRate = box / maxLength;

                         if (normalizePosition)
                         {
                             streamReader.Dispose();
                             streamReader = new StreamReader(fromPath);
                             streamReader.ReadLine();
                             next = 0f;
                             count = 0;
                             lineCount = 0;
                             while (!streamReader.EndOfStream)
                             {
                                 line = streamReader.ReadLine();
                                 if (lineCount >= next || !modifyAmount)
                                 {
                                     next += rate;
                                     var values = line.Split(' ');
                                     var x = float.Parse(values[0]);
                                     var y = float.Parse(values[1]);
                                     var z = float.Parse(values[2]);
                                     var i = float.Parse(values[3]);
                                     var r = float.Parse(values[4]);
                                     var g = float.Parse(values[5]);
                                     var b = float.Parse(values[6]);

                                     var position = new Vector3(x, y, z);


                                     position.x = Map(position.x, min.x, max.x, -1f, 1f) * scaleRate.x;
                                     position.y = Map(position.y, min.y, max.y, -1f, 1f) * scaleRate.y;
                                     position.z = Map(position.z, min.z, max.z, -1f, 1f) * scaleRate.z;
                                     var color = new Vector3(r, g, b);
                                     if (normalizeColor) color /= 255f;
                                     var point = new PTSPoint(position, i, new Vector3(r, g, b));
                                     streamWriter.WriteLine(position.x + " " + position.y + " " + position.z + " " + i + " " + color.x + " " + color.y + " " + color.z);
                                     if (modifyAmount)
                                     {
                                         points.Add(point);
                                     }

                                     count++;
                                     var progress = Mathf.Min((float)count / (float)totalCount, 1f);
                                     var percent = Mathf.Min(progress * 100f, 100f);
                                     var info = new ProgressInfo { message = "normalizing...(" + Mathf.RoundToInt(percent) + "%)", progress = progress };
                                     onProgress?.Invoke(info);
                                     if (count >= totalCount)
                                     {
                                         break;
                                     }
                                 }
                                 lineCount++;
                                 if (!isRunning) break;
                             }
                         }

                         streamReader.Dispose();

                         if (modifyAmount)
                         {
                             if (count < totalCount)
                             {
                                 var num = totalCount - count;
                                 var step = Mathf.FloorToInt((float)count / num);
                                 step = Mathf.Max(1, step);
                                 var additionalLines = new ConcurrentBag<string>();
                                 {
                                     var progress = Mathf.Min((float)(count) / (float)totalCount, 1f);
                                     var percent = Mathf.Min(progress * 100f, 100f);
                                     var info = new ProgressInfo { message = "start order...(" + Mathf.RoundToInt(percent) + "%)", progress = progress };
                                 }

                                 var random = new System.Random();
                                 for (var i = 0; i < num; i++)
                                 {
                                     var index = (i * step) % points.Count;
                                     var nextIndex = index % points.Count;
                                     var p = points[index];
                                     var r = (float)random.NextDouble() * noiseScale;
                                     var angle = (float)random.NextDouble() * Mathf.PI * 2.0f;
                                     var t = (float)random.NextDouble() * 2f - 1f;
                                     var x = (float)Mathf.Cos(angle) * Mathf.Sqrt(1f - t * t) * r;
                                     var y = (float)Mathf.Sin(angle) * Mathf.Sqrt(1f - t * t) * r;
                                     var z = (float)t * r;
                                     var position = p.position + new Vector3(x, y, z);
                                     streamWriter.WriteLine(position.x + " " + position.y + " " + position.z + " " + p.intensity + " " + p.color.x + " " + p.color.y + " " + p.color.z);
                                     var progress = Mathf.Min((float)i / (float)num, 1f);
                                     var percent = Mathf.Min(progress * 100f, 100f);
                                     var info = new ProgressInfo { message = "complementing...(" + Mathf.RoundToInt(percent) + "%)", progress = progress };
                                     onProgress?.Invoke(info);
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
