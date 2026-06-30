using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using ArchiveLibrary.Scripts.Utils;
using ArchiveLibrary.Scripts.Audio;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 拦截 <see cref="NAudioManager.PlayOneShot"/> 事件，
/// 通过 <see cref="CharacterSoundRegistry"/> 查找本地音效替换。
/// </summary>
[HarmonyPatch]
public static class CharacterSoundPatch
{
    static MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(NAudioManager), "PlayOneShot",
            [typeof(string), typeof(Dictionary<string, float>), typeof(float)]);
    }

    static bool Prefix(string path, Dictionary<string, float> parameters, float volume)
    {
        if (string.IsNullOrEmpty(path)) return true;

        if (CharacterSoundRegistry.TryGetSound(path, out var soundPath, out var volOffset))
        {
            FmodSoundHelper.Play(soundPath, volume + volOffset);
            LibraryLogger.Debug($"音效替换: {path} → {soundPath}");
            return false;
        }
        return true;
    }
}
