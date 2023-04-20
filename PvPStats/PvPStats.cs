using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Data;
using PvPStats.PvP_Events;
using PvPStats.Windows;
using PvPStats.Services;


namespace PvPStats;

using PvPStats_Configuration = Configuration;

public sealed class PvPStats : IDalamudPlugin
{
    public string Name => "DutyTracker";
    private const string CommandName = "/dt";

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }

    public Configuration Configuration { get; init; }
    public readonly PvPManager PvPManager;


    public PvPStats(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;

        PluginInterface.Create<Service>();
        Service.PvPEventService = new PvPEventService();
        Service.PlayerCharacterState = new PlayerCharacterState();
        Service.WindowService = new WindowService();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        PvPManager = PluginInterface.Create<PvPManager>(Configuration)!;

        Service.WindowService.AddWindow("MainWindow", new MainWindow(PvPManager, Configuration));
        Service.WindowService.AddWindow("DutyExplorer", new DutyExplorerWindow(PvPManager));
        Service.WindowService.AddWindow("Debug", new DebugWindow());

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Duty Tracker menu",
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenSettings;
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(CommandName);
        Service.WindowService.Dispose();
        Service.PlayerCharacterState.Dispose();
        Service.PvPEventService.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args == "debug")
            Service.WindowService.OpenWindow("Debug");
        else
            Service.WindowService.ToggleWindow("MainWindow");
    }

    private void OpenSettings()
    {
        Service.WindowService.OpenWindow("MainWindow");
    }

    private void DrawUi()
    {
        Service.WindowService.Draw();
    }
}
