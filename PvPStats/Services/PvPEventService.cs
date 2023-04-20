using System;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.DutyState;
using Dalamud.Logging;
using PvPStats.Enums;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;

namespace PvPStats.Services;


public class PvPStartedEventArgs : EventArgs
{
    public TerritoryType TerritoryType { get; }
    public TerritoryIntendedUse IntendedUse => (TerritoryIntendedUse)TerritoryType.TerritoryIntendedUse;

    public PvPStartedEventArgs(TerritoryType territoryType) { TerritoryType = territoryType; }
}

public class PvPEndedEventArgs : EventArgs
{
    public bool Completed { get; }

    public PvPEndedEventArgs(bool completed) { Completed = completed; }
}

public sealed class PvPEventService : IDisposable
{
    private bool _dutyStarted;

    private readonly DutyState _dutyState;
    private readonly DataManager _dataManager;
    private readonly ClientState _clientState;

    public delegate void DutyStartedDelegate(PvPStartedEventArgs eventArgs);
    public delegate void DutyWipedDelegate();
    public delegate void DutyRecommencedDelegate();
    public delegate void DutyEndedDelegate(PvPEndedEventArgs eventArgs);

    public event DutyStartedDelegate? DutyStarted;
    public event DutyWipedDelegate? DutyWiped;
    public event DutyRecommencedDelegate? DutyRecommenced;
    public event DutyEndedDelegate? DutyEnded;


    public PvPEventService()
    {
        _dutyState = Service.DutyState;
        _dataManager = Service.DataManager;
        _clientState = Service.ClientState;

        _dutyState.DutyStarted += OnPvPStarted;
        _dutyState.DutyWiped += OnPvPWiped;
        _dutyState.DutyRecommenced += OnPvPRecommenced;
        _dutyState.DutyCompleted += OnPvPEnded;
        _clientState.TerritoryChanged += OnTerritoryChanged;
    }

    private void OnPvPStarted(object? o, ushort territoryType)
    {
        PluginLog.Information($"Duty Detected. TerritoryType: {territoryType}");
        var territory = _dataManager.Excel.GetSheet<TerritoryType>()?.GetRow(territoryType);
        if (territory is null)
        {
            PluginLog.Warning("Could not load territory sheet.");
            return;
        }
        PluginLog.Information($"IntendedUse: {territory.TerritoryIntendedUse}, Name: {territory.Name ?? "No Name"}, PlaceName: {territory.PlaceName.Value?.Name ?? "No Name"}");


        if (!((TerritoryIntendedUse)territory.TerritoryIntendedUse).ShouldTrack())
            return;

        _dutyStarted = true;
        DutyStarted?.Invoke(new PvPStartedEventArgs(territory!));
    }

    private void OnPvPWiped(object? o, ushort territory)
    {
        PluginLog.Verbose("Duty Wipe");
        DutyWiped?.Invoke();
    }

    private void OnPvPRecommenced(object? o, ushort territory)
    {
        PluginLog.Verbose("Duty Recommenced");
        DutyRecommenced?.Invoke();
    }

    private void OnPvPEnded(object? o, ushort territory)
    {
        if (_dutyStarted)
        {
            PluginLog.Debug("Detected end of duty via DutyState.DutyCompleted");
            EndPvP(true);
        }
    }

    // This gets called before DutyState.DutyCompleted, so we can intercept in case the duty is abandoned instead of completed. 
    private void OnTerritoryChanged(object? o, ushort territoryType)
    {
        if (_dutyStarted && _dutyState.IsDutyStarted == false)
        {
            PluginLog.Debug("Detected end of duty via ClientState.TerritoryChanged");
            EndPvP(false);
        }
    }

    private void EndPvP(bool completed)
    {
        PluginLog.Verbose($"Duty Ended. Completed: {completed}");
        _dutyStarted = false;
        DutyEnded?.Invoke(new PvPEndedEventArgs(completed));
    }

    public void Dispose()
    {
        _dutyState.DutyStarted -= OnPvPStarted;
        _dutyState.DutyWiped -= OnPvPWiped;
        _dutyState.DutyRecommenced -= OnPvPRecommenced;
        _dutyState.DutyCompleted -= OnPvPEnded;
        _clientState.TerritoryChanged -= OnTerritoryChanged;
    }
}
