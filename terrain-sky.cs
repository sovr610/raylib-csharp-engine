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

public class WorldGenerator
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
    private Shader worldShader;
    private Texture2D biomeWeightMap;

    private Model skySphere;
    private Texture2D daySkyTexture;
    private Texture2D nightSkyTexture;
    private Texture2D cloudTexture;
    private List<CelestialBody> celestialBodies;

    public float TimeOfDay { get; set; }
    public float CloudCoverage { get; set; }
    public float CloudSpeed { get; set; }

    public struct CelestialBody
    {
        public Model model;
        public Texture2D texture;
        public float size;
        public float orbitSpeed;
        public float orbitRadius;
        public Vector3 color;
        public bool isSun;
    }

    public WorldGenerator(int width, int depth, float maxRenderDistance, int lodLevels, float skyRadius)
    {
        this.width = width;
        this.depth = depth;
        this.maxRenderDistance = maxRenderDistance;
        this.lodLevels = lodLevels;
        this.heightMap = new float[width, depth];
        this.biomeWeights = new float[width, depth, 4];
        this.lodModels = new List<Model>();
        this.random = new Random();
        this.celestialBodies = new List<CelestialBody>();

        InitializeBiomes();
        GenerateHeightMap();
        GenerateBiomeWeights();
        CreateLODModels();
        LoadWorldShader();
        CreateBiomeWeightMap();

        InitializeSky(skyRadius);

        TimeOfDay = 0.2f;
        CloudCoverage = 0.5f;
        CloudSpeed = 0.001f;
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

    private void LoadWorldShader()
    {
        worldShader = Raylib.LoadShader("shaders/world.vs", "shaders/world.fs");
        for (int i = 0; i < 4; i++)
        {
            Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].diffuse"), new int[] { i * 6 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].normal"), new int[] { i * 6 + 1 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].specular"), new int[] { i * 6 + 2 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].ao"), new int[] { i * 6 + 3 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].bump"), new int[] { i * 6 + 4 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].displacement"), new int[] { i * 6 + 5 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        }
        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "biomeWeightMap"), new int[] { 24 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "daySkyTexture"), new int[] { 25 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "nightSkyTexture"), new int[] { 26 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "cloudTexture"), new int[] { 27 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
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

            model.materials[0].shader = worldShader;

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

    private void InitializeSky(float radius)
    {
        skySphere = Raylib.GenMeshSphere(radius, 32, 32);
        daySkyTexture = GenerateDefaultDaySky();
        nightSkyTexture = GenerateDefaultNightSky();
        cloudTexture = GenerateDefaultClouds();
    }

    private Texture2D GenerateDefaultDaySky()
    {
        Image img = Raylib.GenImageColor(512, 512, Color.SKYBLUE);
        Texture2D texture = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return texture;
    }

    private Texture2D GenerateDefaultNightSky()
    {
        Image img = Raylib.GenImageColor(512, 512, Color.DARKBLUE);
        for (int i = 0; i < 1000; i++)
        {
            int x = Raylib.GetRandomValue(0, 511);
            int y = Raylib.GetRandomValue(0, 511);
            Raylib.ImageDrawPixel(ref img, x, y, Color.WHITE);
        }
        Texture2D texture = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return texture;
    }

    private Texture2D GenerateDefaultClouds()
    {
        Image img = Raylib.GenImagePerlinNoise(512, 512, 0, 0, 5.0f);
        Texture2D texture = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return texture;
    }

public void AddCelestialBody(float size, float orbitSpeed, float orbitRadius, Vector3 color, string texturePath = null, bool isSun = false)
    {
        Model model = Raylib.GenMeshSphere(size, 16, 16);
        Texture2D texture = LoadTextureOrDefault(texturePath, GenerateDefaultCelestialTexture(isSun));

        celestialBodies.Add(new CelestialBody
        {
            model = model,
            texture = texture,
            size = size,
            orbitSpeed = orbitSpeed,
            orbitRadius = orbitRadius,
            color = color,
            isSun = isSun
        });
    }

    private Texture2D LoadTextureOrDefault(string texturePath, Texture2D defaultTexture)
    {
        if (!string.IsNullOrEmpty(texturePath) && System.IO.File.Exists(texturePath))
        {
            return Raylib.LoadTexture(texturePath);
        }
        return defaultTexture;
    }

    private Texture2D GenerateDefaultCelestialTexture(bool isSun)
    {
        Color color = isSun ? Color.YELLOW : Color.LIGHTGRAY;
        Image img = Raylib.GenImageColor(256, 256, color);
        Texture2D texture = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return texture;
    }

    public void Update(float deltaTime)
    {
        TimeOfDay += deltaTime * 0.1f;
        if (TimeOfDay >= 1.0f) TimeOfDay -= 1.0f;

        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "timeOfDay"), new float[] { TimeOfDay }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "cloudCoverage"), new float[] { CloudCoverage }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(worldShader, Raylib.GetShaderLocation(worldShader, "cloudOffset"), new float[] { TimeOfDay * CloudSpeed }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
    }

    public void Draw(Camera3D camera)
    {
        Raylib.BeginShaderMode(worldShader);

        Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, "daySkyTexture"), daySkyTexture);
        Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, "nightSkyTexture"), nightSkyTexture);
        Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, "cloudTexture"), cloudTexture);

        Raylib.DrawModel(skySphere, Vector3.Zero, 1.0f, Color.WHITE);

        for (int x = 0; x < width; x += width / 4)
        {
            for (int z = 0; z < depth; z += depth / 4)
            {
                Vector3 chunkPosition = new Vector3(x - width / 2, 0, z - depth / 2);
                float distance = Vector3.Distance(camera.position, chunkPosition);

                if (distance > maxRenderDistance) continue;

                int lodLevel = (int)(distance / (maxRenderDistance / lodLevels));
                lodLevel = Math.Min(lodLevel, lodLevels - 1);

                chunkPosition.Y = GetTerrainHeight(x, z);

                for (int i = 0; i < 4; i++)
                {
                    Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].diffuse"), biomes[i].Textures.Diffuse);
                    Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].normal"), biomes[i].Textures.Normal);
                    Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].specular"), biomes[i].Textures.Specular);
                    Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].ao"), biomes[i].Textures.AO);
                    Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].bump"), biomes[i].Textures.Bump);
                    Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, $"biomeTextures[{i}].displacement"), biomes[i].Textures.Displacement);
                }
                Raylib.SetShaderValueTexture(worldShader, Raylib.GetShaderLocation(worldShader, "biomeWeightMap"), biomeWeightMap);

                Raylib.DrawModel(lodModels[lodLevel], chunkPosition, 1.0f, Color.WHITE);
            }
        }

        foreach (var body in celestialBodies)
        {
            float angle = (TimeOfDay + (body.isSun ? 0.5f : 0)) * MathF.PI * 2;
            Vector3 position = new Vector3(
                MathF.Cos(angle * body.orbitSpeed) * body.orbitRadius,
                MathF.Sin(angle * body.orbitSpeed) * body.orbitRadius,
                0
            );

            Raylib.DrawModel(body.model, position, body.size, Color.WHITE);
        }

        Raylib.EndShaderMode();
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
        Raylib.UnloadShader(worldShader);
        Raylib.UnloadTexture(biomeWeightMap);
        Raylib.UnloadModel(skySphere);
        Raylib.UnloadTexture(daySkyTexture);
        Raylib.UnloadTexture(nightSkyTexture);
        Raylib.UnloadTexture(cloudTexture);

        foreach (var body in celestialBodies)
        {
            Raylib.UnloadModel(body.model);
            Raylib.UnloadTexture(body.texture);
        }
    }
}

public class SimplexNoise
{
    private int[] A = new int[3];
    private float s, u, v, w;
    private int i, j, k;
    private float onethird = 0.333333333f;
    private float onesixth = 0.166666667f;
    private int[] T;

    public SimplexNoise(int seed)
    {
        if (seed != 0)
        {
            Random rand = new Random(seed);
            for (int i = 0; i < 256; i++) T[i] = i;
            for (int i = 0; i < 256; i++)
            {
                int j = rand.Next(256);
                int temp = T[i];
                T[i] = T[j];
                T[j] = temp;
            }
        }
    }

    public double Evaluate(double x, double y)
    {
        s = (float)(x + y) * onethird;
        i = FastFloor(x + s);
        j = FastFloor(y + s);

        s = (i + j) * onesixth;
        u = (float)(x - i + s);
        v = (float)(y - j + s);

        A[0] = 0;
        A[1] = 0;
        A[2] = 0;

        int hi = u >= v ? 1 : 0;
        int lo = u < v ? 1 : 0;

        return K(hi) + K(3 - hi - lo) + K(lo) + K(0);
    }

    private int FastFloor(double x)
    {
        return x > 0 ? (int)x : (int)x - 1;
    }

    private float K(int a)
    {
        s = (A[0] + A[1] + A[2]) * onesixth;
        float x = u - A[0] + s;
        float y = v - A[1] + s;
        float z = w - A[2] + s;
        float t = 0.6f - x * x - y * y - z * z;
        int h = Shuffle(i + A[0], j + A[1], k + A[2]);
        A[a]++;
        if (t < 0) return 0;
        int b5 = h >> 5 & 1;
        int b4 = h >> 4 & 1;
        int b3 = h >> 3 & 1;
        int b2 = h >> 2 & 1;
        int b1 = h & 3;
        float p = b1 == 1 ? x : b1 == 2 ? y : z;
        float q = b1 == 1 ? y : b1 == 2 ? z : x;
        float r = b1 == 1 ? z : b1 == 2 ? x : y;
        p = b5 == b3 ? -p : p;
        q = b5 == b4 ? -q : q;
        r = b5 != (b4 ^ b3) ? -r : r;
        t *= t;
        return 8 * t * t * (p + (b1 == 0 ? q + r : b2 == 0 ? q : r));
    }

    private int Shuffle(int i, int j, int k)
    {
        return b(i, j, k, 0) + b(j, k, i, 1) + b(k, i, j, 2) + b(i, j, k, 3) +
               b(j, k, i, 4) + b(k, i, j, 5) + b(i, j, k, 6) + b(j, k, i, 7);
    }

    private int b(int i, int j, int k, int B)
    {
        return T[b(i, B) << 2 | b(j, B) << 1 | b(k, B)];
    }

    private int b(int N, int B)
    {
        return N >> B & 1;
    }
}