using HyacineCore.Server.Kcp;
using HyacineCore.Server.Proto;

namespace HyacineCore.Server.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.StartChallengeCsReq)]
public class HandlerStartChallengeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StartChallengeCsReq.Parser.ParseFrom(data);

        ChallengeStoryBuffInfo? storyBuffInfo = null;
        if (req.StageInfo is { StoryInfo: not null })
            storyBuffInfo = req.StageInfo.StoryInfo;

        ChallengeBossBuffInfo? bossBuffInfo = null;
        if (req.StageInfo != null && req.StageInfo.BossInfo != null) bossBuffInfo = req.StageInfo.BossInfo;

        if (req.FirstLineup.Count > 0)
            connection.Player!.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge,
                req.FirstLineup.Select(x => (int)x).ToList());

        if (req.SecondLineup.Count > 0)
            connection.Player!.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge2,
                req.SecondLineup.Select(x => (int)x).ToList());

        await connection.Player!.ChallengeManager!.StartChallenge((int)req.ChallengeId, storyBuffInfo, bossBuffInfo);
    }
}
