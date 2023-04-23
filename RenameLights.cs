using System;
using System.Collections.Generic;
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

    var gridsByBlock = new Dictionary<IMyCubeGrid, IMyMechanicalConnectionBlock>();
    foreach (var block in blocks) {
        if (gridsByBlock.ContainsKey(block.CubeGrid)) {
            throw new Exception("Duplicate: " + block.CubeGrid.CustomName);
        }

        gridsByBlock[block.CubeGrid] = block;
    }

    foreach (var light in lights) {
        IMyMechanicalConnectionBlock block;
        if (gridsByBlock.TryGetValue(light.CubeGrid, out block)) {
            light.CustomName = "Light: " + block.CustomName;
        }
    }
}
    }
}
