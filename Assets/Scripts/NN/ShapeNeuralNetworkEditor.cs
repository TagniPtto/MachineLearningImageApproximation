using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(ShapeNeuralNetwork))]
public class ShapeNeuralNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {

        base.OnInspectorGUI();

        ShapeNeuralNetwork SNN = (ShapeNeuralNetwork)target;

        // Make a button
        if (GUILayout.Button("Setup NN"))
        {
            SNN.SetupNetworkComputeShader();
            EditorUtility.SetDirty(SNN);
        }
        if (GUILayout.Button("Learn"))
        {
            //SNN.Learn();
            EditorUtility.SetDirty(SNN);
        }
        if (GUILayout.Button("Learn 100"))
        {
            for(int i = 0; i < 100; i++)
            {
                //SNN.Learn();
            }
            EditorUtility.SetDirty(SNN);
        }
    }
}
