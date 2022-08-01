using System;
using System.Collections.Generic;
using Dcrew;
using Dcrew.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace BigBoyEngine;

public class Node {
    public string Name = "";

    private NodeId? _parent;
    private SparseSet _children = new(8, 8);

    public NodeId Id { get; private set; }

    public Node Parent => _parent?.Get();
    public static Node Scene => Core.Scene;

    public static Quadtree World = new(-100000, -100000, 200000, 200000, 4096);

    private bool _resetting;

    public Node[] Children {
        get {
            var arr = new Node[_children.Count];
            var i = 0;
            foreach (var child in _children.All) {
                arr[i] = Core.Nodes[child];
                i++;
            }
            return arr;
        }
    }

    public static ContentManager Content => Core.Content;
    public static RNG RNG => Core.RNG;

    private bool _destroyed;

    public bool Persistent = true, Visible = true, Processing = true;

    internal bool AddedToTree { get; private set; }

    public bool HasSetup;
    public bool HasReadied { get; private set; }

    private Vector2 _position, _scale;
    private float _rotation, _depth = .5f;
    private Matrix _transform;
    private Color _tint = Color.White;

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

    public void SetParent(Node parent) => _parent = parent.Id;

    public Node(float x = 0, float y = 0, float scale = 1, float rotation = 0, float depth = 0) {
        Position = new Vector2(x, y);
        Scale = new Vector2(scale, scale);
        Rotation = rotation;
        Depth = depth;
    }

    public T Add<T>(T node) where T : Node {
        if (!node.AddedToTree)
            node.AddToTree(Id);
        if (!node.HasSetup) {
            node.Setup();
            node.HasSetup = true;
        }
        if (Core.HasReadiedScene && !node.HasReadied)
            node.Ready();
        _children.EnsureFits(node.Id.Index);
        _children.Add(node.Id.Index);
        Nodes<T>.Add(node.Id);
        return node;
    }

    public T Remove<T>(string name) where T : Node {
        var node = Get<T>(name);
        node.Destroy();
        return node;
    }

    public void Remove(Node node) {
        foreach (var child in Children) {
            if (child != node) continue;
            child.Destroy();
            return;
        }
    }

    public T Get<T>(string name = "") where T : Node {
        if (name == "")
            name = typeof(T).Name;
        foreach (var node in _children.All) {
            var n = Core.Nodes[node];
            if (n.Name == name)
                return (T)n;
        }
        throw new Exception($"Could not get node {name} in {Name}.");
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

    public T Find<T>(string name = "") where T : Node {
        if (name == "")
            name = typeof(T).Name;
        foreach (var node in _children.All) {
            var n = Core.Nodes[node];
            if (n.Name == name)
                return (T)n;
        }
        foreach (var child in Children) {
            var node = child.Find<T>(name);
            if (node != null) return node;
        }
        return null;
    }

    internal void AddToTree(NodeId? parent = null) {
        var i = Core.Nodes.Add(this);
        var type = GetType();
        if (!Core.NodesOfType.ContainsKey(type))
            Core.NodesOfType.Add(type, new List<Node>());
        Core.NodesOfType[type].Add(this);
        if (!Core.LatestGeneration.ContainsKey(i))
            Core.LatestGeneration.Add(i, 0);
        Id = new NodeId(i, Core.LatestGeneration[i]);
        _parent = parent;
        AddedToTree = true;
        if (Name == "")
            Name = GetType().Name;
        if (Parent != null)
            Persistent = Parent.Persistent;
    }

    public bool Overlaps(Node other) => other.GetAABB().Intersects(GetAABB());

    public HashSet<T> GetAllOverlapping<T>() where T : Node {
        var nodes = new HashSet<T>();
        foreach (var node in Core.NodesOfType[typeof(T)]) {
            if (node.Overlaps(this))
                nodes.Add((T)node);
        }
        return nodes;
    }

    public virtual void Setup() { }

    public virtual void Ready() {
        UpdateTransform();
        UpdateDepth();
        UpdateTint();

        foreach (var id in _children.All) {
            var child = Core.Nodes[id];
            if (child.Persistent && !child.HasReadied || !child.HasReadied)
                Core.Nodes[id].Ready();
        }
        HasReadied = true;
    }

    public virtual void Update() {
        if (_resetting) {
            _resetting = false;
            ClearChildren();
            Setup();
            Ready();
            return;
        }
        if (!Processing) return;
        foreach (var child in _children.All)
            Core.Nodes[child].Update();
    }

    public virtual void Draw() {
        if (!Visible) return;
        foreach (var child in _children.All)
            Core.Nodes[child].Draw();
    }

    public virtual void DebugDraw() {
        foreach (var child in _children.All)
            Core.Nodes[child].DebugDraw();
    }

    public virtual void Destroy() {
        Active = false;
        if (_destroyed) return;
        foreach (var child in _children.All)
            Core.Nodes[child].Destroy();
        _parent?.Get()._children.Del(Id.Index);
        Core.LatestGeneration[Id.Index]++;
        Core.Nodes.Del(Id.Index);
        Core.NodesOfType[GetType()].Remove(this);
        _destroyed = true;
    }

    public void ResetChildren() {
        _resetting = true;
    }

    public void ClearChildren() {
        foreach (var child in Children)
            child.Destroy();
        _children.Clear();
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
        foreach (var child in Children) {
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
        if (Parent != null) {
            GlobalTransform = Transform * Parent.GlobalTransform;
            GlobalDepth = DepthRelativeToCamera + Parent.GlobalDepth;
        } else {
            GlobalTransform = Transform;
            GlobalDepth = DepthRelativeToCamera;
        }
        if (Active)
            UpdateBounds();
        foreach (var child in Children)
            child.UpdateTransform();
        _positionIsDirty = false;
        _scaleIsDirty = false;
        _rotationIsDirty = false;
    }

    protected void UpdateDepth() {
        GlobalDepth = Parent != null ? DepthRelativeToCamera + Parent.GlobalDepth : DepthRelativeToCamera;
        foreach (var child in Children) {
            child.UpdateDepth();
        }
    }

    protected void UpdateTint() {
        if (Parent != null) {
            var vec = Tint.ToVector4() * Parent.GlobalTint.ToVector4();
            GlobalTint = new Color(vec.X, vec.Y, vec.Z, vec.W);
        }
        else GlobalTint = _tint;
        foreach (var child in Children)
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