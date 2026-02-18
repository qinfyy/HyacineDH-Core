using HyacineCore.Server.Data;
using HyacineCore.Server.Data.Custom;
using HyacineCore.Server.GameServer.Plugin;
using HyacineCore.Server.Internationalization;
using HyacineCore.Server.Util;

namespace HyacineCore.Server.Command.Command.Cmd;

[CommandInfo("reload", "Game.Command.Reload.Desc", "Game.Command.Reload.Usage", permission: "HyacineLover.manage")]
public class CommandReload : ICommand
{
    [CommandMethod("0 banner")]
    public async ValueTask ReloadBanner(CommandArg arg)
    {
        // Reload the banners
        GameData.BannersConfig =
            ResourceManager.LoadCustomFile<BannersConfig>("Banner", "Banners", ConfigManager.Config.Path.GameDataPath)
            ?? new BannersConfig();
        await arg.SendMsg(I18NManager.Translate("Game.Command.Reload.ConfigReloaded",
            I18NManager.Translate("Word.Banner")));
    }

    [CommandMethod("0 activity")]
    public async ValueTask ReloadActivity(CommandArg arg)
    {
        // Reload the activities
        GameData.ActivityConfig =
            ResourceManager.LoadCustomFile<ActivityConfig>("Activity", "ActivityConfig",
                ConfigManager.Config.Path.GameDataPath) ??
            new ActivityConfig();
        await arg.SendMsg(I18NManager.Translate("Game.Command.Reload.ConfigReloaded",
            I18NManager.Translate("Word.Activity")));
    }

    [CommandMethod("0 videokey")]
    public async ValueTask ReloadVideoKey(CommandArg arg)
    {
        // Reload the videokeys
        GameData.VideoKeysConfig =
            ResourceManager.LoadCustomFile<VideoKeysConfig>("VideoKeys", "VideoKeysConfig",
                ConfigManager.Config.Path.KeyPath) ??
            new VideoKeysConfig();
        await arg.SendMsg(I18NManager.Translate("Game.Command.Reload.ConfigReloaded",
            I18NManager.Translate("Word.VideoKeys")));
    }

    [CommandMethod("0 plugin")]
    public async ValueTask ReloadPlugin(CommandArg arg)
    {
        // Reload the plugin
        PluginManager.UnloadPlugins();
        PluginManager.LoadPlugins();
        await arg.SendMsg(I18NManager.Translate("Game.Command.Reload.ConfigReloaded",
            I18NManager.Translate("Word.Plugin")));
    }
}
