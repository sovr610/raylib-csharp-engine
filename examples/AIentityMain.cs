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

        Raylib.InitWindow(screenWidth, screenHeight, "AI Entity Demo");
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

        // Create a guard entity
        int guardObjectId = objectRegistry.AddModel("models/guard.glb", new Vector3(0, 0, 0), new Vector3(1, 1, 1), Color.WHITE);
        int guardEntityId = aiManager.AddEntity("Guard", EntityBehavior.Passive, guardObjectId, 100, 10, 5, 15, "guard_ai");

        // Set up guard's waypoints
        List<Vector3> guardWaypoints = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(20, 0, 0),
            new Vector3(20, 0, 20),
            new Vector3(0, 0, 20)
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

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            // Update
            float deltaTime = Raylib.GetFrameTime();
            UpdateCamera(ref camera, deltaTime);
            aiManager.Update(deltaTime);

            // Draw
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.SKYBLUE);

            Raylib.BeginMode3D(camera);
            
            // Draw the ground
            Raylib.DrawPlane(new Vector3(0, 0, 0), new Vector2(1000, 1000), Color.GREEN);
            
            // Draw all objects in the registry
            objectRegistry.Draw();
            
            // Draw AI debug information
            aiManager.DrawDebugInfo();

            Raylib.EndMode3D();

            DrawUI(aiManager, guardEntityId);

            Raylib.EndDrawing();
        }

        aiManager.RemoveEntity(guardEntityId);
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

    static void DrawUI(AIEntityManager aiManager, int guardEntityId)
    {
        Entity guardEntity = aiManager.GetEntity(guardEntityId);
        if (guardEntity != null)
        {
            Raylib.DrawText($"Guard Health: {guardEntity.Health}/{guardEntity.MaxHealth}", 10, 10, 20, Color.WHITE);
            Raylib.DrawText($"Guard State: {guardEntity.State}", 10, 40, 20, Color.WHITE);
            Raylib.DrawText("Press E near the guard to interact", 10, Raylib.GetScreenHeight() - 30, 20, Color.WHITE);
        }

        // Check for guard interaction
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_E))
        {
            Vector3 playerPosition = aiManager.GetPlayerEntity()?.Position ?? Vector3.Zero;
            if (Vector3.Distance(playerPosition, guardEntity.Position) < 5f)
            {
                aiManager.TriggerEntityDialogue(guardEntityId, 1);
            }
        }
    }
}