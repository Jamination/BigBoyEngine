using System;
using System.Collections.Generic;
using Dcrew.Spatial;
using Microsoft.Xna.Framework;

namespace BigBoyEngine; 

public class Spatial : Node {
    public static Quadtree World = new(-100000, -100000, 200000, 200000, 4096);

    private Vector2 _position, _scale;
    private float _rotation, _depth = .5f;
    private Matrix _transform;
    private Color _tint = Color.White;

    protected HashSet<Spatial> SpatialChildren = new();

    private bool
        _positionIsDirty,
        _rotationIsDirty,
        _scaleIsDirty,
        _active;

    public bool Active {
        get => _active;
        set {
            _active = value;
            if (!_active && World.Contains(Id.Index))
                World.Remove(Id.Index);
        }
    }

    public Vector2 Position {
        get => _position;
        set {
            _position = value;
            _positionIsDirty = true;
            UpdateTransform();
        }
    }

    public Vector2 Scale {
        get => _scale;
        set {
            _scale = value;
            _scaleIsDirty = true;
            UpdateTransform();
        }
    }

    public float Rotation {
        get => _rotation;
        set {
            _rotation = value;
            _rotationIsDirty = true;
            UpdateTransform();
        }
    }

    public float Depth {
        get => _depth;
        set {
            _depth = value;
            UpdateDepth();
        }
    }

    public Color Tint {
        get => _tint;
        set {
            _tint = value;
            UpdateTint();
        }
    }

    public bool MouseHovering() => GetAABB().Contains(Core.GlobalMousePosition);

    public Vector2 PointToLocal(Vector2 p) => Vector2.Transform(p, GlobalTransform);
    public Vector2 PointToGlobal(Vector2 p) => Vector2.Transform(p, Matrix.Invert(GlobalTransform));

    public Vector2 WorldToScreen(Vector2 p) => Vector2.Transform(p, Camera.Instance.View);
    public Vector2 ScreenToWorld(Vector2 p) => Vector2.Transform(p, Matrix.Invert(Camera.Instance.View));

    public Spatial(float x = 0, float y = 0, float scale = 1, float rotation = 0, float depth = 0) {
        Position = new Vector2(x, y);
        Scale = new Vector2(scale, scale);
        Rotation = rotation;
        Depth = depth;
        NodeAdded += OnNodeAdded;
        NodeRemoved += OnNodeRemoved;
    }

    private void OnNodeRemoved(Node node) {
        if (node is Spatial spatial)
            SpatialChildren.Remove(spatial);
    }

    private void OnNodeAdded(Node node) {
        if (node is Spatial spatial)
            SpatialChildren.Add(spatial);
    }

    public T GetAt<T>(float x, float y, string name = "") where T : Node {
        if (name == "")
            name = typeof(T).Name;
        foreach (var id in World.Query(new Vector2(x, y)).All) {
            var node = Core.Nodes[id];
            if (node != this && node.Name == name && node is T t)
                return t;
        }
        return null;
    }

    public bool Overlaps(Spatial other) => other.GetAABB().Intersects(GetAABB());

    public HashSet<T> GetAllOverlapping<T>() where T : Spatial {
        var nodes = new HashSet<T>();
        foreach (var node in Core.NodesOfType[typeof(T)]) {
            if (((Spatial)node).Overlaps(this))
                nodes.Add((T)node);
        }
        return nodes;
    }

    public override void Ready() {
        UpdateTransform();
        UpdateDepth();
        UpdateTint();
        base.Ready();
    }

    public override void Destroy() {
        Active = false;
        base.Destroy();
    }

    public void Rotate(float amount) {
        Rotation += amount;
    }

    public void Translate(float ax = 0, float ay = 0) {
        Position += new Vector2(ax, ay);
    }

    public void ScaleBy(float ax, float ay) {
        Scale *= new Vector2(ax, ay);
    }

    public Rectangle GetGlobalAABB() {
        var aabb = GetAABB();
        foreach (var child in SpatialChildren) {
            var childAABB = child.GetGlobalAABB();
            aabb = Rectangle.Union(aabb, childAABB);
        }
        return aabb;
    }

    public virtual Rectangle GetAABB() {
        return new Rectangle((int)GlobalPosition.X, (int)GlobalPosition.Y, 0, 0);
    }

    public virtual void UpdateBounds() {
        if (World.MaxItems < Id.Index)
            World.MaxItems = Id.Index + 1;
        var aabb = GetAABB();
        World.Update(Id.Index, aabb.X, aabb.Y, aabb.Width, aabb.Height);
    }

    public void LookAt(Vector2 target, float speed = 1) {
        Rotation = MathsExt.LerpAngle(Rotation,
            MathF.Atan2(target.Y - GlobalPosition.Y, target.X - GlobalPosition.X), speed) % MathF.PI;
    }

    public void LookAt(Spatial target, float speed = 1) {
        Rotation = MathsExt.LerpAngle(Rotation,
            MathF.Atan2(target.GlobalPosition.Y - GlobalPosition.Y,
                target.GlobalPosition.X - GlobalPosition.X), speed) % MathF.PI;
    }

    public Matrix Transform {
        get {
            if (_positionIsDirty || _scaleIsDirty || _rotationIsDirty) {
                _transform = Matrix.CreateScale(Scale.X, Scale.Y, 1) *
                             Matrix.CreateRotationZ(Rotation) *
                             Matrix.CreateTranslation(-Position.X, -Position.Y, 0);
                _positionIsDirty = false;
                _scaleIsDirty = false;
                _rotationIsDirty = false;
            }
            return _transform;
        }
    }

    public Matrix GlobalTransform { get; private set; }

    protected void UpdateTransform() {
        if (!AddedToTree) return;
        if (Parent != null && Parent is Spatial spatial) {
            GlobalTransform = Transform * spatial.GlobalTransform;
            GlobalDepth = DepthRelativeToCamera + spatial.GlobalDepth;
        } else {
            GlobalTransform = Transform;
            GlobalDepth = DepthRelativeToCamera;
        }
        if (Active)
            UpdateBounds();
        foreach (var child in SpatialChildren)
            child.UpdateTransform();
        _positionIsDirty = false;
        _scaleIsDirty = false;
        _rotationIsDirty = false;
    }

    protected void UpdateDepth() {
        if (Parent is Spatial spatial)
            GlobalDepth = Parent != null ? DepthRelativeToCamera + spatial.GlobalDepth : DepthRelativeToCamera;
        foreach (var child in SpatialChildren)
            child.UpdateDepth();
    }

    protected void UpdateTint() {
        if (Parent is Spatial spatial) {
            var vec = Tint.ToVector4() * spatial.GlobalTint.ToVector4();
            GlobalTint = new Color(vec.X, vec.Y, vec.Z, vec.W);
        } else GlobalTint = _tint;
        foreach (var child in SpatialChildren)
            child.UpdateTint();
    }

    public Vector2 GlobalPosition {
        get {
            var pos = GlobalTransform.Translation;
            return new Vector2(-pos.X, -pos.Y);
        }
        set => Position += value;
    }

    public Vector2 GlobalScale {
        get {
            GlobalTransform.Decompose(out Vector3 scale, out _, out _);
            return new Vector2(scale.X, scale.Y);
        }
    }

    public Color GlobalTint { get; private set; }

    public float GlobalRotation => MathF.Atan2(GlobalTransform.M12, GlobalTransform.M11);
    public float GlobalDepth { get; private set; }
    public float DepthRelativeToCamera => Depth / (Camera.Instance != null ? Camera.Instance.Zoom : 1f);
}