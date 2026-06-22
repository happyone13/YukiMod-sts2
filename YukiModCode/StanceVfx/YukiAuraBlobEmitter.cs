using Godot;

namespace YukiMod.YukiModCode.StanceVfx;

[GlobalClass]
public partial class YukiAuraBlobEmitter : Node2D
{
    private const float VfxScale = 0.9f;
    private const string BlurTexturePath = "res://YukiMod/images/vfx/big_blur.png";

    private static Texture2D? s_blurTexture;
    private static CanvasItemMaterial? s_additiveMaterial;
    private static Gradient? s_colorRamp;

    [Export] public Color BlobColor { get; set; } = new(0.18f, 0.08f, 0.30f);

    public override void _Ready()
    {
        var s = VfxScale;
        Position *= s;

        var cpu = new CpuParticles2D();
        cpu.Texture = GetBlurTexture();
        cpu.Material = GetAdditiveMaterial();

        cpu.Amount = 6;
        cpu.Lifetime = 2.0f;
        cpu.Preprocess = 2.0f;
        cpu.Direction = new Vector2(0, -1);
        cpu.Spread = 180f;
        cpu.Gravity = Vector2.Zero;
        cpu.InitialVelocityMin = 0f;
        cpu.InitialVelocityMax = 10f * s;
        cpu.ScaleAmountMin = 3.44f * s;
        cpu.ScaleAmountMax = 4.5f * s;
        cpu.AngleMin = 0f;
        cpu.AngleMax = 360f;
        cpu.AngularVelocityMin = -20f;
        cpu.AngularVelocityMax = 20f;
        cpu.Color = BlobColor;
        cpu.ColorRamp = GetColorRamp();
        cpu.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
        cpu.EmissionRectExtents = new Vector2(38f * s, 63f * s);
        cpu.Emitting = true;

        AddChild(cpu);
    }

    private static Texture2D? GetBlurTexture()
    {
        return s_blurTexture ??= ResourceLoader.Load<Texture2D>(BlurTexturePath, cacheMode: ResourceLoader.CacheMode.Reuse);
    }

    private static CanvasItemMaterial GetAdditiveMaterial()
    {
        return s_additiveMaterial ??= new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    }

    private static Gradient GetColorRamp()
    {
        if (s_colorRamp != null)
        {
            return s_colorRamp;
        }

        var ramp = new Gradient();
        ramp.Offsets = [0f, 0.3f, 0.5f, 0.7f, 1f];
        ramp.Colors =
        [
            new Color(1, 1, 1, 0f),
            new Color(1, 1, 1, 0.6f),
            new Color(1, 1, 1, 0.6f),
            new Color(1, 1, 1, 0.3f),
            new Color(1, 1, 1, 0f)
        ];
        s_colorRamp = ramp;
        return ramp;
    }
}
