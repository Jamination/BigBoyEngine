using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public class Hitbox : Node {
    public Vector2 Offset, Size;
    public bool Collidable = true;

    public Hitbox(float offsetX, float offsetY, float width, float height) : base(0, 0) {
        Offset = new Vector2(offsetX, offsetY);
        Size = new Vector2(width, height);
        Active = true;
    }

    public override Rectangle GetGlobalAABB() {
        var width = Size.X * GlobalScale.X;
        var height = Size.Y * GlobalScale.Y;
        return new Rectangle((int)(Offset.X + GlobalPosition.X - width * .5f),
            (int)(Offset.Y + GlobalPosition.Y - height * .5f), (int)width, (int)height);
    }

    public Rectangle GetBroadphase(int xAmount, int yAmount) {
        var AABB = GetGlobalAABB();
        return new Rectangle(
            xAmount > 0 ? AABB.X : AABB.X + xAmount,
            yAmount > 0 ? AABB.Y : AABB.Y + yAmount,
            xAmount > 0 ? xAmount + AABB.Width : AABB.Width - xAmount,
            yAmount > 0 ? yAmount + AABB.Height : AABB.Height - yAmount
        );
    }
}