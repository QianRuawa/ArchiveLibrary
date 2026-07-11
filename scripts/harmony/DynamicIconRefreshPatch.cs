using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 通用补丁：任何实现了 <see cref="IDynamicIconPower"/> 的 Power 在 Amount 变化时，
/// 强制刷新 NPower 的小图标和大图标（用于闪光粒子）。
/// NPower 的图标在初始化时缓存，Amount 变化后不会自动更新，
/// 导致图标和闪光均停留在旧状态。
/// 此补丁在 RefreshAmount（由 DisplayAmountChanged 触发）后重新设置图标及闪光纹理，
/// 配合 <see cref="ExternalAssetOverrideRegistry"/> 注册的纹理 provider 实现动态切换。
/// </summary>
[HarmonyPatch(typeof(NPower), "RefreshAmount")]
public static class DynamicIconRefreshPatch
{
    public static void Postfix(NPower __instance)
    {
        try
        {
            var model = __instance.Model;
            if (model is IDynamicIconPower)
            {
                __instance.GetNode<TextureRect>("%Icon").Texture = model.Icon;
                __instance.GetNode<CpuParticles2D>("%PowerFlash").Texture = model.BigIcon;
            }
        }
        catch { } // Model 尚未设置时跳过
    }
}
