namespace Jaket.Net;

using Steamworks;

/// <summary> Class responsible for configuring the lobby. </summary>
public static class LobbyConfig
{
    /// <summary> Multiplayer provider that owns the lobby. </summary>
    public static string Client
    {
        get => Get("client");
        set => Set("client", value);
    }
    /// <summary> Name of the lobby, obviously. </summary>
    public static string Name
    {
        get => Get("name");
        set => Set("name", value ?? $"{SteamClient.Name}'s lobby");
    }
    /// <summary> Mode of the lobby, gamemodes are used to make fun. </summary>
    public static string Mode
    {
        get => Get("mode");
        set => Set("mode", value ?? "campaign");
    }
    /// <summary> Mission that is loaded in the lobby. </summary>
    public static string Level
    {
        get => Get("level");
        set => Set("level", value switch
        {
            "Intro"          => "How?",
            "Main Menu"      => "How?",
            "Tutorial"       => "Tutorial",
            "Intermission1"  => "Intermission",
            "Intermission2"  => "Intermission",
            "uk_construct"   => "Sandbox",
            "Endless"        => "Cyber Grind",
            "CreditsMuseum2" => "Museum",
            _ => value[6..]
        });
    }
    /// <summary> Whether versus aka pure chaos is allowed. </summary>
    public static bool PvPAllowed
    {
        get => Get("allow-pvp") == bool.TrueString;
        set => Set("allow-pvp", value.ToString());
    }
    /// <summary> Whether client-side mods are allowed. </summary>
    public static bool ModsAllowed
    {
        get => Get("allow-mods") == bool.TrueString;
        set => Set("allow-mods", value.ToString());
    }
    /// <summary> Whether bosses are to be healed after player death. </summary>
    public static bool HealBosses
    {
        get => Get("heal-bosses") == bool.TrueString;
        set => Set("heal-bosses", value.ToString());
    }

    /// <summary> Resets the lobby config to its default value. </summary>
    public static void Reset()
    {
        Client = "jaket";
        Name = null;
        Mode = null;
        Level = Scene;

        PvPAllowed = true;
        ModsAllowed = false;
        HealBosses = true;
    }

    /// <summary> Sets lobby data by the given key to the given value. </summary>
    public static void Set(string key, string value) => LobbyController.Lobby?.SetData(key, value);

    /// <summary> Gets lobby data by the given key. </summary>
    public static string Get(string key) => LobbyController.Lobby?.GetData(key);
}
