using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Shows the last few errors/exceptions on screen (IMGUI, independent of the Input System and
    /// UGUI), so a problem on a phone can be reported with a screenshot instead of logcat.
    /// Tap the panel to hide it. Invisible while nothing has gone wrong.
    /// </summary>
    public sealed class ErrorOverlay : MonoBehaviour
    {
        private const int MaxLines = 6;
        private static readonly Queue<string> Lines = new();
        private GUIStyle _style;
        private bool _hidden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Lines.Clear();
            var go = new GameObject("[ErrorOverlay]");
            DontDestroyOnLoad(go);
            go.AddComponent<ErrorOverlay>();
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string firstFrame = string.IsNullOrEmpty(stackTrace) ? "" : "\n   " + stackTrace.Split('\n')[0];
            Lines.Enqueue($"{type}: {message}{firstFrame}");
            while (Lines.Count > MaxLines) Lines.Dequeue();
        }

        private void OnGUI()
        {
            if (Lines.Count == 0 || _hidden) return;
            _style ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Max(18, Screen.height / 70),
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.55f, 0.55f) }
            };

            var rect = new Rect(10, Screen.height * 0.55f, Screen.width - 20, Screen.height * 0.4f);
            if (GUI.Button(rect, string.Join("\n", Lines), _style)) _hidden = true;
        }
    }
}
