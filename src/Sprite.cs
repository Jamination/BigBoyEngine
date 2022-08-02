using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BigBoyEngine;

public class Sprite : Node {
    public Texture2D Texture;
    public Rectangle? Source;
    public Vector2 Origin;
    public SpriteEffects Flip = SpriteEffects.None;
    public bool Centered = true;

    public Sprite(Texture2D texture, Rectangle? source = null, float x = 0,
        float y = 0, float scale = 1, float rotation = 0) : base(x, y, scale, rotation) {
        Source = source;
        Texture = texture;
    }

    public override void Draw() {
        var origin = Origin;

        if (Centered)
            origin += Source != null ? new Vector2(Source.Value.Width * .5f, Source.Value.Height * .5f) :
                new Vector2(Texture.Width * .5f, Texture.Height * .5f);
        Core.SpriteBatch.Draw(
            Texture,
            GlobalPosition,
            Source,
            GlobalTint,
            GlobalRotation,
            origin,
            GlobalScale,
            Flip,
            GlobalDepth);
        base.Draw();
    }

    public override Rectangle GetAABB() {
        if (Texture == null) return new Rectangle((int)GlobalPosition.X, (int)GlobalPosition.Y, 0, 0);
        var width = MathF.Round((Source?.Width ?? Texture.Width) * GlobalScale.X);
        var height = MathF.Round((Source?.Height ?? Texture.Height) * GlobalScale.Y);
        return new Rectangle(
            (int)(GlobalPosition.X - width * .5f),
            (int)(GlobalPosition.Y - height * .5f),
            (int)width,
            (int)height
        );
    }
}