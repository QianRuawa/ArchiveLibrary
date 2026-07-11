using System.Reflection;
using ArchiveLibrary.Scripts.Act;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

/// <summary>自定义层级音乐播放器（独立于 FmodMusicHelper，互不干扰）。</summary>
internal static class ActMusicPlayer
{
    private static AudioStreamPlayer? _player;
    private static Tween? _tween;
    private static Godot.Timer? _loopTimer;
    private static double _loopStart;
    private static double _loopEnd;
    private static float _currentVolumeDb;
    private static bool _isPaused;

    private static string? _currentPath;

    public static void Play(string path, double loopStart, double loopEnd, float fadeIn, float volumeDb)
    {
        // 正在播放同一首则不处理
        if (_currentPath == path && _player != null && GodotObject.IsInstanceValid(_player) && _player.Playing)
        { return; }
        // 暂停中则恢复
        if (_isPaused && _currentPath == path && _player != null && GodotObject.IsInstanceValid(_player))
        { Resume(fadeIn); return; }

        Stop();

        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null) return;

        // 挂载到 Root 节点，避免场景切换时被销毁
        var root = Engine.GetMainLoop() is SceneTree st ? st.Root : null;
        if (root == null) return;

        _player = new AudioStreamPlayer { Stream = stream, Bus = "Music", VolumeDb = -60f };
        root.AddChild(_player);
        _loopStart = loopStart;
        _loopEnd = loopEnd;
        _currentPath = path;
        _currentVolumeDb = volumeDb;
        _isPaused = false;

        if (loopEnd > 0) StartLoopTimer();
        _player.Finished += OnFinished;
        _player.Play();

        _tween = root.CreateTween();
        _tween.TweenProperty(_player, "volume_db", volumeDb, fadeIn)
              .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
    }

    /// <summary>暂停（淡出后暂停，不销毁，下次 Play 会从暂停处恢复）。</summary>
    public static void Pause(float fadeOut = 0.5f)
    {
        if (_isPaused) return;
        if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
        _tween?.Kill();
        _loopTimer?.Stop();

        _isPaused = true;
        var t = _player.GetTree()?.CreateTween();
        if (t != null && fadeOut > 0f)
        {
            t.TweenProperty(_player, "volume_db", -80f, fadeOut)
             .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
            t.Finished += () => { if (GodotObject.IsInstanceValid(_player)) _player.Stop(); };
        }
        else
        {
            _player.Stop();
        }
    }

    /// <summary>恢复播放（淡入）。</summary>
    private static void Resume(float fadeIn = 0.5f)
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player)) { _isPaused = false; return; }
        _isPaused = false;
        _player.VolumeDb = -80f;
        _player.Play();
        _tween = _player.GetTree()?.CreateTween();
        _tween?.TweenProperty(_player, "volume_db", _currentVolumeDb, fadeIn)
              .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
        if (_loopEnd > 0) StartLoopTimer();
    }

    public static void Stop(float fadeOut = 1.5f)
    {
        _loopTimer?.Stop(); _loopTimer?.QueueFree(); _loopTimer = null;
        _isPaused = false; _currentPath = null;
        if (_player == null || !GodotObject.IsInstanceValid(_player)) { _player = null; return; }
        _tween?.Kill();
        var p = _player;
        var t = p.GetTree()?.CreateTween();
        if (t != null && fadeOut > 0f)
        {
            t.TweenProperty(p, "volume_db", -80f, fadeOut)
             .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
            t.Finished += () => { Cleanup(p); };
        }
        else { Cleanup(p); }
    }

    private static void Cleanup(AudioStreamPlayer p)
    {
        if (GodotObject.IsInstanceValid(p)) { p.Stop(); p.QueueFree(); }
        if (_player == p) { _player = null; _tween = null; _currentPath = null; }
    }

    private static void StartLoopTimer()
    {
        _loopTimer = new Godot.Timer { WaitTime = 0.25f, Autostart = true };
        _loopTimer.Timeout += () =>
        {
            if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
            if (_player.GetPlaybackPosition() >= _loopEnd - 0.05f)
                _player.Seek((float)_loopStart);
        };
        _player?.AddChild(_loopTimer);
    }

    private static void OnFinished()
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
        if (_loopStart > 0 && _loopEnd <= 0) _player.Seek((float)_loopStart);
    }
}

/// <summary>进入战斗时暂停自定义音乐（非战斗房间不影响）。</summary>
[HarmonyPatch(typeof(RunManager), "EnterRoom")]
public static class ActEnterRoomMusicPausePatch
{
    static void Postfix(RunManager __instance)
    {
        var s = typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance) as RunState;
        if (s?.Act is not ModActTemplate { CustomMusic: not null }) return;
        if (s.CurrentRoom is CombatRoom)
            ActMusicPlayer.Pause(0.5f);
    }
}

/// <summary>回到地图时恢复自定义音乐。</summary>
[HarmonyPatch(typeof(NMapScreen), "SetTravelEnabled")]
public static class ActMapMusicPlayPatch
{
    static void Postfix(NMapScreen __instance, bool enabled)
    {
        if (!enabled) return;
        var s = typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(RunManager.Instance) as RunState;
        if (s?.Act is ModActTemplate a && a.CustomMusic is { } m)
            ActMusicPlayer.Play(m.MusicPath, m.LoopStart, m.LoopEnd, m.FadeIn, m.VolumeDb);
    }
}

    [HarmonyPatch(typeof(NRunMusicController), "UpdateMusic")]
    public static class ActCustomMusicPatch
    {
        static void Postfix()
        {
            var s = typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(RunManager.Instance) as RunState;
            if (s?.Act is ModActTemplate a && a.CustomMusic is { } m)
                ActMusicPlayer.Play(m.MusicPath, m.LoopStart, m.LoopEnd, m.FadeIn, m.VolumeDb);
            else
                ActMusicPlayer.Stop(0.5f);
        }
    }

[HarmonyPatch(typeof(NRunMusicController), "StopMusic")]
public static class ActStopMusicPatch
{
    static void Postfix()
    {
        var s = typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(RunManager.Instance) as RunState;
        ActMusicPlayer.Stop(s?.Act is ModActTemplate a && a.CustomMusic is { } m ? m.FadeOut : 1.5f);
    }
}

/// <summary>修复：确保 ModActTemplate 的 ModifyUnknownMapPointRoomTypes 在 Hook 中生效。</summary>
[HarmonyPatch(typeof(Hook), "ModifyUnknownMapPointRoomTypes")]
public static class ActModifyUnknownPatch
{
    static void Postfix(IRunState runState, ref IReadOnlySet<RoomType> __result)
    {
        if (runState.Act is ModActTemplate act)
            __result = act.ModifyUnknownMapPointRoomTypes(__result);
    }
}
