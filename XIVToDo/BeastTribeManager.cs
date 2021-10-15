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
        private static ExecuteGetBeastTribeRankDelegate _getBeastTribeRank;
        private static ExecuteGetBeastTribeReputationDelegate _getBeastTribeReputation;

        private readonly ExcelSheet<BeastTribe> _beastTribes;
        private readonly ExcelSheet<BeastReputationRank> _beastRep;
        private readonly unsafe PlayerState* _playerState;
        private readonly int _maxIndex;
        private ConcurrentDictionary<ushort, int> _repCache = new();
        private ConcurrentDictionary<ushort, int> _repRankCache = new();

        public BeastTribeManager()
        {
            _getBeastTribeRank =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeRankDelegate>(
                    SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 C8 FF C9"));
            _getBeastTribeReputation =
                Marshal.GetDelegateForFunctionPointer<ExecuteGetBeastTribeReputationDelegate>(
                    SigScanner.ScanText("E8 ?? ?? ?? ?? 66 3B D8 41 0F 43 FE"));
            
            _beastTribes = DataManager.GameData.GetExcelSheet<BeastTribe>();
            _beastRep = DataManager.GameData.GetExcelSheet<BeastReputationRank>();
            unsafe
            {
                _playerState = &UIState.Instance()->PlayerState;    
            }
            
            _maxIndex = (int)_beastTribes.Max(x => x.RowId);
            unsafe
            {
                for (ushort i = 1; i < _maxIndex; i++)
                {
                    var currentRep = _getBeastTribeReputation(_playerState, i);
                    var currentRank = _getBeastTribeRank(_playerState, i);
                    _repCache[i] = currentRep;
                    _repRankCache[i] = currentRank;
                }    
            }
            
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

            var avail = ImGui.GetContentRegionAvail().X;
            var currentRep = _getBeastTribeReputation(_playerState, beastTribeID);
            var currentRank = _getBeastTribeRank(_playerState, beastTribeID);
            var maxRep = _beastRep.GetRow((uint) currentRank)?.RequiredReputation;

            if (maxRep == null)
                return;
            
            ImGui.PushItemWidth(avail * .25f);
            ImGui.Text(row.Name);
            ImGui.SameLine();
            ImGui.PushItemWidth(avail * .25f);
            ImGui.ProgressBar(currentRep / (float)maxRep);
            ImGui.SameLine();
            ImGui.PushItemWidth(avail * .25f);
            ImGui.Text($"{currentRep} /  {maxRep}");
            ImGui.SameLine();
            ImGui.PushItemWidth(avail * .25f);
            ImGui.ImageButton(TeleportTex.ImGuiHandle, new Vector2(20, 25), Vector2.Zero, Vector2.One, 0,
                Vector4.Zero, Vector4.One);
        }
    }
}