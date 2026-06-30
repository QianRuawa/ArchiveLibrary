using Godot;
using MegaCrit.Sts2.Core.Models;

namespace ArchiveLibrary.Scripts.Utils;

/// <summary>
/// 自动解析遗物图标路径的基类。
/// 图标放在 res://images/relics/tres/{ClassName}.tres，outline 加 _outline 后缀，
/// 大图放在 res://images/relics/{ClassName}.png。
/// 找不到时使用占位图，不会炸。
/// </summary>
public abstract class AutoRelic : RelicModel
{
    private const string PlaceholderTres = "res://images/Combat_Icon_Error.png";
    private const string PlaceholderPng = "res://images/Combat_Icon_Error.png";

    private static string GetValidPath(string path, string placeholder)
    {
        return ResourceLoader.Exists(path) ? path : placeholder;
    }

    private string ClassName => GetType().Name;

    public override string PackedIconPath
    {
        get
        {
            string expected = $"res://images/relics/tres/{ClassName}.tres";
            return GetValidPath(expected, PlaceholderTres);
        }
    }

    protected override string PackedIconOutlinePath
    {
        get
        {
            string expected = $"res://images/relics/tres/{ClassName}_outline.tres";
            return GetValidPath(expected, PlaceholderTres);
        }
    }

    protected override string BigIconPath
    {
        get
        {
            string expected = $"res://images/relics/{ClassName}.png";
            return GetValidPath(expected, PlaceholderPng);
        }
    }
}
