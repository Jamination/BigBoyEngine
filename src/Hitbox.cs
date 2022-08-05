using System;
using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public class Hitbox : Node {
    public Vector2 Offset, Size;
    public bool Collidable = true;

    public Hitbox(float offsetX = 0, float offsetY = 0, float width = -1, float height = -1) {
        Offset = new Vector2(offsetX, offsetY);
        Size = new Vector2(width, height);
        Active = true;
    }

    public override void Ready() {
        var union = GetUnionAABB();
        Size.X = union.Width;
        Size.Y = union.Height;
        base.Ready();
    }

    public override Rectangle GetAABB() {
        var width = MathF.Round(Size.X * GlobalScale.X);
        var height = MathF.Round(Size.Y * GlobalScale.Y);
        return new Rectangle((int)(Offset.X + GlobalPosition.X - width * .5f),
            (int)(Offset.Y + GlobalPosition.Y - height * .5f), (int)width, (int)height);
    }

    public Rectangle GetBroadphase(int xAmount, int yAmount) {
        var AABB = GetAABB();
        return new Rectangle(
            xAmount > 0 ? AABB.X : AABB.X + xAmount,
            yAmount > 0 ? AABB.Y : AABB.Y + yAmount,
            xAmount > 0 ? xAmount + AABB.Width : AABB.Width - xAmount,
            yAmount > 0 ? yAmount + AABB.Height : AABB.Height - yAmount
        );
    }
}