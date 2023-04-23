using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class RenameTopGrid : MyGridProgram
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
    GridTerminalSystem.GetBlocksOfType(blocks, o => o is IMyPistonBase || o is IMyMotorStator);
    foreach (var block in blocks) {
        block.TopGrid.CustomName = block.CustomName + "`s Grid";
    }
}
    }
}
