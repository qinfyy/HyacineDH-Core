namespace HyacineCore.Server.Internationalization.Message;

#region Root

public class LanguageEN
{
    public GameTextEN Game { get; } = new();
    public ServerTextEN Server { get; } = new();
    public WordTextEN Word { get; } = new(); // a placeholder for the actual word text
}

#endregion

#region Layer 1

/// <summary>
///     path: Game
/// </summary>
public class GameTextEN
{
    public CommandTextEN Command { get; } = new();
}

/// <summary>
///     path: Server
/// </summary>
public class ServerTextEN
{
    public WebTextEN Web { get; } = new();
    public ServerInfoTextEN ServerInfo { get; } = new();
}

/// <summary>
///     path: Word
/// </summary>
public class WordTextEN
{
    public string Rank => "Eidolon";
    public string Avatar => "Avatar";
    public string Material => "Material";
    public string Pet => "Pet";
    public string Relic => "Relic";
    public string Equipment => "Light Cone";
    public string Talent => "Trace";
    public string Banner => "Banner";
    public string VideoKeys => "Video Keys";
    public string Activity => "Activity";
    public string Buff => "Blessing";
    public string Miracle => "Curio";
    public string Unlock => "Luxury Item";

    // server info
    public string Config => "Config File";
    public string Language => "Language";
    public string Log => "Log";
    public string GameData => "Game Data";
    public string Cache => "Resource Cache";
    public string Database => "Database";
    public string Command => "Command";
    public string WebServer => "Web Server";
    public string Plugin => "Plugin";
    public string Handler => "Packet Handler";
    public string Dispatch => "Global Dispatch";
    public string Game => "Game";
    public string Handbook => "Handbook";
    public string NotFound => "Not Found";
    public string Error => "Error";
    public string FloorInfo => "Floor File";
    public string FloorGroupInfo => "Floor Group File";
    public string FloorMissingResult => "Teleport & World Generation";
    public string FloorGroupMissingResult => "Teleport, Monster Battle & World Generation";
    public string Mission => "Mission";
    public string MissionInfo => "Mission File";
    public string SubMission => "Sub Mission";
    public string SubMissionInfo => "Sub Mission File";
    public string MazeSkill => "Maze Skill";
    public string MazeSkillInfo => "Maze Skill File";
    public string Dialogue => "Simulated Universe Event";
    public string DialogueInfo => "Simulated Universe Event File";
    public string Performance => "Performance";
    public string PerformanceInfo => "Performance File";
    public string RogueChestMap => "Simulated Universe Map";
    public string RogueChestMapInfo => "Simulated Universe Map File";
    public string ChessRogueRoom => "Simulated Universe DLC";
    public string ChessRogueRoomInfo => "Simulated Universe DLC File";
    public string SummonUnit => "Skill Summon";
    public string SummonUnitInfo => "Skill Summon File";
    public string RogueTournRoom => "Divergent Universe";
    public string RogueTournRoomInfo => "Divergent Universe Room File";
    public string TypesOfRogue => "Types of Simulated Universe";
    public string RogueMagicRoom => "Unknowable Domain";
    public string RogueMagicRoomInfo => "Unknowable Domain Room File";
    public string RogueDiceSurface => "Dice Surface Effect";
    public string RogueDiceSurfaceInfo => "Dice Surface Effect File";
    public string AdventureModifier => "AdventureModifier";
    public string AdventureModifierInfo => "AdventureModifier File";

    public string DatabaseAccount => "Database Account";
    public string Tutorial => "Tutorial";
}

#endregion

#region Layer 2

#region GameText

/// <summary>
///     path: Game.Command
/// </summary>
public class CommandTextEN
{
    public NoticeTextEN Notice { get; } = new();

    public HeroTextEN Hero { get; } = new();
    public AvatarTextEN Avatar { get; } = new();
    public GiveTextEN Give { get; } = new();
    public GiveAllTextEN GiveAll { get; } = new();
    public LineupTextEN Lineup { get; } = new();
    public HelpTextEN Help { get; } = new();
    public ClearTextEN Clear { get; } = new();
    public DebugTextEN Debug { get; } = new();
    public JsonTextEN Json { get; } = new();
    public WindyTextEN Windy { get; } = new();
    public KickTextEN Kick { get; } = new();
    public MissionTextEN Mission { get; } = new();
    public RelicTextEN Relic { get; } = new();
    public ReloadTextEN Reload { get; } = new();
    public RogueTextEN Rogue { get; } = new();
    public SceneTextEN Scene { get; } = new();
    public UnlockAllTextEN UnlockAll { get; } = new();
    public MailTextEN Mail { get; } = new();
    public RaidTextEN Raid { get; } = new();
    public AccountTextEN Account { get; } = new();
    public UnstuckTextEN Unstuck { get; } = new();
    public SetlevelTextEN Setlevel { get; } = new();
    public GridTextEN Grid { get; } = new();
}

#endregion

#region ServerText

/// <summary>
///     path: Server.Web
/// </summary>
public class WebTextEN
{
    public string Maintain => "The server is under maintenance. Please try again later.";
}

/// <summary>
///     path: Server.ServerInfo
/// </summary>
public class ServerInfoTextEN
{
    public string Shutdown => "Shutting down...";
    public string CancelKeyPressed => "Cancel key (Ctrl + C) pressed. The server will shut down shortly...";
    public string StartingServer => "Starting HyacineCore...";
    public string CurrentVersion => "Current server-supported version: {0}";
    public string LoadingItem => "Loading {0}...";
    public string GeneratingItem => "Generating {0}...";
    public string WaitingItem => "Waiting for process {0} to complete...";
    public string RegisterItem => "Registered {0} {1}.";
    public string FailedToLoadItem => "Failed to load {0}.";
    public string NewClientSecretKey => "Client secret key does not exist. Generating a new client secret key.";
    public string FailedToInitializeItem => "Failed to initialize {0}.";
    public string FailedToReadItem => "Failed to read {0}. File {1}";
    public string GeneratedItem => "Generated {0}.";
    public string LoadedItem => "Loaded {0}.";
    public string LoadedItems => "Loaded {0} {1}.";
    public string ServerRunning => "{0} server is listening on {1}";
    public string ServerStarted => "Startup complete! Took {0}s, beating 99% of users. Type 'help' for command help."; // Localized the joke for English version
    public string MissionEnabled => "Mission system enabled. This feature is still under development and may not work as expected. If you encounter any bugs, please report them to the developers.";
    public string CacheLoadSkip => "Cache loading skipped.";
    public string MissionLoadSkipped => "EnableMission is false. Skipping mission/story resource loading.";
    public string PlayerGetTokenRecv => "Recv PlayerGetToken: remote={0} uid={1} accountUid={2} tokenLen={3}";
    public string PlayerGetTokenTokenMismatch => "PlayerGetToken token mismatch: uid={0} endpoint={1}";
    public string KeyExchangeSuccess => "{0} key exchange successful";
    public string PlayerLoginRecv => "Recv PlayerLogin: remote={0} uid={1} loginRandom={2}";

    public string ConfigMissing => "{0} is missing. Please check your resource folder: {1}. {2} may not be available.";
    public string UnloadedItems => "Unloaded all {0}.";
    public string SaveDatabase => "Saved database. Took {0}s";
    public string WaitForAllDone => "Cannot enter the game yet. Please wait until all items are loaded before trying again.";

    public string UnhandledException => "An unhandled exception occurred: {0}";
}

#endregion

#endregion

#region Layer 3

#region CommandText

/// <summary>
///     path: Game.Command.Notice
/// </summary>
public class NoticeTextEN
{
    public string PlayerNotFound => "Player not found!";
    public string InvalidArguments => "Invalid arguments!";
    public string NoPermission => "You do not have permission to do that!";
    public string CommandNotFound => "Command not found! Type '/help' for help";
    public string TargetOffline => "Target {0}({1}) is offline! Clearing current target";
    public string TargetFound => "Target {0}({1}) found. Next command will target them by default.";
    public string TargetNotFound => "Target {0} not found!";
    public string InternalError => "An internal error occurred while processing the command!";
}

/// <summary>
///     path: Game.Command.Hero
/// </summary>
public class HeroTextEN
{
    public string Desc =>
        "Switch the main character's gender/form.\nWhen switching gender, genderId 1 represents male, 2 represents female.\nWhen switching form, 8001 represents Destruction Path, 8003 represents Preservation Path, 8005 represents Harmony Path.\nNote: Switching gender will clear all optional Paths and Traces, and this operation is irreversible!";

    public string Usage => "Usage: /hero gender [genderId]\n\nUsage: /hero type [typeId]";
    public string GenderNotSpecified => "Gender does not exist!";
    public string HeroTypeNotSpecified => "Hero type does not exist!";
    public string GenderChanged => "Gender changed!";
    public string HeroTypeChanged => "Hero type changed!";
}

/// <summary>
///     path: Game.Command.UnlockAll
/// </summary>
public class UnlockAllTextEN
{
    public string Desc =>
        "Unlock all objects within the category.\n" +
        "Use /unlockall mission to complete all missions. You will be kicked after use, and may get stuck in the tutorial upon re-login. Use with caution.\n" +
        "Use /unlockall tutorial to unlock all tutorials. You will be kicked after use. Use for situations where the interface is stuck and you cannot act.\n" +
        "Use /unlockall rogue to unlock all types of Simulated Universe. You will be kicked after use. Recommended to use with /unlockall tutorial for better results.";

    public string Usage => "Usage: /unlockall [mission/tutorial/rogue]";
    public string UnlockedAll => "Unlocked/completed all {0}!";
}

/// <summary>
///     path: Game.Command.Avatar
/// </summary>
public class AvatarTextEN
{
    public string Desc => "Set properties for player's owned avatars.\nWhen setting trace level, setting to level X sets all trace nodes to level X. If greater than the node's max allowed level, sets to max level.\nNote: -1 means all owned avatars.";

    public string Usage =>
        "Usage: /avatar talent [AvatarID/-1] [Trace Level]\n\nUsage: /avatar get [AvatarID]\n\nUsage: /avatar rank [AvatarID/-1] [Eidolon]\n\nUsage: /avatar level [AvatarID/-1] [Avatar Level]";

    public string InvalidLevel => "Invalid {0} level";
    public string AllAvatarsLevelSet => "Set all avatars' {0} level to {1}";
    public string AvatarLevelSet => "Set avatar {0}'s {1} level to {2}";
    public string AvatarNotFound => "Avatar does not exist!";
    public string AvatarGet => "Obtained avatar {0}!";
    public string AvatarFailedGet => "Failed to obtain avatar {0}!";
}

/// <summary>
///     path: Game.Command.Give
/// </summary>
public class GiveTextEN
{
    public string Desc => "Give items to the player. Avatar IDs can be entered here, but traces, levels, and eidolons cannot be set.";
    public string Usage => "Usage: /give <ItemID> l<Level> x<Count> r<Superimposition>";
    public string ItemNotFound => "Item not found!";
    public string GiveItem => "Gave @{0} {1} of item {2}";
}

/// <summary>
///     path: Game.Command.GiveAll
/// </summary>
public class GiveAllTextEN
{
    public string Desc =>
        "Give the player all items of the specified type.\navatar means characters, equipment means Light Cones, relic means relics, unlock means chat bubbles, phone wallpapers, avatars, train means Trailblazer's room contents, pet means pets, path means Paths for multi-Path characters.";

      public string Usage =>
          "Usage: /giveall avatar r<Eidolon> l<Level>\n\n" +
          "Usage: /giveall equipment r<Superimposition> l<Level> x<Count>\n\n" +
          "Usage: /giveall relic l<Level> x<Count>\n\n" +
          "Usage: /giveall skin\n\n" +
          "Usage: /giveall unlock\n\n" +
          "Usage: /giveall train\n\n" +
          "Usage: /giveall pet\n\n" +
          "Usage: /giveall path";

    public string GiveAllItems => "Gave all {0}, {1} each";
}

/// <summary>
///     path: Game.Command.Lineup
/// </summary>
public class LineupTextEN
{
    public string Desc => "Manage the player's lineup.\nMaze points can only be obtained two at a time.";
    public string Usage => "Usage: /lineup mp [Maze Point Amount]\n\nUsage: /lineup heal";
    public string PlayerGainedMp => "Player gained {0} Maze Points";
    public string HealedAllAvatars => "Successfully healed all avatars in the current lineup";
}

/// <summary>
///     path: Game.Command.Help
/// </summary>
public class HelpTextEN
{
    public string Desc => "Show help information";
    public string Usage => "Usage: /help\n\nUsage: /help [command]";
    public string Commands => "Commands:";
    public string CommandPermission => "Required Permission: ";
    public string CommandAlias => "Command Aliases: ";
}

/// <summary>
///     path: Game.Command.Kick
/// </summary>
public class KickTextEN
{
    public string Desc => "Kick a player";
    public string Usage => "Usage: /kick";
    public string PlayerKicked => "Player {0} has been kicked!";
}

/// <summary>
///     path: Game.Command.Mission
/// </summary>
public class MissionTextEN
{
    public string Desc =>
        "Manage player's missions.\n" +
        "Use pass to complete all currently ongoing missions. This command can cause severe lag. Try to use /mission finish instead.\n" +
        "Use finish [SubMissionID] to complete a specific sub mission. Please browse the handbook for SubMissionID.\n" +
        "Use finishmain [MainMissionID] to complete a specific main mission. Please browse the handbook for MainMissionID.\n" +
        "Use running <-all> to get tracked missions. Add '-all' to show all ongoing and potentially stuck missions. This may produce a long list, please review carefully.\n" +
        "Use reaccept [MainMissionID] to restart a specific main mission. Please browse the handbook for MainMissionID.";

    public string Usage =>
        "Usage: /mission pass\n\nUsage: /mission finish [SubMissionID]\n\nUsage: /mission running <-all>\n\nUsage: /mission reaccept [MainMissionID]\n\nUsage: /mission finishmain [MainMissionID]";

    public string AllMissionsFinished => "All missions finished!";
    public string AllRunningMissionsFinished => "Total {0} ongoing missions finished!";
    public string MissionFinished => "Mission {0} finished!";
    public string InvalidMissionId => "Invalid mission ID!";
    public string NoRunningMissions => "No ongoing missions!";

    public string RunningMissions => "Ongoing Missions:";
    public string PossibleStuckMissions => "Potentially Stuck Missions:";
    public string MainMission => "Main Mission";

    public string MissionReAccepted => "Re-accepted mission {0}!";
}

/// <summary>
///     path: Game.Command.Relic
/// </summary>
public class RelicTextEN
{
    public string Desc => "Manage player's relics.\nMain stat is optional, sub stats are optional, but at least one must exist.\nLevel restriction: 1 ≤ Level ≤ 9999.";

    public string Usage =>
        "Usage: /relic <RelicID> <MainAffixID> <SubAffixID1:SubAffixLevel> <SubAffixID2:SubAffixLevel> <SubAffixID3:SubAffixLevel> <SubAffixID4:SubAffixLevel> l<Level> x<Count>";

    public string RelicNotFound => "Relic not found!";
    public string InvalidMainAffixId => "Invalid main affix ID";
    public string InvalidSubAffixId => "Invalid sub affix ID";
    public string RelicGiven => "Gave player @{0} {1} relic(s) {2}, main stat {3}";
}

/// <summary>
///     path: Game.Command.Reload
/// </summary>
public class ReloadTextEN
{
    public string Desc => "Reload specified configuration.\nConfiguration names: banner - banners, activity - activities";
    public string Usage => "Usage: /reload <config name>";
    public string ConfigReloaded => "Configuration {0} reloaded!";
}

/// <summary>
///     path: Game.Command.Clear
/// </summary>
public class ClearTextEN
{
    public string Desc => "Clear unequipped light cones or relics from the player's inventory";
    public string Usage => "Usage: /clear equipment\n\nUsage: /clear relic";
    public string ClearEquipment => "Cleared {0} unequipped light cone(s)";
    public string ClearRelic => "Cleared {0} unequipped relic(s)";
}

/// <summary>
///     path: Game.Command.Debug
/// </summary>
public class DebugTextEN
{
    public string Desc =>
        "Debug command set: configure next battle stage/monster, or load a custom packet queue";
    public string Usage =>
        "Usage: /debug specific [StageID]\n\nUsage: /debug monster [MonsterID]\n\nUsage: /debug customP [PacketJsonPath]";
    public string InvalidStageId => "Invalid stage or monster ID!";
    public string SetStageId => "Debug battle parameter set";
    public string CustomPacketFileNotFound => "Custom packet file not found!";
    public string CustomPacketFileInvalid => "Custom packet file is invalid or queue is empty!";
    public string CustomPacketFileLoaded => "Custom packet queue loaded successfully!";
}

/// <summary>
///     path: Game.Command.Json
/// </summary>
public class JsonTextEN
{
    public string Desc => "Import avatars/lightcones/relics from JSON files under Config/Json (quick sync builds)";
    public string Usage => "Usage: /json [path/number/clear]";
    public string ClearInventory => "Cleared all light cones and relics in player inventory";
    public string FileNotFound => "File not found: {0}";
    public string ReadOrParseFailed => "Failed to read or parse JSON: {0}";
    public string InvalidJsonContent => "JSON content is empty or malformed";
    public string ImportSummary => "Imported from {0}: avatar={1} relic={2} lightcone={3}";
    public string NoFileFoundWithHint =>
        "No JSON file was found in Config/Json (tip: you can also use /json [absolute path] for a custom path)";
    public string InvalidChoice => "Invalid selection. Please enter a number between 1 and {0}";
    public string NoFileFound => "No JSON file was found in Config/Json";
    public string SearchedDirectories => "Searched directories:";
    public string SearchedDirectoryItem => "- {0}";
    public string FoundFiles => "Found the following files in Config/Json:";
    public string FileListItem => "{0}. {1}";
    public string UsageSelectHint => "Use /json [number] to select a file, or /json [path] for a custom path";
    public string AvatarExcelNotFound => "Avatar Excel not found: {0}";
}

/// <summary>
///     path: Game.Command.Windy
/// </summary>
public class WindyTextEN
{
    public string Desc => "Send Lua bytecode to the client (debug). Client reads compiled output from Config/Lua/<file>";
}

/// <summary>
///     path: Game.Command.Rogue
/// </summary>
public class RogueTextEN
{
    public string Desc => "Manage player's Simulated Universe data.\n-1 means all blessings (owned blessings).\nUse buff to obtain blessings.\nUse enhance to enhance blessings.";

    public string Usage =>
        "Usage: /rogue money [Cosmic Fragment amount]\n\nUsage: /rogue buff [BlessingID/-1]\n\nUsage: /rogue miracle [CurioID]\n\nUsage: /rogue enhance [BlessingID/-1]\n\nUsage: /rogue unstuck - Leave event";

    public string PlayerGainedMoney => "Player gained {0} Cosmic Fragments";
    public string PlayerGainedAllItems => "Player gained all {0}";
    public string PlayerGainedItem => "Player gained {0} {1}";
    public string PlayerEnhancedBuff => "Player enhanced blessing {0}";
    public string PlayerEnhancedAllBuffs => "Player enhanced all blessings";
    public string PlayerUnstuck => "Player left the event";
    public string NotFoundItem => "{0} not found!";
    public string PlayerNotInRogue => "Player is not in Simulated Universe!";
}

/// <summary>
///     path: Game.Command.Scene
/// </summary>
public class SceneTextEN
{
    public string Desc =>
        "Manage player's scene.\n" +
        "Note: Most of these are for debugging. Before using commands, ensure you know what you are doing!\n" +
        "Use prop to set prop state. Get state list from Common/Enums/Scene/PropStateEnum.cs.\n" +
        "Use unlockall to unlock all props in the scene (set all props that can be set to open state to open). This command may cause the game to load stuck at about 90%. Use /scene reset <floorId> to solve.\n" +
        "Use change to enter a specific scene. To get EntryId, visit Resources/MapEntrance.json.\n" +
        "Use reload to reload the current scene and return to the starting position.\n" +
        "Use reset to reset all prop states in a specified scene. To get current FloorId, use /scene cur.";

    public string Usage =>
        "Usage: /scene prop [GroupID] [PropID] [State]\n\nUsage: /scene remove [EntityID]\n\nUsage: /scene unlockall\n\nUsage: /scene change [entryId]\n\nUsage: /scene reload\n\nUsage: /scene reset <floorId>";

    public string LoadedGroups => "Loaded groups: {0}";
    public string PropStateChanged => "Prop: {0} state set to {1}";
    public string PropNotFound => "Prop not found!";
    public string EntityRemoved => "Entity {0} removed";
    public string EntityNotFound => "Entity not found!";
    public string AllPropsUnlocked => "All props unlocked!";
    public string SceneChanged => "Entered scene {0}";
    public string SceneReloaded => "Scene reloaded!";
    public string SceneReset => "Reset all prop states in scene {0}!";
    public string CurrentScene => "Current scene Entry Id: {0}, Plane Id: {1}, Floor Id: {2}";
}

/// <summary>
///     path: Game.Command.Mail
/// </summary>
public class MailTextEN
{
    public string Desc => "Manage player's mail";
    public string Usage => "Usage: /mail [Sender Name] [TemplateID] [Expire Days] _TITLE [Title] _CONTENT [Content]";
    public string MailSent => "Mail sent!";
    public string MailSentWithAttachment => "Mail with attachment sent!";
}

/// <summary>
///     path: Game.Command.Raid
/// </summary>
public class RaidTextEN
{
    public string Desc => "Manage player's mission temporary scene";
    public string Usage => "Usage: /raid leave - Leave temporary scene";
    public string Leaved => "Left temporary scene!";
}

/// <summary>
///     path: Game.Command.Account
/// </summary>
public class AccountTextEN
{
    public string Desc => "Create an account.\nNote: This command is untested. Use with caution!";
    public string Usage => "Usage: /account create [Username]";
    public string InvalidUid => "Invalid UID parameter!";
    public string CreateError => "Internal error occurred {0} ";
    public string CreateSuccess => "New account {0} created successfully!";
    public string DuplicateAccount => "Account {0} already exists!";
    public string DuplicateUID => "UID {0} already exists!";
    public string DataError => "Failed to get new account data! {0}!";
}

/// <summary>
///     path: Game.Command.Unstuck
/// </summary>
public class UnstuckTextEN
{
    public string Desc => "Teleport the player back to the default scene";
    public string Usage => "Usage: /unstuck [UID]";
    public string UnstuckSuccess => "Successfully teleported the player back to the default scene";
    public string UidNotExist => "This UID does not exist!";
    public string PlayerIsOnline => "The player is currently online!";
}

/// <summary>
///     path: Game.Command.Setlevel
/// </summary>
public class SetlevelTextEN
{
    public string Desc => "Set player level";
    public string Usage => "Usage: /setlevel [Level]";
    public string SetlevelSuccess => "Level set successfully!";
}

/// <summary>
///     path: Game.Command.Grid
/// </summary>
public class GridTextEN
{
    public string Desc => "Manage Aetherium War content. Note: This part is not yet fully developed. Please report any issues promptly.\nThis command may not check if IDs exist. If it has no effect, please check if parameters are correct.";
    public string Usage => "Usage: /grid gold [Gold Amount]\n\n" +
                           "Usage: /grid role [RoleID] [Role Star]\n\n" +
                           "Usage: /grid equip [EquipmentID]\n\n" +
                           "Usage: /grid consumable [ConsumableID]\n\n" +
                           "Usage: /grid orb [OrbID]";
    public string NotInGame => "Not in Aetherium War!";
    public string InvalidRole => "Role ID or Star does not exist!";
    public string AddedRole => "Role added.";
    public string UpdateGold => "Gained {0} Gold.";
    public string AddEquipment => "Added {0} equipment.";
    public string AddOrb => "Added {0} orb.";
    public string AddConsumable => "Added {0} consumable.";
    public string EnterSection => "Entered {0}-{1}.";
}

#endregion

#endregion


