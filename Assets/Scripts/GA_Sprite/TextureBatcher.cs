using UnityEngine;
using UnityEngine.Rendering;


[ExecuteAlways]
public class TextureBatcher : MonoBehaviour
{
    private static TextureBatcher _instance;
    public static TextureBatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TextureBatcher>();
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

    public Texture2DArray PackTexturesTo2DArray(RenderTexture[] renderTextures)
    {
        if (renderTextures.Length == 0)
        {
            Debug.LogError("No render textures provided.");
            return null;
        }

        int width = renderTextures[0].width;
        int height = renderTextures[0].height;
        int depth = renderTextures.Length;

        Texture2DArray textureArray = new Texture2DArray(width, height, depth, TextureFormat.RGBA32, false);
        textureArray.wrapMode = TextureWrapMode.Repeat;
        textureArray.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < depth; i++)
        {
            RenderTexture.active = renderTextures[i];

            Texture2D tempTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tempTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tempTexture.Apply();

            // Upload pixels to the correct slice
            Graphics.CopyTexture(tempTexture, 0, 0, textureArray, i, 0);

            Object.DestroyImmediate(tempTexture);
        }

        RenderTexture.active = null;

        Debug.Log("Texture2DArray has been created.");
        return textureArray;
    }
}
