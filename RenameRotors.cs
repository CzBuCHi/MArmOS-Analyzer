using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class RenameRotors : MyGridProgram
    {
public void Main(string argument) {
    try {
        TryMain(argument);
        Me.GetSurface(0).WriteText(Me.CustomData + "\r\nOK");
    } catch {
        Me.GetSurface(0).WriteText(Me.CustomData + "\r\nFail: " + DateTime.Now.TimeOfDay);
        throw;
    }
}

void TryMain(string argument) {
    var root = GridTerminalSystem.GetBlockWithName(argument) as IMyMechanicalConnectionBlock;
    if (root == null) {
        throw new Exception("Root block named '" + argument + "' not found");
    }

    ResolveRelevantBlocks(root);
}

void ResolveRelevantBlocks(IMyMechanicalConnectionBlock root) {
    var validGrids = new HashSet<IMyCubeGrid> { root.TopGrid };

    var blocks = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(blocks, o => o is IMyPistonBase || o is IMyMotorStator);

    var validBlocks = new HashSet<IMyMechanicalConnectionBlock> { root };

    var i = 1;
    bool found;
    do {
        found = false;

        foreach (var block in blocks) {
            if (validBlocks.Contains(block)) {
                continue;
            }

            if (validGrids.Contains(block.CubeGrid)) {
                block.CustomName = "Rotor #" + (i++);
                validGrids.Add(block.TopGrid);
                validBlocks.Add(block);
                found = true;
            } else if (validGrids.Contains(block.TopGrid)) {
                block.CustomName = "Rotor #" + (i++);
                validGrids.Add(block.CubeGrid);
                validBlocks.Add(block);
                found = true;
            }
        }
    } while (found);

    foreach (var block in validBlocks) {
        block.TopGrid.CustomName = block.CustomName + "`s Grid";
    }
}
    }
}
