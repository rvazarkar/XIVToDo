using System;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using ImGuiNET;

namespace XIVToDo
{
    public class UI : IDisposable
    {
        private int _selectedTab;

        private bool _visible;
        private bool _beastTribesComplete;

        [PluginService] private Configuration Configuration { get; set; }
        [PluginService] private SigScanner SigScanner { get; set; }
        [PluginService] private TextureStore TextureStore { get; set; }
        [PluginService] private ClientState ClientState { get; set; }
        [PluginService] private BeastTribeManager BeastTribeManager { get; set; }

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawWindow();
        }

        private void DrawWindow()
        {
            if (!_visible) return;

            ImGui.SetNextWindowSize(new Vector2(600, 400) * ImGui.GetIO().FontGlobalScale, ImGuiCond.FirstUseEver);
            var avail = ImGui.GetContentRegionAvail().X;

            if (ImGui.Begin("XIVToDo", ref _visible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGui.BeginTabBar("XIVTodoTabBar", ImGuiTabBarFlags.NoTooltip))
                {
                    if (ImGui.BeginTabItem("Daily"))
                    {
                        _selectedTab = 0;
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Weekly"))
                    {
                        _selectedTab = 1;
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Custom"))
                    {
                        _selectedTab = 2;
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                if (_selectedTab == 0)
                {
                    DrawDailyWindow();
                }
                ImGui.End();
            }
        }

        private void DrawDailyWindow()
        {
            ImGui.BeginChild("#ToDoWeeklySection", new Vector2(400, 400), false,
                ImGuiWindowFlags.AlwaysAutoResize);
            DrawSectionLabel("Beast Tribes");
            DrawBeastTribeToDoItems();
            ImGui.EndChild();
        }

        private void DrawSectionLabel(string label)
        {
            ImGui.Text(label);
        }

        private void DrawToDoItem(string text, int secondsLeft, ref bool val)
        {
            var labelText = $"{text} - {DateTime.Now.AddSeconds(secondsLeft).ToShortTimeString()}";
            ImGui.Checkbox(labelText, ref val);
            var tex = TextureStore.GetTexture(111);
            ImGui.SameLine();
            ImGui.ImageButton(tex.ImGuiHandle, new Vector2(20, 25), Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One);
        }

        private void DrawBeastTribeToDoItems()
        {
            for (var i = 1; i < BeastTribeManager.GetMaxIndex(); i++)
            {
                BeastTribeManager.DrawBeastTribeItem((ushort) i);
            }
        }
    }
}