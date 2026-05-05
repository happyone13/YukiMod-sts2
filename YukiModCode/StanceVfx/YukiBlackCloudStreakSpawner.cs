using System.Collections.Generic;
using Godot;

namespace YukiMod.YukiModCode.StanceVfx;

[GlobalClass]
public partial class YukiBlackCloudStreakSpawner : Node2D
{
    private const float SpawnInterval = 0.04f;
    private const float MinLifetime = 1.1f;
    private const float MaxLifetime = 1.7f;
    private const float VfxScale = 0.9f;

    private readonly List<StreakData> _streaks = [];
    private CanvasItemMaterial _material = null!;
    private RandomNumberGenerator _rng = null!;
    private float _scale;
    private float _spawnTimer;
    private bool _stopping;
    private Texture2D _texture = null!;

    public override void _Ready()
    {
        _scale = VfxScale;
        Position *= _scale;

        _rng = new RandomNumberGenerator();
        _rng.Randomize();
        _material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _texture = ResourceLoader.Load<Texture2D>("res://YukiMod/images/vfx/frost_streak.png", cacheMode: ResourceLoader.CacheMode.Ignore)!;
        if (_texture == null)
        {
            QueueFree();
            return;
        }

        for (var i = 0; i < 15; i++)
        {
            var preAge = _rng.RandfRange(0f, MaxLifetime);
            SpawnStreak(preAge);
        }
    }

    public void StopSpawning()
    {
        _stopping = true;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;

        if (!_stopping)
        {
            _spawnTimer += dt;
            while (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer -= SpawnInterval;
                SpawnStreak(0f);
            }
        }
        else if (_streaks.Count == 0)
        {
            QueueFree();
            return;
        }

        for (var i = _streaks.Count - 1; i >= 0; i--)
        {
            var streak = _streaks[i];
            streak.Age += dt;

            if (streak.Age >= streak.Lifetime)
            {
                streak.Sprite.QueueFree();
                _streaks.RemoveAt(i);
                continue;
            }

            streak.VelocityX += streak.AccelX * dt;
            streak.VelocityY += streak.AccelY * dt;

            streak.VelocityX = Mathf.Min(streak.VelocityX, -20f * _scale);
            streak.VelocityY = Mathf.Max(streak.VelocityY, 0f);

            streak.Sprite.Position += new Vector2(streak.VelocityX * dt, streak.VelocityY * dt);
            streak.Sprite.Rotation = Mathf.Atan2(streak.VelocityY, streak.VelocityX) + Mathf.Pi * 0.5f;

            var progress = streak.Age / streak.Lifetime;

            float yScaleMultiplier;
            if (progress < 0.4f)
                yScaleMultiplier = 0.5f + progress * 2.5f;
            else if (progress < 0.7f)
                yScaleMultiplier = 1.5f;
            else
                yScaleMultiplier = 1.5f - (progress - 0.7f) * 3.3f;
            yScaleMultiplier = Mathf.Max(yScaleMultiplier, 0.2f);
            streak.Sprite.Scale = new Vector2(streak.BaseScale * 0.375f, streak.BaseScale * 1.76f * yScaleMultiplier);

            float alpha;
            if (progress < 0.3f)
                alpha = progress / 0.3f;
            else if (progress < 0.8f)
                alpha = 1.0f;
            else
                alpha = (1f - progress) / 0.2f;
            alpha = alpha * alpha * (3f - 2f * alpha) * 0.75f;

            streak.Sprite.Modulate = new Color(streak.BaseColor.R, streak.BaseColor.G, streak.BaseColor.B, alpha);
            _streaks[i] = streak;
        }
    }

    private void SpawnStreak(float initialAge)
    {
        var sprite = new Sprite2D();
        sprite.Texture = _texture;
        sprite.Material = _material;

        var scale = _rng.RandfRange(0.5f, 1.0f) * _scale;

        sprite.Position = new Vector2(
            _rng.RandfRange(250f, 438f) * _scale * (scale / _scale),
            _rng.RandfRange(-325f, -125f) * _scale
        );

        var velocityX = _rng.RandfRange(-413f, -288f) * _scale;
        var velocityY = _rng.RandfRange(225f, 275f) * _scale;

        var accelX = 75f * (scale / _scale) * _scale;
        var accelY = -106f * _scale;

        var red = _rng.RandfRange(0.55f, 0.72f);
        var green = _rng.RandfRange(0.22f, 0.38f);
        var blue = _rng.RandfRange(0.92f, 1.0f);
        var baseColor = new Color(red, green, blue, 0f);
        sprite.Modulate = baseColor;

        sprite.Scale = new Vector2(scale * 0.375f, scale * 1.76f);
        sprite.Rotation = Mathf.Atan2(velocityY, velocityX) + Mathf.Pi * 0.5f;

        var behind = _rng.Randf() < 0.2f + (scale / _scale - 0.5f);
        sprite.ZIndex = behind ? -1 : 1;

        AddChild(sprite);

        _streaks.Add(new StreakData
        {
            Sprite = sprite,
            Age = initialAge,
            Lifetime = _rng.RandfRange(MinLifetime, MaxLifetime),
            VelocityX = velocityX,
            VelocityY = velocityY,
            AccelX = accelX,
            AccelY = accelY,
            BaseColor = new Color(red, green, blue),
            BaseScale = scale
        });
    }

    private struct StreakData
    {
        public Sprite2D Sprite;
        public float Age;
        public float Lifetime;
        public float VelocityX;
        public float VelocityY;
        public float AccelX;
        public float AccelY;
        public Color BaseColor;
        public float BaseScale;
    }
}
