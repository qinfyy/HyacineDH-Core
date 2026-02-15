using HyacineCore.Server.GameServer.Game.Challenge.Definitions;
using HyacineCore.Server.GameServer.Game.Player;
using HyacineCore.Server.Kcp;
using HyacineCore.Server.Proto;

namespace HyacineCore.Server.GameServer.Server.Packet.Send.Challenge;

public class PacketStartChallengeScRsp : BasePacket
{
    public PacketStartChallengeScRsp(uint Retcode) : base(CmdIds.StartChallengeScRsp)
    {
        var proto = new StartChallengeScRsp
        {
            Retcode = Retcode
        };

        SetData(proto);
    }

    public PacketStartChallengeScRsp(PlayerInstance player, bool sendScene = true) : base(CmdIds.StartChallengeScRsp)
    {
        StartChallengeScRsp proto = new();

        if (player.ChallengeManager!.ChallengeInstance != null)
        {
            if (player.ChallengeManager.ChallengeInstance is BaseLegacyChallengeInstance inst)
            {
                proto.CurChallenge = inst.ToProto();
                proto.StageInfo = inst.ToStageInfo();

                // Only boss challenge should carry boss node payload.
                if (inst is not null && inst.Config.IsBoss())
                {
                    proto.StageInfo ??= new ChallengeStageInfo();
                    proto.StageInfo.BossInfo ??= new ChallengeBossInfo();
                    proto.StageInfo.BossInfo.FirstNode ??= new ChallengeBossSingleNodeInfo();
                    proto.StageInfo.BossInfo.SecondNode ??= new ChallengeBossSingleNodeInfo();
                }
            }

            proto.LineupList.Add(player.LineupManager!.GetExtraLineup(ExtraLineupType.LineupChallenge)!.ToProto());
            if (player.ChallengeManager.ChallengeInstance is BaseLegacyChallengeInstance inst2 &&
                inst2.Config.StageNum >= 2)
                proto.LineupList.Add(player.LineupManager!.GetExtraLineup(ExtraLineupType.LineupChallenge2)!.ToProto());
            if (sendScene) proto.Scene = player.SceneInstance!.ToProto();
        }
        else
        {
            proto.Retcode = 1;
        }

        SetData(proto);
    }
}
