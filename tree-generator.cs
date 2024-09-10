using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

public class TreeGenerator
{
    private Random random;
    private Texture2D trunkDiffuseTexture;
    private Texture2D trunkNormalTexture;
    private Texture2D trunkSpecularTexture;
    private Texture2D trunkBumpTexture;
    private Texture2D trunkAOTexture;
    private Texture2D leafDiffuseTexture;
    private Texture2D leafNormalTexture;
    private Texture2D leafSpecularTexture;
    private Texture2D leafBumpTexture;
    private Texture2D leafAOTexture;
    private float treeHeight;
    private int branchCount;
    private float leafDensity;
    private Model treeModel;
    private Shader treeShader;
    private Vector3 position;
    private Vector3 rotation;

    public TreeGenerator(
        Texture2D trunkDiffuse, Texture2D trunkNormal, Texture2D trunkSpecular, Texture2D trunkBump, Texture2D trunkAO,
        Texture2D leafDiffuse, Texture2D leafNormal, Texture2D leafSpecular, Texture2D leafBump, Texture2D leafAO,
        float height, int branches, float density, Shader shader)
    {
        random = new Random();
        trunkDiffuseTexture = trunkDiffuse;
        trunkNormalTexture = trunkNormal;
        trunkSpecularTexture = trunkSpecular;
        trunkBumpTexture = trunkBump;
        trunkAOTexture = trunkAO;
        leafDiffuseTexture = leafDiffuse;
        leafNormalTexture = leafNormal;
        leafSpecularTexture = leafSpecular;
        leafBumpTexture = leafBump;
        leafAOTexture = leafAO;
        treeHeight = height;
        branchCount = branches;
        leafDensity = density;
        treeShader = shader;
        position = new Vector3(0, 0, 0);
        rotation = new Vector3(0, 0, 0);
    }

    public Model GenerateTree()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> texCoords = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<ushort> indices = new List<ushort>();

        GenerateTrunk(vertices, texCoords, normals, indices);
        GenerateBranches(vertices, texCoords, normals, indices);
        GenerateLeaves(vertices, texCoords, normals, indices);

        Mesh mesh = new Mesh();
        unsafe
        {
            fixed (Vector3* vPtr = vertices.ToArray())
            fixed (Vector2* tPtr = texCoords.ToArray())
            fixed (Vector3* nPtr = normals.ToArray())
            fixed (ushort* iPtr = indices.ToArray())
            {
                mesh.vertices = (float*)vPtr;
                mesh.texcoords = (float*)tPtr;
                mesh.normals = (float*)nPtr;
                mesh.indices = iPtr;
            }
            mesh.vertexCount = vertices.Count;
            mesh.triangleCount = indices.Count / 3;
        }

        treeModel = Raylib.LoadModelFromMesh(mesh);

        treeModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_DIFFUSE].texture = trunkDiffuseTexture;
        treeModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_NORMAL].texture = trunkNormalTexture;
        treeModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_SPECULAR].texture = trunkSpecularTexture;
        treeModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ROUGHNESS].texture = trunkBumpTexture;
        treeModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_OCCLUSION].texture = trunkAOTexture;

        treeModel.materials[1].maps[(int)MaterialMapIndex.MATERIAL_MAP_DIFFUSE].texture = leafDiffuseTexture;
        treeModel.materials[1].maps[(int)MaterialMapIndex.MATERIAL_MAP_NORMAL].texture = leafNormalTexture;
        treeModel.materials[1].maps[(int)MaterialMapIndex.MATERIAL_MAP_SPECULAR].texture = leafSpecularTexture;
        treeModel.materials[1].maps[(int)MaterialMapIndex.MATERIAL_MAP_ROUGHNESS].texture = leafBumpTexture;
        treeModel.materials[1].maps[(int)MaterialMapIndex.MATERIAL_MAP_OCCLUSION].texture = leafAOTexture;

        treeModel.materials[0].shader = treeShader;
        treeModel.materials[1].shader = treeShader;

        UpdateModelTransform();

        return treeModel;
    }

    private void GenerateTrunk(List<Vector3> vertices, List<Vector2> texCoords, List<Vector3> normals, List<ushort> indices)
    {
        float trunkRadius = treeHeight * 0.05f;
        int segments = 8;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * MathF.PI * 2 / segments;
            float x = MathF.Cos(angle) * trunkRadius;
            float z = MathF.Sin(angle) * trunkRadius;

            vertices.Add(new Vector3(x, 0, z));
            vertices.Add(new Vector3(x, treeHeight, z));

            texCoords.Add(new Vector2((float)i / segments, 0));
            texCoords.Add(new Vector2((float)i / segments, 1));

            Vector3 normal = Vector3.Normalize(new Vector3(x, 0, z));
            normals.Add(normal);
            normals.Add(normal);

            if (i < segments)
            {
                ushort baseIndex = (ushort)(i * 2);
                indices.AddRange(new ushort[] { baseIndex, (ushort)(baseIndex + 1), (ushort)(baseIndex + 2) });
                indices.AddRange(new ushort[] { (ushort)(baseIndex + 1), (ushort)(baseIndex + 3), (ushort)(baseIndex + 2) });
            }
        }
    }

    private void GenerateBranches(List<Vector3> vertices, List<Vector2> texCoords, List<Vector3> normals, List<ushort> indices)
    {
        for (int i = 0; i < branchCount; i++)
        {
            float height = (float)random.NextDouble() * 0.6f + 0.2f;
            float angle = (float)random.NextDouble() * MathF.PI * 2;
            float length = treeHeight * (0.3f + (float)random.NextDouble() * 0.3f);

            Vector3 start = new Vector3(0, treeHeight * height, 0);
            Vector3 direction = new Vector3(MathF.Cos(angle), 0.5f + (float)random.NextDouble() * 0.5f, MathF.Sin(angle));
            direction = Vector3.Normalize(direction);

            Vector3 end = start + direction * length;

            GenerateBranch(start, end, vertices, texCoords, normals, indices);
        }
    }

    private void GenerateBranch(Vector3 start, Vector3 end, List<Vector3> vertices, List<Vector2> texCoords, List<Vector3> normals, List<ushort> indices)
    {
        Vector3 direction = Vector3.Normalize(end - start);
        Vector3 perpendicular = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitY));
        Vector3 up = Vector3.Cross(direction, perpendicular);

        float radius = treeHeight * 0.02f;
        int segments = 6;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * MathF.PI * 2 / segments;
            Vector3 offset = perpendicular * MathF.Cos(angle) * radius + up * MathF.Sin(angle) * radius;

            vertices.Add(start + offset);
            vertices.Add(end + offset * 0.5f);

            texCoords.Add(new Vector2((float)i / segments, 0));
            texCoords.Add(new Vector2((float)i / segments, 1));

            Vector3 normal = Vector3.Normalize(offset);
            normals.Add(normal);
            normals.Add(normal);

            if (i < segments)
            {
                ushort baseIndex = (ushort)(vertices.Count - 2 * (i + 1));
                indices.AddRange(new ushort[] { baseIndex, (ushort)(baseIndex + 1), (ushort)(baseIndex + 2) });
                indices.AddRange(new ushort[] { (ushort)(baseIndex + 1), (ushort)(baseIndex + 3), (ushort)(baseIndex + 2) });
            }
        }
    }

    private void GenerateLeaves(List<Vector3> vertices, List<Vector2> texCoords, List<Vector3> normals, List<ushort> indices)
    {
        int leafCount = (int)(branchCount * leafDensity);

        for (int i = 0; i < leafCount; i++)
        {
            float height = (float)random.NextDouble() * 0.8f + 0.2f;
            float angle = (float)random.NextDouble() * MathF.PI * 2;
            float distance = treeHeight * (0.2f + (float)random.NextDouble() * 0.3f);

            Vector3 position = new Vector3(
                MathF.Cos(angle) * distance,
                treeHeight * height,
                MathF.Sin(angle) * distance
            );

            GenerateLeaf(position, vertices, texCoords, normals, indices);
        }
    }

    private void GenerateLeaf(Vector3 position, List<Vector3> vertices, List<Vector2> texCoords, List<Vector3> normals, List<ushort> indices)
    {
        float size = treeHeight * 0.05f;
        Vector3 right = new Vector3(1, 0, 0) * size;
        Vector3 up = new Vector3(0, 1, 0) * size;

        ushort baseIndex = (ushort)vertices.Count;

        vertices.Add(position - right - up);
        vertices.Add(position + right - up);
        vertices.Add(position - right + up);
        vertices.Add(position + right + up);

        texCoords.Add(new Vector2(0, 0));
        texCoords.Add(new Vector2(1, 0));
        texCoords.Add(new Vector2(0, 1));
        texCoords.Add(new Vector2(1, 1));

        Vector3 normal = Vector3.UnitZ;
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        indices.AddRange(new ushort[] { baseIndex, (ushort)(baseIndex + 1), (ushort)(baseIndex + 2) });
        indices.AddRange(new ushort[] { (ushort)(baseIndex + 1), (ushort)(baseIndex + 3), (ushort)(baseIndex + 2) });
    }

    private void UpdateModelTransform()
    {
        Matrix4x4 translation = Matrix4x4.CreateTranslation(position);
        Matrix4x4 rotationX = Matrix4x4.CreateRotationX(rotation.X);
        Matrix4x4 rotationY = Matrix4x4.CreateRotationY(rotation.Y);
        Matrix4x4 rotationZ = Matrix4x4.CreateRotationZ(rotation.Z);

        Matrix4x4 transform = rotationX * rotationY * rotationZ * translation;

        treeModel.transform = transform;
    }

    public void SetPosition(float x, float y, float z)
    {
        position = new Vector3(x, y, z);
        UpdateModelTransform();
    }

    public void SetRotation(float x, float y, float z)
    {
        rotation = new Vector3(x, y, z);
        UpdateModelTransform();
    }

    public float GetPositionX() => position.X;
    public float GetPositionY() => position.Y;
    public float GetPositionZ() => position.Z;

    public float GetRotationX() => rotation.X;
    public float GetRotationY() => rotation.Y;
    public float GetRotationZ() => rotation.Z;

    public float GetHeight() => treeHeight;

    public void Unload()
    {
        Raylib.UnloadModel(treeModel);
    }
}