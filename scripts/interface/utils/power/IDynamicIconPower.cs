namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 为 Power 提供正负 Amount 的两套图标路径。
/// 实现此接口后，系统会自动在 Amount 越过 0 时切换图标，
/// 无需手动注册 provider 或编写 Harmony 补丁。
///
/// 用法：在 Power 类上实现此接口，提供两个图标路径即可：
/// <code>
/// public sealed class MyPower : ModPowerTemplate, IPowerCategorizable, IDynamicIconPower
/// {
///     public string PositiveIconPath => "res://images/benefit/Combat_Icon_Buff_xxx.png";
///     public string NegativeIconPath => "res://images/adverse/Combat_Icon_Debuff_xxx.png";
/// }
/// </code>
/// </summary>
public interface IDynamicIconPower
{
    /// <summary>Amount &gt;= 0 时使用的图标路径（增益图标）</summary>
    string PositiveIconPath { get; }

    /// <summary>Amount &lt; 0 时使用的图标路径（减益图标）</summary>
    string NegativeIconPath { get; }
}
