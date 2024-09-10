using System;
using System.Collections.Generic;
using Raylib_cs;

public enum ObjectType
{
    Box,
    Sphere,
    Cylinder,
    Plane,
    Model
}

public class GameObject
{
    public int Id { get; set; }
    public ObjectType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Size { get; set; }
    public float Radius { get; set; }
    public float Height { get; set; }
    public Color Color { get; set; }
    public Model Model { get; set; }
    public Material Material { get; set; }
    public BoundingBox BoundingBox { get; set; }
    public ModelAnimation[] Animations { get; set; }
    public int CurrentAnimation { get; set; }
    public bool IsAnimationPlaying { get; set; }
    public int AnimationFrame { get; set; }
    public bool IsWireframe { get; set; }
    public int ShaderId { get; set; }

    public GameObject(int id, ObjectType type)
    {
        Id = id;
        Type = type;
        Position = new Vector3(0, 0, 0);
        Size = new Vector3(1, 1, 1);
        Radius = 1;
        Height = 1;
        Color = Color.WHITE;
        Material = Raylib.LoadMaterialDefault();
        CurrentAnimation = -1;
        IsAnimationPlaying = false;
        AnimationFrame = 0;
        IsWireframe = false;
        ShaderId = -1;
    }

    public void UpdateBoundingBox()
    {
        switch (Type)
        {
            case ObjectType.Box:
            case ObjectType.Cylinder:
            case ObjectType.Plane:
                BoundingBox = new BoundingBox(
                    new Vector3(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Position.Z - Size.Z / 2),
                    new Vector3(Position.X + Size.X / 2, Position.Y + Size.Y / 2, Position.Z + Size.Z / 2)
                );
                break;
            case ObjectType.Sphere:
                BoundingBox = new BoundingBox(
                    new Vector3(Position.X - Radius, Position.Y - Radius, Position.Z - Radius),
                    new Vector3(Position.X + Radius, Position.Y + Radius, Position.Z + Radius)
                );
                break;
            case ObjectType.Model:
                BoundingBox = Raylib.GetModelBoundingBox(Model);
                BoundingBox.min = Vector3.Add(BoundingBox.min, Position);
                BoundingBox.max = Vector3.Add(BoundingBox.max, Position);
                break;
        }
    }
}

public class ShaderInfo
{
    public int Id { get; set; }
    public Shader Shader { get; set; }
    public Dictionary<string, int> UniformLocations { get; set; }

    public ShaderInfo(int id, Shader shader)
    {
        Id = id;
        Shader = shader;
        UniformLocations = new Dictionary<string, int>();
    }
}

public class ObjectRegistry
{
    private Dictionary<int, GameObject> objects;
    private Dictionary<int, ShaderInfo> shaders;
    private int nextObjectId;
    private int nextShaderId;

    public ObjectRegistry()
    {
        objects = new Dictionary<int, GameObject>();
        shaders = new Dictionary<int, ShaderInfo>();
        nextObjectId = 1;
        nextShaderId = 1;
    }

    public int AddBox(Vector3 size, Vector3 position, Color color)
    {
        int id = nextObjectId++;
        GameObject obj = new GameObject(id, ObjectType.Box)
        {
            Size = size,
            Position = position,
            Color = color
        };
        obj.UpdateBoundingBox();
        objects[id] = obj;
        return id;
    }

    public int AddSphere(float radius, Vector3 position, Color color)
    {
        int id = nextObjectId++;
        GameObject obj = new GameObject(id, ObjectType.Sphere)
        {
            Radius = radius,
            Position = position,
            Color = color
        };
        obj.UpdateBoundingBox();
        objects[id] = obj;
        return id;
    }

    public int AddCylinder(float radius, float height, Vector3 position, Color color)
    {
        int id = nextObjectId++;
        GameObject obj = new GameObject(id, ObjectType.Cylinder)
        {
            Radius = radius,
            Height = height,
            Position = position,
            Color = color
        };
        obj.UpdateBoundingBox();
        objects[id] = obj;
        return id;
    }

    public int AddPlane(Vector2 size, Vector3 position, Color color)
    {
        int id = nextObjectId++;
        GameObject obj = new GameObject(id, ObjectType.Plane)
        {
            Size = new Vector3(size.X, 1, size.Y),
            Position = position,
            Color = color
        };
        obj.UpdateBoundingBox();
        objects[id] = obj;
        return id;
    }

    public int AddModel(string modelPath, string materialPath = null)
    {
        int id = nextObjectId++;
        GameObject obj = new GameObject(id, ObjectType.Model);
        obj.Model = Raylib.LoadModel(modelPath);
        
        if (!string.IsNullOrEmpty(materialPath))
        {
            Material material = Raylib.LoadMaterialDefault();
            Raylib.SetMaterialTexture(ref material, MaterialMapIndex.MATERIAL_MAP_DIFFUSE, Raylib.LoadTexture(materialPath));
            obj.Model.materials[0] = material;
        }
        
        obj.UpdateBoundingBox();
        objects[id] = obj;
        return id;
    }

    public int AddAnimatedModel(string modelPath, string materialPath = null)
    {
        int id = AddModel(modelPath, materialPath);
        if (objects.TryGetValue(id, out GameObject obj))
        {
            uint animsCount = 0;
            obj.Animations = Raylib.LoadModelAnimations(modelPath, ref animsCount);
        }
        return id;
    }

    public void UpdateObjectPosition(int id, Vector3 position)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.Position = position;
            obj.UpdateBoundingBox();
        }
    }

    public void UpdateObjectColor(int id, Color color)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.Color = color;
        }
    }

    public void DeleteObject(int id)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            if (obj.Type == ObjectType.Model)
            {
                Raylib.UnloadModel(obj.Model);
            }
            objects.Remove(id);
        }
    }

    public Vector3 GetObjectPosition(int id)
    {
        return objects.TryGetValue(id, out GameObject obj) ? obj.Position : new Vector3(0, 0, 0);
    }

    public void SetObjectPosition(int id, Vector3 position)
    {
        UpdateObjectPosition(id, position);
    }

    public bool IsColliding(int objectAId, int objectBId)
    {
        if (!objects.TryGetValue(objectAId, out GameObject objectA) || 
            !objects.TryGetValue(objectBId, out GameObject objectB))
        {
            return false;
        }

        return CheckCollision(objectAId, objectBId);
    }

    public bool CheckCollision(int id1, int id2)
    {
        if (!objects.TryGetValue(id1, out GameObject obj1) || !objects.TryGetValue(id2, out GameObject obj2))
        {
            return false;
        }

        if (obj1.Type == ObjectType.Sphere && obj2.Type == ObjectType.Sphere)
        {
            float distance = Vector3.Distance(obj1.Position, obj2.Position);
            return distance < (obj1.Radius + obj2.Radius);
        }

        return Raylib.CheckCollisionBoxes(obj1.BoundingBox, obj2.BoundingBox);
    }

    public List<int> GetCollidingObjects(int id)
    {
        List<int> collidingObjects = new List<int>();

        if (objects.TryGetValue(id, out GameObject obj))
        {
            foreach (var otherObj in objects.Values)
            {
                if (otherObj.Id != id && CheckCollision(id, otherObj.Id))
                {
                    collidingObjects.Add(otherObj.Id);
                }
            }
        }

        return collidingObjects;
    }

    public Dictionary<int, List<int>> GetAllCollisions()
    {
        Dictionary<int, List<int>> allCollisions = new Dictionary<int, List<int>>();

        foreach (var obj in objects.Values)
        {
            allCollisions[obj.Id] = GetCollidingObjects(obj.Id);
        }

        return allCollisions;
    }

    public void StartAnimation(int id, int animationIndex)
    {
        if (objects.TryGetValue(id, out GameObject obj) && obj.Type == ObjectType.Model)
        {
            if (animationIndex >= 0 && animationIndex < obj.Animations.Length)
            {
                obj.CurrentAnimation = animationIndex;
                obj.IsAnimationPlaying = true;
                obj.AnimationFrame = 0;
            }
        }
    }

    public void PauseAnimation(int id)
    {
        if (objects.TryGetValue(id, out GameObject obj) && obj.Type == ObjectType.Model)
        {
            obj.IsAnimationPlaying = false;
        }
    }

    public void ResumeAnimation(int id)
    {
        if (objects.TryGetValue(id, out GameObject obj) && obj.Type == ObjectType.Model)
        {
            obj.IsAnimationPlaying = true;
        }
    }

    public void StopAnimation(int id)
    {
        if (objects.TryGetValue(id, out GameObject obj) && obj.Type == ObjectType.Model)
        {
            obj.CurrentAnimation = -1;
            obj.IsAnimationPlaying = false;
            obj.AnimationFrame = 0;
        }
    }

    public void SetAnimationFrame(int id, int frame)
    {
        if (objects.TryGetValue(id, out GameObject obj) && obj.Type == ObjectType.Model)
        {
            if (obj.CurrentAnimation != -1 && frame >= 0 && frame < obj.Animations[obj.CurrentAnimation].frameCount)
            {
                obj.AnimationFrame = frame;
                UpdateModelAnimation(obj.Model, obj.Animations[obj.CurrentAnimation], obj.AnimationFrame);
            }
        }
    }

    public void UpdateAnimations()
    {
        foreach (var obj in objects.Values)
        {
            if (obj.Type == ObjectType.Model && obj.IsAnimationPlaying && obj.CurrentAnimation != -1)
            {
                obj.AnimationFrame++;
                if (obj.AnimationFrame >= obj.Animations[obj.CurrentAnimation].frameCount)
                {
                    obj.AnimationFrame = 0;
                }
                UpdateModelAnimation(obj.Model, obj.Animations[obj.CurrentAnimation], obj.AnimationFrame);
            }
        }
    }

    public void SetWireframe(int id, bool isWireframe)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.IsWireframe = isWireframe;
        }
    }

    public int GetTriangleCount(int id)
    {
        if (objects.TryGetValue(id, out GameObject obj) && obj.Type == ObjectType.Model)
        {
            int triangleCount = 0;
            for (int i = 0; i < obj.Model.meshCount; i++)
            {
                triangleCount += obj.Model.meshes[i].triangleCount;
            }
            return triangleCount;
        }
        return 0;
    }

    public int AddShader(string vertexShaderPath, string fragmentShaderPath)
    {
        Shader shader = Raylib.LoadShader(vertexShaderPath, fragmentShaderPath);
        int shaderId = nextShaderId++;
        shaders[shaderId] = new ShaderInfo(shaderId, shader);
        return shaderId;
    }

    public void ApplyShader(int objectId, int shaderId)
    {
        if (objects.TryGetValue(objectId, out GameObject obj) && shaders.TryGetValue(shaderId, out ShaderInfo shaderInfo))
        {
            obj.ShaderId = shaderId;
            if (obj.Type == ObjectType.Model)
            {
                obj.Model.materials[0].shader = shaderInfo.Shader;
            }
        }
    }

    public void UpdateShader(int shaderId, params (string name, ShaderUniformDataType type, object value)[] uniforms)
    {
        if (shaders.TryGetValue(shaderId, out ShaderInfo shaderInfo))
        {
            Raylib.BeginShaderMode(shaderInfo.Shader);
            foreach (var (name, type, value) in uniforms)
            {
                if (!shaderInfo.UniformLocations.TryGetValue(name, out int location))
                {
                    location = Raylib.GetShaderLocation(shaderInfo.Shader, name);
                    shaderInfo.UniformLocations[name] = location;
                }

                switch (type)
                {
                    case ShaderUniformDataType.SHADER_UNIFORM_FLOAT:
                        Raylib.SetShaderValue(shaderInfo.Shader, location, (float)value, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
                        break;
                    case ShaderUniformDataType.SHADER_UNIFORM_VEC2:
                        Raylib.SetShaderValue(shaderInfo.Shader, location, (Vector2)value, ShaderUniformDataType.SHADER_UNIFORM_VEC2);
                        break;
                    case ShaderUniformDataType.SHADER_UNIFORM_VEC3:
                        Raylib.SetShaderValue(shaderInfo.Shader, location, (Vector3)value, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
                        break;
                    case ShaderUniformDataType.SHADER_UNIFORM_VEC4:
                        Raylib.SetShaderValue(shaderInfo.Shader, location, (Vector4)value, ShaderUniformDataType.SHADER_UNIFORM_VEC4);
                        break;
                }
            }
            Raylib.EndShaderMode();
        }
    }

    public void Draw()
    {
        foreach (var obj in objects.Values)
        {
            if (obj.ShaderId != -1 && shaders.TryGetValue(obj.ShaderId, out ShaderInfo shaderInfo))
            {
                Raylib.BeginShaderMode(shaderInfo.Shader);
            }

            if (obj.IsWireframe)
            {
                Raylib.DrawGrid(10, 1.0f);
            }

            switch (obj.Type)
            {
                case ObjectType.Box:
                    if (obj.IsWireframe)
                        Raylib.DrawCubeWires(obj.Position, obj.Size.X, obj.Size.Y, obj.Size.Z, obj.Color);
                    else
                        Raylib.DrawCubeV(obj.Position, obj.Size, obj.Color);
                    break;
                case ObjectType.Sphere:
                    if (obj.IsWireframe)
                        Raylib.DrawSphereWires(obj.Position, obj.Radius, 16, 16, obj.Color);
                    else
                        Raylib.DrawSphere(obj.Position, obj.Radius, obj.Color);
                    break;
                case ObjectType.Cylinder:
                    if (obj.IsWireframe)
                        Raylib.DrawCylinderWires(obj.Position, obj.Radius, obj.Radius, obj.Height, 16, obj.Color);
                    else
                        Raylib.DrawCylinder(obj.Position, obj.Radius, obj.Radius, obj.Height, 16, obj.Color);
                    break;
                case ObjectType.Plane:
                    if (obj.IsWireframe)
                        Raylib.DrawGrid(10, 1.0f);
                    else
                        Raylib.DrawPlane(obj.Position, new Vector2(obj.Size.X, obj.Size.Z), obj.Color);
                    break;
                case ObjectType.Model:
                    if (obj.IsWireframe)
                        Raylib.DrawModelWires(obj.Model, obj.Position, 1.0f, obj.Color);
                    else
                        Raylib.DrawModelEx(obj.Model, obj.Position, new Vector3(0, 1, 0), 0, new Vector3(1, 1, 1), obj.Color);
                    break;
            }

            if (obj.ShaderId != -1)
            {
                Raylib.EndShaderMode();
            }
        }
    }

    public void Unload()
    {
        foreach (var obj in objects.Values)
        {
            if (obj.Type == ObjectType.Model)
            {
                Raylib.UnloadModel(obj.Model);
                if (obj.Animations != null)
                {
                    for (int i = 0; i < obj.Animations.Length; i++)
                    {
                        Raylib.UnloadModelAnimation(obj.Animations[i]);
                    }
                }
            }
            Raylib.UnloadMaterial(obj.Material);
        }
        foreach (var shaderInfo in shaders.Values)
        {
            Raylib.UnloadShader(shaderInfo.Shader);
        }
        objects.Clear();
        shaders.Clear();
    }
}