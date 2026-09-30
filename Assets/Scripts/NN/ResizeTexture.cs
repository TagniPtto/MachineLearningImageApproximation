using System.IO;
using UnityEngine;

public class ResizeTexture : MonoBehaviour
{
    public Texture2D source;
    public int newWidth = 128;
    public int newHeight = 128;

    public void ResizeAndSave()
    {
        // Create RenderTexture and resize
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D resized = new Texture2D(newWidth, newHeight, source.format, false);
        resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resized.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // Encode to PNG
        byte[] pngData = resized.EncodeToPNG();

        if (pngData != null)
        {
            // Get original texture path - assumes texture is in Resources folder or streamingAssets or known path
            string originalPath = Application.dataPath + "/Textures/";
            string fileName = source.name + "_resized.png";

            string savePath = Path.Combine(originalPath, fileName);

            // Save the PNG file
            File.WriteAllBytes(savePath, pngData);
            Debug.Log("Saved resized texture to: " + savePath);
        }
        else
        {
            Debug.LogError("Failed to encode texture to PNG.");
        }
    }
}