namespace BigBoyEngine;

public class YSort : Node {
    public override void Update() {
        base.Update();
        foreach (var child in Children)
            child.Depth = (child.Position.Y + 500000) * .000001f;
    }
}