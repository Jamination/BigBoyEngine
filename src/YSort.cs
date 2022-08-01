namespace BigBoyEngine;

public class YSort : Spatial {
    public override void Update() {
        base.Update();
        foreach (var child in SpatialChildren)
            child.Depth = (child.Position.Y + 500000) * .000001f;
    }
}