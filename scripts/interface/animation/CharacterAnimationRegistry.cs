using ArchiveLibrary.Scripts.Audio;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Animation;

public static class CharacterAnimationRegistry
{
    private static readonly Dictionary<string, ICharacterAnimationProvider> _providers = new();

    public static void Register(string characterId, ICharacterAnimationProvider provider)
    {
        if (string.IsNullOrEmpty(characterId)) return;
        _providers[characterId] = provider;
        LibraryLogger.Debug($"已注册角色动画: {characterId}");
    }

    /// <summary>同时注册动画和音效映射。</summary>
    public static void Register(string characterId, ICharacterAnimationProvider provider,
        Dictionary<string, (string Path, float VolumeOffset)>? soundMap)
    {
        Register(characterId, provider);
        if (soundMap != null && soundMap.Count > 0)
            CharacterSoundRegistry.Register(characterId, soundMap);
    }

    public static ICharacterAnimationProvider? GetProvider(string characterId)
    {
        _providers.TryGetValue(characterId, out var provider);
        return provider;
    }
}
