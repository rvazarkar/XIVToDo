using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using SigScanner = Dalamud.Game.SigScanner;

namespace XIVToDo
{
    public class ToDoPlugin : IDalamudPlugin
    {
        private const string Command = "/todo";

        private readonly UI _ui;
        [PluginService] private DataManager DataManager { get; set; }

        [PluginService] private GameGui GameGui { get; set; }
        [PluginService] private SigScanner SigScanner { get; set; }
        private unsafe PlayerState* _playerState;

        private static ExecuteGetBeastTribeRankDelegate _getBeastTribeRank;
        private static ExecuteGetBeastTribeReputationDelegate _getBeastTribeReputation;
        private static Hook<UpdateGoldSaucerInfoDelegate> _updateGoldSaucerHook;

        private unsafe delegate int ExecuteGetBeastTribeRankDelegate(PlayerState* playerState, ushort beastTribeIndex);

        private unsafe delegate int ExecuteGetBeastTribeReputationDelegate(PlayerState* playerState, ushort beastTribeIndex);

        private unsafe delegate void UpdateGoldSaucerInfoDelegate(AgentInterface* agentInterface, IntPtr param2);
        

        public ToDoPlugin()
        {
            Resolver.Initialize();
            var config = PluginInterface.Create<Configuration>();
            //if (!PluginInterface.Inject(config)) PluginLog.Error("Could not satisfy requirements for Configuration");
            var texStore = PluginInterface.Create<TextureStore>();
            var teleportTexture = texStore.GetTexture(111);
            PluginLog.LogInformation($"{teleportTexture == null}");
            //if (!PluginInterface.Inject(texStore)) PluginLog.Error("Could not satisfy requirements for TextureStore");
            var beastTribeManager = PluginInterface.Create<BeastTribeManager>(teleportTexture);
            if (!PluginInterface.Inject(beastTribeManager)) PluginLog.Error("Could not satisfy requirements for BeastTribeManager");
            //if (!PluginInterface.Inject(beastTribeManager)) PluginLog.Error("Could not satisfy requirements for BeastTribeManager");
            var ui = PluginInterface.Create<UI>(config, texStore, beastTribeManager);
            //if (!PluginInterface.Inject(ui,config, texStore)) PluginLog.Error("Could not satisfy requirements for UI");

            unsafe
            {
                _updateGoldSaucerHook = new Hook<UpdateGoldSaucerInfoDelegate>(
                    SigScanner.ScanText("40 55 41 55 41 56 ?? ?? ?? ?? ?? ?? ?? ?? 48 81 ec b0 01 00 00"),
                    UpdateGoldSaucerDetour);
                _updateGoldSaucerHook.Enable();
            }
            
            _ui = ui;

            CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the ToDo window"
            });

            PluginInterface.UiBuilder.OpenConfigUi += () => { OnCommand(null, null); };
            PluginInterface.UiBuilder.Draw += DrawUI;
        }

        [PluginService] private DalamudPluginInterface PluginInterface { get; set; }

        [PluginService] private CommandManager CommandManager { get; set; }

        public string AssemblyLocation { get; set; } = Assembly.GetExecutingAssembly().Location;

        public static unsafe void UpdateGoldSaucerDetour(AgentInterface* agentInterface, IntPtr data)
        {
            PluginLog.LogInformation("Hooked function called");
            var ticketsPurchased = Marshal.ReadInt16(data + 0x2e);
            var ticketsAllowed = Marshal.ReadInt16(data + 0x30);
            var test = Marshal.ReadInt16(data + 0x10);
            PluginLog.LogInformation($"TicketsA: {ticketsPurchased} / {ticketsAllowed}");
            PluginLog.LogInformation("Test: {val}", test);
            _updateGoldSaucerHook.Original(agentInterface, data);
        }

        public void Dispose()
        {
            _ui.Dispose();
            CommandManager.RemoveHandler(Command);
            PluginInterface.Dispose();
            _updateGoldSaucerHook?.Dispose();
        }

        public string Name => "XIVTODO List";

        private void DrawUI()
        {
            _ui.Draw();
        }

        private void OnCommand(string command, string args)
        {
            _ui.Visible = !_ui.Visible;
        }
    }
}