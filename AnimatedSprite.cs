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

public class AnimatedSprite : Node {
    private string _currentAnim;
    private int _currentFrame;
    private float _timeLeft, _speed = .1f;
    public Texture2D Texture;
    private readonly Dictionary<string, Animation> _anims = new();

    public Color Color = Color.White;
    public Vector2 Origin;
    public SpriteEffects Flip = SpriteEffects.None;
    public bool Centered = true, Playing;

    public Animation Animation => _anims[_currentAnim];

    public AnimatedSprite(Texture2D texture, bool playing = true, params Animation[] anims) {
        Texture = texture;
        Playing = playing;
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
        if (Playing && (_timeLeft -= Time.Delta) <= 0) {
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
            Color,
            GlobalRotation,
            origin,
            GlobalScale,
            Flip,
            GlobalDepth);
        base.Draw();
    }
}