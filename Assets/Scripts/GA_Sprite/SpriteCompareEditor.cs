using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(SpriteCompare))]
public class SpriteCompareEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpriteCompare comparator = (SpriteCompare)target;

        if (GUILayout.Button("Initialize"))
        {
            //comparator.InitializeRenderTextures();
            EditorUtility.SetDirty(comparator);
        }
    }
}
