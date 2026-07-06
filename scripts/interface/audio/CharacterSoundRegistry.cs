namespace ArchiveLibrary.Scripts.Audio;

/// <summary>
/// 角色音效替换注册中心。Mod 制作者在角色静态构造器中注册音效映射，
/// 游戏中的 FMOD 事件会被自动替换为本地音频文件。
/// </summary>
public static class CharacterSoundRegistry
{
    private static readonly Dictionary<string, Dictionary<string, (string Path, float VolumeOffset)>> _characterSounds = new();

    /// <summary>注册角色的音效映射。</summary>
    public static void Register(string characterId, Dictionary<string, (string Path, float VolumeOffset)> eventMap)
    {
        _characterSounds[characterId] = eventMap;
    }

    /// <summary>尝试获取角色对应的本地音效路径。</summary>
    internal static bool TryGetSound(string eventPath, out string soundPath, out float volume)
    {
        soundPath = "";
        volume = 0f;
        foreach (var entry in _characterSounds.Values)
        {
            if (entry.TryGetValue(eventPath, out var result))
            {
                soundPath = result.Path;
                volume = result.VolumeOffset;
                return true;
            }
        }
        return false;
    }
}
