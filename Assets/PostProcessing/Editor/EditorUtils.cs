using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class EditorUtils
{
    static GraphicsDeviceType[] m_BuildTargets;
    /// <summary>Build targets</summary>
    public static GraphicsDeviceType[] buildTargets => m_BuildTargets ?? (m_BuildTargets = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget));
    
    class Styles
    {
        static readonly Color k_Normal_AllTheme = new Color32(0, 0, 0, 0);
        //static readonly Color k_Hover_Dark = new Color32(70, 70, 70, 255);
        //static readonly Color k_Hover = new Color32(193, 193, 193, 255);
        static readonly Color k_Active_Dark = new Color32(80, 80, 80, 255);
        static readonly Color k_Active = new Color32(216, 216, 216, 255);

        static readonly int s_MoreOptionsHash = "MoreOptions".GetHashCode();

        static public GUIContent moreOptionsLabel { get; private set; }
        static public GUIStyle moreOptionsStyle { get; private set; }
        static public GUIStyle moreOptionsLabelStyle { get; private set; }

        static Styles()
        {
            moreOptionsLabel = EditorGUIUtility.TrIconContent("MoreOptions", "More Options");

            moreOptionsStyle = new GUIStyle(GUI.skin.toggle);
            Texture2D normalColor = new Texture2D(1, 1);
            normalColor.SetPixel(1, 1, k_Normal_AllTheme);
            moreOptionsStyle.normal.background = normalColor;
            moreOptionsStyle.onActive.background = normalColor;
            moreOptionsStyle.onFocused.background = normalColor;
            moreOptionsStyle.onNormal.background = normalColor;
            moreOptionsStyle.onHover.background = normalColor;
            moreOptionsStyle.active.background = normalColor;
            moreOptionsStyle.focused.background = normalColor;
            moreOptionsStyle.hover.background = normalColor;

            moreOptionsLabelStyle = new GUIStyle(GUI.skin.label);
            moreOptionsLabelStyle.padding = new RectOffset(0, 0, 0, -1);
        }


        static public bool DrawMoreOptions(Rect rect, bool active)
        {
            int id = GUIUtility.GetControlID(s_MoreOptionsHash, FocusType.Passive, rect);
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    Color background = k_Normal_AllTheme;
                    if (active)
                        background = EditorGUIUtility.isProSkin ? k_Active_Dark : k_Active;
                    EditorGUI.DrawRect(rect, background);
                    GUI.Label(rect, moreOptionsLabel, moreOptionsLabelStyle);
                    break;
                case EventType.KeyDown:
                    bool anyModifiers = (evt.alt || evt.shift || evt.command || evt.control);
                    if ((evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !anyModifiers && GUIUtility.keyboardControl == id)
                    {
                        evt.Use();
                        GUI.changed = true;
                        return !active;
                    }
                    break;
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition))
                    {
                        GrabMouseControl(id);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (HasMouseControl(id))
                    {
                        ReleaseMouseControl();
                        evt.Use();
                        if (rect.Contains(evt.mousePosition))
                        {
                            GUI.changed = true;
                            return !active;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (HasMouseControl(id))
                        evt.Use();
                    break;
            }

            return active;
        }

        static int s_GrabbedID = -1;
        static void GrabMouseControl(int id) => s_GrabbedID = id;
        static void ReleaseMouseControl() => s_GrabbedID = -1;
        static bool HasMouseControl(int id) => s_GrabbedID == id;
    }

    public static void DrawFixMeBox(string text, Action action)
    {
        EditorGUILayout.HelpBox(text, MessageType.Warning);

        GUILayout.Space(-32);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Fix", GUILayout.Width(60)))
                action();

            GUILayout.Space(8);
        }
        GUILayout.Space(11);
    }

    public static void DrawSplitter(bool isBoxed = false)
    {
        var rect = GUILayoutUtility.GetRect(1f, 1f);
        float xMin = rect.xMin;

        // Splitter rect should be full-width
        rect.xMin = 0f;
        rect.width += 4f;

        if (isBoxed)
        {
            rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
            rect.width -= 1;
        }

        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
            ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
            : new Color(0.12f, 0.12f, 0.12f, 1.333f));
    }


    public static bool DrawHeaderToggle(string title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction, string documentationURL, out bool change)
            => DrawHeaderToggle(EditorGUIUtility.TrTextContent(title), group, activeField, contextAction, documentationURL,out change);

    public static bool DrawHeaderToggle(GUIContent title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction, string documentationURL, out bool change)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

        var labelRect = backgroundRect;
        labelRect.xMin += 32f;
        labelRect.xMax -= 20f + 16 + 5;

        var foldoutRect = backgroundRect;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;

        var toggleRect = backgroundRect;
        toggleRect.x += 16f;
        toggleRect.y += 2f;
        toggleRect.width = 13f;
        toggleRect.height = 13f;

        // More options 1/2

        //var moreOptionsRect = new Rect();
        //if (hasMoreOptions != null)
        //{
        //    moreOptionsRect = backgroundRect;

        //    moreOptionsRect.x += moreOptionsRect.width - 16 - 1 - 16 - 5;

        //    if (!string.IsNullOrEmpty(documentationURL))
        //        moreOptionsRect.x -= 16 + 7;

        //    moreOptionsRect.height = 15;
        //    moreOptionsRect.width = 16;
        //}

        // Background rect should be full-width
        backgroundRect.xMin = 0f;
        backgroundRect.width += 4f;

        // Background
        float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
        EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

        // Title
        using (new EditorGUI.DisabledScope(!activeField.boolValue))
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Foldout
        group.serializedObject.Update();
        group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
        group.serializedObject.ApplyModifiedProperties();

        // Active checkbox
        change = false;
        activeField.serializedObject.Update();
        bool tmpActive = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
        if(tmpActive != activeField.boolValue)
        {
            activeField.boolValue = tmpActive;
            change = true;
        }

        activeField.serializedObject.ApplyModifiedProperties();

        // More options 2/2
        //if (hasMoreOptions != null)
        //{
        //    bool moreOptions = hasMoreOptions();
        //    bool newMoreOptions = Styles.DrawMoreOptions(moreOptionsRect, moreOptions);
        //    if (moreOptions ^ newMoreOptions)
        //        toggleMoreOptions?.Invoke();
        //}

        // Context menu
        var menuIcon = CoreEditorStyles.paneOptionsIcon;
        var menuRect = new Rect(labelRect.xMax + 3f + 16 + 5, labelRect.y + 1f, menuIcon.width, menuIcon.height);

        if (contextAction != null)
            GUI.DrawTexture(menuRect, menuIcon);

        // Documentation button

        //if (!String.IsNullOrEmpty(documentationURL))
        //{
        //    var documentationRect = menuRect;
        //    documentationRect.x -= 16 + 5;
        //    documentationRect.y -= 1;

        //    var documentationTooltip = $"Open Reference for {title.text}.";
        //    var documentationIcon = new GUIContent(EditorGUIUtility.TrIconContent("_Help").image, documentationTooltip);
        //    var documentationStyle = new GUIStyle("IconButton");

        //    if (GUI.Button(documentationRect, documentationIcon, documentationStyle))
        //        System.Diagnostics.Process.Start(documentationURL);
        //}

        // Handle events


        var e = Event.current;

        if (e.type == EventType.MouseDown)
        {
            if (contextAction != null && menuRect.Contains(e.mousePosition))
            {
                contextAction(new Vector2(menuRect.x, menuRect.yMax));
                e.Use();
            }
            else if (labelRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                    group.isExpanded = !group.isExpanded;
                else if (contextAction != null)
                    contextAction(e.mousePosition);

                e.Use();
            }
        }

        return group.isExpanded;
    }

    public static bool DrawHeader(string title, SerializedProperty group)
            => DrawHeader(EditorGUIUtility.TrTextContent(title), group);
    public static bool DrawHeader(GUIContent title, SerializedProperty group)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

        var labelRect = backgroundRect;
        labelRect.xMax -= 20f + 16 + 5;

        var foldoutRect = backgroundRect;
        foldoutRect.xMin = 4f;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;

        backgroundRect.xMin = 0f;
        backgroundRect.width += 4f;

        // Background
        float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
        EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

        // Title

        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Foldout
        group.serializedObject.Update();
        group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
        group.serializedObject.ApplyModifiedProperties();



        var menuIcon = CoreEditorStyles.paneOptionsIcon;
        var menuRect = new Rect(labelRect.xMax + 3f + 16 + 5, labelRect.y + 1f, menuIcon.width, menuIcon.height);

        var e = Event.current;

        if (e.type == EventType.MouseDown)
        {
            if (labelRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                    group.isExpanded = !group.isExpanded;
                e.Use();
            }
        }

        return group.isExpanded;

    }

}
public static class CoreEditorStyles
{
    /// <summary>Style for a small checkbox</summary>
    public static readonly GUIStyle smallTickbox;
    /// <summary>Style for a small checkbox in mixed state</summary>
    public static readonly GUIStyle smallMixedTickbox;
    /// <summary>Style for a minilabel button</summary>
    public static readonly GUIStyle miniLabelButton;

    static readonly Texture2D paneOptionsIconDark;
    static readonly Texture2D paneOptionsIconLight;

    /// <summary> PaneOption icon </summary>
    public static Texture2D paneOptionsIcon { get { return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight; } }

    static CoreEditorStyles()
    {
        smallTickbox = new GUIStyle("ShurikenToggle");
        smallMixedTickbox = new GUIStyle("ShurikenToggleMixed");

        var transparentTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        transparentTexture.SetPixel(0, 0, Color.clear);
        transparentTexture.Apply();

        miniLabelButton = new GUIStyle(EditorStyles.miniLabel);
        miniLabelButton.normal = new GUIStyleState
        {
            background = transparentTexture,
            scaledBackgrounds = null,
            textColor = Color.grey
        };
        var activeState = new GUIStyleState
        {
            background = transparentTexture,
            scaledBackgrounds = null,
            textColor = Color.white
        };
        miniLabelButton.active = activeState;
        miniLabelButton.onNormal = activeState;
        miniLabelButton.onActive = activeState;

        paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
        paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
    }
}