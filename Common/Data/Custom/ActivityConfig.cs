using Newtonsoft.Json;

namespace HyacineCore.Server.Data.Custom;

public class ActivityConfig
{
    // Keep legacy format support:
    // {
    //   "scheduleData": [ { activityId, beginTime, endTime, panelId }, ... ]
    // }
    [JsonProperty("scheduleData")]
    public List<ActivityScheduleData> ScheduleData { get; set; } = [];
    [JsonProperty("ScheduleData")] private List<ActivityScheduleData> ScheduleDataPascal { set => ScheduleData = value ?? []; }

    // Support SR-CasPS-like format:
    // {
    //   "activity_config": [
    //      { ActivityID, ActivityPanelID, ResidentModuleList, ActivityModuleIDList, BeginTime?, EndTime? }
    //   ]
    // }
    [JsonProperty("activity_config")]
    public List<ActivityConfigEntry> ActivityConfigEntries { get; set; } = [];
    [JsonProperty("activityConfig")] private List<ActivityConfigEntry> ActivityConfigEntriesCamel { set => ActivityConfigEntries = value ?? []; }
    [JsonProperty("ActivityConfig")] private List<ActivityConfigEntry> ActivityConfigEntriesPascal { set => ActivityConfigEntries = value ?? []; }

    // Large default active range: starts in the past and expires very far in the future.
    public const long DefaultBeginTime = 1664308800; // 2022-09-27 12:00:00 UTC
    public const long DefaultEndTime = 4294967295; // 2106-02-07 06:28:15 UTC

    public void Normalize()
    {
        var merged = new List<ActivityScheduleData>();
        var unique = new HashSet<string>();

        void AddOne(ActivityScheduleData item)
        {
            item.ActivityId = Math.Max(item.ActivityId, 0);
            item.PanelId = item.PanelId > 0 ? item.PanelId : item.ActivityId;
            item.BeginTime = item.BeginTime > 0 ? item.BeginTime : DefaultBeginTime;
            item.EndTime = item.EndTime > 0 ? item.EndTime : DefaultEndTime;
            if (item.ActivityId <= 0) return;

            var key = $"{item.ActivityId}:{item.PanelId}";
            if (!unique.Add(key)) return;
            merged.Add(item);
        }

        // 1) Keep existing manual schedule style (highest priority).
        foreach (var item in ScheduleData)
            AddOne(item);

        // 2) Flatten SR-CasPS-like activity_config entries.
        foreach (var entry in ActivityConfigEntries)
        {
            var panelId = entry.ActivityPanelID > 0 ? entry.ActivityPanelID : entry.ActivityID;
            var begin = entry.BeginTime > 0 ? entry.BeginTime : DefaultBeginTime;
            var end = entry.EndTime > 0 ? entry.EndTime : DefaultEndTime;

            var modules = new List<int>();
            modules.AddRange(entry.ResidentModuleList ?? []);
            modules.AddRange(entry.ActivityModuleIDList ?? []);

            if (modules.Count == 0 && entry.ActivityID > 0)
                modules.Add(entry.ActivityID);

            foreach (var moduleId in modules)
                AddOne(new ActivityScheduleData
                {
                    ActivityId = moduleId,
                    PanelId = panelId,
                    BeginTime = begin,
                    EndTime = end
                });
        }

        ScheduleData = merged;
    }
}

public class ActivityConfigEntry
{
    public int ActivityID { get; set; }
    public int ActivityPanelID { get; set; }
    public List<int> ResidentModuleList { get; set; } = [];
    public List<int> ActivityModuleIDList { get; set; } = [];
    public long BeginTime { get; set; }
    public long EndTime { get; set; }

    [JsonProperty("activityId")] private int ActivityIdCamel { set => ActivityID = value; }
    [JsonProperty("activity_id")] private int ActivityIdSnake { set => ActivityID = value; }
    [JsonProperty("activityPanelId")] private int ActivityPanelIdCamel { set => ActivityPanelID = value; }
    [JsonProperty("activity_panel_id")] private int ActivityPanelIdSnake { set => ActivityPanelID = value; }
    [JsonProperty("panelId")] private int PanelIdCamel { set => ActivityPanelID = value; }
    [JsonProperty("panel_id")] private int PanelIdSnake { set => ActivityPanelID = value; }
    [JsonProperty("residentModuleList")] private List<int> ResidentModulesCamel { set => ResidentModuleList = value ?? []; }
    [JsonProperty("resident_module_list")] private List<int> ResidentModulesSnake { set => ResidentModuleList = value ?? []; }
    [JsonProperty("activityModuleIdList")] private List<int> ActivityModuleIdsCamel { set => ActivityModuleIDList = value ?? []; }
    [JsonProperty("activity_module_id_list")] private List<int> ActivityModuleIdsSnake { set => ActivityModuleIDList = value ?? []; }
    [JsonProperty("beginTime")] private long BeginTimeCamel { set => BeginTime = value; }
    [JsonProperty("endTime")] private long EndTimeCamel { set => EndTime = value; }
    [JsonProperty("begin_time")] private long BeginTimeSnake { set => BeginTime = value; }
    [JsonProperty("end_time")] private long EndTimeSnake { set => EndTime = value; }
}

public class ActivityScheduleData
{
    public int ActivityId { get; set; }
    public long BeginTime { get; set; }
    public long EndTime { get; set; }
    public int PanelId { get; set; }
}
