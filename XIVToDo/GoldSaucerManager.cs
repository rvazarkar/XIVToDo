using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace XIVToDo
{
    public class GoldSaucerManager : IDisposable
    {
        [PluginService] private SigScanner SigScanner { get; set; }
        
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

        public void DrawGoldSaucerChild()
        {
            
        }
        
        public unsafe void UpdateGoldSaucerDetour(AgentInterface* agentInterface, IntPtr data)
        {
            PluginLog.LogInformation("Hooked function called");
            _miniCactbotTicketsPurchased= Marshal.ReadInt16(data + 0x2e);
            _miniCactbotTicketsAllowed = Marshal.ReadInt16(data + 0x30);
            var test = Marshal.ReadInt16(data + 0x10);
            PluginLog.LogInformation("Test: {val}", test);
            _updateGoldSaucerHook.Original(agentInterface, data);
        }

        public void Dispose()
        {
            _updateGoldSaucerHook.Dispose();
        }
    }
}