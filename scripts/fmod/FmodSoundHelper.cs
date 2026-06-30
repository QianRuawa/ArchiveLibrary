using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Audio;

/// <summary>
/// 音效播放辅助。通过 <see cref="CharacterSoundRegistry"/> 注册后自动替换 FMOD 事件。
/// 也可直接调用 <see cref="Play"/> 播放本地音频文件。
/// </summary>
public static class FmodSoundHelper
{
    private static readonly Dictionary<string, AudioStream> _streamCache = new();
    private static readonly List<AudioStreamPlayer> _activePlayers = new();

    /// <summary>播放本地音效文件。</summary>
    public static void Play(string soundPath, float volumeDb = 0f)
    {
        if (string.IsNullOrEmpty(soundPath)) return;

        var stream = GetOrLoadStream(soundPath);
        if (stream == null)
        {
            LibraryLogger.Warn($"音效加载失败: {soundPath}");
            return;
        }
        PlayStream(stream, volumeDb);
    }

    /// <summary>停止所有通过本 Helper 播放的音效。</summary>
    public static void StopAll()
    {
        foreach (var player in _activePlayers)
        {
            if (player != null && player.IsInsideTree())
            {
                player.Stop();
                player.QueueFree();
            }
        }
        _activePlayers.Clear();
    }

    public static void Stop() => StopAll();

    private static AudioStream? GetOrLoadStream(string path)
    {
        if (_streamCache.TryGetValue(path, out var cached)) return cached;
        if (!ResourceLoader.Exists(path))
        {
            LibraryLogger.Warn($"音效资源不存在: {path}");
            return null;
        }
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream != null) _streamCache[path] = stream;
        return stream;
    }

    private static void PlayStream(AudioStream stream, float volumeDb)
    {
        Node parent = NCombatRoom.Instance;
        if (parent == null)
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            parent = tree?.Root;
        }
        if (parent == null) return;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "Master",
            VolumeDb = volumeDb,
            Autoplay = false
        };

        parent.AddChild(player);
        player.Play();
        _activePlayers.Add(player);

        player.Finished += () =>
        {
            _activePlayers.Remove(player);
            if (player.IsInsideTree())
                player.QueueFree();
        };
    }
}
