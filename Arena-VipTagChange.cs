using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using K4ArenaSharedApi;
using TagsApi;

namespace Arena_VipTagChange;

public class Arena_VipTagChange : BasePlugin
{
    public override string ModuleName => "Arena_VipTagChange";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Letaryat";
    public override string ModuleDescription => "Tag change for servers using K4-Arenas";
    public static PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI { get; } = new("k4-arenas:sharedapi");
    public static IK4ArenaSharedApi? SharedAPI_Arena { get; private set; }
    public (bool ArenaFound, bool Checked) ArenaSupport = (false, false);
    public static PluginCapability<ITagApi> Capability_TagsApi = new("tags:api");
    public static ITagApi? SharedApi_Tag { get; private set; }
    public override void Load(bool hotReload)
    {
        Logger.LogInformation("Arena_VipTagChange - Loaded");
        if(SharedApi_Tag is null)
        {
            PluginCapability<ITagApi> Capability_TagApi = new("tags:api");
            SharedApi_Tag = Capability_TagApi.Get();
        }

    }
    public override void Unload(bool hotReload) 
    {
        Logger.LogInformation("Arena_VipTagChange - Unloaded");
    }

    public string? GetPlayerArenaTag(CCSPlayerController controller)
    {
        if (controller == null) { return null; }
        if (!ArenaSupport.Checked)
        {
            string arenaPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "K4-Arenas"));
            ArenaSupport.ArenaFound = Directory.Exists(arenaPath);
            ArenaSupport.Checked = true;
        }
        if (!ArenaSupport.ArenaFound)
        {
            return null;
        }
        if (SharedAPI_Arena is null)
        {
            PluginCapability<IK4ArenaSharedApi> Capability_SharedAPI = new("k4-arenas:sharedapi");
            SharedAPI_Arena = Capability_SharedAPI.Get();
        }
        return SharedAPI_Arena?.GetArenaName(controller);
    }

    public string? GetApi(CCSPlayerController controller)
    {
        if(controller == null) { return null;}
        return SharedApi_Tag?.GetPlayerTag(controller, Tags.Tags_Tags.ChatTag);
    }



    [ConsoleCommand("css_tags", "Ability for VIP to change their Scoreboard and Chat tag")]
    [CommandHelper(minArgs: 1, usage: "[tag name]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@vip-plugin/vip")]
    public void TagChange(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var ArenaName = GetPlayerArenaTag(player!);
        var arg = commandInfo.GetArg(1);
        var test = GetApi(player!);
        var newtag = $"[{arg}] {ArenaName} |";
        if(player == null){
            Server.PrintToChatAll("player null");
        }
        Server.PrintToChatAll($"{player!.PlayerName} - {ArenaName} - new tag: {arg} - Api: {test}");
        SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ScoreTag, newtag);
        SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ChatTag, $"[{arg}]");
        SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.NameColor, "{Blue}");
        SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, "{DarkRed}");
    }
}
