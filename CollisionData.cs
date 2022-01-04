using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public readonly struct CollisionData {
    public readonly Hitbox Hitbox;
    public readonly Vector2 AmountMoved, Target;
    public readonly int Direction;

    public CollisionData(Hitbox hitbox, int direction, Vector2 amountMoved, Vector2 target) {
        Hitbox = hitbox;
        Direction = direction;
        AmountMoved = amountMoved;
        Target = target;
    }
}
