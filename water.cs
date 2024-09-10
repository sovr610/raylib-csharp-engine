using System;
using System.Numerics;
using Raylib_cs;

public class Water
{
    private int width;
    private int height;
    private float waterHeight;
    private Texture2D normalMap;
    private Texture2D specularMap;
    private Shader waterShader;
    private Model waterPlane;

    // Wave properties
    private float waveAmplitude = 0.5f;
    private float waveFrequency = 0.1f;
    private float waveSpeed = 0.5f;

    // Noise properties
    private float noiseScale = 0.1f;
    private float noiseSpeed = 0.05f;

    public Water(int width, int height, float waterHeight)
    {
        this.width = width;
        this.height = height;
        this.waterHeight = waterHeight;

        InitializeWater();
    }

    private void InitializeWater()
    {
        // Create water plane
        Mesh mesh = Raylib.GenMeshPlane(width, height, 10, 10);
        waterPlane = Raylib.LoadModelFromMesh(mesh);

        // Load textures
        normalMap = Raylib.LoadTexture("path/to/normal_map.png");
        specularMap = Raylib.LoadTexture("path/to/specular_map.png");

        // Load and configure shader
        waterShader = Raylib.LoadShader("path/to/water_vertex.glsl", "path/to/water_fragment.glsl");
        waterShader.locs[(int)ShaderLocationIndex.SHADER_LOC_VECTOR_VIEW] = Raylib.GetShaderLocation(waterShader, "viewPos");
        waterShader.locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_MODEL] = Raylib.GetShaderLocation(waterShader, "matModel");
        waterShader.locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_NORMAL] = Raylib.GetShaderLocation(waterShader, "matNormal");

        // Set shader uniforms
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "normalMap"), new int[] { 1 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "specularMap"), new int[] { 2 }, ShaderUniformDataType.SHADER_UNIFORM_INT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "waveAmplitude"), new float[] { waveAmplitude }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "waveFrequency"), new float[] { waveFrequency }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "waveSpeed"), new float[] { waveSpeed }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "noiseScale"), new float[] { noiseScale }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "noiseSpeed"), new float[] { noiseSpeed }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

        // Set water material
        waterPlane.materials[0].shader = waterShader;
        waterPlane.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_NORMAL].texture = normalMap;
        waterPlane.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_SPECULAR].texture = specularMap;
    }

    public void Update(float deltaTime)
    {
        // Update time-based uniforms
        float time = (float)Raylib.GetTime();
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "time"), new float[] { time }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

        // Update view position for fresnel calculation
        Vector3 cameraPos = Raylib.GetCameraPosition();
        Raylib.SetShaderValue(waterShader, waterShader.locs[(int)ShaderLocationIndex.SHADER_LOC_VECTOR_VIEW], new float[] { cameraPos.X, cameraPos.Y, cameraPos.Z }, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
    }

    public void Render()
    {
        // Calculate model matrix
        Matrix4x4 matModel = Matrix4x4.CreateTranslation(0, waterHeight, 0);
        Raylib.SetShaderValueMatrix(waterShader, waterShader.locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_MODEL], matModel);

        // Calculate normal matrix (transpose(inverse(matModel)))
        Matrix4x4 matNormal = Matrix4x4.Transpose(Matrix4x4.Invert(matModel));
        Raylib.SetShaderValueMatrix(waterShader, waterShader.locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_NORMAL], matNormal);

        // Draw water plane
        Raylib.DrawModel(waterPlane, new Vector3(0, waterHeight, 0), 1.0f, Color.WHITE);
    }

    public void SetWaterHeight(float height)
    {
        waterHeight = height;
    }

    public void SetWaveProperties(float amplitude, float frequency, float speed)
    {
        waveAmplitude = amplitude;
        waveFrequency = frequency;
        waveSpeed = speed;

        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "waveAmplitude"), new float[] { waveAmplitude }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "waveFrequency"), new float[] { waveFrequency }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "waveSpeed"), new float[] { waveSpeed }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
    }

    public void SetNoiseProperties(float scale, float speed)
    {
        noiseScale = scale;
        noiseSpeed = speed;

        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "noiseScale"), new float[] { noiseScale }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
        Raylib.SetShaderValue(waterShader, Raylib.GetShaderLocation(waterShader, "noiseSpeed"), new float[] { noiseSpeed }, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
    }

    public void Unload()
    {
        Raylib.UnloadTexture(normalMap);
        Raylib.UnloadTexture(specularMap);
        Raylib.UnloadShader(waterShader);
        Raylib.UnloadModel(waterPlane);
    }
}