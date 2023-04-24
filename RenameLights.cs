using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class RenameLights : MyGridProgram
    {
public void Main() {
    try {
        TryMain();
        Me.GetSurface(0).WriteText(Me.CustomData + "\r\nOK");
    } catch (Exception exc) {
        Me.GetSurface(0).WriteText(Me.CustomData + "\r\nFail: " + DateTime.Now.TimeOfDay);
    }
}

void TryMain() {
    var blocks = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(blocks, o => (o is IMyPistonBase || o is IMyMotorStator) && !o.CustomName.StartsWith("_"));

    var lights = new List<IMyLightingBlock>();
    GridTerminalSystem.GetBlocksOfType(lights);

    foreach (var light in lights) {

        var block = blocks.FirstOrDefault(o => o.CubeGrid == light.CubeGrid && light.Position.RectangularDistance(o.Position) == 1);

        if (block != null) {
            light.CustomName = block.CustomName + "`s light";
            light.ShowInTerminal = false;
        }
    }
}
    }
}
