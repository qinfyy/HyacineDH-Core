using HyacineCore.Server.Data;
using HyacineCore.Server.Data.Excel;
using HyacineCore.Server.Database;
using HyacineCore.Server.Database.Challenge;
using HyacineCore.Server.Database.Friend;
using HyacineCore.Server.Database.Inventory;
using HyacineCore.Server.GameServer.Game.Challenge.Definitions;
using HyacineCore.Server.GameServer.Game.Challenge.Instances;
using HyacineCore.Server.GameServer.Game.Player;
using HyacineCore.Server.GameServer.Server.Packet.Send.Challenge;
using HyacineCore.Server.GameServer.Server.Packet.Send.Scene;
using HyacineCore.Server.Proto;
using HyacineCore.Server.Proto.ServerSide;
using HyacineCore.Server.Util;
using Google.Protobuf;
using static HyacineCore.Server.GameServer.Plugin.Event.PluginEvent;

namespace HyacineCore.Server.GameServer.Game.Challenge;

public class ChallengeManager(PlayerInstance player) : BasePlayerManager(player)
{
    #region Properties

    public BaseChallengeInstance? ChallengeInstance { get; set; }

    public ChallengeData ChallengeData { get; } =
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<ChallengeData>(player.Uid);

    #endregion

    #region Management

    public async ValueTask StartChallenge(int challengeId, ChallengeStoryBuffInfo? storyBuffs,
        ChallengeBossBuffInfo? bossBuffs)
    {
        if (!GameData.ChallengeConfigData.TryGetValue(challengeId, out var excel))
        {
            await Player.SendPacket(new PacketStartChallengeScRsp((uint)Retcode.RetChallengeNotExist));
            return;
        }

        if (excel.StageNum > 0 && !PrepareChallengeLineup(ExtraLineupType.LineupChallenge))
        {
            await Player.SendPacket(new PacketStartChallengeScRsp((uint)Retcode.RetChallengeLineupEmpty));
            return;
        }

        if (excel.StageNum >= 2 && !PrepareChallengeLineup(ExtraLineupType.LineupChallenge2))
        {
            await Player.SendPacket(new PacketStartChallengeScRsp((uint)Retcode.RetChallengeLineupEmpty));
            return;
        }

        var instance = CreateLegacyInstance(excel, 1, ExtraLineupType.LineupChallenge);

        ChallengeInstance = instance;
        await Player.LineupManager!.SetExtraLineup((ExtraLineupType)instance.GetCurrentExtraLineupType(), notify: false);

        if (!await TryEnterChallengeScene(excel.MapEntranceID, false))
        {
            await Player.LineupManager.SetExtraLineup(ExtraLineupType.LineupNone, false);
            ChallengeInstance = null;
            await Player.SendPacket(new PacketStartChallengeScRsp((uint)Retcode.RetChallengeNotExist));
            return;
        }

        // For challenge boss (Apocalyptic Shadow), mirror the phase-switch logic:
        // unload the other node group (if any), then load the current node group.
        if (instance is ChallengeBossInstance boss)
        {
            if (boss.Config.MazeGroupID2 != 0 && boss.Config.MazeGroupID2 != boss.Config.MazeGroupID1)
                await Player.SceneInstance!.EntityLoader!.UnloadGroup(boss.Config.MazeGroupID2);

            if (boss.Config.MazeGroupID1 != 0)
                await Player.SceneInstance!.EntityLoader!.LoadGroup(boss.Config.MazeGroupID1, sendPacket: true);
        }

        // Ensure the client switches to challenge lineup UI immediately (LC behavior).
        await Player.SendPacket(new PacketChallengeLineupNotify((ExtraLineupType)instance.GetCurrentExtraLineupType()));
        await Player.SceneInstance!.SyncLineup();

        instance.SetStartPos(Player.Data.Pos!);
        instance.SetStartRot(Player.Data.Rot!);
        instance.SetSavedMp(Player.LineupManager.GetCurLineup()!.Mp);

        if (excel.IsStory() && storyBuffs != null)
        {
            instance.Data.Story.Buffs.Add(storyBuffs.BuffOne);
            instance.Data.Story.Buffs.Add(storyBuffs.BuffTwo);
        }

        if (bossBuffs != null)
        {
            instance.Data.Boss.Buffs.Add(bossBuffs.BuffOne);
            instance.Data.Boss.Buffs.Add(bossBuffs.BuffTwo);
        }

        InvokeOnPlayerEnterChallenge(Player, instance);
        // Order for client compatibility:
        // 1) StartChallengeScRsp
        // 2) EnterSceneByServerScNotify
        await Player.SendPacket(new PacketStartChallengeScRsp(Player, sendScene: false));
        await Player.SendPacket(new PacketEnterSceneByServerScNotify(Player.SceneInstance!));
        SaveInstance(instance);
    }

    public async ValueTask StartPartialChallenge(int challengeId, uint buffId, bool isFirstHalf)
    {
        if (!GameData.ChallengeConfigData.TryGetValue(challengeId, out var excel))
        {
            await Player.SendPacket(new PacketStartPartialChallengeScRsp((uint)Retcode.RetChallengeNotExist));
            return;
        }

        var lineupType = isFirstHalf ? ExtraLineupType.LineupChallenge : ExtraLineupType.LineupChallenge2;
        if (!PrepareChallengeLineup(lineupType))
        {
            await Player.SendPacket(new PacketStartPartialChallengeScRsp((uint)Retcode.RetChallengeLineupEmpty));
            return;
        }

        var currentStage = isFirstHalf ? 1 : 2;
        var instance = CreateLegacyInstance(excel, currentStage, lineupType);
        instance.IsPartialChallenge = true;
        SetPartialBuff(instance, buffId, isFirstHalf);

        ChallengeInstance = instance;
        await Player.LineupManager!.SetExtraLineup(lineupType, notify: false);

        var mapEntranceId = isFirstHalf
            ? excel.MapEntranceID
            : excel.MapEntranceID2 != 0 ? excel.MapEntranceID2 : excel.MapEntranceID;
        if (!await TryEnterChallengeScene(mapEntranceId, false))
        {
            await Player.LineupManager.SetExtraLineup(ExtraLineupType.LineupNone, false);
            ChallengeInstance = null;
            await Player.SendPacket(new PacketStartPartialChallengeScRsp((uint)Retcode.RetChallengeNotExist));
            return;
        }

        var groupId = isFirstHalf ? excel.MazeGroupID1 : excel.MazeGroupID2;
        var subGroupId = isFirstHalf ? excel.MazeGroupID2 : excel.MazeGroupID1;

        if (subGroupId != 0 && subGroupId != groupId)
            await Player.SceneInstance!.EntityLoader!.UnloadGroup(subGroupId);

        if (groupId != 0)
            await Player.SceneInstance!.EntityLoader!.LoadGroup(groupId, sendPacket: true);

        await Player.SendPacket(new PacketChallengeLineupNotify(lineupType));
        await Player.SceneInstance!.SyncLineup();

        instance.SetStartPos(Player.Data.Pos!);
        instance.SetStartRot(Player.Data.Rot!);
        instance.SetSavedMp(Player.LineupManager.GetCurLineup()!.Mp);

        InvokeOnPlayerEnterChallenge(Player, instance);
        await Player.SendPacket(new PacketStartPartialChallengeScRsp(Player));
        SaveInstance(instance);
    }

    public void AddHistory(int challengeId, int stars, int score)
    {
        if (stars <= 0) return;
        if (ChallengeInstance is BaseLegacyChallengeInstance { IsPartialChallenge: true }) return;
        if (!ChallengeData.History.ContainsKey(challengeId))
            ChallengeData.History[challengeId] = new ChallengeHistoryData(Player.Uid, challengeId);
        
        var info = ChallengeData.History[challengeId];
        info.SetStars(stars);
        info.Score = score;
    }

    public async ValueTask<List<TakenChallengeRewardInfo>?> TakeRewards(int groupId)
    {
        if (!GameData.ChallengeGroupData.TryGetValue(groupId, out var challengeGroup)) return null;
        if (!GameData.ChallengeRewardData.TryGetValue(challengeGroup.RewardLineGroupID, out var challengeRewardLine)) return null;

        var totalStars = 0;
        foreach (var ch in ChallengeData.History.Values)
        {
            if (ch.GroupId == 0)
            {
                if (!GameData.ChallengeConfigData.TryGetValue(ch.ChallengeId, out var challengeExcel)) continue;
                ch.GroupId = challengeExcel.GroupID;
            }
            if (ch.GroupId == groupId) totalStars += ch.GetTotalStars();
        }

        var rewardInfos = new List<TakenChallengeRewardInfo>();
        var data = new List<ItemData>();

        foreach (var challengeReward in challengeRewardLine)
        {
            if (totalStars < challengeReward.StarCount) continue;
            if (!ChallengeData.TakenRewards.ContainsKey(groupId))
                ChallengeData.TakenRewards[groupId] = new ChallengeGroupReward(Player.Uid, groupId);
            
            var reward = ChallengeData.TakenRewards[groupId];
            if (reward.HasTakenReward(challengeReward.StarCount)) continue;

            reward.SetTakenReward(challengeReward.StarCount);
            if (!GameData.RewardDataData.TryGetValue(challengeReward.RewardID, out var rewardExcel)) continue;

            var proto = new TakenChallengeRewardInfo
            {
                StarCount = (uint)challengeReward.StarCount,
                Reward = new ItemList()
            };

            foreach (var item in rewardExcel.GetItems())
            {
                var itemData = new ItemData { ItemId = item.Item1, Count = item.Item2 };
                proto.Reward.ItemList_.Add(itemData.ToProto());
                data.Add(itemData);
            }
            rewardInfos.Add(proto);
        }

        await Player.InventoryManager!.AddItems(data);
        return rewardInfos;
    }

    public void SaveInstance(BaseChallengeInstance instance)
    {
        ChallengeData.ChallengeInstance = Convert.ToBase64String(instance.Data.ToByteArray());
    }

    public void ClearInstance()
    {
        ChallengeData.ChallengeInstance = null;
        ChallengeInstance = null;
    }

    public void ResurrectInstance()
    {
        if (ChallengeData.ChallengeInstance == null) return;
        var protoByte = Convert.FromBase64String(ChallengeData.ChallengeInstance);
        var proto = ChallengeDataPb.Parser.ParseFrom(protoByte);

        if (proto != null)
            ChallengeInstance = proto.ChallengeTypeCase switch
            {
                ChallengeDataPb.ChallengeTypeOneofCase.Memory => new ChallengeMemoryInstance(Player, proto),
                ChallengeDataPb.ChallengeTypeOneofCase.Peak => new ChallengePeakInstance(Player, proto),
                ChallengeDataPb.ChallengeTypeOneofCase.Story => new ChallengeStoryInstance(Player, proto),
                ChallengeDataPb.ChallengeTypeOneofCase.Boss => new ChallengeBossInstance(Player, proto),
                _ => null
            };
    }

    public void SaveBattleRecord(BaseLegacyChallengeInstance inst)
    {
        if (inst.IsPartialChallenge) return;

        // 先尝试通过常规 Switch 处理已知类型
        switch (inst)
        {
            case ChallengeMemoryInstance memory:
            {
                Player.FriendRecordData!.ChallengeGroupStatistics.TryAdd((uint)memory.Config.GroupID, new ChallengeGroupStatisticsPb { GroupId = (uint)memory.Config.GroupID });
                var stats = Player.FriendRecordData.ChallengeGroupStatistics[(uint)memory.Config.GroupID];
                stats.MemoryGroupStatistics ??= [];

                var starCount = 0u;
                for (var i = 0; i < 3; i++) starCount += (memory.Data.Memory.Stars & (1 << i)) != 0 ? 1u : 0u;

                if (stats.MemoryGroupStatistics.GetValueOrDefault((uint)memory.Config.ID)?.Stars > starCount) return;

                var pb = new MemoryGroupStatisticsPb
                {
                    RoundCount = (uint)(memory.Config.ChallengeCountDown - memory.Data.Memory.RoundsLeft),
                    Stars = starCount,
                    RecordId = Player.FriendRecordData!.NextRecordId++,
                    Level = memory.Config.Floor
                };

                foreach (var type in new[] { ExtraLineupType.LineupChallenge, ExtraLineupType.LineupChallenge2 })
                {
                    if (type == ExtraLineupType.LineupChallenge2 && memory.Config.StageNum < 2) continue;
                    var lineup = Player.LineupManager!.GetExtraLineup(type);
                    if (lineup?.BaseAvatars == null) continue;

                    var lineupPb = new List<ChallengeAvatarInfoPb>();
                    uint idx = 0;
                    foreach (var avatar in lineup.BaseAvatars)
                    {
                        var formal = Player.AvatarManager!.GetFormalAvatar(avatar.BaseAvatarId);
                        if (formal == null) continue;
                        lineupPb.Add(new ChallengeAvatarInfoPb { Index = idx++, Id = (uint)formal.BaseAvatarId, AvatarType = AvatarType.AvatarFormalType, Level = (uint)formal.Level });
                    }
                    pb.Lineups.Add(lineupPb);
                }
                stats.MemoryGroupStatistics[(uint)memory.Config.ID] = pb;
                return; // 处理完毕退出
            }

            case ChallengeStoryInstance story:
            {
                Player.FriendRecordData!.ChallengeGroupStatistics.TryAdd((uint)story.Config.GroupID, new ChallengeGroupStatisticsPb { GroupId = (uint)story.Config.GroupID });
                var stats = Player.FriendRecordData.ChallengeGroupStatistics[(uint)story.Config.GroupID];
                stats.StoryGroupStatistics ??= [];

                var starCount = 0u;
                for (var i = 0; i < 3; i++) starCount += (story.Data.Story.Stars & (1 << i)) != 0 ? 1u : 0u;

                if (stats.StoryGroupStatistics.GetValueOrDefault((uint)story.Config.ID)?.Stars > starCount) return;

                var pb = new StoryGroupStatisticsPb
                {
                    Stars = starCount,
                    RecordId = Player.FriendRecordData!.NextRecordId++,
                    Level = story.Config.Floor,
                    BuffOne = story.Data.Story.Buffs.Count > 0 ? story.Data.Story.Buffs[0] : 0,
                    BuffTwo = story.Data.Story.Buffs.Count > 1 ? story.Data.Story.Buffs[1] : 0,
                    Score = (uint)story.GetTotalScore()
                };

                foreach (var type in new[] { ExtraLineupType.LineupChallenge, ExtraLineupType.LineupChallenge2 })
                {
                    if (type == ExtraLineupType.LineupChallenge2 && story.Config.StageNum < 2) continue;
                    var lineup = Player.LineupManager!.GetExtraLineup(type);
                    if (lineup?.BaseAvatars == null) continue;

                    var lineupPb = new List<ChallengeAvatarInfoPb>();
                    uint idx = 0;
                    foreach (var avatar in lineup.BaseAvatars)
                    {
                        var formal = Player.AvatarManager!.GetFormalAvatar(avatar.BaseAvatarId);
                        if (formal == null) continue;
                        lineupPb.Add(new ChallengeAvatarInfoPb { Index = idx++, Id = (uint)formal.BaseAvatarId, AvatarType = AvatarType.AvatarFormalType, Level = (uint)formal.Level });
                    }
                    pb.Lineups.Add(lineupPb);
                }
                stats.StoryGroupStatistics[(uint)story.Config.ID] = pb;
                return;
            }

            case ChallengeBossInstance boss:
            {
                Player.FriendRecordData!.ChallengeGroupStatistics.TryAdd((uint)boss.Config.GroupID, new ChallengeGroupStatisticsPb { GroupId = (uint)boss.Config.GroupID });
                var stats = Player.FriendRecordData.ChallengeGroupStatistics[(uint)boss.Config.GroupID];
                stats.BossGroupStatistics ??= [];

                var starCount = 0u;
                for (var i = 0; i < 3; i++) starCount += (boss.Data.Boss.Stars & (1 << i)) != 0 ? 1u : 0u;

                if (stats.BossGroupStatistics.GetValueOrDefault((uint)boss.Config.ID)?.Stars > starCount) return;

                var pb = new BossGroupStatisticsPb
                {
                    Stars = starCount,
                    RecordId = Player.FriendRecordData!.NextRecordId++,
                    Level = boss.Config.Floor,
                    BuffOne = boss.Data.Boss.Buffs.Count > 0 ? boss.Data.Boss.Buffs[0] : 0,
                    BuffTwo = boss.Data.Boss.Buffs.Count > 1 ? boss.Data.Boss.Buffs[1] : 0,
                    Score = (uint)boss.GetTotalScore()
                };

                foreach (var type in new[] { ExtraLineupType.LineupChallenge, ExtraLineupType.LineupChallenge2 })
                {
                    if (type == ExtraLineupType.LineupChallenge2 && boss.Config.StageNum < 2) continue;
                    var lineup = Player.LineupManager!.GetExtraLineup(type);
                    if (lineup?.BaseAvatars == null) continue;

                    var lineupPb = new List<ChallengeAvatarInfoPb>();
                    uint idx = 0;
                    foreach (var avatar in lineup.BaseAvatars)
                    {
                        var formal = Player.AvatarManager!.GetFormalAvatar(avatar.BaseAvatarId);
                        if (formal == null) continue;
                        lineupPb.Add(new ChallengeAvatarInfoPb { Index = idx++, Id = (uint)formal.BaseAvatarId, AvatarType = AvatarType.AvatarFormalType, Level = (uint)formal.Level });
                    }
                    pb.Lineups.Add(lineupPb);
                }
                stats.BossGroupStatistics[(uint)boss.Config.ID] = pb;
                return;
            }
        }

        // 处理特殊 PeakInstance 类型
        object obj = inst;
        if (obj.GetType().Name == "ChallengePeakInstance")
        {
            dynamic peak = obj;
            uint groupId = (uint)peak.Data.Peak.CurrentPeakGroupId;
            Player.FriendRecordData!.ChallengeGroupStatistics.TryAdd(groupId, new ChallengeGroupStatisticsPb { GroupId = groupId });
            var stats = Player.FriendRecordData.ChallengeGroupStatistics[groupId];
            stats.StoryGroupStatistics ??= [];

            uint levelId = (uint)peak.Data.Peak.CurrentPeakLevelId;
            uint starCount = (uint)peak.Data.Peak.Stars;

            if (stats.StoryGroupStatistics.GetValueOrDefault(levelId)?.Stars > starCount) return;

            var pb = new StoryGroupStatisticsPb
            {
                Stars = starCount,
                RecordId = Player.FriendRecordData!.NextRecordId++,
                Level = (uint)peak.Config.ID,
                BuffOne = peak.Data.Peak.Buffs.Count > 0 ? (uint)peak.Data.Peak.Buffs[0] : 0,
                Score = 0
            };

            foreach (var type in new[] { ExtraLineupType.LineupChallenge, ExtraLineupType.LineupChallenge2 })
            {
                var lineup = Player.LineupManager!.GetExtraLineup(type);
                if (lineup?.BaseAvatars == null) continue;

                var lineupPb = new List<ChallengeAvatarInfoPb>();
                uint index = 0;
                foreach (var avatar in lineup.BaseAvatars)
                {
                    var formal = Player.AvatarManager!.GetFormalAvatar(avatar.BaseAvatarId);
                    if (formal == null) continue;
                    lineupPb.Add(new ChallengeAvatarInfoPb { Index = index++, Id = (uint)formal.BaseAvatarId, AvatarType = AvatarType.AvatarFormalType, Level = (uint)formal.Level });
                }
                if (lineupPb.Count > 0) pb.Lineups.Add(lineupPb);
            }
            stats.StoryGroupStatistics[levelId] = pb;
        }

    }

    private bool PrepareChallengeLineup(ExtraLineupType lineupType)
    {
        var lineup = Player.LineupManager!.GetExtraLineup(lineupType);
        if (lineup == null) return false;

        Player.LineupManager.SanitizeLineup(lineup);
        var avatars = Player.LineupManager.GetAvatarsFromTeam((int)lineupType + 10);
        if (avatars.Count == 0) return false;

        foreach (var avatar in avatars)
        {
            avatar.AvatarInfo.SetCurHp(10000, true);
            avatar.AvatarInfo.SetCurSp(5000, true);
        }

        lineup.Mp = Player.LineupManager.GetMaxMp();
        return true;
    }

    private BaseLegacyChallengeInstance CreateLegacyInstance(ChallengeConfigExcel excel, int currentStage,
        ExtraLineupType lineupType)
    {
        var data = new ChallengeDataPb();
        var currentLineupType = (ChallengeLineupTypePb)lineupType;

        if (excel.IsBoss())
        {
            data.Boss = new ChallengeBossDataPb
            {
                ChallengeMazeId = (uint)excel.ID,
                CurStatus = 1,
                CurrentStage = currentStage,
                CurrentExtraLineup = currentLineupType
            };
            return new ChallengeBossInstance(Player, data);
        }

        if (excel.IsStory())
        {
            data.Story = new ChallengeStoryDataPb
            {
                ChallengeMazeId = (uint)excel.ID,
                CurStatus = 1,
                CurrentStage = currentStage,
                CurrentExtraLineup = currentLineupType
            };
            return new ChallengeStoryInstance(Player, data);
        }

        data.Memory = new ChallengeMemoryDataPb
        {
            ChallengeMazeId = (uint)excel.ID,
            CurStatus = 1,
            CurrentStage = currentStage,
            CurrentExtraLineup = currentLineupType,
            RoundsLeft = (uint)excel.ChallengeCountDown
        };
        return new ChallengeMemoryInstance(Player, data);
    }

    private static void SetPartialBuff(BaseLegacyChallengeInstance instance, uint buffId, bool isFirstHalf)
    {
        if (buffId == 0) return;

        if (instance.Data.Story != null)
        {
            instance.Data.Story.Buffs.Clear();
            if (!isFirstHalf) instance.Data.Story.Buffs.Add(0);
            instance.Data.Story.Buffs.Add(buffId);
            return;
        }

        if (instance.Data.Boss != null)
        {
            instance.Data.Boss.Buffs.Clear();
            if (!isFirstHalf) instance.Data.Boss.Buffs.Add(0);
            instance.Data.Boss.Buffs.Add(buffId);
        }
    }

    private async ValueTask<bool> TryEnterChallengeScene(int mapEntranceId, bool sendPacket)
    {
        if (mapEntranceId <= 0 || !GameData.MapEntranceData.ContainsKey(mapEntranceId))
            return false;

        try
        {
            var changed = await Player.EnterScene(mapEntranceId, 0, sendPacket);
            return changed || Player.Data.EntryId == mapEntranceId;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
