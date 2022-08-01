using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public readonly struct CollisionData {
    public readonly Hitbox Obstacle;
    public readonly Vector2 AmountMoved, Target;
    public readonly int Direction;

    public CollisionData(Hitbox obstacle, int direction, Vector2 amountMoved, Vector2 target) {
        Obstacle = obstacle;
        Direction = direction;
        AmountMoved = amountMoved;
        Target = target;
    }
}
