using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Dcrew;

namespace BigBoyEngine;

public struct Animation {
    public readonly string Name;
    public readonly Rectangle[] Frames;
    public bool Looping;

    public Animation(string name, Rectangle[] frames, bool looping = true) {
        Name = name;
        Frames = frames;
        Looping = looping;
    }

    public Animation(string name, Rectangle region, int hAmount, int vAmount, bool looping = true) {
        Name = name;
        Looping = looping;
        Frames = new Rectangle[hAmount * vAmount];

        var w = region.Width / hAmount;
        var h = region.Height / vAmount;

        for (int i = 0; i < hAmount; i++) {
            for (int j = 0; j < vAmount; j++) {
                Frames[i * vAmount + j] = new Rectangle(w * i + region.X, h * j + region.Y, w, h);
            }
        }
    }
}

public class AnimatedSprite : Spatial {
    private string _currentAnim;
    private int _currentFrame;
    private float _timeLeft, _speed = .1f;
    public Texture2D Texture;
    private readonly Dictionary<string, Animation> _anims = new();

    public Vector2 Origin;
    public SpriteEffects Flip = SpriteEffects.None;
    public bool Centered = true, Playing = true;

    public Animation Animation => _anims[_currentAnim];

    public AnimatedSprite(Texture2D texture, params Animation[] anims) {
        Texture = texture;
        foreach (var anim in anims)
            _anims.Add(anim.Name, anim);
        _currentAnim = anims[0].Name;
    }

    public void Play(string anim, float speed = .1f) {
        if (anim == _currentAnim)
            return;
        Playing = true;
        _currentAnim = anim;
        _speed = speed;
        _timeLeft = _speed;
        _currentFrame = 0;
    }

    public void Stop() {
        Playing = false;
        _timeLeft = 0;
        _currentFrame = 0;
    }

    public override void Update() {
        if (Animation.Frames.Length > 1 && Playing && (_timeLeft -= Time.Delta) <= 0) {
            _timeLeft += _speed;
            _currentFrame++;
            if (_currentFrame >= Animation.Frames.Length) {
                if (Animation.Looping)
                    _currentFrame = 0;
                Playing = Animation.Looping;
            }
        }
        base.Update();
    }

    public override void Draw() {
        var origin = Origin;
        var frame = Animation.Frames[_currentFrame];

        if (Centered)
            origin += new Vector2(frame.Width * .5f, frame.Height * .5f);

        Core.SpriteBatch.Draw(
            Texture,
            GlobalPosition,
            Animation.Frames[_currentFrame],
            GlobalTint,
            GlobalRotation,
            origin,
            GlobalScale,
            Flip,
            GlobalDepth);
        base.Draw();
    }

    public override Rectangle GetAABB() {
        if (Texture == null) return new Rectangle((int)GlobalPosition.X, (int)GlobalPosition.Y, 0, 0);
        var width = MathF.Round(Animation.Frames[_currentFrame].Width * GlobalScale.X);
        var height = MathF.Round(Animation.Frames[_currentFrame].Height * GlobalScale.Y);
        return new Rectangle(
            (int)(GlobalPosition.X - width * .5f),
            (int)(GlobalPosition.Y - height * .5f),
            (int)width,
            (int)height
        );
    }
}