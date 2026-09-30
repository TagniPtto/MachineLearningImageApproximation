using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

[ExecuteAlways]
public class SpriteStamper : MonoBehaviour
{
    [SerializeField] public Sprite[] sprites; // Array of sprites to pick from
    [SerializeField] private Material blitMaterial; // Material for custom blit (handles transformations)

    private static SpriteStamper _instance;
    public static SpriteStamper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SpriteStamper>();
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
    public void ClearRenderTexture(RenderTexture paper_texture)
    {
        RenderTexture.active = paper_texture;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = null;
    }

    public void ApplyStamps(RenderTexture paper_texture,SpriteData[] spriteDataArray)
    {
        Graphics.SetRenderTarget(paper_texture);
        foreach (SpriteData data in spriteDataArray)
        {
            StampSpriteDirect(paper_texture, data);
        }
    }
    public void StampSpriteDirect(RenderTexture targetArray, SpriteData sprite)
    {
        if (sprite.spriteIndex < 0 || sprite.spriteIndex >= sprites.Length)
            return;

        blitMaterial.SetTexture("_SpriteTex", sprites[sprite.spriteIndex].texture);
        blitMaterial.SetVector("_Position", sprite.position);
        blitMaterial.SetVector("_Scale", sprite.scale);
        blitMaterial.SetFloat("_Rotation", sprite.rotation);
        blitMaterial.SetColor("_Color", sprite.color);

        Graphics.Blit(null, blitMaterial);
    }

}