using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;


[CustomEditor(typeof(SpriteGeneticAlgorithm))]
public class SpriteGeneticAlgorithmEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpriteGeneticAlgorithm SGA = (SpriteGeneticAlgorithm)target;


        if (GUILayout.Button("Init Texture array"))
        {
            SGA.InitializeTextureArray();
            EditorUtility.SetDirty(SGA);
        }


        if (GUILayout.Button("Populate Sprites"))
        {
            SGA.PopulateSprites();
            EditorUtility.SetDirty(SGA);
        }

        if (GUILayout.Button("Evaluate Fitness"))
        {
            SGA.EvaluateFitness();
            EditorUtility.SetDirty(SGA);
        }
        if (GUILayout.Button("SelectBest"))
        {
            SGA.SelectBestSprites();
            EditorUtility.SetDirty(SGA);
        }
        if (GUILayout.Button("Crossover"))
        {
            SGA.CrossOver();
            EditorUtility.SetDirty(SGA);
        }
        if (GUILayout.Button("Mutate"))
        {
            SGA.Mutate();
            EditorUtility.SetDirty(SGA);
        }
        if (GUILayout.Button("Process generation"))
        {
            SGA.ProccessGeneration();
            EditorUtility.SetDirty(SGA);
        }
        if (GUILayout.Button("Select best sprite"))
        {
            SGA.SelectBestSprites();
            EditorUtility.SetDirty(SGA);
        }
        if (GUILayout.Button("Draw Best sprites"))
        {
            SGA.DrawBestSprites();
            EditorUtility.SetDirty(SGA);
        }

        if (GUILayout.Button("Debug Slices"))
        {
            SGA.DebugSlices();
            EditorUtility.SetDirty(SGA);
        }

    }
}
