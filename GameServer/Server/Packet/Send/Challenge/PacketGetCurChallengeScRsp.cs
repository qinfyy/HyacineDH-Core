using HyacineCore.Server.GameServer.Game.Challenge.Definitions;
using HyacineCore.Server.GameServer.Game.Player;
using HyacineCore.Server.Kcp;
using HyacineCore.Server.Proto;

namespace HyacineCore.Server.GameServer.Server.Packet.Send.Challenge;

public class PacketGetCurChallengeScRsp : BasePacket
{
    public PacketGetCurChallengeScRsp(PlayerInstance player) : base(CmdIds.GetCurChallengeScRsp)
    {
        var proto = new GetCurChallengeScRsp();

        if (player.ChallengeManager!.ChallengeInstance is BaseLegacyChallengeInstance inst)
        {
            proto.CurChallenge = inst.ToProto();
            player.LineupManager!.SetExtraLineup((ExtraLineupType)inst.GetCurrentExtraLineupType(), notify: false).GetAwaiter().GetResult();
            var proto1 = player.LineupManager?.GetExtraLineup(ExtraLineupType.LineupChallenge)?.ToProto();
            if (proto1 != null)
                proto.LineupList.Add(proto1);

            var proto2 = player.LineupManager?.GetExtraLineup(ExtraLineupType.LineupChallenge2)?.ToProto();
            if (proto2 != null)
                proto.LineupList.Add(proto2);
        }
        else
        {
            proto.Retcode = 0;
        }

        SetData(proto);
    }
}
