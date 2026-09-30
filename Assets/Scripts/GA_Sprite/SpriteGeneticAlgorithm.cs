using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;

using Random = UnityEngine.Random;
using RangeAttribute = UnityEngine.RangeAttribute;
using UnityEngine.U2D;
using TMPro;
using Debug = UnityEngine.Debug;
using UnityEngine.Rendering;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel;
using Unity.Mathematics;
using System.Globalization;
using UnityEngine.InputSystem.Android;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using Unity.VisualScripting;


[System.Serializable]
public class SpriteData
{
    public int spriteIndex; // Index of the sprite in the sprites array
    public Vector2 position;
    public Vector2 scale;
    public float rotation;
    public Color color;
}
[System.Serializable]
public class SpriteDataFitnessPair : ICloneable
{
    public SpriteData spriteData;
    public ulong fitness;
    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
public class SpriteGeneticAlgorithm : MonoBehaviour
{

    [Header("Population properties")]
    [SerializeField][Range(10, 1000)] int max_sprite_population_size;
    [SerializeField][Range(0.0f,1.0f)] float max_passing_sprite_percentage;
    [SerializeField][Range(0.0f,1.0f)] float max_breeding_sprite_percentage;
    [SerializeField][Range(0.1f, 2.0f)] float max_sprite_scale;
    [SerializeField][Range(0.01f, 0.1f)] float min_sprite_scale;

    [SerializeField] bool clear_every_save = false;
    [SerializeField] List<SpriteDataFitnessPair> sprite_population_list = new List<SpriteDataFitnessPair>();
    [SerializeField] List<RenderTexture> debugRenderTextureArray = new List<RenderTexture>();
    
    [Header("Guessing and Target")]
    [SerializeField] public Texture2D targetTexture;
    [SerializeField] public RenderTexture arrayRenderTexture;

    [Header("MutationChances")]
    [SerializeField][Range(0.0f,1.0f)] private float mutationChance_sprite_index;
    [SerializeField][Range(0.0f,1.0f)] private float mutationChance_sprite_position;
    [SerializeField][Range(0.0f,1.0f)] private float mutationChance_sprite_scale;
    [SerializeField][Range(0.0f,1.0f)] private float mutationChance_sprite_rotation;
    [SerializeField][Range(0.0f,1.0f)] private float mutationChance_sprite_color;

    [Header("FindingSprite")]
    [SerializeField] public RenderTexture ResultTexture;
    [SerializeField] public Material ResultMaterial;

    [Header("Best Sprites")]
    [SerializeField] [Range(0.0f,1.0f)]public float PopulationDrawingPercentage;
    [SerializeField] List<SpriteDataFitnessPair> best_sprites_list = new List<SpriteDataFitnessPair>();

    public void PopulateSprites()
    {

        for (int i = sprite_population_list.Count; i < max_sprite_population_size; ++i)
        {
            float random_size = Random.Range(min_sprite_scale, max_sprite_scale);
            SpriteData sprite_data = new SpriteData
            {
                spriteIndex = (int)(Random.value * SpriteStamper.Instance.sprites.Length),
                position = new Vector2(Random.Range(-0.5f,0.5f), Random.Range(-0.5f, 0.5f)),//new Vector2(Random.Range(0, SpriteStamper.Instance.paperTexture.width), Random.Range(0, SpriteStamper.Instance.paperTexture.height)),
                scale = new Vector2(random_size, random_size),
                rotation = Random.Range(0, 360),
                color = Color.white,
            };
            sprite_population_list.Add(new SpriteDataFitnessPair { spriteData = sprite_data, fitness = 0 });
        }
    }
    public void InitializeTextureArray()
    {
        // Create the big 2DArray RenderTexture
        arrayRenderTexture = new RenderTexture(targetTexture.width, targetTexture.height, 0)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
            volumeDepth = max_sprite_population_size,
            enableRandomWrite = false, // Important for compute/Blit read-write
            useMipMap = false,
            autoGenerateMips = false,
            format = RenderTextureFormat.ARGB32,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm
        };
        arrayRenderTexture.Create();
    }
    public IEnumerator GetShapes(int N)
    {
        for (int i = 0; i < N; i++)
        {
            yield return StartCoroutine(ProccessGeneration());
            DrawBestSprites();
            //DrawPopulation();
        }
    }
    public void Start()
    {
        StartCoroutine(GetShapes(100000));
    }


    static int counter = 0;
    public int max_counter = 30;
    public IEnumerator ProccessGeneration()
    {

        PopulateSprites();
        EvaluateFitness();
        SelectBestSprites();
        CrossOver();
        Mutate();
        //SaveBestSprite();
        counter++;
        if (counter > max_counter)
        {
            counter = 0;
            var sorted = sprite_population_list.OrderByDescending(x => x.fitness).ToList();
            //if(best_sprites_list.Count ==0|| sorted.First().fitness > best_sprites_list.Last().fitness - 1000000)
            //{
            best_sprites_list.Add((SpriteDataFitnessPair)sorted.First().Clone());
            sprite_population_list.Remove(sorted.First());
            //}
            if (clear_every_save)
                sprite_population_list.Clear();

            //SaveBestSprite();
        }

        //DrawPopulation();
        yield return null;
    }
    public void SaveBestSprite()
    {

        var sorted = sprite_population_list.OrderByDescending(x => x.fitness).ToList();
        if (best_sprites_list.Count == 0 || best_sprites_list.Last().fitness < sprite_population_list.First().fitness)
        {
            best_sprites_list.Add((SpriteDataFitnessPair)sorted.First().Clone());
            sprite_population_list.Remove(sorted.First());
            sprite_population_list.Clear();
        }
        else
        {
            Debug.Log($"No new best sprite found; best fitness was : {sorted.First().fitness}");
        }
    }
    public void DebugSlices()
    {
        if (arrayRenderTexture != null)
        {
            debugRenderTextureArray.Clear();
            for (int sliceIndex = 0; sliceIndex < arrayRenderTexture.volumeDepth; sliceIndex++)
            {
                RenderTexture sliceTexture = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.CopyTexture(arrayRenderTexture, sliceIndex, 0, sliceTexture, 0, 0);
                debugRenderTextureArray.Add(sliceTexture);
            }
        }

    }
    public void EvaluateFitness()
    {
        if (arrayRenderTexture == null) 
            return;


        {
            sprite_population_list.ForEach(x => x.spriteData.color = Color.white);
            for (int i = 0; i < arrayRenderTexture.volumeDepth; ++i)
            {
                // Set slice as current Render Target
                Graphics.SetRenderTarget(arrayRenderTexture, 0, CubemapFace.Unknown, i);
                GL.Clear(true, true, Color.clear);
                SpriteStamper.Instance.StampSpriteDirect(arrayRenderTexture, sprite_population_list[i].spriteData);

                Graphics.Blit(null, arrayRenderTexture, 0, i);
            }
        }
        
        Color[] averageColorResult = SpriteCompare.Instance.GetAverageColors(targetTexture, arrayRenderTexture);

        for (int i = 0; i < sprite_population_list.Count; ++i)
        {
            sprite_population_list[i].spriteData.color = averageColorResult[i];
        }
        {
            // Create a base RenderTexture to hold the ApplyStamps result
            RenderTexture baseLayer = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height, 0, RenderTextureFormat.ARGB32);

            // ApplyStamps ONCE
            Graphics.SetRenderTarget(baseLayer);
            GL.Clear(true, true, Color.clear); // Optional: clear first
            SpriteStamper.Instance.ApplyStamps(baseLayer, best_sprites_list.Select((value) => value.spriteData).ToArray());
            //Graphics.Blit(null,baseLayer);
            for (int i = 0; i < arrayRenderTexture.volumeDepth; ++i)
            {
                // Set slice as current Render Target
                Graphics.SetRenderTarget(arrayRenderTexture, 0, CubemapFace.Unknown, i);
                GL.Clear(true, true, Color.clear);
                // Copy the baseLayer to the slice
                Graphics.Blit(baseLayer, arrayRenderTexture, 0, i);
                // Now stamp the unique sprite
                SpriteStamper.Instance.StampSpriteDirect(arrayRenderTexture, sprite_population_list[i].spriteData);

                Graphics.Blit(null, arrayRenderTexture, 0, i);
            }

            RenderTexture.ReleaseTemporary(baseLayer);

        }
        ulong[] result = SpriteCompare.Instance.Compare(targetTexture, arrayRenderTexture);

        for (int i = 0; i < sprite_population_list.Count; ++i)
        {
            sprite_population_list[i].fitness = result[i];
        }
    }
  
    public void Mutate()
    {
        const float epsilon_position = 0.2f;
        const float epsilon_scale = 0.2f;
        const float epsilon_rotation = 1.0f;
        foreach (var sprite_pair in sprite_population_list)
        {
            if (Random.Range(0f, 1f) < mutationChance_sprite_index)
                sprite_pair.spriteData.spriteIndex = Random.Range(0, SpriteStamper.Instance.sprites.Length);

            if (Random.Range(0f, 1f) < mutationChance_sprite_position)
            {
                // Mutate the position and ensure it stays within bounds
                sprite_pair.spriteData.position += new Vector2(
                    Random.Range(-epsilon_position, epsilon_position),
                    Random.Range(-epsilon_position, epsilon_position)
                );

                // Clamp position to stay within target image boundaries
                sprite_pair.spriteData.position.x = Mathf.Clamp(sprite_pair.spriteData.position.x, 0, 1.0f);
                sprite_pair.spriteData.position.y = Mathf.Clamp(sprite_pair.spriteData.position.y, 0, 1.0f);
            }
            if (Random.Range(0f, 1f) < mutationChance_sprite_scale)
                sprite_pair.spriteData.scale += new Vector2(Random.Range(-epsilon_scale, epsilon_scale), Random.Range(-epsilon_scale, epsilon_scale)); 

            if (Random.Range(0f, 1f) < mutationChance_sprite_rotation)
                sprite_pair.spriteData.rotation += Random.Range(-epsilon_rotation, epsilon_rotation);
        }
    }
    public void DrawPopulation ()
    {
        if(ResultTexture == null)
        {
            ResultTexture = new RenderTexture(targetTexture.width, targetTexture.height, 0, RenderTextureFormat.ARGB32);
            ResultTexture.filterMode = targetTexture.filterMode;
            ResultTexture.enableRandomWrite = true;
            ResultTexture.Create();
        }
        SpriteStamper.Instance.ClearRenderTexture(ResultTexture);

        var elite = sprite_population_list.OrderByDescending(x => x.fitness).Select((value) => value.spriteData).Take((int)(max_sprite_population_size* PopulationDrawingPercentage));   
        SpriteStamper.Instance.ApplyStamps(ResultTexture, elite.ToArray());
        ResultMaterial.SetTexture("_MainTex", ResultTexture);
    }
    public void DrawBestSprites()
    {
        if (ResultTexture == null)
        {
            ResultTexture = new RenderTexture(targetTexture.width, targetTexture.height, 0, RenderTextureFormat.ARGB32);
            ResultTexture.filterMode = targetTexture.filterMode;
            ResultTexture.enableRandomWrite = true;
            ResultTexture.Create();
        }
        SpriteStamper.Instance.ClearRenderTexture(ResultTexture);
        SpriteStamper.Instance.ApplyStamps(ResultTexture, best_sprites_list.Select(x => x.spriteData).ToArray());
        ResultMaterial.SetTexture("_MainTex", ResultTexture);
    }
    public void CrossOver()
    {

        List<SpriteDataFitnessPair> newGeneration = new List<SpriteDataFitnessPair>(sprite_population_list);
        foreach (var parent1 in sprite_population_list)
        {
            if (newGeneration.Count >= max_sprite_population_size)
                break;
            SpriteDataFitnessPair parent2 = SelectRandomSprite(sprite_population_list);
            var child1 = CrossParents2(parent1.spriteData, parent2.spriteData);
            newGeneration.Add(new SpriteDataFitnessPair
            {
                spriteData = child1,
                fitness = 0,
            });
        }
        sprite_population_list.Clear();
        sprite_population_list = newGeneration;
    }
    private (SpriteData , SpriteData) CrossParents(SpriteData p1 , SpriteData p2)
    {
        return (new SpriteData
        {
            spriteIndex = RandomGene(p1.spriteIndex, p2.spriteIndex),
            position    = RandomGene(p1.position, p2.position),
            scale       = RandomGene(p1.scale, p2.scale),
            rotation    = RandomGene(p1.rotation, p2.rotation),
        }, new SpriteData
        {
            spriteIndex = RandomGene(p1.spriteIndex, p2.spriteIndex),
            position    = RandomGene(p1.position, p2.position),
            scale       = RandomGene(p1.scale, p2.scale),
            rotation    = RandomGene(p1.rotation, p2.rotation),
        });
    }
    private SpriteData CrossParents2(SpriteData p1, SpriteData p2)
    {
        float min_t = -0.25f;
        float max_t = 1.25f;
        return new SpriteData
        {
            spriteIndex = BlendGene(p1.spriteIndex, p2.spriteIndex, Random.Range(min_t, max_t)),
            position = BlendGene(p1.position, p2.position, Random.Range(min_t, max_t)),
            scale = BlendGene(p1.scale, p2.scale, Random.Range(min_t, max_t)),
            rotation = BlendGene(p1.rotation, p2.rotation, Random.Range(min_t, max_t)),
        };
    }
    private T RandomGene<T>(T Gene1 , T Gene2)
    {
       return Random.Range(0.0f, 1.0f) < 0.5f ? Gene1 : Gene2;
    }
    private T BlendGene<T>(T Gene1, T Gene2 ,float t)
    {
        if (typeof(T) == typeof(Vector2))
            return (T)(object)Vector2.Lerp((Vector2)(object)Gene1, (Vector2)(object)Gene2, t);
        if (typeof(T) == typeof(float))
            return (T)(object)Mathf.Lerp((float)(object)Gene1, (float)(object)Gene2, t);
        if (typeof(T) == typeof(Color))
            return (T)(object)Color.Lerp((Color)(object)Gene1, (Color)(object)Gene2, t);
        return Random.Range(0.0f, 1.0f) < 0.5f ? Gene1 : Gene2;
    }

    private SpriteDataFitnessPair TournamentSelect(List<SpriteDataFitnessPair> population, int size = 3)
    {
        var competitors = population.OrderBy(x => Random.value).Take(size).ToList();
        return competitors.OrderByDescending(x => x.fitness).First();
    }

    public void SelectBestSprites()
    {
        if (sprite_population_list.Count < max_sprite_population_size)
        {
            Debug.LogError("Not enough sprites to select from");
            return;
        }

        int eliteCount = Mathf.Max(1, (int)(max_sprite_population_size * max_passing_sprite_percentage));
        int survivorCount = (int)(max_sprite_population_size * max_breeding_sprite_percentage) - eliteCount;

        var sorted = sprite_population_list.OrderByDescending(x => x.fitness).ToList();
        var elites = sorted.Take(eliteCount).ToList();
        var survivorsForTournament = sorted.Skip(eliteCount).ToList();

        List<SpriteDataFitnessPair> survivors = new List<SpriteDataFitnessPair>(elites);
        for (int i = 0; i < survivorCount; ++i)
        {
            survivors.Add(TournamentSelect(survivorsForTournament));
        }

        sprite_population_list = survivors;
    }
  

    public SpriteDataFitnessPair SelectRandomSprite(List<SpriteDataFitnessPair> sprites_population)
    {
        ulong total_fitness = 0;
        for (int i = 0; i < sprites_population.Count; ++i)
        {
            total_fitness += sprites_population[i].fitness;
        }
        float randomValue = Random.Range(0f, 100.0f);

        float cumulativeChance = 0f;
        for (int i = 0; i < sprite_population_list.Count; ++i)
        {
            if (total_fitness == 0) 
                break;
            ulong chancePercentage = (ulong)(sprites_population[i].fitness / total_fitness * 100.0f);
            cumulativeChance += chancePercentage;


            if (randomValue <= cumulativeChance)
            {
                return sprites_population[i];
            }
        }
        return sprites_population.Last(); 
    }



}
