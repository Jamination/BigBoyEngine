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
            Matrix.CreateTranslation((int)-Position.X, (int)-Position.Y, 0) *
            Matrix.CreateRotationZ(Rotation) *
            Matrix.CreateScale(Zoom) *
            Matrix.CreateTranslation(Core.ViewportWidth * .5f, Core.ViewportHeight * .5f, 0);
        var bWidth = (int)(Core.ViewportWidth * Zoom);
        var bHeight = (int)(Core.ViewportHeight * Zoom);
        Bounds = new Rectangle((int)MathF.Round(Position.X - bWidth * .5f),
            (int)MathF.Round(Position.Y - bHeight * .5f), bWidth, bHeight);
    }
}