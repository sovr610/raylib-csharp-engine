using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class AIEntityManager
{
    private List<Entity> entities;
    private Dictionary<int, JavaScriptAIInterpreter> entityInterpreters;
    private ObjectRegistry objectRegistry;
    private Pathfinder pathfinder;
    private SteeringBehavior steeringBehavior;
    private DialogueSystem dialogueSystem;
    private Dictionary<int, int> entityDialogues; // Maps entity IDs to dialogue IDs

    public AIEntityManager(ObjectRegistry objectRegistry, Vector3 worldSize, float nodeRadius, DialogueSystem dialogueSystem)
    {
        entities = new List<Entity>();
        entityInterpreters = new Dictionary<int, JavaScriptAIInterpreter>();
        this.objectRegistry = objectRegistry;
        pathfinder = new Pathfinder(worldSize, nodeRadius);
        steeringBehavior = new SteeringBehavior();
        this.dialogueSystem = dialogueSystem;
        entityDialogues = new Dictionary<int, int>();
    }

    public int AddEntity(string name, EntityBehavior behavior, int objectId, float health, float damage, float speed, float detectionRadius, string scriptName)
    {
        int id = entities.Count;
        Entity entity = new Entity(id, name, behavior, objectId, health, damage, speed, detectionRadius);
        entities.Add(entity);

        GameObject obj = objectRegistry.GetObject(objectId);
        if (obj != null)
        {
            entity.HomePosition = obj.Position;
        }

        if (!string.IsNullOrEmpty(scriptName))
        {
            JavaScriptAIInterpreter interpreter = new JavaScriptAIInterpreter(this, dialogueSystem, objectRegistry);
            interpreter.LoadAndExecuteScript(scriptName, entity);
            interpreter.CallInitFunction();
            entityInterpreters[id] = interpreter;
        }

        return id;
    }

    public void RemoveEntity(int id)
    {
        if (entityInterpreters.TryGetValue(id, out JavaScriptAIInterpreter interpreter))
        {
            interpreter.CallEndFunction();
            entityInterpreters.Remove(id);
        }
        entities.RemoveAll(e => e.Id == id);
        entityDialogues.Remove(id);
    }

    public void Update(float deltaTime)
    {
        foreach (var entity in entities)
        {
            if (entity.State == EntityState.Dead)
                continue;

            UpdateEffects(entity, deltaTime);

            if (entityInterpreters.TryGetValue(entity.Id, out JavaScriptAIInterpreter interpreter))
            {
                interpreter.CallMainFunction();
            }
            else
            {
                UpdateDefaultBehavior(entity, deltaTime);
            }

            if (entity.State == EntityState.Moving)
            {
                MoveEntityWithAvoidance(entity, deltaTime);
            }

            UpdateEntityObject(entity);
        }
    }

    private void UpdateEffects(Entity entity, float deltaTime)
    {
        for (int i = entity.ActiveEffects.Count - 1; i >= 0; i--)
        {
            Effect effect = entity.ActiveEffects[i];
            effect.Duration -= deltaTime;

            if (effect.Duration <= 0)
            {
                entity.ActiveEffects.RemoveAt(i);
            }
            else
            {
                effect.ApplyEffect(entity);
            }
        }
    }

    private void UpdateDefaultBehavior(Entity entity, float deltaTime)
    {
        switch (entity.Behavior)
        {
            case EntityBehavior.Aggressive:
                UpdateAggressiveEntity(entity, deltaTime);
                break;
            case EntityBehavior.Passive:
                UpdatePassiveEntity(entity, deltaTime);
                break;
            case EntityBehavior.Friendly:
                UpdateFriendlyEntity(entity, deltaTime);
                break;
        }
    }

    private void UpdateAggressiveEntity(Entity entity, float deltaTime)
    {
        Entity target = FindNearestTarget(entity, e => e.Behavior != EntityBehavior.Aggressive);

        if (target != null)
        {
            float distance = Vector3.Distance(GetEntityPosition(entity), GetEntityPosition(target));

            if (distance <= entity.DetectionRadius)
            {
                if (distance <= 2f)
                {
                    entity.State = EntityState.Attacking;
                    AttackTarget(entity, target);
                }
                else
                {
                    entity.State = EntityState.Moving;
                    MoveEntityTowards(entity.Id, GetEntityPosition(target));
                }
            }
            else
            {
                WanderOrFollowWaypoints(entity);
            }
        }
        else
        {
            WanderOrFollowWaypoints(entity);
        }
    }

    private void UpdatePassiveEntity(Entity entity, float deltaTime)
    {
        Entity threat = FindNearestTarget(entity, e => e.Behavior == EntityBehavior.Aggressive);

        if (threat != null && Vector3.Distance(GetEntityPosition(entity), GetEntityPosition(threat)) <= entity.DetectionRadius)
        {
            entity.State = EntityState.Fleeing;
            Vector3 fleeDirection = Vector3.Normalize(GetEntityPosition(entity) - GetEntityPosition(threat));
            MoveEntityTowards(entity.Id, GetEntityPosition(entity) + fleeDirection * 10f);
        }
        else
        {
            WanderOrFollowWaypoints(entity);
        }
    }

    private void UpdateFriendlyEntity(Entity entity, float deltaTime)
    {
        Entity playerEntity = GetPlayerEntity();

        if (playerEntity != null && Vector3.Distance(GetEntityPosition(entity), GetEntityPosition(playerEntity)) <= entity.DetectionRadius)
        {
            entity.State = EntityState.Moving;
            MoveEntityTowards(entity.Id, GetEntityPosition(playerEntity));
        }
        else
        {
            WanderOrFollowWaypoints(entity);
        }
    }

    private void WanderOrFollowWaypoints(Entity entity)
    {
        if (entity.Waypoints.Count > 0)
        {
            FollowWaypoints(entity);
        }
        else
        {
            Wander(entity);
        }
    }

    private void Wander(Entity entity)
    {
        if (entity.State != EntityState.Moving || Vector3.Distance(GetEntityPosition(entity), entity.HomePosition) > entity.WanderRadius)
        {
            entity.State = EntityState.Moving;
            float angle = (float)new Random().NextDouble() * MathF.PI * 2;
            float radius = (float)new Random().NextDouble() * entity.WanderRadius;
            Vector3 offset = new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius);
            MoveEntityTowards(entity.Id, entity.HomePosition + offset);
        }
    }

    private void FollowWaypoints(Entity entity)
    {
        if (entity.State != EntityState.Moving || Vector3.Distance(GetEntityPosition(entity), entity.Waypoints[entity.CurrentWaypointIndex]) < 0.1f)
        {
            entity.CurrentWaypointIndex = (entity.CurrentWaypointIndex + 1) % entity.Waypoints.Count;
            entity.State = EntityState.Moving;
            MoveEntityTowards(entity.Id, entity.Waypoints[entity.CurrentWaypointIndex]);
        }
    }

    private void MoveEntityWithAvoidance(Entity entity, float deltaTime)
    {
        if (entity.CurrentPath != null && entity.CurrentPath.Count > 0)
        {
            Vector3 steeringForce = steeringBehavior.FollowPath(entity.CurrentPath, GetEntityPosition(entity), entity.Velocity, entity.MaxSpeed, entity.MaxForce, 1f);
            entity.Velocity += steeringForce * deltaTime;
            entity.Velocity = Vector3.ClampMagnitude(entity.Velocity, entity.MaxSpeed);

            Vector3 newPosition = GetEntityPosition(entity) + entity.Velocity * deltaTime;
            SetEntityPosition(entity, newPosition);

            if (entity.CurrentPath.Count == 0)
            {
                entity.State = EntityState.Idle;
            }
        }
    }

    private void AttackTarget(Entity attacker, Entity target)
    {
        target.TakeDamage(attacker.Damage);
    }

    private Entity FindNearestTarget(Entity source, Func<Entity, bool> predicate)
    {
        Entity nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in entities)
        {
            if (target != source && predicate(target))
            {
                float distance = Vector3.Distance(GetEntityPosition(source), GetEntityPosition(target));
                if (distance < nearestDistance)
                {
                    nearest = target;
                    nearestDistance = distance;
                }
            }
        }

        return nearest;
    }

    private void UpdateEntityObject(Entity entity)
    {
        GameObject obj = objectRegistry.GetObject(entity.ObjectId);
        if (obj != null)
        {
            obj.Position = GetEntityPosition(entity);
        }
    }

    // Public methods to be called from JavaScript or other parts of the game

    public void SetEntityWaypoints(int entityId, List<Vector3> waypoints)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.Waypoints = new List<Vector3>(waypoints);
            entity.CurrentWaypointIndex = 0;
        }
    }

    public void AddEntityWaypoint(int entityId, Vector3 waypoint)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.Waypoints.Add(waypoint);
        }
    }

    public void ClearEntityWaypoints(int entityId)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.Waypoints.Clear();
        }
    }

    public void MoveEntityTowards(int entityId, Vector3 target)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.State = EntityState.Moving;
            List<Vector3> path = pathfinder.FindPath(GetEntityPosition(entity), target);
            if (path != null && path.Count > 0)
            {
                entity.CurrentPath = path;
            }
        }
    }

    public void EntityAttack(int attackerId, int targetId)
    {
        Entity attacker = GetEntity(attackerId);
        Entity target = GetEntity(targetId);
        if (attacker != null && target != null)
        {
            AttackTarget(attacker, target);
        }
    }

    public int FindNearestEntity(int entityId, EntityBehavior targetBehavior)
    {
        Entity source = GetEntity(entityId);
        if (source == null) return -1;

        Entity nearest = FindNearestTarget(source, e => e.Behavior == targetBehavior);
        return nearest?.Id ?? -1;
    }

    public float GetDistanceBetweenEntities(int entity1Id, int entity2Id)
    {
        Entity entity1 = GetEntity(entity1Id);
        Entity entity2 = GetEntity(entity2Id);
        if (entity1 == null || entity2 == null) return float.MaxValue;

        return Vector3.Distance(GetEntityPosition(entity1), GetEntityPosition(entity2));
    }

    public void SetEntityMaxSpeed(int entityId, float maxSpeed)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.MaxSpeed = maxSpeed;
        }
    }

    public void SetEntityMaxForce(int entityId, float maxForce)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.MaxForce = maxForce;
        }
    }

    public Vector3 GetEntityPosition(Entity entity)
    {
        GameObject obj = objectRegistry.GetObject(entity.ObjectId);
        return obj != null ? obj.Position : Vector3.Zero;
    }

    public void SetEntityPosition(Entity entity, Vector3 position)
    {
        GameObject obj = objectRegistry.GetObject(entity.ObjectId);
        if (obj != null)
        {
            obj.Position = position;
        }
    }

    public Vector3 GetEntityVelocity(int entityId)
    {
        Entity entity = GetEntity(entityId);
        return entity != null ? entity.Velocity : Vector3.Zero;
    }

    public Entity GetEntity(int id)
    {
        return entities.Find(e => e.Id == id);
    }

    public void SetEntityWanderRadius(int entityId, float radius)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.WanderRadius = radius;
        }
    }

    public void ApplyEffectToEntity(int entityId, Effect effect)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            entity.AddEffect(effect);
        }
    }

    public void AssociateDialogueWithEntity(int entityId, int dialogueId)
    {
        if (GetEntity(entityId) != null && dialogueSystem.GetDialogue(dialogueId) != null)
        {
            entityDialogues[entityId] = dialogueId;
        }
    }

    public void RemoveDialogueFromEntity(int entityId)
    {
        entityDialogues.Remove(entityId);
    }

    public int GetEntityDialogueId(int entityId)
    {
        if (entityDialogues.TryGetValue(entityId, out int dialogueId))
        {
            return dialogueId;
        }
        return -1;
    }

    public void TriggerEntityDialogue(int entityId, int startNodeId)
    {
        if (entityDialogues.TryGetValue(entityId, out int dialogueId))
        {
            DialogueNode node = dialogueSystem.GetDialogueNode(dialogueId, startNodeId);
            if (node != null)
            {
                Console.WriteLine($"Entity {entityId} says: {node.Text}");
                for (int i = 0; i < node.Options.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {node.Options[i].Text}");
                }
            }
        }
    }

    public void HandleDialogueOption(int entityId, int optionIndex)
    {
        if (entityDialogues.TryGetValue(entityId, out int dialogueId))
        {
            Console.WriteLine($"Selected option {optionIndex + 1} for entity {entityId}");
        }
    }

    public Entity GetPlayerEntity()
    {
        return entities.FirstOrDefault(e => e.Name == "Player");
    }

    public List<Entity> GetEntitiesInRadius(Vector3 position, float radius)
    {
        return entities.Where(e => Vector3.Distance(GetEntityPosition(e), position) <= radius).ToList();
    }

public void DrawDebugInfo()
    {
        foreach (var entity in entities)
        {
            if (entity.State != EntityState.Dead)
            {
                Vector3 position = GetEntityPosition(entity);
                Color color = entity.Behavior switch
                {
                    EntityBehavior.Aggressive => Color.RED,
                    EntityBehavior.Passive => Color.GREEN,
                    EntityBehavior.Friendly => Color.BLUE,
                    _ => Color.WHITE
                };

                // Draw detection radius
                Raylib.DrawCircle3D(new Vector3(position.X, 0.1f, position.Z), entity.DetectionRadius, new Vector3(1, 0, 0), 90f, new Color(color.r, color.g, color.b, 100));

                // Draw waypoints and path
                if (entity.Waypoints.Count > 0)
                {
                    for (int i = 0; i < entity.Waypoints.Count; i++)
                    {
                        Raylib.DrawSphere(entity.Waypoints[i], 0.2f, Color.YELLOW);
                        if (i < entity.Waypoints.Count - 1)
                        {
                            Raylib.DrawLine3D(entity.Waypoints[i], entity.Waypoints[i + 1], Color.YELLOW);
                        }
                    }
                    Raylib.DrawLine3D(position, entity.Waypoints[entity.CurrentWaypointIndex], Color.ORANGE);
                }

                // Draw current path
                if (entity.CurrentPath != null && entity.CurrentPath.Count > 0)
                {
                    for (int i = 0; i < entity.CurrentPath.Count - 1; i++)
                    {
                        Raylib.DrawLine3D(entity.CurrentPath[i], entity.CurrentPath[i + 1], Color.PURPLE);
                    }
                }

                // Draw entity name and health bar
                Vector2 screenPos = Raylib.GetWorldToScreen(new Vector3(position.X, position.Y + 2f, position.Z), Raylib.GetCamera());
                Raylib.DrawText(entity.Name, (int)screenPos.X - 20, (int)screenPos.Y - 20, 10, Color.WHITE);
                
                // Health bar
                int healthBarWidth = 40;
                int healthBarHeight = 5;
                float healthPercentage = entity.Health / entity.MaxHealth;
                Raylib.DrawRectangle((int)screenPos.X - healthBarWidth / 2, (int)screenPos.Y - 30, healthBarWidth, healthBarHeight, Color.RED);
                Raylib.DrawRectangle((int)screenPos.X - healthBarWidth / 2, (int)screenPos.Y - 30, (int)(healthBarWidth * healthPercentage), healthBarHeight, Color.GREEN);

                // Draw state
                Raylib.DrawText(entity.State.ToString(), (int)screenPos.X - 20, (int)screenPos.Y - 40, 10, Color.YELLOW);

                // Draw velocity vector
                Raylib.DrawLine3D(position, Vector3.Add(position, Vector3.Multiply(entity.Velocity, 2)), Color.BLUE);
            }
        }
    }

    // Helper method to get all entities
    public List<Entity> GetAllEntities()
    {
        return new List<Entity>(entities);
    }

    // Helper method to update entity properties
    public void UpdateEntityProperties(int entityId, Action<Entity> updateAction)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null)
        {
            updateAction(entity);
        }
    }

    // Method to handle entity death
    public void HandleEntityDeath(int entityId)
    {
        Entity entity = GetEntity(entityId);
        if (entity != null && entity.State == EntityState.Dead)
        {
            // Perform any necessary cleanup or death effects
            objectRegistry.RemoveObject(entity.ObjectId);
            RemoveEntity(entityId);
        }
    }
}