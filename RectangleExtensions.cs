using System.Drawing;

namespace BigBoyEngine;

public static class RectangleExtensions {
    public static Microsoft.Xna.Framework.Rectangle ToRect(this RectangleF rect)
        => new Microsoft.Xna.Framework.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
    public static RectangleF ToRectF(this Microsoft.Xna.Framework.Rectangle rect)
        => new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
}