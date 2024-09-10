using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

public class SkySphere
{
    private Model sphere;
    private Shader skyShader;
    private Texture2D daySkyTexture;
    private Texture2D nightSkyTexture;
    private Texture2D cloudTexture;
    private List<CelestialBody> celestialBodies;

    public float TimeOfDay { get; set; } // 0.0 to 1.0, where 0.5 is noon
    public float CloudCoverage { get; set; } // 0.0 to 1.0
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

    public SkySphere(float radius, string daySkyTexturePath = null, string nightSkyTexturePath = null, string cloudTexturePath = null)
    {
        sphere = Raylib.GenMeshSphere(radius, 32, 32);
        skyShader = Raylib.LoadShader("shaders/sky.vs", "shaders/sky.fs");

        daySkyTexture = LoadTextureOrDefault(daySkyTexturePath, GenerateDefaultDaySky());
        nightSkyTexture = LoadTextureOrDefault(nightSkyTexturePath, GenerateDefaultNightSky());
        cloudTexture = LoadTextureOrDefault(cloudTexturePath, GenerateDefaultClouds());

        celestialBodies = new List<CelestialBody>();
        TimeOfDay = 0.2f; // Start at dawn
        CloudCoverage = 0.5f;
        CloudSpeed = 0.001f;

        SetupShaderLocations();
    }

    private Texture2D LoadTextureOrDefault(string texturePath, Texture2D defaultTexture)
    {
        if (!string.IsNullOrEmpty(texturePath) && System.IO.File.Exists(texturePath))
        {
            return Raylib.LoadTexture(texturePath);
        }
        return defaultTexture;
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

    private void SetupShaderLocations()
    {
        Raylib.SetShaderValue(skyShader, Raylib.GetShaderLocation(skyShader, "daySkyTexture"), new int[] { 0 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(skyShader, Raylib.GetShaderLocation(skyShader, "nightSkyTexture"), new int[] { 1 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(skyShader, Raylib.GetShaderLocation(skyShader, "cloudTexture"), new int[] { 2 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
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
        TimeOfDay += deltaTime * 0.1f; // Adjust this value to change day/night cycle speed
        if (TimeOfDay >= 1.0f) TimeOfDay -= 1.0f;

        Raylib.SetShaderValue(skyShader, Raylib.GetShaderLocation(skyShader, "timeOfDay"), new float[] { TimeOfDay }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(skyShader, Raylib.GetShaderLocation(skyShader, "cloudCoverage"), new float[] { CloudCoverage }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(skyShader, Raylib.GetShaderLocation(skyShader, "cloudOffset"), new float[] { TimeOfDay * CloudSpeed }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
    }

    public void Draw(Camera3D camera)
    {
        // Draw sky sphere
        Raylib.BeginShaderMode(skyShader);
        Raylib.SetShaderValueTexture(skyShader, Raylib.GetShaderLocation(skyShader, "daySkyTexture"), daySkyTexture);
        Raylib.SetShaderValueTexture(skyShader, Raylib.GetShaderLocation(skyShader, "nightSkyTexture"), nightSkyTexture);
        Raylib.SetShaderValueTexture(skyShader, Raylib.GetShaderLocation(skyShader, "cloudTexture"), cloudTexture);
        Raylib.DrawModel(sphere, Vector3.Zero, 1.0f, Color.WHITE);
        Raylib.EndShaderMode();

        // Draw celestial bodies
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
    }

    public void Unload()
    {
        Raylib.UnloadModel(sphere);
        Raylib.UnloadShader(skyShader);
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