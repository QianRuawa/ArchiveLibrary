using ArchiveLibrary.Scripts.Utils;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ArchiveLibrary.Scripts.ConsoleCommands;

/// <summary>
/// 档案库测试命令：输入 "altest" 看是否能正常注册。
/// </summary>
public class AlTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "altest";
    public override string Args => "";
    public override string Description => "档案库测试命令";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;
    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        return base.GetArgumentCompletions(player, args);
    }

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        LibraryLogger.Info("altest 命令执行成功！档案库控制台已加载。");
        return new CmdResult(success: true, "档案库控制台运行正常！");
    }

}
