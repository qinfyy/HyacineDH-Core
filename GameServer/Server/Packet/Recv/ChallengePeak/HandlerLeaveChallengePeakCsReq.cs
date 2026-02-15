using HyacineCore.Server.Kcp;
using HyacineCore.Server.Proto;
using HyacineCore.Server.Util;
using HyacineCore.Server.GameServer.Server.Packet.Send.Challenge;
using HyacineCore.Server.GameServer.Server.Packet.Send.Lineup;
using static HyacineCore.Server.GameServer.Plugin.Event.PluginEvent;

namespace HyacineCore.Server.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.LeaveChallengePeakCsReq)]
public class HandlerLeaveChallengePeakCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;

        // TODO: check for plane type
        if (player.SceneInstance != null)
        {
            await player.ForceQuitBattle();

            // Reset challenge lineups
            player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge, []);
            player.LineupManager.SetExtraLineup(ExtraLineupType.LineupChallenge2, []);

            InvokeOnPlayerQuitChallenge(player, player.ChallengeManager!.ChallengeInstance);

            player.ChallengeManager!.ChallengeInstance = null;
            player.ChallengeManager!.ClearInstance();

            // Force exit challenge lineup mode.
            await player.LineupManager.SetExtraLineup(ExtraLineupType.LineupNone, notify: false);
            await player.SendPacket(new PacketChallengeLineupNotify(ExtraLineupType.LineupNone));

            // Ensure current lineup points to a usable normal lineup.
            if (player.LineupManager.GetCurLineup()?.IsExtraLineup() == true ||
                player.LineupManager.GetCurLineup()?.BaseAvatars?.Count == 0)
            {
                var allLineups = player.LineupManager.GetAllLineup();
                var normalIndex = allLineups.FindIndex(x => x.LineupType == 0 && x.BaseAvatars is { Count: > 0 });
                if (normalIndex >= 0) await player.LineupManager.SetCurLineup(normalIndex);
            }

            if (player.LineupManager.GetCurLineup() != null)
                await player.SendPacket(new PacketSyncLineupNotify(player.LineupManager.GetCurLineup()!));

            // Heal avatars (temporary solution)
            foreach (var avatar in player.LineupManager.GetCurLineup()!.AvatarData!.FormalAvatars)
                avatar.CurrentHp = 10000;

            var leaveEntryId = GameConstants.CHALLENGE_PEAK_ENTRANCE;
            if (player.SceneInstance.LeaveEntryId != 0) leaveEntryId = player.SceneInstance.LeaveEntryId;
            await player.EnterScene(leaveEntryId, 0, true);
        }

        await connection.SendPacket(CmdIds.LeaveChallengePeakScRsp);
    }
}
