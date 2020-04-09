using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Network;
using ISMP_ClientMod.Conf;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.ObjectBuilders;

namespace ISMP_ClientMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false, new string[] { "SmallProgrammableBlock", "LargeProgrammableBlock" })]

    class ISMPGameLogics : MyGameLogicComponent
    {

        IMyTerminalControlCombobox ScriptDropBox;

        static bool Initialized = false;
        public override void OnAddedToContainer()
        {
            if (Initialized)
                return;

            AddPBScriptSelector();
            HideEditButton();
            //base.OnAddedToContainer();
            Initialized = true;
        }
        private void AddPBScriptSelector()
        {
            ScriptDropBox =
                MyAPIGateway.TerminalControls.CreateControl<
                    IMyTerminalControlCombobox,
                    IMyProgrammableBlock>("ScriptWhitelist");
            ScriptDropBox.Title = MyStringId.GetOrCompute("Active Script");
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(ScriptDropBox);

            ScriptDropBox.ComboBoxContent = (List<MyTerminalControlComboBoxItem> items) =>
            {
                foreach (var script in WitheListData.Scripts)
                {
                    items.Add(new MyTerminalControlComboBoxItem()
                    {
                        Key = script.Key,
                        Value = MyStringId.GetOrCompute(script.Value)
                    });
                }
            };
            ScriptDropBox.Getter = GetActiveScript;
            ScriptDropBox.Setter = SetActiveScript;

        }
        private void HideEditButton()
        {
            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            var editButton = controls.Find((IMyTerminalControl control) => control.Id == "Edit") as IMyTerminalControlButton;
            var isVisible = editButton.Visible;
            editButton.Visible = (b) =>
            {
                var subtype = b.SlimBlock.BlockDefinition.Id.SubtypeId;
                if (subtype == MyStringHash.GetOrCompute("SmallProgrammableBlock")
                    || subtype == MyStringHash.GetOrCompute("LargeProgrammableBlock"))
                    return false;
                return isVisible?.Invoke(b) ?? false;
            };
        }
        private static long GetActiveScript(IMyTerminalBlock pb)
        {
            long l = Config.NOSCRIPT.Key;
            if (pb.Storage != null && pb.Storage.ContainsKey(Config.GUID))
            {
                Int64.TryParse(pb.Storage[Config.GUID], out l);
            }
            if (WitheListData.Scripts.ContainsKey(l))
            {
                return l;
            }
            ModLogger.Warning("No script with id '{0}' found in Whitelist.", l);
            return Config.NOSCRIPT.Key;
        }
        private static void SetActiveScript(IMyTerminalBlock pb, long l)
        {
            if (pb.Storage == null)
            {
                pb.Storage = new MyModStorageComponent
                {
                    [Config.GUID] = l.ToString()
                };
            }
            //(b as IMyProgrammableBlock).ProgramData = Scripts[l].Code;
            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                ISMP_ClientModCore.RequestPBRecompile(pb as IMyProgrammableBlock, l);
            }
        }
    }


}
