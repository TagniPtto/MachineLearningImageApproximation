using UnityEngine;


public class CNN : MonoBehaviour
{
    [SerializeField] private Material ConvolutionMaterial;
    [SerializeField] private ComputeShader ConvolutionComputeShader;
    [SerializeField] private Texture2D InputTexture;
    [SerializeField] private RenderTexture OutputTexture;

    public void BlitConvolution()
    {
        if (!OutputTexture)
        {
            OutputTexture = RenderTexture.GetTemporary(InputTexture.width, InputTexture.height,0,RenderTextureFormat.ARGB32);
        }
        Graphics.SetRenderTarget(OutputTexture);
        GL.Clear(true,true,Color.clear);
        Graphics.Blit(InputTexture, OutputTexture , ConvolutionMaterial);
    }
    public void Proccess()
    {

        int InputLayerHandle = ConvolutionComputeShader.FindKernel("CSInputLayer");
        int ConvolutionLayerHandle = ConvolutionComputeShader.FindKernel("CSConvolutionLayer");
        int PoolingLayerHandle = ConvolutionComputeShader.FindKernel("CSPoolingLayer");

    }

}
