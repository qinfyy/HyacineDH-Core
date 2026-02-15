using HyacineCore.Server.GameServer.Game.Challenge.Instances;
using HyacineCore.Server.Kcp;
using HyacineCore.Server.Proto;

namespace HyacineCore.Server.GameServer.Server.Packet.Send.Challenge;

public class PacketChallengeBossPhaseSettleNotify : BasePacket
{
    public PacketChallengeBossPhaseSettleNotify(ChallengeBossInstance challenge, BattleTargetList? targetLists = null,
        bool isReward = true) :
        base(CmdIds
            .ChallengeBossPhaseSettleNotify)
    {
        var proto = new ChallengeBossPhaseSettleNotify
        {
            ChallengeId = (uint)challenge.Config.ID,
            IsWin = challenge.IsWin,
            ChallengeScore = challenge.Data.Boss.ScoreStage1,
            ScoreTwo = challenge.Data.Boss.ScoreStage2,
            Star = challenge.Data.Boss.Stars,
            Phase = (uint)challenge.Data.Boss.CurrentStage,
            IsReward = isReward
        };

        proto.BattleTargetList.AddRange(targetLists?.BattleTargetList_ ?? []);

        SetData(proto);
    }
}
