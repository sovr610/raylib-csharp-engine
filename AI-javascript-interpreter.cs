using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.IO;
using Jint;
using Jint.Native;

public class JavaScriptAIInterpreter
{
    private AIEntityManager entityManager;
    private DialogueSystem dialogueSystem;
    private ObjectRegistry objectRegistry;
    private Engine jsEngine;
    private Entity currentEntity;

    public JavaScriptAIInterpreter(AIEntityManager entityManager, DialogueSystem dialogueSystem, ObjectRegistry objectRegistry)
    {
        this.entityManager = entityManager;
        this.dialogueSystem = dialogueSystem;
        this.objectRegistry = objectRegistry;
        InitializeJavaScriptEngine();
    }

    private void InitializeJavaScriptEngine()
    {
        jsEngine = new Engine(cfg => cfg.AllowClr(typeof(Vector3).Assembly));

        // Entity properties and basic actions
        jsEngine.SetValue("getPosition", new Func<Vector3>(() => entityManager.GetEntityPosition(currentEntity)));
        jsEngine.SetValue("getHealth", new Func<float>(() => currentEntity.Health));
        jsEngine.SetValue("getMaxHealth", new Func<float>(() => currentEntity.MaxHealth));
        jsEngine.SetValue("getDamage", new Func<float>(() => currentEntity.Damage));
        jsEngine.SetValue("getSpeed", new Func<float>(() => currentEntity.Speed));
        jsEngine.SetValue("getState", new Func<string>(() => currentEntity.State.ToString()));
        jsEngine.SetValue("getBehavior", new Func<string>(() => currentEntity.Behavior.ToString()));
        jsEngine.SetValue("getObjectId", new Func<int>(() => currentEntity.ObjectId));
        jsEngine.SetValue("getVelocity", new Func<Vector3>(() => entityManager.GetEntityVelocity(currentEntity.Id)));

        // Movement and pathfinding
        jsEngine.SetValue("setWaypoint", new Action<float, float, float>((x, y, z) => 
            entityManager.SetEntityWaypoints(currentEntity.Id, new List<Vector3> { new Vector3(x, y, z) })));
        jsEngine.SetValue("addWaypoint", new Action<float, float, float>((x, y, z) => 
            entityManager.AddEntityWaypoint(currentEntity.Id, new Vector3(x, y, z))));
        jsEngine.SetValue("clearWaypoints", new Action(() => 
            entityManager.ClearEntityWaypoints(currentEntity.Id)));
        jsEngine.SetValue("moveTowards", new Action<float, float, float>((x, y, z) => 
            entityManager.MoveEntityTowards(currentEntity.Id, new Vector3(x, y, z))));
        jsEngine.SetValue("setMaxSpeed", new Action<float>(speed => 
            entityManager.SetEntityMaxSpeed(currentEntity.Id, speed)));
        jsEngine.SetValue("setMaxForce", new Action<float>(force => 
            entityManager.SetEntityMaxForce(currentEntity.Id, force)));

        // Combat and interaction
        jsEngine.SetValue("attack", new Action<int>(targetId => 
            entityManager.EntityAttack(currentEntity.Id, targetId)));
        jsEngine.SetValue("heal", new Action<float>(amount => 
            currentEntity.Heal(amount)));
        jsEngine.SetValue("setState", new Action<string>(state => 
            currentEntity.State = (EntityState)Enum.Parse(typeof(EntityState), state)));
        jsEngine.SetValue("findNearestEntity", new Func<string, int>(behavior => 
            entityManager.FindNearestEntity(currentEntity.Id, (EntityBehavior)Enum.Parse(typeof(EntityBehavior), behavior))));
        jsEngine.SetValue("getDistanceTo", new Func<int, float>(targetId => 
            entityManager.GetDistanceBetweenEntities(currentEntity.Id, targetId)));

        // Dialogue management
        jsEngine.SetValue("createDialogue", new Func<string, int>(name => dialogueSystem.CreateDialogue(name)));
        jsEngine.SetValue("addDialogueNode", new Action<int, int, string>((dialogueId, nodeId, text) => 
            dialogueSystem.AddDialogueNode(dialogueId, nodeId, text)));
        jsEngine.SetValue("addDialogueOption", new Action<int, int, string, int>((dialogueId, nodeId, optionText, nextNodeId) => 
            dialogueSystem.AddDialogueOption(dialogueId, nodeId, optionText, nextNodeId)));
        jsEngine.SetValue("removeDialogueOption", new Action<int, int, int>((dialogueId, nodeId, optionIndex) => 
            dialogueSystem.RemoveDialogueOption(dialogueId, nodeId, optionIndex)));
        jsEngine.SetValue("updateDialogueNodeText", new Action<int, int, string>((dialogueId, nodeId, newText) => 
            dialogueSystem.UpdateDialogueNodeText(dialogueId, nodeId, newText)));
        jsEngine.SetValue("updateDialogueOptionText", new Action<int, int, int, string>((dialogueId, nodeId, optionIndex, newText) => 
            dialogueSystem.UpdateDialogueOptionText(dialogueId, nodeId, optionIndex, newText)));
        jsEngine.SetValue("removeDialogue", new Action<int>(dialogueId => dialogueSystem.RemoveDialogue(dialogueId)));
        jsEngine.SetValue("associateDialogue", new Action<int>((dialogueId) => 
            entityManager.AssociateDialogueWithEntity(currentEntity.Id, dialogueId)));
        jsEngine.SetValue("removeAssociatedDialogue", new Action(() => 
            entityManager.RemoveDialogueFromEntity(currentEntity.Id)));
        jsEngine.SetValue("getAssociatedDialogueId", new Func<int>(() => 
            entityManager.GetEntityDialogueId(currentEntity.Id)));
        jsEngine.SetValue("triggerDialogue", new Action<int>((startNodeId) => 
            entityManager.TriggerEntityDialogue(currentEntity.Id, startNodeId)));
        jsEngine.SetValue("cloneDialogue", new Func<int, int>((sourceDialogueId) => 
            dialogueSystem.CloneDialogue(sourceDialogueId)));

        // World information
        jsEngine.SetValue("getPlayerPosition", new Func<Vector3>(() => GetPlayerPosition()));
        jsEngine.SetValue("getNearbyObjects", new Func<float, JsValue>(radius => GetNearbyObjects(radius)));
        jsEngine.SetValue("getNearbyEntities", new Func<float, JsValue>(radius => GetNearbyEntities(radius)));
        jsEngine.SetValue("getEntityInfo", new Func<int, JsValue>(entityId => GetEntityInfo(entityId)));
        jsEngine.SetValue("isLineOfSightClear", new Func<Vector3, bool>(target => IsLineOfSightClear(target)));
        jsEngine.SetValue("getCurrentTime", new Func<float>(() => GetCurrentTime()));
        jsEngine.SetValue("getTerrainHeight", new Func<float, float, float>((x, z) => GetTerrainHeight(x, z)));

        // Utility
        jsEngine.SetValue("log", new Action<object>(message => 
            Console.WriteLine($"Entity {currentEntity.Id}: {message}")));
        
        // Vector3 operations
        jsEngine.SetValue("Vector3", typeof(Vector3));
        jsEngine.Execute(@"
            function createVector3(x, y, z) {
                return new Vector3(x, y, z);
            }
            function addVectors(v1, v2) {
                return Vector3.Add(v1, v2);
            }
            function subtractVectors(v1, v2) {
                return Vector3.Subtract(v1, v2);
            }
            function multiplyVector(v, scalar) {
                return Vector3.Multiply(v, scalar);
            }
            function normalizeVector(v) {
                return Vector3.Normalize(v);
            }
            function distanceBetweenVectors(v1, v2) {
                return Vector3.Distance(v1, v2);
            }
        ");
    }

    public void LoadAndExecuteScript(string scriptName, Entity entity)
    {
        currentEntity = entity;
        string scriptPath = Path.Combine("scripts", scriptName + ".js");
        
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine($"Script file not found: {scriptPath}");
            return;
        }

        string script = File.ReadAllText(scriptPath);

        try
        {
            jsEngine.Execute(script);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading script for entity {entity.Id}: {ex.Message}");
        }
    }

    public void CallInitFunction()
    {
        CallFunction("init");
    }

    public void CallMainFunction()
    {
        CallFunction("main");
    }

    public void CallEndFunction()
    {
        CallFunction("end");
    }

    private void CallFunction(string functionName)
    {
        try
        {
            if (jsEngine.GetValue(functionName).Is<Jint.Native.Function.FunctionInstance>())
            {
                jsEngine.Invoke(functionName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling {functionName} function for entity {currentEntity.Id}: {ex.Message}");
        }
    }

    private Vector3 GetPlayerPosition()
    {
        Entity playerEntity = entityManager.GetPlayerEntity();
        return playerEntity != null ? entityManager.GetEntityPosition(playerEntity) : Vector3.Zero;
    }

    private JsValue GetNearbyObjects(float radius)
    {
        Vector3 entityPosition = entityManager.GetEntityPosition(currentEntity);
        var nearbyObjects = objectRegistry.GetObjectsInRadius(entityPosition, radius);
        
        return JsValue.FromObject(jsEngine, nearbyObjects.Select(obj => new
        {
            id = obj.Id,
            type = obj.Type.ToString(),
            position = new { x = obj.Position.X, y = obj.Position.Y, z = obj.Position.Z }
        }).ToArray());
    }

    private JsValue GetNearbyEntities(float radius)
    {
        Vector3 entityPosition = entityManager.GetEntityPosition(currentEntity);
        var nearbyEntities = entityManager.GetEntitiesInRadius(entityPosition, radius);
        
        return JsValue.FromObject(jsEngine, nearbyEntities.Select(entity => new
        {
            id = entity.Id,
            name = entity.Name,
            behavior = entity.Behavior.ToString(),
            position = new { x = entity.Position.X, y = entity.Position.Y, z = entity.Position.Z },
            health = entity.Health,
            maxHealth = entity.MaxHealth
        }).ToArray());
    }

    private JsValue GetEntityInfo(int entityId)
    {
        Entity entity = entityManager.GetEntity(entityId);
        if (entity != null)
        {
            return JsValue.FromObject(jsEngine, new
            {
                id = entity.Id,
                name = entity.Name,
                behavior = entity.Behavior.ToString(),
                position = new { x = entity.Position.X, y = entity.Position.Y, z = entity.Position.Z },
                health = entity.Health,
                maxHealth = entity.MaxHealth,
                damage = entity.Damage,
                speed = entity.Speed,
                state = entity.State.ToString()
            });
        }
        return JsValue.Null;
    }

    private bool IsLineOfSightClear(Vector3 target)
    {
        Vector3 start = entityManager.GetEntityPosition(currentEntity);
        // Implement ray casting logic here
        // For simplicity, we'll just return true. In a real implementation, you'd check for obstacles.
        return true;
    }

    private float GetCurrentTime()
    {
        // Implement your game's time system here
        // For this example, we'll just return the current real-time seconds
        return (float)DateTime.Now.TimeOfDay.TotalSeconds;
    }

    private float GetTerrainHeight(float x, float z)
    {
        // Implement your terrain height lookup here
        // For this example, we'll just return 0
        return 0f;
    }
}