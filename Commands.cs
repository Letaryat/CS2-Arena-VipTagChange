using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using TagsApi;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Entities;
namespace Arena_VipTagChange;

public partial class Arena_VipTagChange
{

    [ConsoleCommand("css_settag", "Ability for VIP to change their Scoreboard and Chat tag")]
    [CommandHelper(minArgs: 1, usage: "TagName", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public async void TagChange(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var ArenaName = GetPlayerArenaTag(player!);
        var arg = commandInfo.GetArg(1);
        var newtag = $"{ArenaName} | {arg} ";
        if (player == null) return;
        try
        {
            //Server.PrintToChatAll($"{player!.PlayerName} - {ArenaName} - new tag: {arg} - Api: {test} | Steamid: {player.AuthorizedSteamID!.SteamId64}");
            if (Players.ContainsKey(player.AuthorizedSteamID!.SteamId64))
            {
                Players[player.AuthorizedSteamID.SteamId64]!.tag = arg;
            }
            else
            {
                Players[player.AuthorizedSteamID.SteamId64] = new Player
                {
                    steamid = player.AuthorizedSteamID.SteamId64,
                    tag = arg,
                    tagcolor = null,
                    namecolor = null,
                    chatcolor = null,
                    visibility = true
                };
            }

            SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ScoreTag, newtag);
            SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ChatTag, $"{{{Players[player.AuthorizedSteamID!.SteamId64]!.tagcolor}}}{arg} ");
            player.PrintToChat($"{Localizer["Prefix"]}{Localizer["TagSet", arg]}");
            await AddTag(player, arg);
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"TagChange: {ex}");
        }

        //SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.NameColor, "{Blue}");
        //SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, "{DarkRed}");
    }

    [ConsoleCommand("css_tagmenu", "Ability for VIP to change their Scoreboard and Chat tag")]
    public void TestMenu(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) { return; }
        var menu = _api?.NewMenu(Localizer["VipMenu"]);
        menu?.AddMenuOption($"{Localizer["ToggleTag"]} - {SharedApi_Tag?.GetPlayerToggleTags(player)}", (player, option) =>
        {
            SharedApi_Tag?.SetPlayerToggleTags(player, !SharedApi_Tag.GetPlayerToggleTags(player));
            SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ScoreTag, $"{GetPlayerArenaTag(player)!} | ");
            Players[player.AuthorizedSteamID!.SteamId64]!.visibility = false;
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption(Localizer["TagColorMenu"], (player, option) =>
        {
            CreateMenu(player, 1);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption(Localizer["ChatColorMenu"], (player, option) =>
        {
            CreateMenu(player, 2);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption(Localizer["NameColorMenu"], (player, option) =>
        {
            CreateMenu(player, 3);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.Open(player);
    }

    [ConsoleCommand("css_tescik", "Ability for VIP to change their Scoreboard and Chat tag")]
    public async void TestCMD(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var steamid = player!.AuthorizedSteamID!.SteamId64;
        Server.PrintToChatAll($"ID: {Players[steamid]!.chatcolor}");
    }
}