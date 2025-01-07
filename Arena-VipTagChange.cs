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
using System.Text.Json.Serialization;
using MySqlConnector;
using System.Data;
using Serilog;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.UserMessages;
using Dapper;

namespace Arena_VipTagChange;

public class TagConfig : BasePluginConfig
{
    [JsonPropertyName("VipFlag")] public string VipFlag { get; set; } = "@vip/plugin";
    [JsonPropertyName("DBHost")] public string DBHost { get; set; } = "localhost";
    [JsonPropertyName("DBPort")] public uint DBPort { get; set; } = 3306;
    [JsonPropertyName("DBUsername")] public string DBUsername { get; set; } = "root";
    [JsonPropertyName("DBName")] public string DBName { get; set; } = "db_69";
    [JsonPropertyName("DBPassword")] public string DBPassword { get; set; } = "123";

}

public class Arena_VipTagChange : BasePlugin, IPluginConfig<TagConfig>
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
    //private MySqlConnection? _connection;
    public TagConfig Config { get; set; }

    public string DbConnection = string.Empty;

    List<string> Colors = [
        "Default",
        "White",
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
        if (SharedApi_Tag is null)
        {
            PluginCapability<ITagApi> Capability_TagApi = new("tags:api");
            SharedApi_Tag = Capability_TagApi.Get();
        }
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
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

    public async void OnConfigParsed(TagConfig config)
    {
        Config = config;
        if (Config.DBHost.Length < 1 || Config.DBName.Length < 1 || Config.DBPassword.Length < 1 || Config.DBUsername.Length < 1)
        {
            Logger.LogInformation($"You need to setup a mysql database!");
        }

        MySqlConnectionStringBuilder builder = new()
        {
            Server = Config.DBHost,
            UserID = Config.DBUsername,
            Password = Config.DBPassword,
            Database = Config.DBName,
        };

        DbConnection = builder.ConnectionString;

        try
        {
            var _connection = new MySqlConnection(builder.ConnectionString);
            await _connection.OpenAsync();
            Logger.LogInformation($"Succesfully connected to mysql database");
            var sqlcmd = _connection.CreateCommand();
            string createTable = @"CREATE TABLE IF NOT EXISTS VipTags_Players(
                SteamID VARCHAR(255) PRIMARY KEY,
                Tag VARCHAR(50),
                TagColor VARCHAR(50),
                NameColor VARCHAR(50),
                ChatColor VARCHAR(50)
            );";

            using (var cmd = new MySqlCommand(createTable, _connection))
            {
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"Table VipTags_Players has been created or was already created!");
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Error while trying to connect to database: {ex}");
            return;
        }
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

    public string? GetITag(CCSPlayerController controller)
    {
        if (controller == null) { return null; }
        return SharedApi_Tag?.GetPlayerTag(controller, Tags.Tags_Tags.ChatTag);
    }

    public async Task<bool> UserExist(ulong SteamID)
    {
        try
        {
            using var connection = new MySqlConnection(DbConnection);
            await connection.OpenAsync();
            string sqlExists = "SELECT * FROM `VipTags_Players` WHERE `SteamID` = @SteamID";
            using (var cmd = new MySqlCommand(sqlExists, connection))
            {
                cmd.Parameters.AddWithValue("@SteamID", SteamID);
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"Player {SteamID} exists");
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"{ex}");
        }
        Logger.LogInformation($"Player {SteamID} does not exists");
        return false;
    }
    public async Task AddTag(CCSPlayerController player, string tag)
    {
        var SteamID = player!.AuthorizedSteamID!.SteamId64;
        var userExists = await UserExist(SteamID);
        try
        {
            await using var connection = new MySqlConnection(DbConnection);
            await Task.Run(() => userExists);
            if (userExists)
            {
                Logger.LogInformation("User exists! Updating tag!");
                await connection.OpenAsync();
                string sqlUpdate = "UPDATE `VipTags_Players` SET `Tag` = @Tag WHERE `SteamID` = @SteamID";
                using (var cmd = new MySqlCommand(sqlUpdate, connection))
                {
                    cmd.Parameters.AddWithValue("@SteamID", SteamID);
                    cmd.Parameters.AddWithValue("@Tag", tag);
                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"Tag for user {SteamID} was updated!");
                }
                return;
            }
            await connection.OpenAsync();
            string sqlInsert = "INSERT INTO `VipTags_Players` (`SteamID`, `Tag`) VALUES (@SteamID, @Tag)";
            using (var cmd = new MySqlCommand(sqlInsert, connection))
            {
                cmd.Parameters.AddWithValue("@SteamID", SteamID);
                cmd.Parameters.AddWithValue("@Tag", tag);
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"Table VipTags_Players has been created or was already created!");
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"{ex}");
        }
        return;
    }

    public async Task ChangeColor(CCSPlayerController player, string color, int type)
    {
        var SteamID = player!.AuthorizedSteamID!.SteamId64;
        var userExists = await UserExist(SteamID);
        string? type1 = null;
        switch (type)
        {
            case 1:
                type1 = "TagColor";
                break;
            case 2:
                type1 = "ChatColor";
                break;
            case 3:
                type1 = "NameColor";
                break;
        }
        try
        {
            await using var connection = new MySqlConnection(DbConnection);
            await Task.Run(() => userExists);
            if (userExists)
            {
                Logger.LogInformation($"User exists! Updating color - {type}!");
                await connection.OpenAsync();
                string sqlUpdate = $"UPDATE `VipTags_Players` SET {type1} = @Color WHERE `SteamID` = @SteamID";
                using (var cmd = new MySqlCommand(sqlUpdate, connection))
                {
                    cmd.Parameters.AddWithValue("@SteamID", SteamID);
                    cmd.Parameters.AddWithValue("@Color", color);
                    //cmd.Parameters.AddWithValue("@Type", type1);
                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"Tag for user {SteamID} was updated!");
                }
                return;
            }
            player.PrintToChat("You need to set tag!");
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"ChangeColor Method: {ex}");
        }
    }


    [ConsoleCommand("css_tags", "Ability for VIP to change their Scoreboard and Chat tag")]
    [CommandHelper(minArgs: 1, usage: "[tag name]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@vip-plugin/vip")]
    public async void TagChange(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var ArenaName = GetPlayerArenaTag(player!);
        var arg = commandInfo.GetArg(1);
        var test = GetITag(player!);
        var newtag = $"{ArenaName} | {arg} ";
        if (player == null)
        {
            Server.PrintToChatAll("player null");
        }
        Server.PrintToChatAll($"{player!.PlayerName} - {ArenaName} - new tag: {arg} - Api: {test} | Steamid: {player.AuthorizedSteamID.SteamId64}");
        SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ScoreTag, newtag);
        SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ChatTag, $" {arg} ");
        await AddTag(player, arg);
        //SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.NameColor, "{Blue}");
        //SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, "{DarkRed}");

    }

    [ConsoleCommand("css_menu", "Ability for VIP to change their Scoreboard and Chat tag")]
    public void TestMenu(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) { return; }
        var menu = _api?.NewMenu("Tag menu");
        menu?.AddMenuOption($"Toggle Tag - {SharedApi_Tag?.GetPlayerToggleTags(player)}", (player, option) =>
        {
            SharedApi_Tag?.SetPlayerToggleTags(player, !SharedApi_Tag.GetPlayerToggleTags(player));
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption("Name Color", (player, option) =>
        {
            CreateMenu(player, 3);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption("Chat Color", (player, option) =>
        {
            CreateMenu(player, 2);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.AddMenuOption("Tag Color", (player, option) =>
        {
            CreateMenu(player, 1);
            CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
        });
        menu?.Open(player);
    }

    public void CreateMenu(CCSPlayerController? player, int type)
    {
        if (player == null) { return; }
        var menu = _api?.NewMenu("Tags menu");
        switch (type)
        {
            case 1:
                menu = _api?.NewMenu("Tag color");
                break;
            case 2:
                menu = _api?.NewMenu("Chat Color");
                break;
            case 3:
                menu = _api?.NewMenu("Name Color");
                break;
        }

        foreach (var chatcolors in Colors)
        {
            menu?.AddMenuOption($"{chatcolors}", async (player, option) =>
            {
                switch (type)
                {
                    case 1:
                        string? playertag = SharedApi_Tag?.GetPlayerTag(player, Tags.Tags_Tags.ScoreTag);
                        var splittedTag = playertag!.Split("ARENA")[0];
                        SharedApi_Tag?.SetPlayerTag(player, Tags.Tags_Tags.ChatTag, $"{{{chatcolors}}}{splittedTag}");
                        player.PrintToChat($"Your new tag color:{{{chatcolors}}}{chatcolors}".ReplaceColorTags());
                        await ChangeColor(player, chatcolors, 1);
                        break;
                    case 2:
                        SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, $"{{{chatcolors}}}");
                        player.PrintToChat($"Your new chat color:{{{chatcolors}}}{chatcolors}".ReplaceColorTags());
                        await ChangeColor(player, chatcolors, 2);
                        break;
                    case 3:
                        SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.NameColor, $"{{{chatcolors}}}");
                        player.PrintToChat($"Your new name color:{{{chatcolors}}}{chatcolors}".ReplaceColorTags());
                        await ChangeColor(player, chatcolors, 3);
                        break;
                }
                //SharedApi_Tag?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, $"{{{chatcolors}}}");
                CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
                //string message = $"Your new color: {{{chatcolors}}}{chatcolors}";
                //player.PrintToChat($"{message.ReplaceColorTags()}");
            });
        }
        menu?.Open(player);
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {

        return HookResult.Continue;
    }

    public async Task<Player?> FetchPlayerInfo(ulong SteamID)
    {
        await using var connection = new MySqlConnection(DbConnection);
        var userExists = await UserExist(SteamID);
        try
        {
            await Task.Run(() => userExists);
            if (!userExists)
            {
                Logger.LogInformation($"No player in database with steamid: {SteamID}");
                return null;
            }
            await connection.OpenAsync();
            string sqlSelect = $"SELECT * FROM `Vip_TagChange WHERE `SteamID` = @SteamID";
            var user = await connection.QueryFirstOrDefaultAsync<Player>(sqlSelect);
            return user;
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Fetchplayerinfo: {ex}");
        }
        return null;
    }

    public class Player
    {
        public required ulong steamid { get; set; }
        public required string tag { get; set; }
        public string tagcolor { get; set; }
        public string namecolor { get; set; }
        public string chatcolor { get; set; }
    }

}
