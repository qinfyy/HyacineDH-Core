using HyacineCore.Server.GameServer.Game.Challenge.Instances;
using HyacineCore.Server.GameServer.Server.Packet.Send.Challenge;
using HyacineCore.Server.Kcp;
using HyacineCore.Server.Proto;

namespace HyacineCore.Server.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.EnterChallengeNextPhaseCsReq)]
public class HandlerEnterChallengeNextPhaseCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var challenge = connection.Player!.ChallengeManager?.ChallengeInstance;
        if (challenge == null)
        {
            await connection.SendPacket(new PacketEnterChallengeNextPhaseScRsp(Retcode.RetChallengeNotDoing));
            return;
        }

        if (challenge is ChallengeBossInstance boss)
        {
            var ok = await boss.NextPhase();
            if (!ok)
            {
                await connection.SendPacket(new PacketEnterChallengeNextPhaseScRsp(Retcode.RetChallengeNotDoing));
                return;
            }

            await connection.SendPacket(new PacketEnterChallengeNextPhaseScRsp(connection.Player));
            return;
        }

        // MOC/PF switch phase silently by server; this request path is AS-only.
        await connection.SendPacket(new PacketEnterChallengeNextPhaseScRsp(Retcode.RetChallengeNotDoing));
    }
}
