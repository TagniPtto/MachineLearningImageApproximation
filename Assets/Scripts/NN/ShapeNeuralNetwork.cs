using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class ShapeNeuralNetwork : MonoBehaviour
{
    [Header("Input")]
    [Space(10)]
    [SerializeField] private Texture2D inputTexture;

    [Space(32)]
    [Header("Dependencies")]
    [Space(10)]
    [SerializeField] private ComputeShader ShapeNNCompute;

    [SerializeField] private Material outputMaterial;
    [SerializeField] private RenderTexture resultTexture;

    [Space(32)]
    [Header("Network settings")]
    [Space(10)]
    [SerializeField] private int[] hiddenLayerSizes = new int[] { 512, 512, 512, 4 };
    [SerializeField] private float learningRate = 0.01f;
    [SerializeField] private float edgeWeight = 0.1f;
    [SerializeField] private int batchSize = 16;
    [SerializeField] private int LearningIterations = 20;

    [Space(32)]
    [Header("Presentation")]
    [Space(10)]
    [SerializeField]
    private bool CreateSnapShots;
    [Space(32)]

    const int NNFeedThreadAmount = 16; // 64 threads per batch, 16 batches per frame, 1024 threads per frame
    ComputeBuffer packedNodeBuffer;
    ComputeBuffer weightsGradientBuffer;
    ComputeBuffer biasesGradientBuffer;


    ComputeBuffer weightsBuffer;
    ComputeBuffer biasesBuffer;

    ComputeBuffer packedLayerPropertyOffsetBuffer;

    ComputeBuffer layerNodeCountBuffer;


    int feedForwardKernelIndex;
    int feedBackwardKernelIndex;
    int accumulateGradientKernelIndex;
    int applyGradientKernelIndex;
    int clearGradientKernelIndex;

    int networkNodeCount;


    ComputeBuffer pixelIndexBuffer;
    List<int2> shuffledPixels;



    public void SetupNetworkComputeShader()
    {
        Debug.Log(SystemInfo.graphicsDeviceType);
        Debug.Log("Active Shader Compiler: " + SystemInfo.graphicsShaderLevel);

        if (!resultTexture)
        {
            SetupRenderTexture();
        }
        DisposeBuffers();

        feedForwardKernelIndex = ShapeNNCompute.FindKernel("CSFeedForward");
        feedBackwardKernelIndex = ShapeNNCompute.FindKernel("CSFeedBackward");
        accumulateGradientKernelIndex = ShapeNNCompute.FindKernel("CSAccumulateGradients");

        applyGradientKernelIndex = ShapeNNCompute.FindKernel("CSApplyGradients");
        clearGradientKernelIndex = ShapeNNCompute.FindKernel("CSClearGradients");

        int totalLayers = hiddenLayerSizes.Length;
        System.Random rng = new System.Random();

        List<float> weights = new List<float>();
        List<float> biases = new List<float>();

        List<int> packedLayerPropertyOffset = new List<int>(); // [biasOffset, weightOffset, nodeOffset] per layer
        List<int> layer_NodeCount = new List<int>();

        int biasOffset = 0;
        int weightOffset = 0;
        int nodeOffset = 0;

        for (int i = 0; i < totalLayers; i++) // layer 1 onward
        {

            int currNodeCount = hiddenLayerSizes[i];
            int prevNodeCount;
            
            if (i == 0)
            {
                prevNodeCount = 2;
            }
            else
            {
                prevNodeCount = hiddenLayerSizes[i - 1];
            }

            packedLayerPropertyOffset.Add(weightOffset);
            packedLayerPropertyOffset.Add(biasOffset);
            packedLayerPropertyOffset.Add(nodeOffset);

            layer_NodeCount.Add(currNodeCount);

            for (int j = 0; j < currNodeCount; ++j)
            {
                // Bias

                biases.Add((float)(rng.NextDouble() * 2.0 - 1.0)*0.1f);
                biasOffset ++;

                for (int k = 0; k < prevNodeCount; ++k)
                {
                    weights.Add((float)(rng.NextDouble() * 2.0 - 1.0)*0.1f);
                    weightOffset ++;
                }
            }

            nodeOffset += currNodeCount;
        }


        networkNodeCount = nodeOffset;
        int NodePropertyCount = 2; // value , activated value , gradient
        int totalNodeCount = networkNodeCount * batchSize* NNFeedThreadAmount;
        float[] initialNodeValues = new float[NodePropertyCount * totalNodeCount];
        float[] initialWeightGradientValues = new float[weights.Count  * batchSize * NNFeedThreadAmount];
        float[] initialBiasGradientValues = new float[biases.Count * batchSize * NNFeedThreadAmount];

        packedNodeBuffer = new ComputeBuffer(totalNodeCount, NodePropertyCount * sizeof(float));
        weightsGradientBuffer = new ComputeBuffer(batchSize* NNFeedThreadAmount * weights.Count, sizeof(float));
        biasesGradientBuffer = new ComputeBuffer(batchSize* NNFeedThreadAmount * biases.Count, sizeof(float));

        weightsBuffer = new ComputeBuffer(weights.Count , sizeof(float));
        biasesBuffer = new ComputeBuffer(biases.Count , sizeof(float));

        packedLayerPropertyOffsetBuffer = new ComputeBuffer(packedLayerPropertyOffset.Count / 3, 3 * sizeof(int));
        layerNodeCountBuffer = new ComputeBuffer(layer_NodeCount.Count, sizeof(int));

        layerNodeCountBuffer.SetData(layer_NodeCount.ToArray());
        weightsBuffer.SetData(weights.ToArray());
        biasesBuffer.SetData(biases.ToArray());
        weightsGradientBuffer.SetData(initialWeightGradientValues);
        biasesGradientBuffer.SetData(initialBiasGradientValues);
        packedNodeBuffer.SetData(initialNodeValues);
        packedLayerPropertyOffsetBuffer.SetData(packedLayerPropertyOffset.ToArray());


        //----------------------------------------------------------------------------
        // SETUP GLOBAL COMPUTE SHADER VARIABLES
        //----------------------------------------------------------------------------
        ShapeNNCompute.SetInt("layerCount", hiddenLayerSizes.Length);
        ShapeNNCompute.SetInt("networkNodeCount", networkNodeCount);
        ShapeNNCompute.SetInt("networkWeightCount", weights.Count);
        ShapeNNCompute.SetInt("networkBiasCount", biases.Count);

        ShapeNNCompute.SetFloat("learningRate", learningRate);

        //----------------------------------------------------------------------------
        // FEED FORWARD SETUP
        //----------------------------------------------------------------------------
        ShapeNNCompute.SetBuffer(feedForwardKernelIndex, "_Layer_NodeCount", layerNodeCountBuffer);
        ShapeNNCompute.SetBuffer(feedForwardKernelIndex, "_LayerPropertyOffsetPacked", packedLayerPropertyOffsetBuffer);

        ShapeNNCompute.SetBuffer(feedForwardKernelIndex, "_Weights", weightsBuffer);
        ShapeNNCompute.SetBuffer(feedForwardKernelIndex, "_Biases", biasesBuffer);

        //unique to each group
        ShapeNNCompute.SetBuffer(feedForwardKernelIndex, "_NodeValuesPacked", packedNodeBuffer);
        ShapeNNCompute.SetTexture(feedForwardKernelIndex, "_InputImage", inputTexture);
        ShapeNNCompute.SetTexture(feedForwardKernelIndex, "_ResultImage", resultTexture);



        //----------------------------------------------------------------------------
        // FEED BACKWARD SETUP
        //----------------------------------------------------------------------------
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_Layer_NodeCount", layerNodeCountBuffer);
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_LayerPropertyOffsetPacked", packedLayerPropertyOffsetBuffer);

        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_Weights", weightsBuffer);
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_Biases", biasesBuffer);
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_WeightGradientValues", weightsGradientBuffer);
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_BiasGradientValues", biasesGradientBuffer);
        //unique to each group
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "_NodeValuesPacked", packedNodeBuffer);
        ShapeNNCompute.SetTexture(feedBackwardKernelIndex, "_InputImage", inputTexture);
        ShapeNNCompute.SetTexture(feedBackwardKernelIndex, "_ResultImage", resultTexture);


        //----------------------------------------------------------------------------
        // ACCUMULATE GRADIENTS SETUP
        //----------------------------------------------------------------------------
        ShapeNNCompute.SetBuffer(accumulateGradientKernelIndex, "_WeightGradientValues", weightsGradientBuffer);
        ShapeNNCompute.SetBuffer(accumulateGradientKernelIndex, "_BiasGradientValues", biasesGradientBuffer);
        ShapeNNCompute.SetBuffer(accumulateGradientKernelIndex, "_Layer_NodeCount", layerNodeCountBuffer);
        ShapeNNCompute.SetBuffer(accumulateGradientKernelIndex, "_LayerPropertyOffsetPacked", packedLayerPropertyOffsetBuffer);
        ShapeNNCompute.SetBuffer(accumulateGradientKernelIndex, "_NodeValuesPacked", packedNodeBuffer);

        //----------------------------------------------------------------------------
        // APPLY GRADIENTS SETUP
        //----------------------------------------------------------------------------
        ShapeNNCompute.SetBuffer(applyGradientKernelIndex, "_Weights", weightsBuffer);
        ShapeNNCompute.SetBuffer(applyGradientKernelIndex, "_Biases", biasesBuffer);

        ShapeNNCompute.SetBuffer(applyGradientKernelIndex, "_WeightGradientValues", weightsGradientBuffer);
        ShapeNNCompute.SetBuffer(applyGradientKernelIndex, "_BiasGradientValues", biasesGradientBuffer);

        //----------------------------------------------------------------------------
        // CLEAR GRADIENTS SETUP
        //----------------------------------------------------------------------------
        ShapeNNCompute.SetBuffer(clearGradientKernelIndex, "_WeightGradientValues", weightsGradientBuffer);
        ShapeNNCompute.SetBuffer(clearGradientKernelIndex, "_BiasGradientValues", biasesGradientBuffer);

    }
    public void DisposeBuffers()
    {
        packedNodeBuffer?.Release(); packedNodeBuffer = null;
        weightsBuffer?.Release(); weightsBuffer = null;
        biasesBuffer?.Release(); biasesBuffer = null;
        layerNodeCountBuffer?.Release(); layerNodeCountBuffer = null;
        packedLayerPropertyOffsetBuffer?.Release(); packedLayerPropertyOffsetBuffer = null;
        weightsGradientBuffer?.Release(); weightsGradientBuffer = null;
        biasesGradientBuffer?.Release(); biasesGradientBuffer = null;
        pixelIndexBuffer?.Release(); pixelIndexBuffer = null;
    }
    private void Start()
    {
        SetupRenderTexture();
        StartCoroutine(Learn());
    }
    IEnumerator Learn()
    {
        SetupNetworkComputeShader();

        for (int l = 0; l < LearningIterations; ++l)
        {
            learningRate = 1.0f / Mathf.Pow(l/2.0f + 1,0.25f); // Decrease learning rate over time
            edgeWeight =  1+ 1.0f /(-MathF.Sqrt(l/2.0f+1));
            ShapeNNCompute.SetFloat("learningRate", learningRate);
            ShapeNNCompute.SetFloat("edgeLossWeight", edgeWeight);


            yield return StartCoroutine(LearnOnce());
            if (CreateSnapShots)
            { 
                SaveResultTexture(l);
            }
        }

    }
    IEnumerator LearnOnce()
    {
        int threadsPerBatch = batchSize * NNFeedThreadAmount;
        int batchAmount = Mathf.CeilToInt(inputTexture.width * inputTexture.height / ((float)threadsPerBatch));
        ShufflePixels();

        for (int i = 0; i < batchAmount; i++)
        {

            ShapeNNCompute.SetInt("batchStartingOffset", i * threadsPerBatch);
            ShapeNNCompute.SetInt("batchThreadAmount", threadsPerBatch);


            ShapeNNCompute.Dispatch(feedForwardKernelIndex, batchSize, 1, 1);
            yield return null;
            ShapeNNCompute.Dispatch(feedBackwardKernelIndex, batchSize, 1, 1);
            yield return null;



            int threadCount = Mathf.CeilToInt(Mathf.Max(weightsBuffer.count, biasesBuffer.count) / 1024.0f);
            int dispatchCount = Mathf.CeilToInt(threadCount / 1f);

            ShapeNNCompute.Dispatch(accumulateGradientKernelIndex, dispatchCount, 1, 1);
            yield return null;

            ShapeNNCompute.Dispatch(applyGradientKernelIndex, dispatchCount, 1, 1);
            yield return null;

            ShapeNNCompute.Dispatch(clearGradientKernelIndex, dispatchCount, 1, 1);
            yield return null;

        }
        Debug.Log("Learning iteration ");
    }
    private void SaveResultTexture(int iteration)
    {
        // Set the active RenderTexture to resultTexture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = resultTexture;

        // Create a new Texture2D and read the RenderTexture into it
        Texture2D tex = new Texture2D(resultTexture.width, resultTexture.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, resultTexture.width, resultTexture.height), 0, 0);
        tex.Apply();

        // Encode texture to PNG
        byte[] bytes = tex.EncodeToPNG();

        // Save to disk (Assets/NN_Results/iteration_x.png)
        string dir = Application.dataPath + "/NN_Results";
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string filePath = $"{dir}/iteration_{iteration}.png";
        System.IO.File.WriteAllBytes(filePath, bytes);

        // Clean up
        UnityEngine.Object.Destroy(tex);
        RenderTexture.active = currentRT;

        Debug.Log($"Saved result texture to {filePath}");
    }


    public void SetupRenderTexture()
    {
        resultTexture = new RenderTexture(inputTexture.width, inputTexture.height, 0);
        resultTexture.enableRandomWrite = true;
        resultTexture.Create();

        if (outputMaterial != null)
        {
            Debug.Log("Setting ResultTexture");
            outputMaterial.mainTexture = resultTexture;
        }
    }
    public void ShufflePixels()
    {
        shuffledPixels = new List<int2>();
        for (int y = 0; y < inputTexture.height; y++)
            for (int x = 0; x < inputTexture.width; x++)
                shuffledPixels.Add(new int2(x, y));

        Shuffle(shuffledPixels);
        if(pixelIndexBuffer == null)
        {
            pixelIndexBuffer = new ComputeBuffer(shuffledPixels.Count, sizeof(int) * 2);
        }
        pixelIndexBuffer.SetData(shuffledPixels.ToArray());
        ShapeNNCompute.SetBuffer(feedForwardKernelIndex, "randomPixelCoords", pixelIndexBuffer);
        ShapeNNCompute.SetBuffer(feedBackwardKernelIndex, "randomPixelCoords", pixelIndexBuffer);
    }
    void Shuffle<T>(List<T> list)
    {
        var rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}
