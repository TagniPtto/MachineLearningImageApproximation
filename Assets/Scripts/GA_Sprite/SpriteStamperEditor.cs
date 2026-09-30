using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SpriteStamper))]
public class SpriteStamperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpriteStamper stamper = (SpriteStamper)target;

        // Make a button
        if (GUILayout.Button("Stamp Sprite"))
        {
            //stamper.ApplyStamps(new SpriteData[] { });
            EditorUtility.SetDirty(stamper);
        }
    }
}

