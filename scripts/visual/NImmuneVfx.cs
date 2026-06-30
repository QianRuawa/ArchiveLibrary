using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 免疫弹出文字，仿原版 NDamageNumVfx 风格。
/// 文本通过 LocString 支持多语言，默认 key = "IMMUNE_TEXT"。
/// </summary>
public partial class NImmuneVfx : Node2D
{
    private static readonly Vector2 _gravity = new Vector2(0f, 2000f);
    private static readonly Vector2 _positionOffset = new Vector2(0f, -100f);

    private string _text;
    private Vector2 _globalSpawnPosition;
    private Vector2 _velocity;
    private Label _label;
    private Tween? _tween;

    public static NImmuneVfx? Display(Creature target, string? customText = null)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(target);
        if (node == null) return null;

        var vfx = new NImmuneVfx
        {
            _text = customText ?? EntiyArchiveLibrary.UI.GetText("IMMUNE_TEXT"),
            _globalSpawnPosition = node.VfxSpawnPosition + _positionOffset
                + new Vector2(Rng.Chaotic.NextFloat(-10f, 10f), 0f)
        };

        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(vfx);
        return vfx;
    }

    public override void _Ready()
    {
        var font = ResourceLoader.Load<Font>("res://themes/kreon_regular_glyph_space_two.tres");

        _label = new Label
        {
            Text = _text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(-128f, -128f),
            Size = new Vector2(256f, 256f)
        };
        _label.AddThemeColorOverride("font_color", new Color(1, 0.9647f, 0.8863f, 1));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.1255f));
        _label.AddThemeColorOverride("font_outline_color", new Color(0.21f, 0.2027f, 0.1869f, 1));
        _label.AddThemeConstantOverride("shadow_offset_x", 5);
        _label.AddThemeConstantOverride("shadow_offset_y", 4);
        _label.AddThemeConstantOverride("outline_size", 20);
        _label.AddThemeConstantOverride("shadow_outline_size", 20);
        if (font != null)
            _label.AddThemeFontOverride("font", font);
        _label.AddThemeFontSizeOverride("font_size", 64);
        AddChild(_label);

        GlobalPosition = _globalSpawnPosition;
        _velocity = new Vector2(0f, Rng.Chaotic.NextFloat(-500f, -400f));
        Scale = Vector2.One * Rng.Chaotic.NextFloat(1.2f, 1.3f);
        RotationDegrees = Rng.Chaotic.NextFloat(-5f, 5f);

        TaskHelper.RunSafely(AnimVfx());
    }

    private async Task AnimVfx()
    {
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(this, "modulate", StsColors.cream, 0.5)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _tween.TweenProperty(this, "modulate:a", 0f, 1.5f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tween.TweenProperty(this, "scale", Vector2.One, 1.0f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad)
            .From(Vector2.One * 2.5f);
        await _tween.AwaitFinished(this);
        QueueFree();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Position += _velocity * dt;
        _velocity += _gravity * dt;
    }

    public override void _ExitTree()
    {
        _tween?.Kill();
    }
}
