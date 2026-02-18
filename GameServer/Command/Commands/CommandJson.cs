using System.Text.Json;
using System.Text.Json.Serialization;
using HyacineCore.Server.Data;
using HyacineCore.Server.Database;
using HyacineCore.Server.Database.Avatar;
using HyacineCore.Server.Database.Inventory;
using HyacineCore.Server.Enums.Item;
using HyacineCore.Server.GameServer.Server.Packet.Send.PlayerSync;
using HyacineCore.Server.Internationalization;
using HyacineCore.Server.Util;

namespace HyacineCore.Server.Command.Command.Cmd;

[CommandInfo("json", "Game.Command.Json.Desc", "Game.Command.Json.Usage")]
public class CommandJson : ICommand
{
    private const string JsonDirectoryRelativePath = "Config/Json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static DirectoryInfo GetJsonDirectory(bool createIfMissing = false)
    {
        var dir = new DirectoryInfo(Path.GetFullPath(JsonDirectoryRelativePath));
        if (createIfMissing && !dir.Exists)
            dir.Create();
        return dir;
    }

    [CommandDefault]
    public async ValueTask Import(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var input = (arg.Raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            await ShowFileList(arg);
            return;
        }

        if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            var (changedAvatars, removedItems) = await ClearRelicAndEquipment(player);
            if (changedAvatars.Count > 0)
                await player.SendPacket(new PacketPlayerSyncScNotify(changedAvatars));
            if (removedItems.Count > 0)
                await player.SendPacket(new PacketPlayerSyncScNotify(removedItems));

            DatabaseHelper.ToSaveUidList.SafeAdd(player.Uid);
            await arg.SendMsg(I18NManager.Translate("Game.Command.Json.ClearInventory"));
            return;
        }

        var selectedPath = ResolveInputPath(input, out var pathError);
        if (selectedPath == null)
        {
            if (!string.IsNullOrWhiteSpace(pathError))
                await arg.SendMsg(pathError);
            return;
        }

        if (!File.Exists(selectedPath))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Json.FileNotFound", selectedPath));
            return;
        }

        FreeSrData? data;
        try
        {
            var json = await File.ReadAllTextAsync(selectedPath);
            data = JsonSerializer.Deserialize<FreeSrData>(json, JsonOptions);
        }
        catch (Exception e)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Json.ReadOrParseFailed", e.Message));
            return;
        }

        if (data == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Json.InvalidJsonContent"));
            return;
        }

        var (clearedAvatars, clearedItems) = await ClearRelicAndEquipment(player);
        if (clearedAvatars.Count > 0)
            await player.SendPacket(new PacketPlayerSyncScNotify(clearedAvatars));
        if (clearedItems.Count > 0)
            await player.SendPacket(new PacketPlayerSyncScNotify(clearedItems));

        var avatarChanged = await ImportAvatars(player, data, arg);
        var importedItems = await ImportRelicsAndLightcones(player, data, avatarChanged);

        if (importedItems.Count > 0)
            await player.SendPacket(new PacketPlayerSyncScNotify(importedItems));
        if (avatarChanged.Count > 0)
            await player.SendPacket(new PacketPlayerSyncScNotify(avatarChanged));

        DatabaseHelper.ToSaveUidList.SafeAdd(player.Uid);

        await arg.SendMsg(I18NManager.Translate(
            "Game.Command.Json.ImportSummary",
            Path.GetFileName(selectedPath),
            (data.Avatars?.Count ?? 0).ToString(),
            (data.Relics?.Count ?? 0).ToString(),
            (data.Lightcones?.Count ?? 0).ToString()));
    }

    private static string? ResolveInputPath(string input, out string? error)
    {
        error = null;
        input = input.Trim();
        if (input.Length >= 2 && input.StartsWith('"') && input.EndsWith('"'))
            input = input[1..^1];

        if (int.TryParse(input, out var choice))
        {
            var files = GetJsonFiles().OrderBy(f => f.LastWriteTimeUtc).ToList();
            if (files.Count == 0)
            {
                error = I18NManager.Translate("Game.Command.Json.NoFileFoundWithHint");
                return null;
            }

            if (choice < 1 || choice > files.Count)
            {
                error = I18NManager.Translate("Game.Command.Json.InvalidChoice", files.Count.ToString());
                return null;
            }

            return files[choice - 1].FullName;
        }

        var looksLikePath = input.Contains('/') || input.Contains('\\') || Path.IsPathRooted(input);
        if (looksLikePath)
            return Path.GetFullPath(input);

        // Treat as filename under Config/Json
        var jsonDir = GetJsonDirectory(createIfMissing: true);
        var fileName = input.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? input : input + ".json";
        var candidate = Path.Combine(jsonDir.FullName, input);
        if (File.Exists(candidate)) return candidate;
        candidate = Path.Combine(jsonDir.FullName, fileName);
        if (File.Exists(candidate)) return candidate;

        // Fallback to the default Json directory.
        return Path.Combine(jsonDir.FullName, fileName);
    }

    private static List<FileInfo> GetJsonFiles()
    {
        var dir = GetJsonDirectory(createIfMissing: true);
        if (!dir.Exists) return [];
        try
        {
            return dir.GetFiles("*.json", SearchOption.TopDirectoryOnly).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static async ValueTask ShowFileList(CommandArg arg)
    {
        var files = GetJsonFiles().OrderBy(f => f.LastWriteTimeUtc).ToList();
        if (files.Count == 0)
        {
            var searched = new List<string> { GetJsonDirectory(createIfMissing: true).FullName };
            await arg.SendMsg(I18NManager.Translate("Game.Command.Json.NoFileFound"));
            if (searched.Count > 0)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Json.SearchedDirectories"));
                foreach (var s in searched)
                    await arg.SendMsg(I18NManager.Translate("Game.Command.Json.SearchedDirectoryItem", s));
            }
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Json.FoundFiles"));
        for (var i = 0; i < files.Count; i++)
            await arg.SendMsg(I18NManager.Translate("Game.Command.Json.FileListItem", (i + 1).ToString(), files[i].Name));
        await arg.SendMsg(I18NManager.Translate("Game.Command.Json.UsageSelectHint"));
    }

    private static async ValueTask<(List<FormalAvatarInfo> changedAvatars, List<ItemData> removedItems)>
        ClearRelicAndEquipment(HyacineCore.Server.GameServer.Game.Player.PlayerInstance player)
    {
        var changed = new Dictionary<int, FormalAvatarInfo>();

        void MarkChanged(FormalAvatarInfo avatar)
        {
            if (!changed.ContainsKey(avatar.AvatarId))
                changed.Add(avatar.AvatarId, avatar);
        }

        var inv = player.InventoryManager!.Data;

        foreach (var item in inv.EquipmentItems)
        {
            if (item.EquipAvatar <= 0) continue;
            var avatar = player.AvatarManager?.GetFormalAvatar(item.EquipAvatar);
            if (avatar == null) continue;
            var pathInfo = avatar.PathInfos.GetValueOrDefault(item.EquipAvatar)
                           ?? avatar.PathInfos.Values.FirstOrDefault(x => x.EquipId == item.UniqueId);
            if (pathInfo != null && pathInfo.EquipId == item.UniqueId)
                pathInfo.EquipId = 0;
            item.EquipAvatar = 0;
            MarkChanged(avatar);
        }

        foreach (var item in inv.RelicItems)
        {
            if (item.EquipAvatar <= 0) continue;
            var avatar = player.AvatarManager?.GetFormalAvatar(item.EquipAvatar);
            if (avatar == null) continue;
            var pathInfo = avatar.PathInfos.GetValueOrDefault(item.EquipAvatar)
                           ?? avatar.PathInfos.Values.FirstOrDefault(x => x.Relic.Values.Contains(item.UniqueId));
            if (pathInfo != null)
            {
                var toRemoveSlots = pathInfo.Relic.Where(kv => kv.Value == item.UniqueId).Select(kv => kv.Key).ToList();
                foreach (var slot in toRemoveSlots) pathInfo.Relic.Remove(slot);
            }

            item.EquipAvatar = 0;
            MarkChanged(avatar);
        }

        var toRemove = new List<(int itemId, int count, int uniqueId)>(inv.EquipmentItems.Count + inv.RelicItems.Count);
        toRemove.AddRange(inv.EquipmentItems.Select(x => (x.ItemId, 1, x.UniqueId)));
        toRemove.AddRange(inv.RelicItems.Select(x => (x.ItemId, 1, x.UniqueId)));

        // Remove without syncing; caller sends a single batch sync.
        var removed = await player.InventoryManager.RemoveItems(toRemove, sync: false);

        return ([.. changed.Values], removed);
    }

    private static async ValueTask<List<FormalAvatarInfo>> ImportAvatars(
        HyacineCore.Server.GameServer.Game.Player.PlayerInstance player,
        FreeSrData data,
        CommandArg arg)
    {
        var changed = new Dictionary<int, FormalAvatarInfo>();

        if (data.Avatars == null || data.Avatars.Count == 0) return [];

        foreach (var (avatarKey, avatarJson) in data.Avatars)
        {
            var avatarId = avatarJson.AvatarId > 0 ? avatarJson.AvatarId : avatarKey;
            var baseAvatarId = GameData.MultiplePathAvatarConfigData.TryGetValue(avatarId, out var multiplePath)
                ? multiplePath.BaseAvatarID
                : avatarId;

            if (!GameData.AvatarConfigData.ContainsKey(avatarId))
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Json.AvatarExcelNotFound", avatarId.ToString()));
                continue;
            }

            if (player.AvatarManager?.GetFormalAvatar(baseAvatarId) == null)
            {
                await player.InventoryManager!.AddItem(baseAvatarId, 1, notify: false, sync: false);
            }

            var avatar = player.AvatarManager?.GetFormalAvatar(baseAvatarId);
            if (avatar == null) continue;
            if (!avatar.PathInfos.ContainsKey(avatarId))
            {
                avatar.PathInfos[avatarId] = new PathInfo(avatarId);
                avatar.PathInfos[avatarId].GetSkillTree();
            }

            avatar.Level = Math.Clamp(avatarJson.Level, 1, 80);
            avatar.Promotion = avatarJson.Promotion > 0
                ? Math.Clamp(avatarJson.Promotion, 0, 6)
                : GameData.GetMinPromotionForLevel(avatar.Level);

            var pathInfo = avatar.PathInfos[avatarId];
            pathInfo.Rank = Math.Clamp(avatarJson.Data?.Rank ?? 0, 0, 6);

            // skills: pointId -> level
            if (avatarJson.Data?.Skills != null)
            {
                var skillTree = pathInfo.GetSkillTree();
                skillTree.Clear();
                foreach (var (pointId, level) in avatarJson.Data.Skills)
                    skillTree[pointId] = Math.Max(1, level);
            }

            changed[avatar.BaseAvatarId] = avatar;
        }

        return [.. changed.Values];
    }

    private static async ValueTask<List<ItemData>> ImportRelicsAndLightcones(
        HyacineCore.Server.GameServer.Game.Player.PlayerInstance player,
        FreeSrData data,
        List<FormalAvatarInfo> avatarChanged)
    {
        var importedItems = new List<ItemData>(Math.Max(16, (data.Relics?.Count ?? 0) + (data.Lightcones?.Count ?? 0)));
        var avatarChangedMap = avatarChanged.ToDictionary(x => x.BaseAvatarId, x => x);

        FormalAvatarInfo? GetAvatar(int pathOrBaseAvatarId)
        {
            var baseAvatarId = GameData.MultiplePathAvatarConfigData.TryGetValue(pathOrBaseAvatarId, out var multiPath)
                ? multiPath.BaseAvatarID
                : pathOrBaseAvatarId;

            if (avatarChangedMap.TryGetValue(baseAvatarId, out var existing)) return existing;
            var avatar = player.AvatarManager?.GetFormalAvatar(baseAvatarId);
            if (avatar == null) return null;
            avatarChangedMap[baseAvatarId] = avatar;
            return avatar;
        }

        void EnsurePath(FormalAvatarInfo avatar, int avatarId)
        {
            if (!avatar.PathInfos.ContainsKey(avatarId))
            {
                avatar.PathInfos[avatarId] = new PathInfo(avatarId);
                avatar.PathInfos[avatarId].GetSkillTree();
            }
        }

        if (data.Relics != null)
        {
            foreach (var relic in data.Relics)
            {
                if (!GameData.RelicConfigData.TryGetValue(relic.RelicId, out var relicConfig)) continue;
                if (!GameData.ItemConfigData.TryGetValue(relic.RelicId, out var itemConfig) ||
                    itemConfig.ItemMainType != ItemMainTypeEnum.Relic)
                    continue;
                if (!GameData.RelicMainAffixData.TryGetValue(relicConfig.MainAffixGroup, out var mainAffixGroup) ||
                    mainAffixGroup.Count == 0)
                    continue;

                var subAffixes = new List<ItemSubAffix>(relic.SubAffixes?.Count ?? 0);
                if (relic.SubAffixes != null &&
                    GameData.RelicSubAffixData.TryGetValue(relicConfig.SubAffixGroup, out var subGroup) &&
                    subGroup != null)
                    foreach (var sub in relic.SubAffixes)
                    {
                        if (!subGroup.ContainsKey(sub.SubAffixId)) continue;
                        subAffixes.Add(new ItemSubAffix
                        {
                            Id = sub.SubAffixId,
                            Count = Math.Max(1, sub.Count),
                            Step = Math.Max(0, sub.Step)
                        });
                    }

                var mainAffixId = mainAffixGroup.ContainsKey(relic.MainAffixId)
                    ? relic.MainAffixId
                    : mainAffixGroup.Keys.First();

                var item = await player.InventoryManager!.PutItem(
                    relic.RelicId,
                    1,
                    level: Math.Clamp(relic.Level, 0, relicConfig.MaxLevel),
                    mainAffix: mainAffixId,
                    subAffixes: subAffixes,
                    uniqueId: ++player.InventoryManager.Data.NextUniqueId);

                importedItems.Add(item);

                if (relic.EquipAvatar > 0)
                {
                    var targetPathId = relic.EquipAvatar;
                    if (!GameData.AvatarConfigData.ContainsKey(targetPathId)) continue;

                    var avatar = GetAvatar(targetPathId);
                    if (avatar == null) continue;

                    EnsurePath(avatar, targetPathId);
                    var slot = (int)relicConfig.Type;
                    avatar.PathInfos[targetPathId].Relic[slot] = item.UniqueId;
                    item.EquipAvatar = targetPathId;
                }
            }
        }

        if (data.Lightcones != null)
        {
            foreach (var lightcone in data.Lightcones)
            {
                if (!GameData.ItemConfigData.TryGetValue(lightcone.ItemId, out var itemConfig) ||
                    itemConfig.ItemMainType != ItemMainTypeEnum.Equipment)
                    continue;
                if (!GameData.EquipmentConfigData.TryGetValue(lightcone.ItemId, out var equipmentConfig))
                    continue;

                var item = await player.InventoryManager!.PutItem(
                    lightcone.ItemId,
                    1,
                    rank: Math.Clamp(lightcone.Rank, 1, Math.Max(1, equipmentConfig.MaxRank)),
                    promotion: Math.Clamp(lightcone.Promotion, 0, Math.Max(0, equipmentConfig.MaxPromotion)),
                    level: Math.Clamp(lightcone.Level, 1, 80),
                    uniqueId: ++player.InventoryManager.Data.NextUniqueId);

                importedItems.Add(item);

                if (lightcone.EquipAvatar > 0)
                {
                    var targetPathId = lightcone.EquipAvatar;
                    if (!GameData.AvatarConfigData.ContainsKey(targetPathId)) continue;

                    var avatar = GetAvatar(targetPathId);
                    if (avatar == null) continue;
                    EnsurePath(avatar, targetPathId);
                    avatar.PathInfos[targetPathId].EquipId = item.UniqueId;
                    item.EquipAvatar = targetPathId;
                }
            }
        }

        // refresh caller list
        avatarChanged.Clear();
        avatarChanged.AddRange(avatarChangedMap.Values);

        return importedItems;
    }

    private sealed class FreeSrData
    {
        [JsonPropertyName("relics")] public List<RelicJson>? Relics { get; set; }
        [JsonPropertyName("lightcones")] public List<LightconeJson>? Lightcones { get; set; }
        [JsonPropertyName("avatars")] public Dictionary<int, AvatarJson>? Avatars { get; set; }
    }

    private sealed class RelicJson
    {
        [JsonPropertyName("level")] public int Level { get; set; }
        [JsonPropertyName("relic_id")] public int RelicId { get; set; }
        [JsonPropertyName("main_affix_id")] public int MainAffixId { get; set; }
        [JsonPropertyName("equip_avatar")] public int EquipAvatar { get; set; }
        [JsonPropertyName("sub_affixes")] public List<RelicSubAffixJson>? SubAffixes { get; set; }
    }

    private sealed class RelicSubAffixJson
    {
        [JsonPropertyName("sub_affix_id")] public int SubAffixId { get; set; }
        [JsonPropertyName("count")] public int Count { get; set; }
        [JsonPropertyName("step")] public int Step { get; set; }
    }

    private sealed class LightconeJson
    {
        [JsonPropertyName("level")] public int Level { get; set; }
        [JsonPropertyName("equip_avatar")] public int EquipAvatar { get; set; }
        [JsonPropertyName("item_id")] public int ItemId { get; set; }
        [JsonPropertyName("rank")] public int Rank { get; set; }
        [JsonPropertyName("promotion")] public int Promotion { get; set; }
    }

    private sealed class AvatarJson
    {
        [JsonPropertyName("avatar_id")] public int AvatarId { get; set; }
        [JsonPropertyName("level")] public int Level { get; set; }
        [JsonPropertyName("promotion")] public int Promotion { get; set; }
        [JsonPropertyName("data")] public AvatarExtraJson? Data { get; set; }
    }

    private sealed class AvatarExtraJson
    {
        [JsonPropertyName("rank")] public int Rank { get; set; }
        [JsonPropertyName("skills")] public Dictionary<int, int>? Skills { get; set; }
    }
}
