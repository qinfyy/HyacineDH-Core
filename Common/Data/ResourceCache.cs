using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Text;
using HyacineCore.Server.Data.Config.Scene;
using HyacineCore.Server.Internationalization;
using HyacineCore.Server.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HyacineCore.Server.Data;

public static class CompressionHelper
{
    public static byte[] Compress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) return [];

        try
        {
            if (data.Length < 1024)
            {
                var result = new byte[data.Length + 1];
                result[0] = 0;
                Buffer.BlockCopy(data, 0, result, 1, data.Length);
                return result;
            }

            using var output = new MemoryStream();
            output.WriteByte(1);
            using (var compressor = new DeflateStream(output, CompressionMode.Compress, true))
            {
                compressor.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }
        catch
        {
            var result = new byte[data.Length + 1];
            result[0] = 0;
            Buffer.BlockCopy(data, 0, result, 1, data.Length);
            return result;
        }
    }

    public static byte[] Decompress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) return [];

        try
        {
            if (data[0] == 0)
            {
                var result = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, result, 0, result.Length);
                return result;
            }

            using var input = new MemoryStream(data, 1, data.Length - 1);
            using var decompressor = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            decompressor.CopyTo(output);
            return output.ToArray();
        }
        catch
        {
            return data;
        }
    }
}

public class ResourceCacheData
{
    public Dictionary<string, byte[]> GameDataValues { get; set; } = [];
}

public class IgnoreJsonIgnoreContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        // Cache should include runtime fields marked with [JsonIgnore], but skip members that
        // would collide with another non-ignored member using the same JSON property name.
        if (!property.Ignored)
            return property;

        if (member.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            return property;

        if (!HasJsonNameConflict(member, property.PropertyName))
            property.Ignored = false;

        return property;
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(type, memberSerialization);
        var bestByName = new Dictionary<string, JsonProperty>(StringComparer.Ordinal);

        foreach (var property in properties)
        {
            var name = property.PropertyName ?? string.Empty;
            if (!bestByName.TryGetValue(name, out var existing))
            {
                bestByName[name] = property;
                continue;
            }

            if (GetPriority(property) > GetPriority(existing))
                bestByName[name] = property;
        }

        var result = new List<JsonProperty>(bestByName.Count);
        foreach (var property in bestByName.Values)
            result.Add(property);
        return result;
    }

    private static int GetPriority(JsonProperty property)
    {
        var score = 0;
        if (property.Readable) score += 2;
        if (property.Writable) score += 1;
        if (!property.Ignored) score += 1;
        return score;
    }

    private static bool HasJsonNameConflict(MemberInfo member, string? jsonName)
    {
        if (string.IsNullOrWhiteSpace(jsonName)) return false;
        var declaringType = member.DeclaringType;
        if (declaringType == null) return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var sibling in declaringType.GetMembers(flags))
        {
            if (sibling == member) continue;
            if (sibling.MemberType != MemberTypes.Property && sibling.MemberType != MemberTypes.Field) continue;
            if (sibling.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

            var siblingName = ResolveJsonName(sibling);
            if (string.Equals(siblingName, jsonName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string ResolveJsonName(MemberInfo member)
    {
        var attr = member.GetCustomAttribute<JsonPropertyAttribute>();
        if (!string.IsNullOrWhiteSpace(attr?.PropertyName))
            return attr.PropertyName!;
        return member.Name;
    }
}

public class ResourceCache
{
    public static readonly JsonSerializerSettings Serializer = new()
    {
        ContractResolver = new IgnoreJsonIgnoreContractResolver(),
        TypeNameHandling = TypeNameHandling.Auto,
        Converters =
        {
            new ConcurrentBagConverter<PropInfo>(),
            new ConcurrentDictionaryConverter<string, FloorInfo>()
        }
    };

    public static Logger Logger { get; } = new("ResCache"); // ShortName,it's ResourceCache
    public static string CachePath { get; } = ConfigManager.Config.Path.ConfigPath + "/Resource.cache";
    public static bool IsComplete { get; set; } = true; // Custom in errors to ignore some error

    public static Task SaveCache()
    {
        return Task.Run(() =>
        {
            try
            {
                var cacheData = new ResourceCacheData();
                var successCount = 0;
                var failCount = 0;

                foreach (var prop in typeof(GameData).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    try
                    {
                        var value = prop.GetValue(null);
                        if (value == null) continue;

                        var json = JsonConvert.SerializeObject(value, Serializer);
                        var compressed = CompressionHelper.Compress(Encoding.UTF8.GetBytes(json));
                        cacheData.GameDataValues[prop.Name] = compressed;
                        successCount++;
                    }
                    catch (Exception e)
                    {
                        failCount++;
                        Logger.Error($"Failed to cache GameData.{prop.Name}", e);
                    }
                }

                var file = new FileInfo(CachePath);
                if (file.Directory is { Exists: false }) file.Directory.Create();
                File.WriteAllText(file.FullName, JsonConvert.SerializeObject(cacheData));

                Logger.Info($"Cache saved: {successCount} entries, {failCount} failed.");
                Logger.Info(I18NManager.Translate("Server.ServerInfo.GeneratedItem",
                    I18NManager.Translate("Word.Cache")));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save resource cache.", e);
            }
        });
    }

    public static bool LoadCache()
    {
        try
        {
            var file = new FileInfo(CachePath);
            if (!file.Exists) return false;

            var buffer = new byte[file.Length];
            using var mmf = MemoryMappedFile.CreateFromFile(file.FullName, FileMode.Open);
            using var viewAccessor = mmf.CreateViewAccessor();
            viewAccessor.ReadArray(0, buffer, 0, buffer.Length);

            var cacheData = JsonConvert.DeserializeObject<ResourceCacheData>(Encoding.UTF8.GetString(buffer));
            if (cacheData == null) return false;

            var failedCount = 0;
            foreach (var prop in typeof(GameData).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                try
                {
                    Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem",
                        $"{prop.DeclaringType?.Name}.{prop.Name}"));
                    if (!cacheData.GameDataValues.TryGetValue(prop.Name, out var valueBytes))
                        continue;

                    var value = JsonConvert.DeserializeObject(
                        Encoding.UTF8.GetString(CompressionHelper.Decompress(valueBytes)), prop.PropertyType,
                        Serializer);
                    if (!TryApplyCachedValue(prop, value))
                        throw new InvalidOperationException("Property set method not found.");
                }
                catch (Exception e)
                {
                    failedCount++;
                    Logger.Error(I18NManager.Translate("Server.ServerInfo.FailedToLoadItem",
                        $"{prop.DeclaringType?.Name}.{prop.Name}"));
                    Logger.Error(e.Message);
                }
            }

            if (failedCount > 0)
            {
                Logger.Error($"Cache load failed on {failedCount} GameData properties.");
                return false;
            }

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem",
                I18NManager.Translate("Word.Cache")));

            return true;
        }
        catch (Exception e)
        {
            Logger.Error("Failed to load resource cache.", e);
            return false;
        }
    }

    private static bool TryApplyCachedValue(PropertyInfo prop, object? value)
    {
        if (prop.SetMethod != null)
        {
            prop.SetValue(null, value);
            return true;
        }

        if (value == null) return true;

        var target = prop.GetValue(null);
        if (target == null) return false;
        return TryOverwriteCollection(target, value);
    }

    private static bool TryOverwriteCollection(object target, object source)
    {
        if (source is not System.Collections.IEnumerable enumerableSource)
            return false;

        var targetType = target.GetType();
        var clearMethod = targetType.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
        if (clearMethod == null) return false;
        clearMethod.Invoke(target, null);

        var tryAddPairMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "TryAdd" && m.GetParameters().Length == 2);
        var addPairMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 2);
        var addSingleMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 1);
        var indexerSetter = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => p.Name == "Item" && p.GetIndexParameters().Length == 1 && p.SetMethod != null);

        foreach (var item in enumerableSource)
        {
            if (item == null) continue;

            var itemType = item.GetType();
            var keyProp = itemType.GetProperty("Key");
            var valueProp = itemType.GetProperty("Value");

            if (keyProp != null && valueProp != null)
            {
                var key = keyProp.GetValue(item);
                var val = valueProp.GetValue(item);

                if (tryAddPairMethod != null)
                {
                    var res = tryAddPairMethod.Invoke(target, [key, val]);
                    if (res is bool b && b) continue;
                }

                if (addPairMethod != null)
                {
                    addPairMethod.Invoke(target, [key, val]);
                    continue;
                }

                if (indexerSetter != null)
                {
                    indexerSetter.SetValue(target, val, [key!]);
                    continue;
                }

                return false;
            }

            if (addSingleMethod == null) return false;
            addSingleMethod.Invoke(target, [item]);
        }

        return true;
    }

    public static void ClearGameData()
    {
        var properties = typeof(GameData).GetProperties(BindingFlags.Public | BindingFlags.Static);

        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;
            var emptyValue = propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                ? Activator.CreateInstance(propType)
                : propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>)
                    ? Activator.CreateInstance(propType)
                    : propType.IsClass
                        ? Activator.CreateInstance(propType)
                        : null;

            prop.SetValue(null, emptyValue);
        }
    }
}
