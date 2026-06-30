using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.addons.mega_text;
using ArchiveLibrary.Scripts.Utils;
using MegaCrit.Sts2.Core.Unlocks;

namespace ArchiveLibrary.Scripts.Relic;

/// <summary>
/// 先古遗物图鉴分类注册中心。自动扫描所有 Mod 中继承 <see cref="ModAncientEventTemplate"/>
/// 的先古事件并注册到遗物图鉴，Mod 制作者无需手动调用 Register。
/// </summary>
public static class AncientCollectionRegistry
{
    private static List<AncientEntry>? _entries;
    private static bool _scanned;

    private static void EnsureScanned()
    {
        if (_scanned) return;
        _scanned = true;
        _entries = new List<AncientEntry>();

        // 从 ModelDb 获取所有继承 AncientEventModel 的类型
        var ancientTypes = ModelDb.AllAbstractModelSubtypes
            .Where(t => t.IsSubclassOf(typeof(AncientEventModel)) && !t.IsAbstract)
            .ToList();

        int registered = 0;
        foreach (var type in ancientTypes)
        {
            string? ns = type.Namespace;
            if (ns != null && ns.StartsWith("MegaCrit")) continue; // 跳过原版

            // 通过反射调用 ModelDb.AncientEvent<T>() 获取实例
            var method = typeof(ModelDb).GetMethod("AncientEvent", Type.EmptyTypes)
                ?.MakeGenericMethod(type);
            if (method?.Invoke(null, null) is not AncientEventModel ancient) continue;

            _entries.Add(new AncientEntry(() => ancient, "ANCIENT_SUBCATEGORY"));
            registered++;
            LibraryLogger.Info($"先古遗物图鉴: 已注册 {type.Name}");
        }
        LibraryLogger.Info($"先古遗物图鉴: 扫描完成（共{ancientTypes.Count}个先古类型，已注册{registered}个 Mod 先古）");
    }

    /// <summary>注册先古遗物分类（一般不需手动调用，自动扫描已覆盖）。</summary>
    public static void Register<T>(string locKey) where T : AncientEventModel
    {
        _entries ??= new List<AncientEntry>();
        _entries.Add(new AncientEntry(() => ModelDb.AncientEvent<T>(), locKey));
    }

    internal static IReadOnlyList<AncientEntry> Entries
    {
        get { EnsureScanned(); return _entries ??= new(); }
    }

    public readonly record struct AncientEntry(Func<AncientEventModel?> GetAncient, string LocKey);
}

/// <summary>
/// 在遗物图鉴中为先古事件添加子分类。
/// </summary>
[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
public static class AncientCollectionPatch
{
    static void Postfix(NRelicCollectionCategory __instance, RelicRarity relicRarity, NRelicCollection collection,
        HashSet<RelicModel> seenRelics, UnlockState unlockState, HashSet<RelicModel> allUnlockedRelics)
    {
        if (relicRarity != RelicRarity.Ancient) return;

        foreach (var entry in AncientCollectionRegistry.Entries)
        {
            var transporter = entry.GetAncient();
            if (transporter == null) continue;
            AddSubcategory(__instance, collection, transporter, seenRelics, allUnlockedRelics, entry.LocKey);
        }
    }

    private static void AddSubcategory(NRelicCollectionCategory parent, NRelicCollection collection,
        AncientEventModel ancient, HashSet<RelicModel> seenRelics, HashSet<RelicModel> allUnlockedRelics, string locKey)
    {
        var subCatField = typeof(NRelicCollectionCategory).GetField("_subCategories", BindingFlags.NonPublic | BindingFlags.Instance);
        if (subCatField?.GetValue(parent) is not List<NRelicCollectionCategory> subCats) return;

        bool exists = subCats.Any(sub =>
        {
            var label = sub.GetNodeOrNull<MegaRichTextLabel>("%Header");
            return label != null && label.Text.Contains(ancient.Title.GetFormattedText());
        });
        if (exists) return;

        var createMethod = typeof(NRelicCollectionCategory).GetMethod("CreateForSubcategory", BindingFlags.NonPublic | BindingFlags.Instance);
        if (createMethod?.Invoke(parent, null) is not NRelicCollectionCategory subCat) return;

        var header = parent.GetNode<MegaRichTextLabel>("%Header");
        parent.AddChildSafely(subCat);
        parent.MoveChild(subCat, header.GetIndex() + 1);

        var cacheField = typeof(NRelicCollectionCategory).GetField("_relicModelCache", BindingFlags.NonPublic | BindingFlags.Static);
        var cache = cacheField?.GetValue(null) as List<RelicModel>;
        if (cache == null) return;

        var relics = ancient.AllPossibleOptions
            .Select(o => o.Relic?.CanonicalInstance)
            .OfType<RelicModel>()
            .Intersect(cache)
            .OrderBy(r => r.Title.GetFormattedText(), StringComparer.Create(LocManager.Instance.CultureInfo, true))
            .ToList();

        if (relics.Count == 0) return;

        var stats = SaveManager.Instance.Progress.AncientStats;
        bool hasSeen = stats.ContainsKey(ancient.Id) || relics.Any(r => seenRelics.Contains(r));

        var headerText = new LocString("relic_collection", locKey);
        var unknown = new LocString("relic_collection", "UNKNOWN_ANCIENT");
        headerText.Add("Ancient", hasSeen ? ancient.Title : unknown);

        var loadMethod = typeof(NRelicCollectionCategory).GetMethod("LoadSubcategory", BindingFlags.NonPublic | BindingFlags.Instance);
        loadMethod?.Invoke(subCat, [collection, headerText, relics, seenRelics, allUnlockedRelics]);

        var iconMethod = typeof(NRelicCollectionCategory).GetMethod("LoadIcon", BindingFlags.NonPublic | BindingFlags.Instance);
        if (iconMethod != null && ancient.RunHistoryIcon != null)
            iconMethod.Invoke(subCat, [ancient.RunHistoryIcon]);

        subCats.Add(subCat);
    }
}
