using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using UnityEditor.Build.Content;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;


[ExecuteAlways]
public class SpriteCompare : MonoBehaviour
{
    [SerializeField] public ComputeShader computeShader;


    private static SpriteCompare _instance;
    public static SpriteCompare Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SpriteCompare>();
                if (_instance == null)
                {
                    Debug.LogWarning("No SpriteStamper found in the scene!");
                }
            }
            return _instance;
        }
    }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        _instance = this;
    }

    public Color[] GetAverageColors(Texture targetTexture, Texture guessTexture)
    {
        int threadGroupSize = 32;
        int numGroupsX = Mathf.CeilToInt(targetTexture.width / (float)threadGroupSize);
        int numGroupsY = Mathf.CeilToInt(targetTexture.height / (float)threadGroupSize);

        int sliceCount = 1;
        if (guessTexture is Texture2DArray array)
            sliceCount = array.depth;
        else if (guessTexture is RenderTexture rt && rt.dimension == TextureDimension.Tex2DArray)
            sliceCount = rt.volumeDepth;

        int groupsPerSlice = numGroupsX * numGroupsY;
        int totalGroups = groupsPerSlice * sliceCount;

        // ========== Average Color Calculation ==========
        ComputeBuffer colorSumsBuffer = new ComputeBuffer(totalGroups, sizeof(uint) * 4);
        ComputeBuffer colorCountBuffer = new ComputeBuffer(totalGroups, sizeof(uint));

        int avgKernel = computeShader.FindKernel("CSGetAverageColor");
        computeShader.SetBuffer(avgKernel, "_ColorSums", colorSumsBuffer);
        computeShader.SetBuffer(avgKernel, "_ColorCounts", colorCountBuffer);
        computeShader.SetTexture(avgKernel, "_TextureTarget", targetTexture);
        computeShader.SetTexture(avgKernel, "_TextureGuess", guessTexture);
        computeShader.SetInts("_DispatchDim", numGroupsX, numGroupsY, sliceCount);
        computeShader.Dispatch(avgKernel, numGroupsX, numGroupsY, sliceCount);

        // Read back buffers for average color
        uint4[] colorSums = new uint4[totalGroups];
        colorSumsBuffer.GetData(colorSums);
        colorSumsBuffer.Dispose();

        uint[] pixelCounts = new uint[totalGroups];
        colorCountBuffer.GetData(pixelCounts);
        colorCountBuffer.Dispose();

        Color[] sliceAverageColors = new Color[sliceCount];

        for (int i = 0; i < sliceCount; ++i)
        {
            int sliceStart = i * groupsPerSlice;
            int sliceEnd = sliceStart + groupsPerSlice;

            Color colorSum = new Color();
            uint pixelCount = 0;

            for (int j = sliceStart; j < sliceEnd; ++j)
            {
                colorSum.r += colorSums[j].x;
                colorSum.g += colorSums[j].y;
                colorSum.b += colorSums[j].z;
                colorSum.a += colorSums[j].w;
                pixelCount += pixelCounts[j];
            }

            sliceAverageColors[i] = pixelCount > 0 ? colorSum / (255f * pixelCount) : new Color();
        }
        return sliceAverageColors;
    }
    public ulong[] Compare(Texture targetTexture, Texture guessTexture)
    {
        int threadGroupSize = 32;
        int numGroupsX = Mathf.CeilToInt(targetTexture.width / (float)threadGroupSize);
        int numGroupsY = Mathf.CeilToInt(targetTexture.height / (float)threadGroupSize);

        int sliceCount = 1;
        if (guessTexture is Texture2DArray array)
            sliceCount = array.depth;
        else if (guessTexture is RenderTexture rt && rt.dimension == TextureDimension.Tex2DArray)
            sliceCount = rt.volumeDepth;

        int groupsPerSlice = numGroupsX * numGroupsY;
        int totalGroups = groupsPerSlice * sliceCount;

        // ========== Fitness Calculation ==========
        int fitnessKernel = computeShader.FindKernel("CSMain");
        ComputeBuffer groupSumBuffer = new ComputeBuffer(totalGroups, sizeof(uint));

        computeShader.SetBuffer(fitnessKernel, "_GroupSums", groupSumBuffer);
        computeShader.SetTexture(fitnessKernel, "_TextureTarget", targetTexture);
        computeShader.SetTexture(fitnessKernel, "_TextureGuess", guessTexture);
        computeShader.SetInts("_DispatchDim", numGroupsX, numGroupsY, sliceCount);
        computeShader.Dispatch(fitnessKernel, numGroupsX, numGroupsY, sliceCount);

        uint[] sums = new uint[totalGroups];
        groupSumBuffer.GetData(sums);
        groupSumBuffer.Dispose();

        ulong[] sliceFitnesses = new ulong[sliceCount];

        Parallel.For(0, sliceCount, i =>
        {
            int sliceStart = i * groupsPerSlice;
            int sliceEnd = sliceStart + groupsPerSlice;

            ulong fitnessSum = 0;
            for (int j = sliceStart; j < sliceEnd; ++j)
            {
                fitnessSum += sums[j];
            }

            sliceFitnesses[i] = fitnessSum;
        });

        return sliceFitnesses;
    }

}