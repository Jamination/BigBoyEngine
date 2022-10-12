using System;
using System.Collections.Generic;
using Dcrew;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BigBoyEngine;

public class Core : Game {
    public static GraphicsDeviceManager GDM;
    public static GraphicsDevice Graphics => GDM.GraphicsDevice;
    public static SpriteBatch SpriteBatch;
    public new static ContentManager Content;

    public static bool ExitOnEscape = true, DebugEnabled, HasReadiedScene, SmoothScreen, OptimiseTree = true;
    public static string WindowTitle = "BigBoyEngine";

    public static Color ClearColor = new(38, 44, 59);

    public static SpriteSortMode SpriteSortMode = SpriteSortMode.FrontToBack;
    public static SamplerState SamplerState = SamplerState.PointClamp;
    public static BlendState BlendState = BlendState.AlphaBlend;

    public static RNG RNG = new(Random.Shared.Next());

    private static Node _nextScene;

    public static Vector2 MousePosition, GlobalMousePosition, SmoothRemainder;

    public static Node Scene { get; set; }

    public static FreeList<Node> Nodes = new(1024), Singletons = new(16);
    public static Dictionary<Type, List<Node>> NodesOfType = new();
    internal static readonly Dictionary<int, int> LatestGeneration = new();

    private static float AccumulatedTime;
    public static float StepSpeed = 1f / 60f;

    public static RenderTarget2D ScreenTarget;
    private static Rectangle ScreenRect;

    public static int PreferredWindowWidth, PreferredWindowHeight, ViewportWidth, ViewportHeight;

    public static void ChangeScene(Node scene) {
        _nextScene ??= scene;
    }

    public Core() {
        GDM = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Content = base.Content;
        Content.RootDirectory = "Content";
    }

    public virtual void Config(string title, int width, int height, bool startsFullscreen = false, int? viewportWidth = null,
        int? viewportHeight = null, bool smoothscreen = false) {
        GDM.IsFullScreen = startsFullscreen;
        GDM.PreferredBackBufferWidth = width;
        GDM.PreferredBackBufferHeight = height;
        PreferredWindowWidth = width;
        PreferredWindowHeight = height;
        Window.Title = title;
        GDM.ApplyChanges();
        ViewportWidth = viewportWidth ?? width;
        ViewportHeight = viewportHeight ?? height;
        WindowTitle = title;
        SmoothScreen = smoothscreen;
        ScreenTarget = SmoothScreen ? new RenderTarget2D(GraphicsDevice, ViewportWidth + 4, ViewportHeight + 4) : 
            new RenderTarget2D(GraphicsDevice, ViewportWidth, ViewportHeight);
        Window.ClientSizeChanged += WindowOnClientSizeChanged;
        WindowOnClientSizeChanged(this, null);
    }

    private void WindowOnClientSizeChanged(object sender, EventArgs e) {
        float outputAspect = Window.ClientBounds.Width / (float)Window.ClientBounds.Height;
        float preferredAspect = PreferredWindowWidth / (float)PreferredWindowHeight;
        if (outputAspect <= preferredAspect) {
            // output is taller than it is wider, bars on top/bottom
            int presentHeight = (int)(Window.ClientBounds.Width / preferredAspect + 0.5f);
            int barHeight = (Window.ClientBounds.Height - presentHeight) / 2;
            ScreenRect = new Rectangle(SmoothScreen ? -2 : 0, barHeight - (SmoothScreen ? 2 : 0), Window.ClientBounds.Width, presentHeight);
        } else {
            // output is wider than it is tall, bars left/right
            int presentWidth = (int)(Window.ClientBounds.Height * preferredAspect + 0.5f);
            int barWidth = (Window.ClientBounds.Width - presentWidth) / 2;
            ScreenRect = new Rectangle(barWidth - (SmoothScreen ? 2 : 0), SmoothScreen ? -2 : 0, presentWidth, Window.ClientBounds.Height);
        }
    }

    public T AddSingleton<T>(T singleton) where T : Node {
        Singletons.Add(singleton);
        return singleton;
    }

    protected override void LoadContent() {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Setup();
    }

    protected virtual void Setup() {
        Camera.Instance = new Camera();

        foreach (var singleton in Singletons.All)
            Singletons[singleton].Setup();
        foreach (var singleton in Singletons.All)
            Singletons[singleton].Ready();

        if (Scene != null) {
            Scene.AddToTree();
            Scene.Setup();
            Scene.Ready();
            HasReadiedScene = true;
        }
    }

    protected override void Update(GameTime gameTime) {
        if (Input.KeyPressed(Keys.Escape) && ExitOnEscape)
            Exit();

#if DEBUG
        if (Input.KeyPressed(Keys.Enter))
            Scene.ResetChildren();
        if (Input.KeyPressed(Keys.Tab))
            DebugEnabled = !DebugEnabled;
#endif
        if (SmoothScreen)
            MousePosition = Input.NewMouse.Position.ToVector2() + SmoothRemainder + new Vector2(2, 2);
        else MousePosition = Input.NewMouse.Position.ToVector2();
        GlobalMousePosition = Vector2.Transform(
        new Vector2(MousePosition.X / (PreferredWindowWidth / (float)ViewportWidth),
            MousePosition.Y / (PreferredWindowHeight / (float)ViewportHeight)),
        Matrix.Invert(Camera.Instance.View));

        foreach (var singleton in Singletons.All)
            Singletons[singleton].Update();

        if (Scene != null) {
            if (_nextScene != null) {
                Scene.Destroy();
                Scene = _nextScene;
                _nextScene = null;
                HasReadiedScene = false;
                Scene.AddToTree();
                Scene.Setup();
                Scene.Ready();
                HasReadiedScene = true;
            }
            else {
                AccumulatedTime += Time.Delta;
                while (AccumulatedTime >= StepSpeed) {
                    Scene.Update();
                    AccumulatedTime -= StepSpeed;
                }
            }
            Node.World.Update();
            if (OptimiseTree)
                Node.World.Optimize();
            Camera.Instance.Update();
        }
#if DEBUG
        Window.Title = WindowTitle + " - " + (int)MathF.Round(1f / Time.Delta) + " fps";
#endif
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.SetRenderTarget(ScreenTarget);
        Graphics.Clear(ClearOptions.Target, ClearColor, 0, 0);
        var view = Camera.Instance.View;
        SpriteBatch.Begin(SpriteSortMode, BlendState,
            SamplerState, transformMatrix: Camera.Instance != null ? view : Matrix.Identity);
        if (Scene != null) {
            Scene.Draw();
            if (DebugEnabled) {
                Scene.DebugDraw();
                if (Camera.Instance != null)
                    Node.World.Draw(SpriteBatch, Camera.Instance.Bounds, RectStyle.Inline);
            }
        }
        foreach (var singleton in Singletons.All)
            Singletons[singleton].Draw();
        SpriteBatch.End();
        GraphicsDevice.SetRenderTarget(null);

        SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
        var screenPos = new Vector2(ScreenRect.X, ScreenRect.Y);
        var screenScale = new Vector2(ScreenRect.Width / (float)ViewportWidth, ScreenRect.Height / (float)ViewportHeight);
        if (Camera.Instance != null && SmoothScreen) {
            SmoothRemainder = new Vector2(Camera.Instance.Position.X - MathF.Floor(Camera.Instance.Position.X),
                Camera.Instance.Position.Y - MathF.Floor(Camera.Instance.Position.Y)) *
                              (ScreenRect.Width / (float)ViewportWidth * Camera.Instance.Zoom);
            if (SmoothScreen)
                screenPos -= SmoothRemainder;
        }
        SpriteBatch.Draw(ScreenTarget, screenPos, null, Color.White, 0, Vector2.Zero, screenScale, SpriteEffects.None, 0);
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}