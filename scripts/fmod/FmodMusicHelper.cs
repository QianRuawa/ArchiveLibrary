using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Audio;

/// <summary>
/// BGM 播放辅助工具，支持淡入淡出和分段循环。
/// 所有方法均为静态，全局只同时播放一首 BGM。
/// </summary>
public static class FmodMusicHelper
{
    private static AudioStreamPlayer _currentPlayer;
    private static Tween _currentTween;
    private static string _currentPath;
    private static bool _isStopping;
    private static double _loopStartTime;
    private static double _loopEndTime;
    private static Godot.Timer _loopTimer;

    /// <summary>检查 AudioStreamPlayer 是否有效。</summary>
    private static bool IsValid(AudioStreamPlayer player)
    {
        if (player == null) return false;
        try
        {
            return player.GetInstanceId() != 0 && !player.IsQueuedForDeletion();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>播放 BGM，淡入淡出。</summary>
    /// <param name="musicPath">音频资源路径（如 res://music/boss.ogg）</param>
    /// <param name="fadeInDuration">淡入时长（秒）</param>
    /// <param name="volumeDb">目标音量（dB）</param>
    public static void Play(string musicPath, float fadeInDuration = 1.5f, float volumeDb = 0f)
    {
        PlayInternal(musicPath, 0f, 0f, fadeInDuration, volumeDb);
    }

    /// <summary>播放带分段循环的 BGM。</summary>
    /// <param name="musicPath">音频资源路径</param>
    /// <param name="loopStart">循环起始时间（秒）</param>
    /// <param name="loopEnd">循环结束时间（秒），超过此时间后跳回 loopStart</param>
    /// <param name="fadeInDuration">淡入时长（秒）</param>
    /// <param name="volumeDb">目标音量（dB）</param>
    public static void PlayWithLoop(string musicPath, double loopStart, double loopEnd = 0f, float fadeInDuration = 1.5f, float volumeDb = 0f)
    {
        PlayInternal(musicPath, loopStart, loopEnd, fadeInDuration, volumeDb);
    }

    /// <summary>内部播放逻辑。</summary>
    private static void PlayInternal(string musicPath, double loopStart, double loopEnd, float fadeInDuration, float volumeDb)
    {
        if (IsValid(_currentPlayer))
            StopImmediate();

        var audioStream = ResourceLoader.Load<AudioStream>(musicPath);
        if (audioStream == null)
        {
            LibraryLogger.Error($"FmodMusicHelper: 无法加载音频文件 {musicPath}");
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = audioStream,
            Bus = "Music",
            VolumeDb = -60f,
            Autoplay = false
        };

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null)
        {
            LibraryLogger.Error("FmodMusicHelper: 无法获取 NCombatRoom 实例");
            return;
        }

        combatRoom.AddChild(player);

        _loopStartTime = loopStart;
        _loopEndTime = loopEnd;
        _currentPath = musicPath;

        if (loopEnd > 0)
        {
            LibraryLogger.Info($"FmodMusicHelper: 设置分段循环 [{loopStart}s → {loopEnd}s]");
            StartLoopTimer(player);
        }
        else if (loopStart > 0)
        {
            LibraryLogger.Info($"FmodMusicHelper: 设置循环，播放到末尾后从 {loopStart}s 重新播放");
        }

        player.Finished += OnPlayerFinished;
        player.Play();

        var tween = combatRoom.CreateTween();
        tween.TweenProperty(player, "volume_db", volumeDb, fadeInDuration)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Quad);

        _currentPlayer = player;
        _currentTween = tween;
        _isStopping = false;
    }

    /// <summary>启动分段循环定时器，定时检查播放位置并跳转。</summary>
    private static void StartLoopTimer(AudioStreamPlayer player)
    {
        if (_loopTimer != null && GodotObject.IsInstanceValid(_loopTimer))
        {
            _loopTimer.Stop();
            _loopTimer.QueueFree();
        }
        _loopTimer = null;

        var newTimer = new Godot.Timer
        {
            WaitTime = 0.25f,
            Autostart = true
        };
        newTimer.Timeout += () => OnLoopTimerTimeout(player);
        player.AddChild(newTimer);
        _loopTimer = newTimer;
    }

    /// <summary>定时器回调：检查播放位置是否超过循环结束点，是则跳回循环起点。</summary>
    private static void OnLoopTimerTimeout(AudioStreamPlayer player)
    {
        if (!IsValid(player) || _loopEndTime <= 0) return;

        double currentPos = player.GetPlaybackPosition();
        if (currentPos >= _loopEndTime - 0.05f)
        {
            player.Seek((float)_loopStartTime);
            LibraryLogger.Info($"FmodMusicHelper: 循环跳转，从 {_loopStartTime}s 重新播放 (当前 {currentPos:F2}s)");
        }
    }

    /// <summary>播放完成回调：处理无分段循环的循环播放。</summary>
    private static void OnPlayerFinished()
    {
        if (!IsValid(_currentPlayer)) return;

        if (_loopStartTime > 0 && _loopEndTime <= 0)
        {
            _currentPlayer.Seek((float)_loopStartTime);
            LibraryLogger.Info($"FmodMusicHelper: 循环（文件末尾），从 {_loopStartTime}s 重新播放");
        }
    }

    /// <summary>淡出停止 BGM。</summary>
    /// <param name="fadeOutDuration">淡出时长（秒）</param>
    public static void Stop(float fadeOutDuration = 1.5f)
    {
        if (_isStopping) return;
        if (!IsValid(_currentPlayer))
        {
            Cleanup();
            return;
        }

        _isStopping = true;
        _currentTween?.Kill();

        var tree = _currentPlayer.GetTree();
        if (tree == null)
        {
            Cleanup();
            return;
        }

        var tween = _currentPlayer.GetTree().CreateTween();
        tween.TweenProperty(_currentPlayer, "volume_db", -80f, fadeOutDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Quad);
        tween.Finished += () =>
        {
            if (IsValid(_currentPlayer))
            {
                _currentPlayer?.Stop();
                _currentPlayer?.QueueFree();
            }
            Cleanup();
        };
        _currentTween = tween;
    }

    /// <summary>立即停止 BGM（无淡出）。</summary>
    public static void StopImmediate()
    {
        if (_loopTimer != null && GodotObject.IsInstanceValid(_loopTimer))
        {
            _loopTimer.Stop();
            _loopTimer.QueueFree();
        }
        _loopTimer = null;

        _currentTween?.Kill();

        if (IsValid(_currentPlayer))
        {
            _currentPlayer.Finished -= OnPlayerFinished;
            _currentPlayer.Stop();
            _currentPlayer.QueueFree();
        }
        Cleanup();
    }

    /// <summary>清理所有状态。</summary>
    private static void Cleanup()
    {
        if (_loopTimer != null && GodotObject.IsInstanceValid(_loopTimer))
        {
            _loopTimer.Stop();
            _loopTimer.QueueFree();
        }
        _loopTimer = null;
        _currentPlayer = null;
        _currentTween = null;
        _currentPath = null;
        _isStopping = false;
        _loopStartTime = 0f;
        _loopEndTime = 0f;
    }

    /// <summary>运行时更新循环段范围。</summary>
    /// <param name="newLoopStart">新的循环起始时间（秒）</param>
    /// <param name="newLoopEnd">新的循环结束时间（秒）</param>
    public static void UpdateLoopSegment(double newLoopStart, double newLoopEnd)
    {
        if (!IsValid(_currentPlayer)) return;
        _loopStartTime = newLoopStart;
        _loopEndTime = newLoopEnd;
        if (_loopEndTime > 0)
            StartLoopTimer(_currentPlayer);
        else
        {
            if (_loopTimer != null && GodotObject.IsInstanceValid(_loopTimer))
            {
                _loopTimer.Stop();
                _loopTimer.QueueFree();
            }
            _loopTimer = null;
        }
        LibraryLogger.Info($"FmodMusicHelper: 更新循环段为 [{newLoopStart}s → {newLoopEnd}s]");
    }
}
