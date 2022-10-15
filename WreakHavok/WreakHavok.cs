using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System.IO;
using System.Reflection;

namespace WreakHavok
{
    public class WreakHavok : IDalamudPlugin
    {
        public string Name => "Wreak Havok";

        private const string CommandName = "/havok";

        private WreakHavokUI UI { get; init; }

        public WreakHavok([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            DalamudContainer.Initialize(pluginInterface);

            UI = new WreakHavokUI();
            
            DalamudContainer.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Display Havok with /havok"
            });

            DalamudContainer.PluginInterface.UiBuilder.Draw += DrawUI;
            FFXIVClientStructs.Resolver.InitializeParallel();
        }

        public void Dispose()
        {
            UI.Dispose();
            DalamudContainer.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            if (command == "hinspector")
                UI.InspectorVisible = true;
            if (command == "hexperiment")
                UI.ExperimentsVisible = true;
        }

        private void DrawUI()
        {
            UI.Draw();
        }
    }
}
