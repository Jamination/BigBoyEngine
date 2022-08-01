using System.Drawing;

namespace BigBoyEngine;

public class Control : Node {
    private bool _parentIsControl;
    public RectangleF Bounds;

    public Control(float x = 0, float y = 0, float width = 1, float height = 1) {
        Bounds = new RectangleF(x, y, width, height);
    }

    public override void Ready() {
        base.Ready();
        _parentIsControl = Parent is Control;
    }

    public RectangleF GetGlobalBounds() {
        var parent = Parent;
        if (parent is Spatial spatial) {
            if (_parentIsControl) {
                var control = parent as Control;
                return new RectangleF(
                    control.Bounds.X + (.5f + (Bounds.X * .5f) * control.Bounds.Width),
                    control.Bounds.Y + (.5f + (Bounds.Y * .5f) * control.Bounds.Height),
                    Bounds.Width * control.Bounds.Width,
                    Bounds.Height * control.Bounds.Height
                );
            }

            return new RectangleF(
                spatial.Position.X + Bounds.X * Core.GDM.PreferredBackBufferWidth,
                spatial.Position.Y + Bounds.Y * Core.GDM.PreferredBackBufferHeight,
                spatial.Scale.X * Bounds.Width * Core.GDM.PreferredBackBufferWidth,
                spatial.Scale.Y * Bounds.Height * Core.GDM.PreferredBackBufferHeight
            );
        }
        return Bounds;
    }
}