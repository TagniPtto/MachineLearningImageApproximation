using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;


[CustomEditor(typeof(CNN))]
public class CNN_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CNN CNN_target = (CNN)target;

        if (GUILayout.Button("Blit Convolution"))
        {
            CNN_target.BlitConvolution();
            EditorUtility.SetDirty(CNN_target);
        }
    }
}
