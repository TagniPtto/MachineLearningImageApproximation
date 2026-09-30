using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(ResizeTexture))]
public class ResizeTextureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ResizeTexture resizeTexture = (ResizeTexture)target;

        if (GUILayout.Button("Resize Texture"))
        {
            resizeTexture.ResizeAndSave();
            EditorUtility.SetDirty(resizeTexture);
        }
    }
}