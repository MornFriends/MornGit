using System;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MornGit
{
    public static class MornGitUtil
    {
#if DISABLE_MORN_GIT_LOG
        private const bool ShowLOG = false;
#else
        private const bool ShowLOG = true;
#endif
        private const string Prefix = "[<color=green>MornGit</color>] ";

        internal static bool GetFlag(string path, string parameterName, bool defaultValue)
        {
            return EditorPrefs.GetBool($"MornGit_{path}_{parameterName}", defaultValue);
        }

        internal static void SetFlag(string path, string parameterName, bool flag)
        {
            EditorPrefs.SetBool($"MornGit_{path}_{parameterName}", flag);
        }

        internal static void H(Action action)
        {
            using (new GUILayout.HorizontalScope())
            {
                action();
            }
        }

        internal static void V(Action action)
        {
            using (new GUILayout.VerticalScope())
            {
                action();
            }
        }

        internal static void ColorIf(bool active, Color color, Action action)
        {
            if (active)
            {
                Color(color, action);
            }
            else
            {
                action();
            }
        }

        internal static void Color(Color color, Action action)
        {
            var cachedColor = GUI.color;
            GUI.color = color;
            action();
            GUI.color = cachedColor;
        }

        internal static void Box(Action action)
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                action();
            }
        }

        internal static void Indent(Action action)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                using (new GUILayout.VerticalScope())
                {
                    action();
                }
            }
        }

        internal static void IndentBox(Action action)
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Space(20);
                using (new GUILayout.VerticalScope())
                {
                    action();
                }
            }
        }

        internal static void EnableColor(bool isEnabled, Color color, Action action)
        {
            Enable(isEnabled, () => Color(isEnabled ? color : GUI.color, action));
        }

        internal static void Enable(bool isEnabled, Action action)
        {
            var cachedEnabled = GUI.enabled;
            GUI.enabled = isEnabled;
            action();
            GUI.enabled = cachedEnabled;
        }

        internal static void Log(string message)
        {
            if (ShowLOG)
            {
                Debug.Log(Prefix + message);
            }
        }

        internal static void LogError(string message)
        {
            if (ShowLOG)
            {
                Debug.LogError(Prefix + message);
            }
        }

        internal static void LogWarning(string message)
        {
            if (ShowLOG)
            {
                Debug.LogWarning(Prefix + message);
            }
        }
    }
}