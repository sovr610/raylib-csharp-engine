using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

public class GameHUD
{
    private Dictionary<string, DraggableWindow> windows;
    private DraggableWindow activeWindow;
    private Vector2 dragOffset;
    public UIStyle DefaultStyle { get; set; }
    public Dictionary<string, UIStyle> Themes { get; private set; }
    private Tooltip activeTooltip;

    public GameHUD(int screenWidth, int screenHeight)
    {
        DefaultStyle = new UIStyle();
        Themes = new Dictionary<string, UIStyle>();
        InitializeThemes();

        windows = new Dictionary<string, DraggableWindow>
        {
            { "Inventory", new DraggableWindow("Inventory", screenWidth - 300, 50, 250, 400, DefaultStyle) },
            { "Settings", new DraggableWindow("Settings", 50, 50, 300, 400, DefaultStyle) },
            { "Professions", new DraggableWindow("Professions", 400, 50, 300, 400, DefaultStyle) },
            { "MissionJournal", new DraggableWindow("Mission Journal", 50, 500, 300, 400, DefaultStyle) },
            { "WorldMap", new DraggableWindow("World Map", 400, 500, 500, 400, DefaultStyle) },
            { "Custom", new DraggableWindow("Custom UI", 750, 50, 200, 200, DefaultStyle) }
        };

        InitializeUIContents();
    }

    private void InitializeThemes()
    {
        // Sci-Fi/Space Theme
        Themes["SciFi"] = new UIStyle
        {
            BackgroundColor = new Color(10, 20, 40, 230),
            BorderColor = new Color(0, 200, 255, 255),
            TextColor = new Color(0, 255, 255, 255),
            BorderThickness = 2f,
            FontSize = 18,
            Font = Raylib.GetFontDefault(),
            ButtonHoverColor = new Color(0, 100, 200, 255)
        };

        // Fantasy Theme
        Themes["Fantasy"] = new UIStyle
        {
            BackgroundColor = new Color(60, 30, 10, 230),
            BorderColor = new Color(255, 215, 0, 255),
            TextColor = new Color(255, 223, 186, 255),
            BorderThickness = 3f,
            FontSize = 20,
            Font = Raylib.GetFontDefault(),
            ButtonHoverColor = new Color(100, 50, 20, 255)
        };

        // Generic Theme
        Themes["Generic"] = new UIStyle
        {
            BackgroundColor = new Color(240, 240, 240, 230),
            BorderColor = new Color(100, 100, 100, 255),
            TextColor = new Color(20, 20, 20, 255),
            BorderThickness = 1f,
            FontSize = 16,
            Font = Raylib.GetFontDefault(),
            ButtonHoverColor = new Color(200, 200, 200, 255)
        };

        // Set the default theme
        DefaultStyle = Themes["Generic"];
    }

    private void InitializeUIContents()
    {
        // Example of adding elements to the Settings window
        windows["Settings"].AddUIElement(new Button(20, 50, 100, 30, "Save", () => Console.WriteLine("Saved!"), DefaultStyle, "Save your settings"));
        windows["Settings"].AddUIElement(new ToggleSwitch(20, 90, 60, 30, false, (isOn) => Console.WriteLine($"Toggle is {(isOn ? "on" : "off")}"), DefaultStyle, "Enable/Disable feature"));
        windows["Settings"].AddUIElement(new TextInput(20, 130, 200, 30, "Enter username", (text) => Console.WriteLine($"Username: {text}"), DefaultStyle));
        windows["Settings"].AddUIElement(new Slider(20, 170, 200, 20, 0, 100, 50, (value) => Console.WriteLine($"Slider value: {value}"), DefaultStyle, "Adjust volume"));

        // Add elements to other windows as needed
    }

    public void Update(float deltaTime)
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        activeTooltip = null;

        foreach (var window in windows.Values)
        {
            if (window.IsVisible)
            {
                window.Update(deltaTime);

                foreach (var element in window.UIElements)
                {
                    element.Update();
                    if (element is ITooltipProvider tooltipProvider && Raylib.CheckCollisionPointRec(mousePosition, tooltipProvider.Bounds))
                    {
                        activeTooltip = tooltipProvider.GetTooltip();
                    }
                }
            }
        }

        if (activeTooltip != null)
        {
            activeTooltip.Position = mousePosition + new Vector2(10, 10);
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
        {
            foreach (var window in windows.Values)
            {
                if (window.IsVisible && Raylib.CheckCollisionPointRec(mousePosition, window.Bounds))
                {
                    activeWindow = window;
                    dragOffset = mousePosition - new Vector2(window.Bounds.x, window.Bounds.y);
                    break;
                }
            }
        }
        else if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON) && activeWindow != null)
        {
            activeWindow.Bounds.x = mousePosition.X - dragOffset.X;
            activeWindow.Bounds.y = mousePosition.Y - dragOffset.Y;
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON))
        {
            activeWindow = null;
        }
    }

    public void Draw()
    {
        foreach (var window in windows.Values)
        {
            if (window.IsVisible)
            {
                window.Draw();
            }
        }

        if (activeTooltip != null)
        {
            activeTooltip.Draw();
        }
    }

    public void ApplyTheme(string themeName)
    {
        if (Themes.TryGetValue(themeName, out UIStyle theme))
        {
            DefaultStyle = theme;
            foreach (var window in windows.Values)
            {
                window.Style = theme;
                foreach (var element in window.UIElements)
                {
                    element.SetStyle(theme);
                }
            }
        }
        else
        {
            Console.WriteLine($"Theme '{themeName}' not found.");
        }
    }

    public void SetWindowTitle(string windowKey, string newTitle)
    {
        if (windows.ContainsKey(windowKey))
        {
            windows[windowKey].Title = newTitle;
        }
        else
        {
            Console.WriteLine($"Window '{windowKey}' not found.");
        }
    }

    public void SetWindowSize(string windowKey, float width, float height)
    {
        if (windows.ContainsKey(windowKey))
        {
            windows[windowKey].SetSize(width, height);
        }
        else
        {
            Console.WriteLine($"Window '{windowKey}' not found.");
        }
    }

    public void ToggleWindow(string windowName)
    {
        if (windows.ContainsKey(windowName))
        {
            windows[windowName].IsVisible = !windows[windowName].IsVisible;
        }
    }

    public void AddImageToWindow(string windowKey, string imagePath, float x, float y, float width, float height, Color tint)
    {
        if (windows.ContainsKey(windowKey))
        {
            Image image = new Image(x, y, width, height, imagePath, tint);
            windows[windowKey].AddUIElement(image);
        }
        else
        {
            Console.WriteLine($"Window '{windowKey}' not found.");
        }
    }

    public void SetElementPosition(string windowKey, int elementIndex, float x, float y)
    {
        if (windows.ContainsKey(windowKey) && elementIndex < windows[windowKey].UIElements.Count)
        {
            windows[windowKey].UIElements[elementIndex].SetPosition(x, y);
        }
    }

    public void SetImageSize(string windowKey, int imageIndex, float width, float height)
    {
        if (windows.ContainsKey(windowKey) && imageIndex < windows[windowKey].UIElements.Count && windows[windowKey].UIElements[imageIndex] is Image image)
        {
            image.SetSize(width, height);
        }
    }

    public void Dispose()
    {
        foreach (var window in windows.Values)
        {
            foreach (var element in window.UIElements)
            {
                if (element is Image image)
                {
                    image.Unload();
                }
            }
        }
    }

    public class UIStyle
    {
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }
        public Color TextColor { get; set; }
        public float BorderThickness { get; set; }
        public int FontSize { get; set; }
        public Font Font { get; set; }
        public Color ButtonHoverColor { get; set; }

        public UIStyle Clone()
        {
            return new UIStyle
            {
                BackgroundColor = this.BackgroundColor,
                BorderColor = this.BorderColor,
                TextColor = this.TextColor,
                BorderThickness = this.BorderThickness,
                FontSize = this.FontSize,
                Font = this.Font,
                ButtonHoverColor = this.ButtonHoverColor
            };
        }
    }

    private interface IUIElement
    {
        void Draw();
        void Update();
        void SetPosition(float x, float y);
        void SetStyle(UIStyle style);
    }

    private interface ITooltipProvider
    {
        Rectangle Bounds { get; }
        Tooltip GetTooltip();
    }

    private interface IAnimatable
    {
        void Animate(float deltaTime);
    }

    private class Button : IUIElement, ITooltipProvider, IAnimatable
    {
        public Rectangle Bounds { get; private set; }
        private string text;
        private Action callback;
        public UIStyle Style;
        private Tooltip tooltip;
        private float hoverScale = 1f;
        private const float MAX_SCALE = 1.1f;
        private const float SCALE_SPEED = 2f;

        public Button(float x, float y, float width, float height, string text, Action callback, UIStyle style, string tooltipText = null)
        {
            Bounds = new Rectangle(x, y, width, height);
            this.text = text;
            this.callback = callback;
            Style = style;
            if (tooltipText != null)
            {
                tooltip = new Tooltip(tooltipText, style);
            }
        }

        public void Draw()
        {
            Color backgroundColor = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Bounds)
                ? Style.ButtonHoverColor
                : Style.BackgroundColor;

            Rectangle scaledBounds = new Rectangle(
                Bounds.x + Bounds.width * (1 - hoverScale) / 2,
                Bounds.y + Bounds.height * (1 - hoverScale) / 2,
                Bounds.width * hoverScale,
                Bounds.height * hoverScale
            );

            Raylib.DrawRectangleRec(scaledBounds, backgroundColor);
            Raylib.DrawRectangleLinesEx(scaledBounds, Style.BorderThickness, Style.BorderColor);
            Raylib.DrawTextEx(Style.Font, text, new Vector2(scaledBounds.x + scaledBounds.width / 2 - Raylib.MeasureTextEx(Style.Font, text, Style.FontSize, 1).X / 2, scaledBounds.y + scaledBounds.height / 2 - Style.FontSize / 2), Style.FontSize, 1, Style.TextColor);
        }

        public void Update()
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Bounds))
            {
                if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
                {
                    callback?.Invoke();
                }
            }
        }

        public void Animate(float deltaTime)
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Bounds))
            {
                hoverScale = Math.Min(hoverScale + SCALE_SPEED * deltaTime, MAX_SCALE);
            }
            else
            {
                hoverScale = Math.Max(hoverScale - SCALE_SPEED * deltaTime, 1f);
            }
        }

        public void SetPosition(float x, float y)
        {
            Bounds.x = x;
            Bounds.y = y;
        }

        public void SetStyle(UIStyle style)
        {
            Style = style;
            if (tooltip != null)
            {
                tooltip.Style = style;
            }
        }

        public Tooltip GetTooltip() => tooltip;
    }

    private class ToggleSwitch : IUIElement, ITooltipProvider
    {
        public Rectangle Bounds { get; private set; }
        private bool isOn;
        private Action<bool> callback;
        public UIStyle Style;
        private Tooltip tooltip;

        public ToggleSwitch(float x, float y, float width, float height, bool initialState, Action<bool> callback, UIStyle style, string tooltipText = null)
        {
            Bounds = new Rectangle(x, y, width, height);
            isOn = initialState;
            this.callback = callback;
            Style = style;
            if (tooltipText != null)
            {
                tooltip = new Tooltip(tooltipText, style);
            }
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Bounds, isOn ? Style.ButtonHoverColor : Color.GRAY);
            float circleRadius = Bounds.height * 0.8f;
            float circleX = isOn ? Bounds.x + Bounds.width - circleRadius - 2 : Bounds.x + 2;
            Raylib.DrawCircle((int)(circleX + circleRadius / 2), (int)(Bounds.y + Bounds.height / 2), circleRadius / 2, Style.TextColor);
        }

        public void Update()
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Bounds) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
            {
                isOn = !isOn;
                callback?.Invoke(isOn);
            }
        }

        public void SetPosition(float x, float y)
        {
            Bounds.x = x;
            Bounds.y = y;
        }

        public void SetStyle(UIStyle style)
        {
            Style = style;
            if (tooltip != null)
            {
                tooltip.Style = style;
            }
        }

        public Tooltip GetTooltip() => tooltip;
    }

    private class TextInput : IUIElement, ITooltipProvider
    {
        public Rectangle Bounds { get; private set; }
        private string text;
        private string placeholder;
        private bool isActive;
        private Action<string> callback;
        public UIStyle Style;
        private Tooltip tooltip;

        public TextInput(float x, float y, float width, float height, string placeholder, Action<string> callback, UIStyle style, string tooltipText = null)
        {
            Bounds = new Rectangle(x, y, width, height);
            this.placeholder = placeholder;
            text = "";
            this.callback = callback;
            Style = style;
            if (tooltipText != null)
            {
                tooltip = new Tooltip(tooltipText, style);
            }
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Bounds, Style.BackgroundColor);
            Raylib.DrawRectangleLinesEx(Bounds, Style.BorderThickness, isActive ? Style.BorderColor : Color.DARKGRAY);
            string displayText = string.IsNullOrEmpty(text) ? placeholder : text;
            Raylib.DrawTextEx(Style.Font, displayText, new Vector2(Bounds.x + 5, Bounds.y + Bounds.height / 2 - Style.FontSize / 2), Style.FontSize, 1, string.IsNullOrEmpty(text) ? Color.GRAY : Style.TextColor);
        }

        public void Update()
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Bounds) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
            {
                isActive = true;
            }
            else if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
            {
                isActive = false;
            }

            if (isActive)
            {
                int key = Raylib.GetCharPressed();
                while (key > 0)
                {
                    if ((key >= 32) && (key <= 125))
                    {
                        text += (char)key;
                        callback?.Invoke(text);
                    }
                    key = Raylib.GetCharPressed();
                }

                if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE) && text.Length > 0)
                {
                    text = text.Substring(0, text.Length - 1);
                    callback?.Invoke(text);
                }
            }
        }

        public void SetPosition(float x, float y)
        {
            Bounds.x = x;
            Bounds.y = y;
        }

        public void SetStyle(UIStyle style)
        {
            Style = style;
            if (tooltip != null)
            {
                tooltip.Style = style;
            }
        }

        public string GetText() => text;
        public void SetText(string newText)
        {
            text = newText;
            callback?.Invoke(text);
        }

        public Tooltip GetTooltip() => tooltip;
    }

    private class Slider : IUIElement, ITooltipProvider
    {
        public Rectangle Bounds { get; private set; }
        private float minValue;
        private float maxValue;
        private float currentValue;
        private Action<float> callback;
        public UIStyle Style;
        private Tooltip tooltip;

        public Slider(float x, float y, float width, float height, float min, float max, float initial, Action<float> callback, UIStyle style, string tooltipText = null)
        {
            Bounds = new Rectangle(x, y, width, height);
            minValue = min;
            maxValue = max;
            currentValue = initial;
            this.callback = callback;
            Style = style;
            if (tooltipText != null)
            {
                tooltip = new Tooltip(tooltipText, style);
            }
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Bounds, Color.GRAY);
            float knobPosition = (currentValue - minValue) / (maxValue - minValue) * Bounds.width;
            Raylib.DrawRectangle((int)(Bounds.x + knobPosition - 5), (int)Bounds.y, 10, (int)Bounds.height, Style.ButtonHoverColor);
        }

        public void Update()
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Bounds) && Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON))
            {
                float normalizedValue = (Raylib.GetMousePosition().X - Bounds.x) / Bounds.width;
                currentValue = minValue + normalizedValue * (maxValue - minValue);
                currentValue = Math.Clamp(currentValue, minValue, maxValue);
                callback?.Invoke(currentValue);
            }
        }

        public void SetPosition(float x, float y)
        {
            Bounds.x = x;
            Bounds.y = y;
        }

        public void SetStyle(UIStyle style)
        {
            Style = style;
            if (tooltip != null)
            {
                tooltip.Style = style;
            }
        }

        public Tooltip GetTooltip() => tooltip;
    }

    private class Image : IUIElement
    {
        public Rectangle Bounds { get; private set; }
        private Texture2D texture;
        private Color tint;

        public Image(float x, float y, float width, float height, string imagePath, Color tint)
        {
            Bounds = new Rectangle(x, y, width, height);
            texture = Raylib.LoadTexture(imagePath);
            this.tint = tint;
        }

        public void Draw()
        {
            Raylib.DrawTexturePro(texture, 
                new Rectangle(0, 0, texture.width, texture.height),
                Bounds,
                new Vector2(0, 0),
                0f,
                tint);
        }

        public void Update() { }

        public void SetPosition(float x, float y)
        {
            Bounds.x = x;
            Bounds.y = y;
        }

        public void SetSize(float width, float height)
        {
            Bounds.width = width;
            Bounds.height = height;
        }

        public void SetStyle(UIStyle style) { }

        public void Unload()
        {
            Raylib.UnloadTexture(texture);
        }
    }

    private class Tooltip
    {
        public string Text { get; set; }
        public Vector2 Position { get; set; }
        public UIStyle Style { get; set; }

        public Tooltip(string text, UIStyle style)
        {
            Text = text;
            Style = style;
        }

        public void Draw()
        {
            Vector2 textSize = Raylib.MeasureTextEx(Style.Font, Text, Style.FontSize, 1);
            Rectangle bounds = new Rectangle(Position.X, Position.Y, textSize.X + 20, textSize.Y + 10);

            Raylib.DrawRectangleRec(bounds, Style.BackgroundColor);
            Raylib.DrawRectangleLinesEx(bounds, 1, Style.BorderColor);
            Raylib.DrawTextEx(Style.Font, Text, new Vector2(bounds.x + 10, bounds.y + 5), Style.FontSize, 1, Style.TextColor);
        }
    }

    private class DraggableWindow
    {
        public Rectangle Bounds;
        public string Title;
        public bool IsVisible;
        private Action drawContent;
        public List<IUIElement> UIElements { get; private set; }
        public UIStyle Style;

        public DraggableWindow(string title, float x, float y, float width, float height, UIStyle style)
        {
            Title = title;
            Bounds = new Rectangle(x, y, width, height);
            IsVisible = false;
            UIElements = new List<IUIElement>();
            Style = style;
        }

        public void AddContent(Action content)
        {
            drawContent = content;
        }

        public void AddUIElement(IUIElement element)
        {
            UIElements.Add(element);
        }

        public void SetSize(float width, float height)
        {
            Bounds.width = width;
            Bounds.height = height;
        }

        public void Draw()
        {
            if (!IsVisible) return;

            Raylib.DrawRectangleRec(Bounds, Style.BackgroundColor);
            Raylib.DrawRectangleLinesEx(Bounds, Style.BorderThickness, Style.BorderColor);
            Raylib.DrawTextEx(Style.Font, Title, new Vector2(Bounds.x + 10, Bounds.y + 10), Style.FontSize, 1, Style.TextColor);

            drawContent?.Invoke();

            foreach (var element in UIElements)
            {
                element.Draw();
            }
        }

        public void Update(float deltaTime)
        {
            foreach (var element in UIElements)
            {
                element.Update();
                if (element is IAnimatable animatable)
                {
                    animatable.Animate(deltaTime);
                }
            }
        }
    }
}