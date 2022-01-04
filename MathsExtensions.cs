using System;
using Microsoft.Xna.Framework;

namespace BigBoyEngine;

public static class MathsExtensions {
    public static float LerpAngle(float source, float destination, float amount) {
        const float TwoPI = (float)(MathF.PI * 2);
        float c, d;
        if (destination < source) {
            c = destination + TwoPI;
            d = c - source > source - destination ? MathHelper.Lerp(source, destination, amount) : MathHelper.Lerp(source, c, amount);
        } else if (destination > source) {
            c = destination - TwoPI;
            d = destination - source > source - c ? MathHelper.Lerp(source, c, amount) : MathHelper.Lerp(source, destination, amount);
        } else
            return source;
        return MathHelper.WrapAngle(d);
    }
}