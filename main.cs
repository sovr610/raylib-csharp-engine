using Raylib_cs;
using System;
using System.Numerics;

class Program
{
    static void Main(string[] args)
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        Raylib.InitWindow(screenWidth, screenHeight, "World Generator Demo");
        Raylib.SetTargetFPS(60);

        Camera3D camera = new Camera3D(
            new Vector3(50, 50, 50),
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            45,
            CameraProjection.CAMERA_PERSPECTIVE
        );

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

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            // Update
            float deltaTime = Raylib.GetFrameTime();
            UpdateCamera(ref camera, deltaTime);
            world.Update(deltaTime);

            // Draw
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);

            Raylib.BeginMode3D(camera);
            world.Draw(camera);
            Raylib.EndMode3D();

            DrawUI(world);

            Raylib.EndDrawing();
        }

        world.Unload();
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

    static void DrawUI(WorldGenerator world)
    {
        Raylib.DrawText($"Time of Day: {world.TimeOfDay:F2}", 10, 10, 20, Color.WHITE);
        Raylib.DrawText($"Cloud Coverage: {world.CloudCoverage:F2}", 10, 40, 20, Color.WHITE);
        Raylib.DrawText($"Cloud Speed: {world.CloudSpeed:F2}", 10, 70, 20, Color.WHITE);

        Raylib.DrawText("Press F1-F4 to adjust world parameters", 10, Raylib.GetScreenHeight() - 30, 20, Color.WHITE);

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_F1)) world.TimeOfDay = (world.TimeOfDay + 0.1f) % 1.0f;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_F2)) world.TimeOfDay = (world.TimeOfDay - 0.1f + 1.0f) % 1.0f;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_F3)) world.CloudCoverage = Math.Clamp(world.CloudCoverage + 0.1f, 0f, 1f);
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_F4)) world.CloudCoverage = Math.Clamp(world.CloudCoverage - 0.1f, 0f, 1f);
    }
}