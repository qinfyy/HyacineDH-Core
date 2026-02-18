using System.Numerics;
using System.Text;
using HyacineCore.Server.Data;
using HyacineCore.Server.Internationalization;
using HyacineCore.Server.Program.Program;
using HyacineCore.Server.Util;
using Newtonsoft.Json;

namespace HyacineCore.Server.Program.Handbook;

public static class HandbookGenerator
{
    private const string HandbookDirectoryPath = "Config/Handbook";

    public static void GenerateAll()
    {
        var config = ConfigManager.Config;
        var directory = new DirectoryInfo(config.Path.ResourcePath + "/TextMap");
        var handbook = new DirectoryInfo(HandbookDirectoryPath);
        if (!handbook.Exists)
            handbook.Create();
        if (!directory.Exists)
            return;

        foreach (var langFile in directory.GetFiles())
        {
            if (langFile.Extension != ".json") continue;
            var lang = langFile.Name.Replace("TextMap", "").Replace(".json", "");

            // Check if handbook needs to regenerate
            var handbookPath = $"{HandbookDirectoryPath}/GM Handbook {lang}.txt";
            if (File.Exists(handbookPath))
            {
                var handbookInfo = new FileInfo(handbookPath);
                if (handbookInfo.LastWriteTime >= langFile.LastWriteTime)
                    continue; // Skip if handbook is newer than language file
            }

            Generate(lang);
        }

        Logger.GetByClassName()
            .Info(I18NManager.Translate("Server.ServerInfo.GeneratedItem", I18NManager.Translate("Word.Handbook")));
    }

    public static void Generate(string lang)
    {
        var config = ConfigManager.Config;
        var textMapPath = config.Path.ResourcePath + "/TextMap/TextMap" + lang + ".json";
        var fallbackTextMapPath = config.Path.ResourcePath + "/TextMap/TextMap" + config.ServerOption.FallbackLanguage +
                                  ".json";
        if (!File.Exists(textMapPath))
        {
            Logger.GetByClassName().Error(I18NManager.Translate("Server.ServerInfo.FailedToReadItem", textMapPath,
                I18NManager.Translate("Word.NotFound")));
            return;
        }

        if (!File.Exists(fallbackTextMapPath))
        {
            Logger.GetByClassName().Error(I18NManager.Translate("Server.ServerInfo.FailedToReadItem", textMapPath,
                I18NManager.Translate("Word.NotFound")));
            return;
        }

        // Old format: Dictionary<BigInteger, string>
        // Newer format (3.8.5x): Array of { ID: { Hash, Hash64 }, Text }
        var textMap = LoadTextMapDictionary(textMapPath);
        var fallbackTextMap = LoadTextMapDictionary(fallbackTextMapPath);

        if (textMap == null || fallbackTextMap == null || textMap.Count == 0 || fallbackTextMap.Count == 0)
        {
            Logger.GetByClassName().Error(I18NManager.Translate("Server.ServerInfo.FailedToReadItem", textMapPath,
                I18NManager.Translate("Word.Error")));
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("#Handbook generated in " + DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
        builder.AppendLine();
        builder.AppendLine("#Command");
        builder.AppendLine();
        GenerateCmd(builder, lang);

        builder.AppendLine();
        builder.AppendLine("#Avatar");
        builder.AppendLine();
        GenerateAvatar(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#Item");
        builder.AppendLine();
        GenerateItem(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#StageId");
        builder.AppendLine();
        GenerateStageId(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#MainMission");
        builder.AppendLine();
        GenerateMainMissionId(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#SubMission");
        builder.AppendLine();
        GenerateSubMissionId(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#RogueBuff");
        builder.AppendLine();
        GenerateRogueBuff(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#RogueMiracle");
        builder.AppendLine();
        GenerateRogueMiracleDisplay(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#CurrencyWarRole");
        builder.AppendLine();
        GenerateCurrencyWarRole(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#CurrencyWarEquipment");
        builder.AppendLine();
        GenerateCurrencyWarEquipment(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#CurrencyWarConsumable");
        builder.AppendLine();
        GenerateCurrencyWarConsumable(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#CurrencyWarOrb");
        builder.AppendLine();
        GenerateCurrencyWarOrb(builder, textMap, fallbackTextMap);

#if DEBUG
        builder.AppendLine();
        builder.AppendLine("#RogueDiceSurface");
        builder.AppendLine();
        GenerateRogueDiceSurfaceDisplay(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#RogueDialogue");
        builder.AppendLine();
        GenerateRogueDialogueDisplay(builder, textMap, fallbackTextMap);
#endif

        builder.AppendLine();
        WriteToFile(lang, builder.ToString());
    }

    private static Dictionary<BigInteger, string>? LoadTextMapDictionary(string path)
    {
        try
        {
            // Fast-path: old dictionary format { "hash": "text" }
            try
            {
                var legacyJson = File.ReadAllText(path);
                var legacyDict = JsonConvert.DeserializeObject<Dictionary<BigInteger, string>>(legacyJson);
                if (legacyDict is { Count: > 0 }) return legacyDict;
            }
            catch
            {
                // ignore and fall back to streaming parse (array format)
            }

            using var sr = File.OpenText(path);
            using var reader = new JsonTextReader(sr) { DateParseHandling = DateParseHandling.None };

            while (reader.Read() && reader.TokenType == JsonToken.Comment)
            {
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                return JsonSerializer.CreateDefault().Deserialize<Dictionary<BigInteger, string>>(reader);
            }

            if (reader.TokenType != JsonToken.StartArray) return null;

            var dict = new Dictionary<BigInteger, string>(capacity: 1024);

            static BigInteger? ReadBigInteger(object? value)
            {
                if (value == null) return null;
                if (value is BigInteger bi) return bi;
                if (value is long l) return new BigInteger(l);
                if (value is int i) return new BigInteger(i);
                if (value is ulong ul) return new BigInteger(ul);
                if (value is uint ui) return new BigInteger(ui);
                if (value is string s && BigInteger.TryParse(s, out var parsed)) return parsed;
                return null;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray) break;
                if (reader.TokenType != JsonToken.StartObject) continue;

                BigInteger? hash = null;
                BigInteger? hash64 = null;
                string? text = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject) break;
                    if (reader.TokenType != JsonToken.PropertyName) continue;

                    var prop = (string?)reader.Value;
                    if (prop == "ID")
                    {
                        reader.Read();
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            reader.Skip();
                            continue;
                        }

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.EndObject) break;
                            if (reader.TokenType != JsonToken.PropertyName) continue;

                            var idProp = (string?)reader.Value;
                            reader.Read();

                            if (reader.TokenType == JsonToken.Integer)
                            {
                                var num = ReadBigInteger(reader.Value);
                                if (idProp == "Hash") hash = num;
                                else if (idProp == "Hash64") hash64 = num;
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }
                    else if (prop == "Text")
                    {
                        reader.Read();
                        if (reader.TokenType == JsonToken.String)
                            text = (string?)reader.Value;
                        else
                            reader.Skip();
                    }
                    else
                    {
                        reader.Read();
                        reader.Skip();
                    }
                }

                if (string.IsNullOrEmpty(text)) continue;
                if (hash64.HasValue && !dict.ContainsKey(hash64.Value)) dict[hash64.Value] = text;
                if (hash.HasValue && !dict.ContainsKey(hash.Value)) dict[hash.Value] = text;
            }

            return dict;
        }
        catch
        {
            return null;
        }
    }

    public static void GenerateCmd(StringBuilder builder, string lang)
    {
        foreach (var cmd in EntryPoint.CommandManager.CommandInfo)
        {
            builder.Append("\t" + cmd.Key);
            var desc = I18NManager.TranslateAsCertainLang(lang == "CN" ? "CHS" : lang, cmd.Value.Description).Replace("\n", "\n\t\t");
            builder.AppendLine(": " + desc);
        }
    }

    public static void GenerateItem(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback, bool setName)
    {
        foreach (var item in GameData.ItemConfigData.Values)
        {
            var name = map.TryGetValue(item.ItemName.Hash, out var value) ? value :
                fallback.TryGetValue(item.ItemName.Hash, out value) ? value : $"[{item.ItemName.Hash}]";
            builder.AppendLine(item.ID + ": " + name);

            if (setName && name != $"[{item.ItemName.Hash}]") item.Name = name;
        }
    }

    public static void GenerateAvatar(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback, bool setName)
    {
        foreach (var avatar in GameData.AvatarConfigData.Values)
        {
            var name = map.TryGetValue(avatar.AvatarName.Hash, out var value) ? value :
                fallback.TryGetValue(avatar.AvatarName.Hash, out value) ? value : $"[{avatar.AvatarName.Hash}]";
            builder.AppendLine(avatar.AvatarID + ": " + name);

            if (setName && name != $"[{avatar.AvatarName.Hash}]") avatar.Name = name;
        }
    }

    public static void GenerateMainMissionId(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        foreach (var mission in GameData.MainMissionData.Values)
        {
            var name = map.TryGetValue(mission.Name.Hash, out var value) ? value :
                fallback.TryGetValue(mission.Name.Hash, out value) ? value : $"[{mission.Name.Hash}]";
            builder.AppendLine(mission.MainMissionID + ": " + name);
        }
    }

    public static void GenerateSubMissionId(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        foreach (var mission in GameData.SubMissionData.Values)
        {
            var name = map.TryGetValue(mission.TargetText.Hash, out var value) ? value :
                fallback.TryGetValue(mission.TargetText.Hash, out value) ? value : $"[{mission.TargetText.Hash}]";
            builder.AppendLine(mission.SubMissionID + ": " + name);
        }
    }

    public static void GenerateStageId(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        foreach (var stage in GameData.StageConfigData.Values)
        {
            var name = map.TryGetValue(stage.StageName.Hash, out var value) ? value :
                fallback.TryGetValue(stage.StageName.Hash, out value) ? value : $"[{stage.StageName.Hash}]";
            builder.AppendLine(stage.StageID + ": " + name);
        }
    }

    public static void GenerateRogueBuff(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback, bool setName)
    {
        foreach (var buff in GameData.MazeBuffData.Values.OrderBy(x => x.ID).ThenBy(x => x.Lv))
            builder.AppendLine($"{buff.ID}: {buff.ModifierName} --- Level:{buff.Lv}");
    }

    public static void GenerateRogueMiracleDisplay(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback, bool setName)
    {
        builder.AppendLine("Not available in current build.");
    }

    public static void GenerateCurrencyWarRole(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        builder.AppendLine("Not available in current build.");
    }

    public static void GenerateCurrencyWarEquipment(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        builder.AppendLine("Not available in current build.");
    }

    public static void GenerateCurrencyWarConsumable(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        builder.AppendLine("Not available in current build.");
    }

    public static void GenerateCurrencyWarOrb(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        builder.AppendLine("Not available in current build.");
    }

    public static string GetNameFromTextMap(BigInteger key, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        if (map.TryGetValue(key, out var value)) return value;
        if (fallback.TryGetValue(key, out value)) return value;
        return $"[{key}]";
    }

    public static void WriteToFile(string lang, string content)
    {
        File.WriteAllText($"{HandbookDirectoryPath}/GM Handbook {lang}.txt", content);
    }

#if DEBUG
    public static void GenerateRogueDiceSurfaceDisplay(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        builder.AppendLine("Not available in current build.");
    }

    public static void GenerateRogueDialogueDisplay(StringBuilder builder, Dictionary<BigInteger, string> map,
        Dictionary<BigInteger, string> fallback)
    {
        builder.AppendLine("Not available in current build.");
    }
#endif
}
