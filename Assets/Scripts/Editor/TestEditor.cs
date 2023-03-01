using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Test))]
public class TestEditor : Editor
{
    override public void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (GUILayout.Button("Generate") || EditorGUI.EndChangeCheck())
        {
            ((Test)target).Generate();
        }
    }
}
