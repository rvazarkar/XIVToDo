using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace XIVToDo.Managers
{
    public class GoldSaucerManager : IDisposable
    {
        [PluginService] private SigScanner SigScanner { get; set; }
        [PluginService] private D3DTextureWrap TeleportTex { get; set; }
        [PluginService] private TeleportManager TeleportManager { get; set; }
        
        private static Hook<UpdateGoldSaucerInfoDelegate> _updateGoldSaucerHook;
        private unsafe delegate void UpdateGoldSaucerInfoDelegate(AgentInterface* agentInterface, IntPtr param2);

        private int _miniCactbotTicketsPurchased;
        private int _miniCactbotTicketsAllowed;
        private int _jumboCactbotTicketsPurchased;

        public GoldSaucerManager()
        {
            unsafe
            {
                _updateGoldSaucerHook = new Hook<UpdateGoldSaucerInfoDelegate>(
                    SigScanner.ScanText("40 55 41 55 41 56 ?? ?? ?? ?? ?? ?? ?? ?? 48 81 ec b0 01 00 00"),
                    UpdateGoldSaucerDetour);
                _updateGoldSaucerHook.Enable();    
            }
        }

        public void DrawGoldSaucerDailyItems()
        {
            if (ImGui.CollapsingHeader("Gold Saucer", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginChild(ToDoPlugin.GetID("goldSaucerChild"));
                ImGui.Columns(3, ToDoPlugin.GetID("goldSaucerColumns"));
                ImGui.SetColumnWidth(0, 90);
                ImGui.SetColumnWidth(1, 250);
                ImGui.SetColumnWidth(2, 30);
                ImGui.Text("Mini Cactbot");
                ImGui.NextColumn();
                ImGui.ProgressBar(_miniCactbotTicketsPurchased / (float)_miniCactbotTicketsAllowed, new Vector2(175, 20));
                ImGui.SameLine();
                ImGui.Text($"{_miniCactbotTicketsPurchased} /  {_miniCactbotTicketsAllowed}");
                ImGui.NextColumn();
                ImGui.PushID(ToDoPlugin.GetID($"#goldSaucerTeleport1"));
                if (ImGui.ImageButton(TeleportTex.ImGuiHandle, new Vector2(20, 25), Vector2.Zero, Vector2.One, 0,
                    Vector4.Zero, Vector4.One))
                {
                    TeleportManager.Teleport(62);
                }
                ImGui.EndChild();
            }
        }
        
        public unsafe void UpdateGoldSaucerDetour(AgentInterface* agentInterface, IntPtr data)
        {
            _miniCactbotTicketsPurchased= Marshal.ReadInt16(data + 0x2e);
            _miniCactbotTicketsAllowed = Marshal.ReadInt16(data + 0x30);
            var jumboPurchased = 0;
            //TODO: Test this logic
            const int offset = 0x28;
            for (var i = 0; i < 3; i++)
            {
                var num = Marshal.ReadInt16(data + offset + (i * 2));
                if (num is > 0 and < 10000)
                {
                    jumboPurchased++;
                }
            }
            PluginLog.LogInformation("Jumbo Tickets Purchased: {num}", jumboPurchased);
            _updateGoldSaucerHook.Original(agentInterface, data);
        }

        public void Dispose()
        {
            _updateGoldSaucerHook.Dispose();
        }
    }
}