using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UIElements;

[CustomEditor(typeof(HeightEditTool))]
public class HeightToolCustomEditor : Editor
{
    public ReorderableList reorderableList;
    public HeightEditTool targetObject;
    public GameObject selectObject;
    public Tool currentTool;
    
    private void OnEnable()
    {
        SerializedProperty listProperty = serializedObject.FindProperty("projectors");
        targetObject = (HeightEditTool) target;
        reorderableList = new ReorderableList(serializedObject, listProperty, true, false, true, true);
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);            
            SerializedProperty colorProperty = element.FindPropertyRelative("gizmoColor");
            EditorGUI.DrawRect(rect, colorProperty.colorValue);
            
            // ProjectorSerializeの各フィールドの描画
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("projectorObject"), new GUIContent("Projector Object")
            );

            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 3, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("scaleXY"), new GUIContent("Scale XY")
            );
            
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 3) * 2, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("projectionTexture"), new GUIContent("Projection Texture")
            );
            
            EditorGUI.Slider(
                new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 3) * 3, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("heightStrength"), 0.0f, 1.0f, new GUIContent("Height Strength")
            );
            
            EditorGUI.Slider(
                new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 3) * 4, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("heightOffset"),0.0f, 1.0f, new GUIContent("Height Offset")
            );
            
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 3) * 5, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("shaderKeyword"), new GUIContent("Shader Keyword")
            );

            colorProperty.colorValue = EditorGUI.ColorField(
                new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 2) * 6, rect.width, EditorGUIUtility.singleLineHeight),
                new GUIContent("Gizmo Color"), colorProperty.colorValue
            );
        };
        reorderableList.onAddCallback += AddButtonClick;
        reorderableList.onRemoveCallback += RemoveButtonClick;
        reorderableList.onSelectCallback += SelectElement;
        // 高さ変更
        reorderableList.elementHeightCallback = (int index) => {
            return (EditorGUIUtility.singleLineHeight + 3) * 7; 
        };
    }

    private void AddButtonClick(ReorderableList orderList)
    {
        var obj = new GameObject($"Projector: {orderList.index+1}");
        obj.transform.parent = targetObject.transform;
        obj.transform.localPosition = new Vector3(100, targetObject.gizmoHeight, 100);
        targetObject.projectors.Add(new ProjectorSerialize
        {
            projectorObject = obj,
            scaleXY = new Vector2(100, 100),
            heightStrength = 0.1f,
            heightOffset = 0,
            shaderKeyword = ProjectionShaderKeyword.paint_max,
            gizmoColor = Color.green
        });
    }

    private void RemoveButtonClick(ReorderableList orderList)
    {
        var element = targetObject.projectors[orderList.index];
        DestroyImmediate(targetObject.projectors[orderList.index].projectorObject);
        targetObject.projectors.Remove(element);
    }

    private void SelectElement(ReorderableList orderList)
    {
        var element = targetObject.projectors[orderList.index];
        // 例: projectorObjectの名前を取得して表示
        GameObject projectorObject = element.projectorObject;
        selectObject = projectorObject;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        serializedObject.Update();
        reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        if(GUILayout.Button("Bake"))
        {
            targetObject.Bake();
        }

        if (Tools.current == Tool.Move || Tools.current == Tool.Rotate)
            currentTool = Tools.current;

        if (EditorGUI.EndChangeCheck())
            targetObject.PropertyChange();
    }

    private void OnSceneGUI()
    {
        // ターゲットのGameObjectのTransformを取得
        if (selectObject != null)
        {
            Transform objTransform = selectObject.transform;

            // トランスフォームギズモを表示

            switch (currentTool)
            {
                case Tool.Move:
                    Vector3 new2Position = Handles.PositionHandle(objTransform.position, objTransform.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(objTransform, "Move Object");
                        objTransform.position = new2Position;
                    }
                    break;
                case Tool.Rotate:
                    Quaternion newQuaternion = Handles.RotationHandle(objTransform.rotation, objTransform.position);
                    // トランスフォームギズモを使用して位置が変更された場合
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(objTransform, "Move Object");
                        objTransform.rotation = newQuaternion;
                    }
                    break;
                default:
                    return;
            }
        }
    }
}
