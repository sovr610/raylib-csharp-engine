using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public enum BiomeType
{
    GrassPlain,
    Desert,
    Ocean,
    Mountains,
    Tundra,
    Rainforest,
    Savanna
}

public class BiomeTextures
{
    public Texture2D Diffuse { get; set; }
    public Texture2D Normal { get; set; }
    public Texture2D Specular { get; set; }
    public Texture2D AO { get; set; }
    public Texture2D Bump { get; set; }
    public Texture2D Displacement { get; set; }

    public BiomeTextures(string basePath)
    {
        Diffuse = Raylib.LoadTexture($"{basePath}_diffuse.png");
        Normal = Raylib.LoadTexture($"{basePath}_normal.png");
        Specular = Raylib.LoadTexture($"{basePath}_specular.png");
        AO = Raylib.LoadTexture($"{basePath}_ao.png");
        Bump = Raylib.LoadTexture($"{basePath}_bump.png");
        Displacement = Raylib.LoadTexture($"{basePath}_displacement.png");
    }

    public void Unload()
    {
        Raylib.UnloadTexture(Diffuse);
        Raylib.UnloadTexture(Normal);
        Raylib.UnloadTexture(Specular);
        Raylib.UnloadTexture(AO);
        Raylib.UnloadTexture(Bump);
        Raylib.UnloadTexture(Displacement);
    }
}

public class Biome
{
    public BiomeType Type { get; set; }
    public Color Color { get; set; }
    public float HeightMultiplier { get; set; }
    public float Percentage { get; set; }
    public BiomeTextures Textures { get; set; }

    public Biome(BiomeType type, Color color, float heightMultiplier, float percentage, string texturePath)
    {
        Type = type;
        Color = color;
        HeightMultiplier = heightMultiplier;
        Percentage = percentage;
        Textures = new BiomeTextures(texturePath);
    }
}

public class TerrainGenerator
{
    private int width;
    private int depth;
    private float[,] heightMap;
    private float[,,] biomeWeights;
    private List<Model> lodModels;
    private float maxRenderDistance;
    private int lodLevels;
    private List<Biome> biomes;
    private Random random;
    private Shader terrainShader;
    private Texture2D biomeWeightMap;

    public TerrainGenerator(int width, int depth, float maxRenderDistance, int lodLevels)
    {
        this.width = width;
        this.depth = depth;
        this.maxRenderDistance = maxRenderDistance;
        this.lodLevels = lodLevels;
        this.heightMap = new float[width, depth];
        this.biomeWeights = new float[width, depth, 4];
        this.lodModels = new List<Model>();
        this.random = new Random();
        InitializeBiomes();
        GenerateHeightMap();
        GenerateBiomeWeights();
        CreateLODModels();
        LoadTerrainShader();
        CreateBiomeWeightMap();
    }

    private void InitializeBiomes()
    {
        biomes = new List<Biome>
        {
            new Biome(BiomeType.GrassPlain, Color.GREEN, 1.0f, 0.3f, "textures/grass"),
            new Biome(BiomeType.Desert, Color.YELLOW, 0.8f, 0.2f, "textures/desert"),
            new Biome(BiomeType.Ocean, Color.BLUE, 0.2f, 0.3f, "textures/ocean"),
            new Biome(BiomeType.Mountains, Color.GRAY, 1.5f, 0.1f, "textures/mountain"),
            new Biome(BiomeType.Tundra, Color.WHITE, 0.9f, 0.05f, "textures/tundra"),
            new Biome(BiomeType.Rainforest, Color.DARKGREEN, 1.2f, 0.03f, "textures/rainforest"),
            new Biome(BiomeType.Savanna, Color.ORANGE, 0.9f, 0.02f, "textures/savanna")
        };

        float totalPercentage = biomes.Sum(b => b.Percentage);
        foreach (var biome in biomes)
        {
            biome.Percentage /= totalPercentage;
        }
    }

    private void GenerateHeightMap()
    {
        SimplexNoise noise = new SimplexNoise(random.Next());
        
        float scale = 0.005f;
        int octaves = 6;
        float persistence = 0.5f;
        float lacunarity = 2f;

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = x * scale * frequency;
                    float sampleZ = z * scale * frequency;

                    float perlinValue = (float)noise.Evaluate(sampleX, sampleZ) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heightMap[x, z] = noiseHeight;

                if (noiseHeight > maxHeight) maxHeight = noiseHeight;
                if (noiseHeight < minHeight) minHeight = noiseHeight;
            }
        }

        // Normalize height values
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                heightMap[x, z] = (heightMap[x, z] - minHeight) / (maxHeight - minHeight);
            }
        }
    }

    private void GenerateBiomeWeights()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float totalWeight = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float weight = (float)Math.Pow(random.NextDouble(), 2);
                    biomeWeights[x, z, i] = weight;
                    totalWeight += weight;
                }
                
                for (int i = 0; i < 4; i++)
                {
                    biomeWeights[x, z, i] /= totalWeight;
                }
            }
        }
    }

    private void CreateBiomeWeightMap()
    {
        Image weightMapImage = Raylib.GenImageColor(width, depth, Color.BLACK);
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Color pixelColor = new Color(
                    (byte)(biomeWeights[x, z, 0] * 255),
                    (byte)(biomeWeights[x, z, 1] * 255),
                    (byte)(biomeWeights[x, z, 2] * 255),
                    (byte)(biomeWeights[x, z, 3] * 255)
                );
                Raylib.ImageDrawPixel(ref weightMapImage, x, z, pixelColor);
            }
        }
        
        biomeWeightMap = Raylib.LoadTextureFromImage(weightMapImage);
        Raylib.UnloadImage(weightMapImage);
    }

    private void LoadTerrainShader()
    {
        terrainShader = Raylib.LoadShader("shaders/terrain_blend.vs", "shaders/terrain_blend.fs");
        for (int i = 0; i < 4; i++)
        {
            Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].diffuse"), new int[] { i * 6 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].normal"), new int[] { i * 6 + 1 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].specular"), new int[] { i * 6 + 2 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].ao"), new int[] { i * 6 + 3 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].bump"), new int[] { i * 6 + 4 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].displacement"), new int[] { i * 6 + 5 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        }
        Raylib.SetShaderValue(terrainShader, Raylib.GetShaderLocation(terrainShader, "biomeWeightMap"), new int[] { 24 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
    }

    private void CreateLODModels()
    {
        for (int i = 0; i < lodLevels; i++)
        {
            int lodWidth = width / (int)Math.Pow(2, i);
            int lodDepth = depth / (int)Math.Pow(2, i);

            Mesh mesh = Raylib.GenMeshHeightmap(
                Raylib.GenImagePerlinNoise(lodWidth, lodDepth, 0, 0, 5),
                new Vector3(width, 20, depth)
            );
            Model model = Raylib.LoadModelFromMesh(mesh);

            model.materials[0].shader = terrainShader;

            unsafe
            {
                float* vertices = (float*)model.meshes.vertices;
                for (int v = 0; v < model.meshes.vertexCount; v++)
                {
                    int x = (v % lodWidth) * (width / lodWidth);
                    int z = (v / lodWidth) * (depth / lodDepth);
                    vertices[v * 3 + 1] = heightMap[x, z];
                }
            }

            Raylib.UpdateMeshBuffer(model.meshes, 0, model.meshes.vertices, model.meshes.vertexCount * 3 * sizeof(float), 0);

            lodModels.Add(model);
        }
    }

    public void Draw(Vector3 cameraPosition)
    {
        for (int x = 0; x < width; x += width / 4)
        {
            for (int z = 0; z < depth; z += depth / 4)
            {
                Vector3 chunkPosition = new Vector3(x - width / 2, 0, z - depth / 2);
                float distance = Vector3.Distance(cameraPosition, chunkPosition);

                if (distance > maxRenderDistance) continue;

                int lodLevel = (int)(distance / (maxRenderDistance / lodLevels));
                lodLevel = Math.Min(lodLevel, lodLevels - 1);

                chunkPosition.Y = GetTerrainHeight(x, z);

                for (int i = 0; i < 4; i++)
                {
                    Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].diffuse"), biomes[i].Textures.Diffuse);
                    Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].normal"), biomes[i].Textures.Normal);
                    Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].specular"), biomes[i].Textures.Specular);
                    Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].ao"), biomes[i].Textures.AO);
                    Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].bump"), biomes[i].Textures.Bump);
                    Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, $"biomeTextures[{i}].displacement"), biomes[i].Textures.Displacement);
                }
                Raylib.SetShaderValueTexture(terrainShader, Raylib.GetShaderLocation(terrainShader, "biomeWeightMap"), biomeWeightMap);

                Raylib.DrawModel(lodModels[lodLevel], chunkPosition, 1.0f, Color.WHITE);
            }
        }
    }

    public float GetTerrainHeight(int x, int z)
    {
        x = Math.Clamp(x, 0, width - 1);
        z = Math.Clamp(z, 0, depth - 1);
        return heightMap[x, z];
    }

    public BiomeType GetBiomeAt(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < depth)
        {
            return (BiomeType)Array.IndexOf(biomeWeights.GetRow(x).GetRow(z), biomeWeights.GetRow(x).GetRow(z).Max());
        }
        return BiomeType.Ocean;
    }

    public void SetBiomePercentage(BiomeType biomeType, float percentage)
    {
        var biome = biomes.Find(b => b.Type == biomeType);
        if (biome != null)
        {
            biome.Percentage = percentage;
            float totalPercentage = biomes.Sum(b => b.Percentage);
            foreach (var b in biomes)
            {
                b.Percentage /= totalPercentage;
            }
            GenerateBiomeWeights();
            CreateBiomeWeightMap();
        }
    }

    public void Unload()
    {
        foreach (var model in lodModels)
        {
            Raylib.UnloadModel(model);
        }
        foreach (var biome in biomes)
        {
            biome.Textures.Unload();
        }
        Raylib.UnloadShader(terrainShader);
        Raylib.UnloadTexture(biomeWeightMap);
    }
}