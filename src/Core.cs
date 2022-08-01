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

    public static bool ExitOnEscape = true, DebugEnabled, HasReadiedScene, OptimiseTree = true;
    public static string WindowTitle = "BigBoyEngine";

    public static Color ClearColor = new(38, 44, 59);

    public static SpriteSortMode SpriteSortMode = SpriteSortMode.FrontToBack;
    public static SamplerState SamplerState = SamplerState.PointClamp;
    public static BlendState BlendState = BlendState.AlphaBlend;

    public static RNG RNG = new(Random.Shared.Next());

    private static Node _scene, _nextScene;

    public static Vector2 MousePosition, GlobalMousePosition;

    public static Node Scene {
        get => _scene;
        set {
            value.Active = false;
            _scene = value;
        }
    }

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
        _nextScene.Active = false;
    }

    public Core() {
        GDM = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Content = base.Content;
        Content.RootDirectory = "Content";
    }

    protected override void Initialize() {
        GDM.PreferredBackBufferWidth = 1280;
        GDM.PreferredBackBufferHeight = 720;
        GDM.IsFullScreen = false;
        GDM.ApplyChanges();
        base.Initialize();
    }

    public virtual void Config(string title, int width, int height, bool startsFullscreen = false, int? viewportWidth = null,
        int? viewportHeight = null) {
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
        ScreenTarget = new RenderTarget2D(GraphicsDevice, ViewportWidth + 2, ViewportHeight + 2);
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
            ScreenRect = new Rectangle(-1, barHeight - 1, Window.ClientBounds.Width, presentHeight);
        } else {
            // output is wider than it is tall, bars left/right
            int presentWidth = (int)(Window.ClientBounds.Height * preferredAspect + 0.5f);
            int barWidth = (Window.ClientBounds.Width - presentWidth) / 2;
            ScreenRect = new Rectangle(barWidth - 1, -1, presentWidth, Window.ClientBounds.Height);
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

        MousePosition = Input.NewMouse.Position.ToVector2();
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
        SpriteBatch.Begin(SpriteSortMode, BlendState,
            SamplerState, transformMatrix: Camera.Instance != null ? Camera.Instance.View : Matrix.Identity);
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
        if (Camera.Instance != null) {
            var remainder = new Vector2(Camera.Instance.Position.X - (int)Camera.Instance.Position.X,
                Camera.Instance.Position.Y - (int)Camera.Instance.Position.Y);
            if (Camera.Instance.Smooth)
                screenPos -= remainder;
        }
        SpriteBatch.Draw(ScreenTarget, screenPos, null, Color.White, 0, Vector2.Zero, screenScale, SpriteEffects.None, 0);
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}