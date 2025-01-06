using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using K4ArenaSharedApi;
using TagsApi;
using MenuManager;
using CounterStrikeSharp.API.Core.Translations;



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
    private IMenuApi? _api;
    private readonly PluginCapability<IMenuApi?> _pluginCapability = new("menu:nfcore");    

    List<string> Colors = [
        "Default",
        "White",
        "Forteam",
        "DarkRed",
        "Green",
        "LightYellow",
        "LightBlue",
        "Olive",
        "Lime",
        "Red",
        "LightPurple",
        "Purple",
        "Grey",
        "Yellow",
        "Gold",
        "Silver",
        "Blue",
        "DarkBlue",
        "BlueGrey",
        "Magenta",
        "LightRed",
        "Orange"
    ];
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

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api == null) Console.WriteLine("MenuManager Core not found...");
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

    [ConsoleCommand("css_menu", "Ability for VIP to change their Scoreboard and Chat tag")]
    public void TestMenu(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if(player == null){ return; }
        var menu = _api?.NewMenu("Test");
        menu?.AddMenuOption($"Toggle Tag - {SharedApi_Tag?.GetPlayerToggleTags(player)}", (player, option) => {
            SharedApi_Tag?.SetPlayerToggleTags(player, !SharedApi_Tag.GetPlayerToggleTags(player));
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption("Tag Color", (player, option) => {
            NoweMenu(player);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption("Chat Color", (player, option) => {
            NoweMenu(player);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption("Name Color", (player, option) => {
            NoweMenu(player);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.Open(player);
    }

    public void NoweMenu(CCSPlayerController? player)
    {
        if(player == null){ return; }
        var menu = _api?.NewMenu("Test drugie menu");

        foreach(var chatcolors in Colors)
        {
            menu?.AddMenuOption($"{chatcolors}", (player, option) => {
                SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, $"{{{chatcolors}}}");
                CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
                string message = $"Your new color: {{{chatcolors}}}{chatcolors}";
                message.ReplaceColorTags();
                player.PrintToChat($"{message.ReplaceColorTags()}");
            });
        }
        menu?.Open(player);
    }

}
