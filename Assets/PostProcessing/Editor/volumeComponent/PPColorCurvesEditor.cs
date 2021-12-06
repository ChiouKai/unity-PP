
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CurveState = InspectorCurveEditor.CurveState;

    [VolumeComponentEditor(typeof(PPColorCurves))]
sealed class PPColorCurvesEditor : PPVolumeComponentEditor
{
    SerializedDataParameter m_Master;
    SerializedDataParameter m_Red;
    SerializedDataParameter m_Green;
    SerializedDataParameter m_Blue;

    SerializedDataParameter m_HueVsHue;
    SerializedDataParameter m_HueVsSat;
    SerializedDataParameter m_SatVsSat;
    SerializedDataParameter m_LumVsSat;

    // Internal references to the actual animation curves
    // Needed for the curve editor
    SerializedProperty m_RawMaster;
    SerializedProperty m_RawRed;
    SerializedProperty m_RawGreen;
    SerializedProperty m_RawBlue;

    SerializedProperty m_RawHueVsHue;
    SerializedProperty m_RawHueVsSat;
    SerializedProperty m_RawSatVsSat;
    SerializedProperty m_RawLumVsSat;

    InspectorCurveEditor m_CurveEditor;
    Dictionary<SerializedProperty, Color> m_CurveDict;
    static Material s_MaterialGrid;

    static GUIStyle s_PreLabel;

    static GUIContent[] s_Curves =
    {
            new GUIContent("Master"),
            new GUIContent("Red"),
            new GUIContent("Green"),
            new GUIContent("Blue"),
            new GUIContent("Hue Vs Hue"),
            new GUIContent("Hue Vs Sat"),
            new GUIContent("Sat Vs Sat"),
            new GUIContent("Lum Vs Sat")
        };

    SavedInt m_SelectedCurve;

    List<SerializedDataParameter> parameters;
    List<SerializedProperty> curves;

    public override void FindSerializedDataParameter()
    {
        parameters = new List<SerializedDataParameter>();
        curves = new List<SerializedProperty>();

        m_Master = AddList(parameters, Unpack(serializedObject.FindProperty("master")));
        m_Red = AddList(parameters, Unpack(serializedObject.FindProperty("red")));
        m_Green = AddList(parameters, Unpack(serializedObject.FindProperty("green")));
        m_Blue = AddList(parameters, Unpack(serializedObject.FindProperty("blue")));

        m_HueVsHue = AddList(parameters, Unpack(serializedObject.FindProperty("hueVsHue")));
        m_HueVsSat = AddList(parameters, Unpack(serializedObject.FindProperty("hueVsSat")));
        m_SatVsSat = AddList(parameters, Unpack(serializedObject.FindProperty("satVsSat")));
        m_LumVsSat = AddList(parameters, Unpack(serializedObject.FindProperty("lumVsSat")));

        m_RawMaster = AddList(curves, m_Master.value.FindPropertyRelative("m_Curve"));
        m_RawRed = AddList(curves, m_Red.value.FindPropertyRelative("m_Curve"));
        m_RawGreen = AddList(curves, m_Green.value.FindPropertyRelative("m_Curve"));
        m_RawBlue = AddList(curves, m_Blue.value.FindPropertyRelative("m_Curve"));

        m_RawHueVsHue = AddList(curves, m_HueVsHue.value.FindPropertyRelative("m_Curve"));
        m_RawHueVsSat = AddList(curves, m_HueVsSat.value.FindPropertyRelative("m_Curve"));
        m_RawSatVsSat = AddList(curves, m_SatVsSat.value.FindPropertyRelative("m_Curve"));
        m_RawLumVsSat = AddList(curves, m_LumVsSat.value.FindPropertyRelative("m_Curve"));

        m_SelectedCurve = new SavedInt($"{target.GetType()}.SelectedCurve", 0);

        // Prepare the curve editor
        m_CurveEditor = new InspectorCurveEditor();
        m_CurveDict = new Dictionary<SerializedProperty, Color>();

        SetupCurve(m_RawMaster, new Color(1f, 1f, 1f), 2, false);
        SetupCurve(m_RawRed, new Color(1f, 0f, 0f), 2, false);
        SetupCurve(m_RawGreen, new Color(0f, 1f, 0f), 2, false);
        SetupCurve(m_RawBlue, new Color(0f, 0.5f, 1f), 2, false);
        SetupCurve(m_RawHueVsHue, new Color(1f, 1f, 1f), 0, true);
        SetupCurve(m_RawHueVsSat, new Color(1f, 1f, 1f), 0, true);
        SetupCurve(m_RawSatVsSat, new Color(1f, 1f, 1f), 0, false);
        SetupCurve(m_RawLumVsSat, new Color(1f, 1f, 1f), 0, false);
    }

    T AddList<T>(List<T> list, T a)
    {
        list.Add(a);
        return a;
    }


    void SetupCurve(SerializedProperty prop, Color color, uint minPointCount, bool loop)
    {
        var state = CurveState.defaultState;
        state.color = color;
        state.visible = false;
        state.minPointCount = minPointCount;
        state.onlyShowHandlesOnSelection = true;
        state.zeroKeyConstantValue = 0.5f;
        state.loopInBounds = loop;
        m_CurveEditor.Add(prop, state);
        m_CurveDict.Add(prop, color);
    }

    void ResetVisibleCurves()
    {
        foreach (var curve in m_CurveDict)
        {
            var state = m_CurveEditor.GetCurveState(curve.Key);
            state.visible = false;
            m_CurveEditor.SetCurveState(curve.Key, state);
        }
    }

    void SetCurveVisible(SerializedProperty rawProp, SerializedProperty overrideProp)
    {
        var state = m_CurveEditor.GetCurveState(rawProp);
        state.visible = true;
        state.editable = overrideProp.boolValue;
        m_CurveEditor.SetCurveState(rawProp, state);
    }

    void CurveOverrideToggle(SerializedProperty overrideProp)
    {
        overrideProp.boolValue = GUILayout.Toggle(overrideProp.boolValue, EditorGUIUtility.TrTextContent("Override"), EditorStyles.toolbarButton);
    }

    int DoCurveSelectionPopup(int id)
    {
        GUILayout.Label(s_Curves[id], EditorStyles.toolbarPopup, GUILayout.MaxWidth(150f));

        var lastRect = GUILayoutUtility.GetLastRect();
        var e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && lastRect.Contains(e.mousePosition))
        {
            var menu = new GenericMenu();

            for (int i = 0; i < s_Curves.Length; i++)
            {
                if (i == 4)
                    menu.AddSeparator("");

                int current = i; // Capture local for closure
                menu.AddItem(s_Curves[i], current == id, () =>
                {
                    m_SelectedCurve.value = current;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(new Rect(lastRect.xMin, lastRect.yMax, 1f, 1f));
        }

        return id;
    }

    void DrawBackgroundTexture(Rect rect, int pass)
    {
        if (s_MaterialGrid == null)
            s_MaterialGrid = new Material(Shader.Find("Hidden/Universal Render Pipeline/Editor/CurveBackground")) { hideFlags = HideFlags.HideAndDontSave };

        float scale = EditorGUIUtility.pixelsPerPoint;

        var oldRt = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(Mathf.CeilToInt(rect.width * scale), Mathf.CeilToInt(rect.height * scale), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        s_MaterialGrid.SetFloat("_DisabledState", GUI.enabled ? 1f : 0.5f);

        Graphics.Blit(null, rt, s_MaterialGrid, pass);
        RenderTexture.active = oldRt;

        GUI.DrawTexture(rect, rt);
        RenderTexture.ReleaseTemporary(rt);
    }

    void MarkTextureCurveAsDirty(int curveId)
    {
        var t = target as PPColorCurves;

        if (t == null)
            return;

        switch (curveId)
        {
            case 0: t.master.value.SetDirty(); break;
            case 1: t.red.value.SetDirty(); break;
            case 2: t.green.value.SetDirty(); break;
            case 3: t.blue.value.SetDirty(); break;
            case 4: t.hueVsHue.value.SetDirty(); break;
            case 5: t.hueVsSat.value.SetDirty(); break;
            case 6: t.satVsSat.value.SetDirty(); break;
            case 7: t.lumVsSat.value.SetDirty(); break;
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();
        ResetVisibleCurves();

        using (new EditorGUI.DisabledGroupScope(serializedObject.isEditingMultipleObjects))
        {
            int curveEditingId;
            SerializedProperty currentCurveRawProp = null;

            // Top toolbar
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                curveEditingId = DoCurveSelectionPopup(m_SelectedCurve.value);
                curveEditingId = Mathf.Clamp(curveEditingId, 0, 7);

                EditorGUILayout.Space();

                CurveOverrideToggle(parameters[curveEditingId].overrideState);
                SetCurveVisible(curves[curveEditingId], parameters[curveEditingId].overrideState);
                currentCurveRawProp = curves[curveEditingId];


                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                {
                    MarkTextureCurveAsDirty(curveEditingId);
                    if (curveEditingId < 4)
                        curves[curveEditingId].animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                    else
                        curves[curveEditingId].animationCurveValue = new AnimationCurve();
                }
                m_SelectedCurve.value = curveEditingId;
            }

            // Curve area
            var rect = GUILayoutUtility.GetAspectRect(2f);
            var innerRect = new RectOffset(10, 10, 10, 10).Remove(rect);

            if (Event.current.type == EventType.Repaint)
            {
                // Background
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

                if (curveEditingId == 4 || curveEditingId == 5)
                    DrawBackgroundTexture(innerRect, 0);
                else if (curveEditingId == 6 || curveEditingId == 7)
                    DrawBackgroundTexture(innerRect, 1);

                // Bounds
                Handles.color = Color.white * (GUI.enabled ? 1f : 0.5f);
                Handles.DrawSolidRectangleWithOutline(innerRect, Color.clear, new Color(0.8f, 0.8f, 0.8f, 0.5f));

                // Grid setup
                Handles.color = new Color(1f, 1f, 1f, 0.05f);
                int hLines = (int)Mathf.Sqrt(innerRect.width);
                int vLines = (int)(hLines / (innerRect.width / innerRect.height));

                // Vertical grid
                int gridOffset = Mathf.FloorToInt(innerRect.width / hLines);
                int gridPadding = ((int)(innerRect.width) % hLines) / 2;

                for (int i = 1; i < hLines; i++)
                {
                    var offset = i * Vector2.right * gridOffset;
                    offset.x += gridPadding;
                    Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.x, innerRect.yMax - 1) + offset);
                }

                // Horizontal grid
                gridOffset = Mathf.FloorToInt(innerRect.height / vLines);
                gridPadding = ((int)(innerRect.height) % vLines) / 2;

                for (int i = 1; i < vLines; i++)
                {
                    var offset = i * Vector2.up * gridOffset;
                    offset.y += gridPadding;
                    Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.xMax - 1, innerRect.y) + offset);
                }
            }

            // Curve editor
            using (new GUI.ClipScope(innerRect))
            {
                if (m_CurveEditor.OnGUI(new Rect(0, 0, innerRect.width - 1, innerRect.height - 1)))
                {
                    Repaint();
                    GUI.changed = true;
                    MarkTextureCurveAsDirty(m_SelectedCurve.value);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Borders
                Handles.color = Color.black;
                Handles.DrawLine(new Vector2(rect.x, rect.y - 20f), new Vector2(rect.xMax, rect.y - 20f));
                Handles.DrawLine(new Vector2(rect.x, rect.y - 21f), new Vector2(rect.x, rect.yMax));
                Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
                Handles.DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.y - 20f));

                bool editable = m_CurveEditor.GetCurveState(currentCurveRawProp).editable;
                string editableString = editable ? string.Empty : "(Not Overriding)\n";

                // Selection info
                var selection = m_CurveEditor.GetSelection();
                var infoRect = innerRect;
                infoRect.x += 5f;
                infoRect.width = 100f;
                infoRect.height = 30f;

                if (s_PreLabel == null)
                    s_PreLabel = new GUIStyle("ShurikenLabel");

                if (selection.curve != null && selection.keyframeIndex > -1)
                {
                    var key = selection.keyframe.Value;
                    GUI.Label(infoRect, $"{key.time:F3}\n{key.value:F3}", s_PreLabel);
                }
                else
                {
                    GUI.Label(infoRect, editableString, s_PreLabel);
                }
            }
        }
    }
}
