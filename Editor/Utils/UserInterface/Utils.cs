/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Editor.UserInterface
{
    internal static class Utils
    {
        private static readonly HashSet<object> Foldouts = new HashSet<object>();

        public static bool IsMainEditor()
        {
            // Early Return when the process service is not the Editor itself
#if UNITY_2021_1_OR_NEWER
            return (uint)UnityEditor.MPE.ProcessService.level != (uint)UnityEditor.MPE.ProcessLevel.Secondary;
#else
            return (uint)UnityEditor.MPE.ProcessService.level != (uint)UnityEditor.MPE.ProcessLevel.Slave;
#endif
        }

        // Helper function to create a texture with a given color
        private static Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.hideFlags = HideFlags.DontSave;
            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        private static readonly Dictionary<Color, Texture2D> _colorTextures = new();

        public static Texture2D ToTexture(this Color color)
        {
            if (!_colorTextures.TryGetValue(color, out var texture))
            {
                texture = MakeTexture(1, 1, color);
                _colorTextures.Add(color, texture);
            }

            return texture;
        }

        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte r = (byte)(Convert.ToInt32(hex.Substring(0, 2), 16));
            byte g = (byte)(Convert.ToInt32(hex.Substring(2, 2), 16));
            byte b = (byte)(Convert.ToInt32(hex.Substring(4, 2), 16));
            byte a = 255;

            if (hex.Length == 8)
            {
                a = (byte)(Convert.ToInt32(hex.Substring(6, 2), 16));
            }

            return new Color32(r, g, b, a);
        }

        public readonly struct IndentScope : System.IDisposable
        {
            private readonly int _previousIndentLevel;

            public IndentScope(int indentLevel)
            {
                _previousIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indentLevel;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel = _previousIndentLevel;
            }
        }

        public readonly struct ColorScope : System.IDisposable
        {
            public enum Scope
            {
                All,
                Background,
                Content
            }

            private readonly Color _previousColor;
            private readonly Scope _scope;

            public ColorScope(Scope scope, Color newColor)
            {
                _scope = scope;
                _previousColor = Color.white;
                switch (scope)
                {
                    case Scope.All:
                        _previousColor = GUI.color;
                        GUI.color = newColor;
                        break;
                    case Scope.Background:
                        _previousColor = GUI.backgroundColor;
                        GUI.backgroundColor = newColor;
                        break;
                    case Scope.Content:
                        _previousColor = GUI.contentColor;
                        GUI.contentColor = newColor;
                        break;
                }
            }

            public void Dispose()
            {
                switch (_scope)
                {
                    case Scope.All:
                        GUI.color = _previousColor;
                        break;
                    case Scope.Background:
                        GUI.backgroundColor = _previousColor;
                        break;
                    case Scope.Content:
                        GUI.contentColor = _previousColor;
                        break;
                }
            }
        }

        public static bool Foldout(object handle, string label, float offset = 0.0f, GUIStyle style = null)
        {
            var rect = GUILayoutUtility.GetRect(256, EditorGUIUtility.singleLineHeight + 4);

            rect.x += offset;

            var foldout = Foldouts.Contains(handle);
            var newFoldout = EditorGUI.Foldout(rect, foldout, label, true, style ?? Styles.GUIStyles.FoldoutLeft);
            if (foldout != newFoldout)
            {
                foldout = newFoldout;
                if (newFoldout)
                {
                    Foldouts.Add(handle);
                }
                else
                {
                    Foldouts.Remove(handle);
                }
            }

            return newFoldout;
        }
    }
}
