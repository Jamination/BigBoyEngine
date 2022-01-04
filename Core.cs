using System.Collections.Generic;
using Dcrew;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BigBoyEngine;

public class Core : Game {
    public static GraphicsDeviceManager GraphicsDeviceManager;
    public static GraphicsDevice Graphics => GraphicsDeviceManager.GraphicsDevice;
    public static SpriteBatch SpriteBatch;
    public new static ContentManager Content;

    public static bool ExitOnEscape = true, DebugEnabled, HasReadiedScene, OptimiseTree = true;
    public static string WindowTitle = "BigBoyEngine";

    public static Color ClearColor = new(38, 44, 59);

    public static SpriteSortMode SpriteSortMode = SpriteSortMode.FrontToBack;
    public static SamplerState SamplerState = SamplerState.PointClamp;
    public static BlendState BlendState = BlendState.AlphaBlend;

    public static Vector2 GlobalMousePosition;

    public static Node Scene;
    private static Node _nextScene;
    public static FreeList<Node> Nodes = new(1024), Singletons = new(16);
    internal static readonly Dictionary<int, int> LatestGeneration = new();
    
    public static void ChangeScene(Node scene) {
        _nextScene ??= scene;
    }

    public static void ReloadScene() {
        Scene.Destroy();
        HasReadiedScene = false;
        Scene.AddToTree();
        Scene.Setup();
        Scene.Ready();
        HasReadiedScene = true;
    }

    public Core() {
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Content = base.Content;
        Content.RootDirectory = "Content";
    }

    protected override void Initialize() {
        GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
        GraphicsDeviceManager.PreferredBackBufferHeight = 720;
        GraphicsDeviceManager.IsFullScreen = false;
        GraphicsDeviceManager.ApplyChanges();
        SpriteBatchExtensions.Init();
        base.Initialize();
    }

    public virtual void Config(string title, int width, int height, bool startsFullscreen) {
        GraphicsDeviceManager.IsFullScreen = startsFullscreen;
        GraphicsDeviceManager.PreferredBackBufferWidth = width;
        GraphicsDeviceManager.PreferredBackBufferHeight = height;
        Window.Title = title;
        WindowTitle = title;
        GraphicsDeviceManager.ApplyChanges();
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
            ReloadScene();
        if (Input.KeyPressed(Keys.Tab))
            DebugEnabled = !DebugEnabled;
#endif

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
            } else Scene.Update();
        }

        Node.World.Update();
        if (OptimiseTree)
            Node.World.Optimize();
        Camera.Instance.Update();
#if DEBUG
        Window.Title = WindowTitle + " - " + (int)MathF.Round(1f / Time.Delta) + " fps";
#endif
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        Graphics.Clear(ClearOptions.Target, ClearColor, 0, 0);
        SpriteBatch.Begin(SpriteSortMode, BlendState,
            SamplerState, transformMatrix: Camera.Instance != null ? Camera.Instance.View : Matrix.Identity);
        if (Scene != null) {
            Scene.Draw();
            if (DebugEnabled) {
                Scene.DebugDraw();
                Node.World.Draw(SpriteBatch, RectStyle.Inline, 4);
            }
        }
        foreach (var singleton in Singletons.All)
            Singletons[singleton].Draw();
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}