﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace XIVToDo.Managers
{
    public class BeastTribeManager
    {
        [PluginService] private SigScanner SigScanner { get; set; }
        [PluginService] private DataManager DataManager { get; set; }
        [PluginService] private D3DTextureWrap TeleportTex { get; set; }
        [PluginService] private TeleportManager TeleportManager { get; set; }
        private unsafe delegate int ExecuteGetBeastTribeReputationDelegate(PlayerState* playerState, ushort beastTribeIndex);
        private unsafe delegate int ExecuteGetBeastTribeCurrentRepDelegate(PlayerState* playerState, ushort beastTribeIndex);
        private unsafe delegate int ExecuteGetBeastTribeAllowances(void* globalBeastTribeThingy);

        private unsafe void* _globalBeastTribeThing;
        
        private static ExecuteGetBeastTribeReputationDelegate _getBeastTribeNeededReputation;
        private static ExecuteGetBeastTribeCurrentRepDelegate _getBeastTribeCurrentReputation;
        private static ExecuteGetBeastTribeAllowances _executeGetBeastTribeAllowances;

        private readonly ExcelSheet<BeastTribe> _beastTribes;
        private readonly unsafe PlayerState* _playerState;
        private readonly int _maxIndex;

        private readonly Dictionary<uint, uint> _aetheryteMap = new()
        {
            {1, 19},
            {2, 4},
            {3, 16},
            {4, 14},
            {5, 7},
            {6, 73},
            {7, 76},
            {8, 78},
            {9, 105},
            {10, 99},
            {11, 128},
            {12, 144},
            {13, 143},
            {14, 139}
        };

        public BeastTribeManager()
        {
            _getBeastTribeNeededReputation =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeReputationDelegate>(
                    SigScanner.ScanText("E8 ?? ?? ?? ?? 66 3B D8 41 0F 43 FE"));
            _getBeastTribeCurrentReputation =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeCurrentRepDelegate>(
                    SigScanner.ScanText("e8 ?? ?? ?? ?? 0f b7 c0 8d 57 74"));
            
            _executeGetBeastTribeAllowances =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeAllowances>(
                    SigScanner.ScanText("E8 ?? ?? ?? ?? 49 8B 8F ?? ?? ?? ?? 44 8B E0"));
            _beastTribes = DataManager.GameData.GetExcelSheet<BeastTribe>();
            unsafe
            {
                _playerState = &UIState.Instance()->PlayerState;
                _globalBeastTribeThing = (void*)SigScanner.GetStaticAddressFromSig("48 8d 0d ?? ?? ?? ?? e8 c9 d3 38 00");
            }
            
            _maxIndex = (int)_beastTribes.Max(x => x.RowId);
        }

        private int GetMaxIndex()
        {
            return _maxIndex;
        }
        
        public unsafe void DrawBeastTribeItems()
        {
            var header = $"Beast Tribes ({_executeGetBeastTribeAllowances(_globalBeastTribeThing)} Allowances)";
            
            if (ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginChild(ToDoPlugin.GetID("beastTribeChild"));
                ImGui.Columns(3, ToDoPlugin.GetID("beastTribeColumns"),false);
                ImGui.SetColumnWidth(0, 90);
                ImGui.SetColumnWidth(1, 250);
                ImGui.SetColumnWidth(2, 30);
            
                for (var i = 1; i < GetMaxIndex(); i++)
                {
                    DrawBeastTribeItem((ushort) i);
                }
                ImGui.EndChild();
            }
            
        }

        private unsafe void DrawBeastTribeItem(ushort beastTribeID)
        {
            var row = _beastTribes?.DefaultIfEmpty(null).FirstOrDefault(x => x.RowId == beastTribeID);
            if (row == null)
                return;

            var maxRep = _getBeastTribeNeededReputation(_playerState, beastTribeID);

            if (maxRep == 0)
                return;
            
            var currentRep = _getBeastTribeCurrentReputation(_playerState, beastTribeID);
            
            ImGui.Text(row.Name.ToString().FirstCharToUpper());
            ImGui.NextColumn();
            ImGui.ProgressBar(currentRep / (float)maxRep, new Vector2(175, 20));
            ImGui.SameLine();
            ImGui.Text($"{currentRep} /  {maxRep}");
            ImGui.NextColumn();
            ImGui.PushID(ToDoPlugin.GetID($"#beastTribeTeleport{beastTribeID}"));
            if (ImGui.ImageButton(TeleportTex.ImGuiHandle, new Vector2(20, 25), Vector2.Zero, Vector2.One, 0,
                Vector4.Zero, Vector4.One))
            {
                PluginLog.LogInformation("Trying teleport to {location}", _aetheryteMap[beastTribeID]);
                TeleportManager.Teleport(_aetheryteMap[beastTribeID]);
            }
            ImGui.PopID();
            ImGui.NextColumn();
        }
    }
}