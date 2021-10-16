using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace XIVToDo
{
    public class BeastTribeManager
    {
        [PluginService] private SigScanner SigScanner { get; set; }
        [PluginService] private DataManager DataManager { get; set; }
        [PluginService] private D3DTextureWrap TeleportTex { get; set; }

        private unsafe delegate int ExecuteGetBeastTribeRankDelegate(PlayerState* playerState, ushort beastTribeIndex);
        private unsafe delegate int ExecuteGetBeastTribeReputationDelegate(PlayerState* playerState, ushort beastTribeIndex);
        private unsafe delegate int ExecuteGetBeastTribeCurrentRepDelegate(PlayerState* playerState, ushort beastTribeIndex);
        private static ExecuteGetBeastTribeRankDelegate _getBeastTribeRank;
        private static ExecuteGetBeastTribeReputationDelegate _getBeastTribeNeededReputation;
        private static ExecuteGetBeastTribeCurrentRepDelegate _getBeastTribeCurrentReputation;

        private readonly ExcelSheet<BeastTribe> _beastTribes;
        private readonly unsafe PlayerState* _playerState;
        private readonly int _maxIndex;

        public BeastTribeManager()
        {
            _getBeastTribeRank =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeRankDelegate>(
                    SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 C8 FF C9"));
            _getBeastTribeNeededReputation =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeReputationDelegate>(
                    SigScanner.ScanText("E8 ?? ?? ?? ?? 66 3B D8 41 0F 43 FE"));
            _getBeastTribeCurrentReputation =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeCurrentRepDelegate>(
                    SigScanner.ScanText("e8 ?? ?? ?? ?? 0f b7 c0 8d 57 74"));
            
            _beastTribes = DataManager.GameData.GetExcelSheet<BeastTribe>();
            unsafe
            {
                _playerState = &UIState.Instance()->PlayerState;    
            }
            
            _maxIndex = (int)_beastTribes.Max(x => x.RowId);
        }

        public int GetMaxIndex()
        {
            return _maxIndex;
        }

        public unsafe void DrawBeastTribeItem(ushort beastTribeID)
        {
            var row = _beastTribes?.DefaultIfEmpty(null).FirstOrDefault(x => x.RowId == beastTribeID);
            if (row == null)
                return;

            var currentRep = _getBeastTribeCurrentReputation(_playerState, beastTribeID);
            var maxRep = _getBeastTribeNeededReputation(_playerState, beastTribeID);

            if (maxRep == 0)
                return;

            ImGui.SetColumnWidth(0, 90);
            ImGui.SetColumnWidth(1, 250);
            ImGui.SetColumnWidth(2, 30);
            ImGui.Separator();
            ImGui.Text(row.Name.ToString().FirstCharToUpper());
            ImGui.NextColumn();
            ImGui.ProgressBar(currentRep / (float)maxRep, new Vector2(175, 20));
            ImGui.SameLine();
            ImGui.Text($"{currentRep} /  {maxRep}");
            ImGui.NextColumn();
            ImGui.ImageButton(TeleportTex.ImGuiHandle, new Vector2(20, 25), Vector2.Zero, Vector2.One, 0,
                Vector4.Zero, Vector4.One);
            ImGui.NextColumn();
        }
    }
}