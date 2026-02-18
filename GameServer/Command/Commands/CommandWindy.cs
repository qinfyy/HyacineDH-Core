using HyacineCore.Server.Internationalization;
using HyacineCore.Server.Kcp;
using HyacineCore.Server.Util;

namespace HyacineCore.Server.Command.Command.Cmd;

[CommandInfo("windy", "Game.Command.Windy.Desc", "/windy <lua>")]
public class CommandWindy : ICommand
{
    private const string LuaDirectoryName = "Lua";

    [CommandDefault]
    public async ValueTask Windy(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var filePath = Path.Combine(Environment.CurrentDirectory, ConfigManager.Config.Path.ConfigPath,
            LuaDirectoryName, arg.Raw);
        if (File.Exists(filePath))
        {
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            await arg.Target.SendPacket(new HandshakePacket(fileBytes));
            await arg.SendMsg("Read BYTECODE from Lua script: " + filePath.Replace("\\", "/"));
        }
        else
        {
            await arg.SendMsg("Error reading Lua script: " + arg.Raw.Replace("\\", "/"));
        }
    }
}
