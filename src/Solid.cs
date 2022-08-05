using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BigBoyEngine; 

public class Solid : Hitbox {
    private Vector2 _moveCounter;

    public static HashSet<Solid> Instances = new();

    public HashSet<Actor> GetAllAttachedActors() {
        var riding = new HashSet<Actor>();
        foreach (var actor in Actor.Instances) {
            if (actor.IsAttached(this))
                riding.Add(actor);
        }
        return riding;
    }

    public override void Ready() {
        Instances.Add(this);
        base.Ready();
    }

    public override void Destroy() {
        Instances.Remove(this);
        base.Destroy();
    }

    public void Move(Vector2 amount) {
        _moveCounter += amount;
        var moveX = (int)MathF.Round(_moveCounter.X);
        var moveY = (int)MathF.Round(_moveCounter.Y);
        _moveCounter -= new Vector2(moveX, moveY);
        MoveExact(moveX, moveY);
    }

    public void MoveExact(int amountX, int amountY) {
        if (amountX == 0 && amountY == 0) return;
        var attached = GetAllAttachedActors();
        Collidable = false;

        if (amountX != 0) {
            Position += new Vector2(amountX, 0);
            var aabb = GetUnionAABB();
            foreach (var actor in Actor.Instances) {
                var actorAABB = actor.GetUnionAABB();
                if (actor.GetUnionAABB().Intersects(aabb))
                    actor.MoveX(amountX > 0 ? aabb.Right - actorAABB.Left
                        : aabb.Left - actorAABB.Right, actor.Squish);
                else if (attached.Contains(actor))
                    actor.MoveX(amountX);
            }
        }

        if (amountY != 0) {
            Position += new Vector2(0, amountY);
            var aabb = GetUnionAABB();
            foreach (var actor in Actor.Instances) {
                var actorAABB = actor.GetUnionAABB();
                if (actor.GetUnionAABB().Intersects(aabb))
                    actor.MoveY(amountY > 0 ? aabb.Bottom - actorAABB.Top
                        : aabb.Top - actorAABB.Bottom, actor.Squish);
                else if (attached.Contains(actor))
                    actor.MoveY(amountY);
            }
        }
        Collidable = true;
    }
}