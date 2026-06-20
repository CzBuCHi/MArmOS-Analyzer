using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class BlockResolution : MyGridProgram
    {
IMyTextPanel lcd;

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
    lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
    lcd.WriteText("");

    var root = GridTerminalSystem.GetBlockWithName(argument) as IMyMechanicalConnectionBlock;
    if (root == null) {
        throw new Exception("Root block named '" + argument + "' not found");
    }

    var relevantBlocks = ResolveRelevantBlocks(root);


    foreach (var pair in relevantBlocks) {
        if (pair.Value != null) {
            lcd.WriteText(pair.Key.CustomName + " -> " + pair.Value.CustomName + "\r\n", true);
        }
    }

}

        // resolves relevant blocks
        // dictionary: block -> parent block
Dictionary<IMyMechanicalConnectionBlock, IMyMechanicalConnectionBlock> ResolveRelevantBlocks(IMyMechanicalConnectionBlock root) {
    var blocks = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(blocks, o => o is IMyPistonBase || o is IMyMotorStator);

    var validGrids = new Dictionary<IMyCubeGrid, IMyMechanicalConnectionBlock> { { root.TopGrid, root } };
    var validBlocks = new Dictionary<IMyMechanicalConnectionBlock, IMyMechanicalConnectionBlock> { { root, null } };

    bool found;
    do {
        found = false;
        blocks.RemoveAll(o => validBlocks.ContainsKey(o));

        foreach (var block in blocks) {

            IMyMechanicalConnectionBlock owner;
            if (validGrids.TryGetValue(block.CubeGrid, out owner)) {
                // todo: what to do when multiple parents are found? (parallel pistons etc.)
                if (!validGrids.ContainsKey(block.TopGrid)) {
                    validGrids.Add(block.TopGrid, block);
                }

                validBlocks.Add(block, owner);
                found = true;
            }
        }
    } while (found);

    return validBlocks;
}




    }
}
