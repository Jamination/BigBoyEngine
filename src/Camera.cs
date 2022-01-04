using System;
using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public class Camera {
    public Vector2 Position = Vector2.Zero;
    public float Rotation = 0, Zoom = 1;
    public Matrix View;

    public Rectangle Bounds;

    public static Camera Instance;

    public void Update() {
        View =
            Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
            Matrix.CreateRotationZ(Rotation) *
            Matrix.CreateScale(Zoom) *
            Matrix.CreateTranslation(Core.Graphics.Viewport.Width * .5f, Core.Graphics.Viewport.Height * .5f, 0);
        var bWidth = (int)(Core.GraphicsDeviceManager.PreferredBackBufferWidth * Zoom);
        var bHeight = (int)(Core.GraphicsDeviceManager.PreferredBackBufferHeight * Zoom);
        Bounds = new Rectangle((int)MathF.Round(Position.X - bWidth * .5f),
            (int)MathF.Round(Position.Y- bHeight * .5f), bWidth, bHeight);
        Core.GlobalMousePosition = Vector2.Transform(Microsoft.Xna.Framework.Input.Mouse.GetState().Position.ToVector2(), Matrix.Invert(View));
    }
}