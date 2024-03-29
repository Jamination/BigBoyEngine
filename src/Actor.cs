﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public class Actor : Hitbox {
    private Vector2 _movementCounter;

    public delegate void Collision(CollisionData collision);

    public Collision Squish;

    public bool Collides = true;

    public static HashSet<Actor> Instances = new();

    public event Collision OnCollision;

    public virtual bool IsAttached(Solid solid) {
        var aabb = GetAABB();
        return new Rectangle(aabb.X, aabb.Y + 1, aabb.Width, aabb.Height).Intersects(solid.GetAABB());
    }

    public Actor(float offsetX, float offsetY, float width, float height) : base(offsetX, offsetY, width, height) {
        Squish = _ => Destroy();
    }

    public override void Ready() {
        Instances.Add(this);
        base.Ready();
    }

    public override void Destroy() {
        Instances.Remove(this);
        base.Destroy();
    }

    public virtual bool AllowCollision(Hitbox hitbox) {
        return true;
    }

    public Hitbox CollideAt(Vector2 position, IEnumerable<Hitbox> hitboxes = null) {
        var oldPos = Position;
        Position = position;
        var AABB = GetAABB();
        Position = oldPos;
        if (hitboxes != null) {
            foreach (var hitbox in hitboxes) {
                if (hitbox != this && hitbox.Collidable && AllowCollision(hitbox) && AABB.Intersects(hitbox.GetAABB())) {
                    return hitbox;
                }
            }
            return null;
        }

        var query = World.Query(AABB).All;
        return query.IsEmpty ? null : (Hitbox)Core.Nodes[query[0]];
    }

    public void MoveX(float amount, Collision onCollide = null) {
        _movementCounter.X += amount;
        var move = (int)MathF.Round(_movementCounter.X);
        if (move == 0) return;
        _movementCounter.X -= move;
        MoveXExact(move, onCollide);
    }

    public void MoveXExact(int amount, Collision onCollide = null) {
        if (amount == 0) return;
        if (!Collides) {
            Position += new Vector2(amount, 0);
            return;
        }
        var broadphase = GetBroadphase(amount, 0);

        var hitboxes = new HashSet<Hitbox>();

        foreach (var id in World.Query(broadphase).All) {
            if (Core.Nodes[id] is Hitbox s && s != this)
                hitboxes.Add(s);
        }

        if (hitboxes.Count == 0) {
            Position += new Vector2(amount, 0);
            return;
        }

        var sign = MathF.Sign(amount);
        var moved = 0;

        var target = Position + new Vector2(amount, 0);

        while (amount != 0) {
            var hitbox = CollideAt(Position + new Vector2(sign, 0), hitboxes);
            if (hitbox == null) {
                Position += new Vector2(sign, 0);
                moved++;
                amount -= sign;
            }
            else {
                var data = new CollisionData(hitbox, sign,
                    new Vector2(moved, 0), target);
                onCollide?.Invoke(data);
                OnCollision?.Invoke(data);
                break;
            }
        }
    }

    public void MoveY(float amount, Collision onCollide = null) {
        _movementCounter.Y += amount;
        var move = (int)MathF.Round(_movementCounter.Y);
        if (move == 0) return;
        _movementCounter.Y -= move;
        MoveYExact(move, onCollide);
    }

    public void MoveYExact(int amount, Collision onCollide = null) {
        if (amount == 0) return;
        if (!Collides) {
            Position += new Vector2(0, amount);
            return;
        }
        var broadphase = GetBroadphase(0, amount);

        var hitboxes = new HashSet<Hitbox>();

        foreach (var id in World.Query(broadphase).All) {
            if (Core.Nodes[id] is Hitbox s && s != this)
                hitboxes.Add(s);
        }

        if (hitboxes.Count == 0) {
            Position += new Vector2(0, amount);
            return;
        }

        var sign = MathF.Sign(amount);
        var moved = 0;

        var target = Position + new Vector2(0, amount);

        while (amount != 0) {
            var hitbox = CollideAt(Position + new Vector2(0, sign), hitboxes);
            if (hitbox == null) {
                Position += new Vector2(0, sign);
                moved++;
                amount -= sign;
            }
            else {
                var data = new CollisionData(hitbox, sign,
                    new Vector2(0, moved), target);
                onCollide?.Invoke(data);
                OnCollision?.Invoke(data);
                break;
            }
        }
    }

    public void Move(Vector2 amount, Collision onCollide = null) {
        MoveX(amount.X, onCollide);
        MoveY(amount.Y, onCollide);
    }

    public void MoveExact(Vector2 amount, Collision onCollide = null) {
        MoveXExact((int)amount.X, onCollide);
        MoveYExact((int)amount.Y, onCollide);
    }

    public void MoveTo(Vector2 target, Collision onCollide = null) {
        var diff = target - Position;
        if (diff.X == 0 && diff.Y == 0) return;
        var amount = new Vector2(MathF.Sign(diff.X), MathF.Sign(diff.Y));
        Move(amount, onCollide);
    }
}