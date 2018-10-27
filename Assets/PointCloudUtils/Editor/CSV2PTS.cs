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
    public class CSV2PTS : EditorWindow
    {
        Queue queue;

        bool isRunning;

        [MenuItem("Custom/CSV2PTS")]
        static void Init()
        {
            EditorWindow.GetWindow<CSV2PTS>(true, "PTS2Texture");
        }

        void OnGUI()
        {
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
                EditorUtility.DisplayProgressBar("CSV2PTS", "converting...(" + Mathf.RoundToInt(percent) + "%)", progress);
            }
        }

        async void Convert()
        {
            queue = new Queue();
            queue = Queue.Synchronized(queue);
            isRunning = true;
            var csvFilePath = EditorUtility.OpenFilePanel("csvFile", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(csvFilePath)) return;
            var saveFile = EditorUtility.SaveFilePanel("saveFile", Application.dataPath, Path.GetFileNameWithoutExtension(csvFilePath), "pts");
            if (string.IsNullOrEmpty(saveFile)) return;
            await Convert(csvFilePath, saveFile, onProgress);
            EditorUtility.ClearProgressBar();
            queue = null;

        }

        async Task Convert(string csvFilePath, string saveFile, Action<float> onProgress = null)
        {
            await Task.Run(
                async () =>
                {
                    var lineCount = File.ReadAllLines(csvFilePath).Length;
                    using (var streamReader = new StreamReader(csvFilePath))
                    using (var streamWriter = new StreamWriter(saveFile))
                    {
                        streamWriter.WriteLine(lineCount);
                        var count = 0;
                        while (!streamReader.EndOfStream)
                        {
                            var line = streamReader.ReadLine();
                            streamWriter.WriteLine(line.Replace(",", " "));
                            count++;
                            onProgress?.Invoke((float)count / lineCount);
                            if (!isRunning) break;
                        }
                    }
                }
                );

            if (!isRunning) return;
        }

        private void OnDestroy()
        {
            isRunning = false;
        }
    }
}
#endif
