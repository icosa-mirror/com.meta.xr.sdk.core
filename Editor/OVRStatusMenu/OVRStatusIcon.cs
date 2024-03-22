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
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class OVRStatusIcon
{
    private static readonly Type _toolbarType;
    private static readonly PropertyInfo _guiBackend;
    private static readonly PropertyInfo _visualTree;
    private static readonly FieldInfo _onGuiHandler;

    private static GUIStyle _iconStyle;
    private static OVRGUIContent _currentIcon;
    private static Object _appStatusBar;
    private static VisualElement _container;

    static OVRStatusIcon()
    {
        if (!OVREditorUtils.IsMainEditor()) return;

        var editorAssembly = typeof(UnityEditor.Editor).Assembly;
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        _toolbarType = editorAssembly.GetType("UnityEditor.AppStatusBar");
        var guiViewType = editorAssembly.GetType("UnityEditor.GUIView");
        var backendType = editorAssembly.GetType("UnityEditor.IWindowBackend");
        var containerType = typeof(IMGUIContainer);

        _guiBackend = guiViewType?.GetProperty("windowBackend", bindingFlags);
        _visualTree = backendType?.GetProperty("visualTree", bindingFlags);
        _onGuiHandler = containerType?.GetField("m_OnGUIHandler", bindingFlags);

        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (_appStatusBar == null)
        {
            Refresh();
        }
    }

    private static void Refresh()
    {
        var toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
        if (toolbars == null || toolbars.Length == 0)
        {
            return;
        }

        _appStatusBar = toolbars[0];

        var backend = _guiBackend?.GetValue(_appStatusBar);
        if (backend == null)
        {
            return;
        }

        var elements = _visualTree?.GetValue(backend, null) as VisualElement;
        _container = elements?[0];
        if (_container == null)
        {
            return;
        }

        var handler = _onGuiHandler?.GetValue(_container) as Action;
        if (handler == null)
        {
            return;
        }

        handler -= RefreshGUI;
        handler += RefreshGUI;
        _onGuiHandler.SetValue(_container, handler);
    }

    private static void RefreshGUI()
    {
        if (!OVRProjectSetup.ShowStatusIcon.Value)
        {
            return;
        }

        var screenWidth = _container.layout.width;
        // Hardcoded position
        // Currently overlaps with progress bar, and works with 2020 status bar icons
        // TODO: Better hook to dynamically position the button
        var currentRect = new Rect(screenWidth - 130, 0, 26, 30); // Hardcoded position
        GUILayout.BeginArea(currentRect);
        {
            if (ShowIcon(currentRect))
            {
                OVRStatusMenu.ShowDropdown(GUIUtility.GUIToScreenPoint(Vector2.zero));
            }
        }
        GUILayout.EndArea();
    }

    private static bool ShowIcon(Rect rect)
    {
        var clicked = GUILayout.Button(OVRStatusMenu.styles.StatusIcon, OVRStatusMenu.styles.StatusIconStyle);
        var buttonRect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
        ShowPill(rect);
        return clicked;
    }

    private static void ShowPill(Rect rect)
    {
        var item = OVRStatusMenu.GetHighestItem();

        if (item.PillIcon == null) return;

        var (_, color) = item.PillIcon();

        if (color == null) return;

        rect.x = 12;
        rect.width = OVRStatusMenu.styles.StatusPillIconStyle.fixedWidth;
        rect.height = OVRStatusMenu.styles.StatusPillIconStyle.fixedHeight;
        using (new OVREditorUtils.OVRGUIColorScope(OVREditorUtils.OVRGUIColorScope.Scope.Content, color ?? Color.white))
        {
            GUI.Label(rect, OVRStatusMenu.styles.StatusPillIcon, OVRStatusMenu.styles.StatusPillIconStyle);
        }
    }
}
