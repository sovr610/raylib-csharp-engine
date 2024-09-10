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

    // Physics properties
    public Vector3 Velocity { get; set; }
    public float Mass { get; set; }
    public bool IsStatic { get; set; }
    public float Restitution { get; set; }
    public float FrictionCoefficient { get; set; }
    public float DragCoefficient { get; set; }

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

        // Initialize physics properties
        Velocity = new Vector3(0, 0, 0);
        Mass = 1.0f;
        IsStatic = false;
        Restitution = 0.5f;
        FrictionCoefficient = 0.3f;
        DragCoefficient = 0.1f;
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

    private Vector3 gravity = new Vector3(0, -9.81f, 0);
    private float timeStep = 1.0f / 60.0f;
    private float airDensity = 1.225f; // kg/m^3, approximate air density at sea level

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

    public void UpdatePhysics()
    {
        foreach (var obj in objects.Values)
        {
            if (!obj.IsStatic)
            {
                // Apply gravity
                Vector3 gravityForce = Vector3.Multiply(gravity, obj.Mass);

                // Apply air resistance
                Vector3 dragForce = CalculateDragForce(obj);

                // Apply friction (we'll assume friction with a horizontal plane for simplicity)
                Vector3 frictionForce = CalculateFrictionForce(obj, gravityForce);

                // Calculate net force
                Vector3 netForce = Vector3.Add(gravityForce, Vector3.Add(dragForce, frictionForce));

                // Update velocity (F = ma -> a = F/m)
                Vector3 acceleration = Vector3.Divide(netForce, obj.Mass);
                obj.Velocity = Vector3.Add(obj.Velocity, Vector3.Multiply(acceleration, timeStep));

                // Update position
                Vector3 displacement = Vector3.Multiply(obj.Velocity, timeStep);
                obj.Position = Vector3.Add(obj.Position, displacement);

                // Update bounding box
                obj.UpdateBoundingBox();

                // Check for collisions and resolve them
                ResolveCollisions(obj);
            }
        }
    }

    private Vector3 CalculateDragForce(GameObject obj)
    {
        float dragMagnitude = 0.5f * airDensity * obj.DragCoefficient * obj.Size.X * obj.Size.Y * Vector3.LengthSquared(obj.Velocity);
        return Vector3.Multiply(Vector3.Normalize(Vector3.Negate(obj.Velocity)), dragMagnitude);
    }

    private Vector3 CalculateFrictionForce(GameObject obj, Vector3 normalForce)
    {
        Vector3 frictionDirection = Vector3.Negate(new Vector3(obj.Velocity.X, 0, obj.Velocity.Z));
        if (Vector3.LengthSquared(frictionDirection) < float.Epsilon)
            return new Vector3(0, 0, 0);

        frictionDirection = Vector3.Normalize(frictionDirection);
        float frictionMagnitude = obj.FrictionCoefficient * Vector3.Length(normalForce);
        return Vector3.Multiply(frictionDirection, frictionMagnitude);
    }

    private void ResolveCollisions(GameObject obj)
    {
        foreach (var otherObj in objects.Values)
        {
            if (obj.Id != otherObj.Id && CheckCollision(obj.Id, otherObj.Id))
            {
                // Simple collision response
                Vector3 normal = Vector3.Normalize(Vector3.Subtract(obj.Position, otherObj.Position));
                float relativeVelocity = Vector3.Dot(obj.Velocity, normal) - Vector3.Dot(otherObj.Velocity, normal);

                if (relativeVelocity > 0)
                    continue;

                float e = Math.Min(obj.Restitution, otherObj.Restitution);
                float j = -(1 + e) * relativeVelocity;
                j /= 1 / obj.Mass + 1 / otherObj.Mass;

                Vector3 impulse = Vector3.Multiply(normal, j);

                if (!obj.IsStatic)
                    obj.Velocity = Vector3.Add(obj.Velocity, Vector3.Multiply(impulse, 1 / obj.Mass));

                if (!otherObj.IsStatic)
                    otherObj.Velocity = Vector3.Subtract(otherObj.Velocity, Vector3.Multiply(impulse, 1 / otherObj.Mass));

                // Separate objects to prevent sticking
                float penetrationDepth = obj.BoundingBox.min.Y - otherObj.BoundingBox.max.Y;
                Vector3 separation = Vector3.Multiply(normal, penetrationDepth / 2);
                if (!obj.IsStatic)
                    obj.Position = Vector3.Add(obj.Position, separation);
                if (!otherObj.IsStatic)
                    obj.Position = Vector3.Subtract(otherObj.Position, separation);

                obj.UpdateBoundingBox();
                otherObj.UpdateBoundingBox();
            }
        }
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

    public void SetGravity(Vector3 newGravity)
    {
        gravity = newGravity;
    }

    public Vector3 GetGravity()
    {
        return gravity;
    }

    public float GetGravityIntensity()
    {
        return Vector3.Length(gravity);
    }

    public void SetGravityIntensity(float intensity)
    {
        if (Vector3.LengthSquared(gravity) > 0)
        {
            gravity = Vector3.Multiply(Vector3.Normalize(gravity), intensity);
        }
        else
        {
            // If gravity was (0, 0, 0), default to downward gravity
            gravity = new Vector3(0, -intensity, 0);
        }
    }

    public void SetTimeStep(float newTimeStep)
    {
        timeStep = newTimeStep;
    }

    public void SetAirDensity(float density)
    {
        airDensity = density;
    }

    public void SetObjectVelocity(int id, Vector3 velocity)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.Velocity = velocity;
        }
    }

    public void SetObjectMass(int id, float mass)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.Mass = mass;
        }
    }

    public void SetObjectStatic(int id, bool isStatic)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.IsStatic = isStatic;
        }
    }

    public void SetObjectRestitution(int id, float restitution)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.Restitution = restitution;
        }
    }

    public void SetObjectFrictionCoefficient(int id, float frictionCoefficient)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.FrictionCoefficient = frictionCoefficient;
        }
    }

    public void SetObjectDragCoefficient(int id, float dragCoefficient)
    {
        if (objects.TryGetValue(id, out GameObject obj))
        {
            obj.DragCoefficient = dragCoefficient;
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

            // Visualize velocities
            if (!obj.IsStatic)
            {
                Vector3 endPoint = Vector3.Add(obj.Position, Vector3.Multiply(obj.Velocity, 0.5f));
                Raylib.DrawLine3D(obj.Position, endPoint, Color.RED);
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