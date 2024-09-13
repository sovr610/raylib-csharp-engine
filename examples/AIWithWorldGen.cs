using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        Raylib.InitWindow(screenWidth, screenHeight, "World Generator with AI Entities Demo");
        Raylib.SetTargetFPS(60);

        Camera3D camera = new Camera3D(
            new Vector3(50, 50, 50),
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            45,
            CameraProjection.CAMERA_PERSPECTIVE
        );

        // Initialize game systems
        ObjectRegistry objectRegistry = new ObjectRegistry();
        DialogueSystem dialogueSystem = new DialogueSystem();
        Vector3 worldSize = new Vector3(1000, 50, 1000);
        float nodeRadius = 1f;
        AIEntityManager aiManager = new AIEntityManager(objectRegistry, worldSize, nodeRadius, dialogueSystem);

        // Initialize world generator
        WorldGenerator world = new WorldGenerator(
            width: 1000,
            depth: 1000,
            maxRenderDistance: 500f,
            lodLevels: 4,
            skyRadius: 2000f
        );

        // Add celestial bodies
        world.AddCelestialBody(
            size: 50f,
            orbitSpeed: 0.1f,
            orbitRadius: 1800f,
            color: new Vector3(1f, 0.9f, 0.7f),
            texturePath: null,
            isSun: true
        );

        world.AddCelestialBody(
            size: 30f,
            orbitSpeed: 0.3f,
            orbitRadius: 1600f,
            color: new Vector3(0.8f, 0.8f, 0.9f),
            texturePath: null,
            isSun: false
        );

        // Create and place 3D objects
        int treeCount = 100;
        Random random = new Random();
        for (int i = 0; i < treeCount; i++)
        {
            float x = (float)random.NextDouble() * 1000 - 500;
            float z = (float)random.NextDouble() * 1000 - 500;
            float y = world.GetTerrainHeight(x, z);
            int treeObjectId = objectRegistry.AddModel("models/tree.glb", new Vector3(x, y, z), new Vector3(1, 1, 1), Color.WHITE);
        }

        // Create AI-controlled entities
        CreateGuard(aiManager, objectRegistry, dialogueSystem, new Vector3(0, world.GetTerrainHeight(0, 0), 0));
        CreateWanderingNPC(aiManager, objectRegistry, dialogueSystem, new Vector3(20, world.GetTerrainHeight(20, 20), 20));

        // Create player entity
        int playerObjectId = objectRegistry.AddModel("models/player.glb", new Vector3(10, world.GetTerrainHeight(10, 10), 10), new Vector3(1, 1, 1), Color.WHITE);
        int playerEntityId = aiManager.AddEntity("Player", EntityBehavior.Friendly, playerObjectId, 100, 10, 10, 20, null);

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            // Update
            float deltaTime = Raylib.GetFrameTime();
            UpdateCamera(ref camera, deltaTime);
            world.Update(deltaTime);
            aiManager.Update(deltaTime);
            UpdatePlayer(aiManager.GetEntity(playerEntityId), world);

            // Draw
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.SKYBLUE);

            Raylib.BeginMode3D(camera);
            
            // Draw the world
            world.Draw(camera);
            
            // Draw all objects in the registry
            objectRegistry.Draw();
            
            // Draw AI debug information
            aiManager.DrawDebugInfo();

            Raylib.EndMode3D();

            DrawUI(aiManager, playerEntityId);

            Raylib.EndDrawing();
        }

        // Cleanup
        world.Unload();
        objectRegistry.Unload();
        Raylib.CloseWindow();
    }

    static void UpdateCamera(ref Camera3D camera, float deltaTime)
    {
        // Simple camera controls
        if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) camera.position += camera.target * deltaTime * 10f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) camera.position -= camera.target * deltaTime * 10f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) camera.position -= Vector3.Normalize(Vector3.Cross(camera.target, camera.up)) * deltaTime * 10f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) camera.position += Vector3.Normalize(Vector3.Cross(camera.target, camera.up)) * deltaTime * 10f;

        // Mouse look
        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
        {
            Vector2 mouseDelta = Raylib.GetMouseDelta() * 0.003f;
            camera.target = Vector3.Transform(camera.target, Matrix4x4.CreateFromAxisAngle(camera.up, -mouseDelta.X));
            Vector3 right = Vector3.Normalize(Vector3.Cross(camera.target, camera.up));
            camera.target = Vector3.Transform(camera.target, Matrix4x4.CreateFromAxisAngle(right, -mouseDelta.Y));
        }
    }

    static void UpdatePlayer(Entity playerEntity, WorldGenerator world)
    {
        if (playerEntity == null) return;

        Vector3 movement = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) movement.Z -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) movement.Z += 1;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) movement.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) movement.X += 1;

        if (movement != Vector3.Zero)
        {
            movement = Vector3.Normalize(movement) * playerEntity.Speed * Raylib.GetFrameTime();
            Vector3 newPosition = playerEntity.Position + movement;
            newPosition.Y = world.GetTerrainHeight(newPosition.X, newPosition.Z);
            playerEntity.Position = newPosition;
        }
    }

    static void DrawUI(AIEntityManager aiManager, int playerEntityId)
    {
        Entity playerEntity = aiManager.GetEntity(playerEntityId);
        if (playerEntity != null)
        {
            Raylib.DrawText($"Player Health: {playerEntity.Health}/{playerEntity.MaxHealth}", 10, 10, 20, Color.WHITE);
            Raylib.DrawText($"Player Position: {playerEntity.Position}", 10, 40, 20, Color.WHITE);
            Raylib.DrawText("Use arrow keys to move the player", 10, Raylib.GetScreenHeight() - 30, 20, Color.WHITE);
        }

        // Display interaction prompt if near an NPC
        List<Entity> nearbyEntities = aiManager.GetEntitiesInRadius(playerEntity.Position, 5f);
        foreach (var entity in nearbyEntities)
        {
            if (entity.Id != playerEntityId)
            {
                Raylib.DrawText($"Press E to interact with {entity.Name}", 10, Raylib.GetScreenHeight() - 60, 20, Color.YELLOW);
                break;
            }
        }

        // Check for NPC interaction
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_E))
        {
            foreach (var entity in nearbyEntities)
            {
                if (entity.Id != playerEntityId)
                {
                    aiManager.TriggerEntityDialogue(entity.Id, 1);
                    break;
                }
            }
        }
    }

    static void CreateGuard(AIEntityManager aiManager, ObjectRegistry objectRegistry, DialogueSystem dialogueSystem, Vector3 position)
    {
        int guardObjectId = objectRegistry.AddModel("models/guard.glb", position, new Vector3(1, 1, 1), Color.WHITE);
        int guardEntityId = aiManager.AddEntity("Guard", EntityBehavior.Passive, guardObjectId, 100, 10, 5, 15, "guard_ai");

        // Set up guard's waypoints
        List<Vector3> guardWaypoints = new List<Vector3>
        {
            position,
            position + new Vector3(20, 0, 0),
            position + new Vector3(20, 0, 20),
            position + new Vector3(0, 0, 20)
        };
        aiManager.SetEntityWaypoints(guardEntityId, guardWaypoints);

        // Create a dialogue for the guard
        int guardDialogueId = dialogueSystem.CreateDialogue("Guard Conversation");
        dialogueSystem.AddDialogueNode(guardDialogueId, 1, "Halt! Who goes there?");
        dialogueSystem.AddDialogueOption(guardDialogueId, 1, "I'm a friend", 2);
        dialogueSystem.AddDialogueOption(guardDialogueId, 1, "None of your business", 3);
        dialogueSystem.AddDialogueNode(guardDialogueId, 2, "Oh, welcome friend! Please, enter the city.");
        dialogueSystem.AddDialogueNode(guardDialogueId, 3, "I'm afraid I can't let you pass then. Please leave.");

        // Associate the dialogue with the guard entity
        aiManager.AssociateDialogueWithEntity(guardEntityId, guardDialogueId);
    }

    static void CreateWanderingNPC(AIEntityManager aiManager, ObjectRegistry objectRegistry, DialogueSystem dialogueSystem, Vector3 position)
    {
        int npcObjectId = objectRegistry.AddModel("models/npc.glb", position, new Vector3(1, 1, 1), Color.WHITE);
        int npcEntityId = aiManager.AddEntity("Wandering NPC", EntityBehavior.Passive, npcObjectId, 100, 5, 3, 10, "wandering_npc_ai");

        // Set wander radius for the NPC
        aiManager.SetEntityWanderRadius(npcEntityId, 30f);

        // Create a dialogue for the NPC
        int npcDialogueId = dialogueSystem.CreateDialogue("Wandering NPC Conversation");
        dialogueSystem.AddDialogueNode(npcDialogueId, 1, "Hello there! Lovely day for a walk, isn't it?");
        dialogueSystem.AddDialogueOption(npcDialogueId, 1, "Indeed it is!", 2);
        dialogueSystem.AddDialogueOption(npcDialogueId, 1, "I'm in a hurry, goodbye.", 3);
        dialogueSystem.AddDialogueNode(npcDialogueId, 2, "I'm glad you agree! Enjoy your day!");
        dialogueSystem.AddDialogueNode(npcDialogueId, 3, "Oh, alright then. Take care!");

        // Associate the dialogue with the NPC entity
        aiManager.AssociateDialogueWithEntity(npcEntityId, npcDialogueId);
    }
}