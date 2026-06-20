using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

// ReSharper disable once CheckNamespace
namespace IngameScript2
{
    public class Program : MyGridProgram
    {
IMyTextPanel lcd;

public void Main(string argument) {
    try {
        TryMain(argument);
        Me.GetSurface(0).WriteText(Me.CustomData + "\r\nOK");
    } catch(Exception exc) {
        Me.GetSurface(0).WriteText(Me.CustomData + "\r\nFail: " + DateTime.Now.TimeOfDay);
        lcd.WriteText(exc.ToString());
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

    var blockData = GetBlockData(relevantBlocks);

    foreach (var data in blockData) {
        lcd.WriteText(data.Block.CustomName + "\r\n", true);

        var direction = GetDirection(data.Block, relevantBlocks);

        var axis = GetAxis(direction);

        // make all relevant lights to have axis color
        if (data.Lights != null) {
            foreach (var light in data.Lights) {
                light.Color = GetAxisColor(axis);
            }
        }
    }
}

Base6Directions.Direction GetDirection(IMyMechanicalConnectionBlock block, Dictionary<IMyMechanicalConnectionBlock, List<IMyMechanicalConnectionBlock>> relevantBlocks) {
    // block.Orientation.Forward is not important (tested by placing 4 pistons with 0, 90, 180 and 270 deg rotation in each direction - Up vector was the same)
    var orientation = block.Orientation.Up;

    List<IMyMechanicalConnectionBlock> parents;
    while (relevantBlocks.TryGetValue(block, out parents)) {
        if (parents == null || parents.Count == 0) {
            break;
        }

        // TODO: no clue what to do when multiple parents are present ...
        var parent = parents[0];

        var merged = MergeWithParent(orientation, parent.Orientation.Up);

        lcd.WriteText("  " + parent.CustomName + ": " + parent.Orientation.Up + " => " + merged + "\r\n", true);

        orientation = merged;
        block = parent;
    }

    return orientation;
}

Base6Directions.Direction MergeWithParent(Base6Directions.Direction direction, Base6Directions.Direction parentDirection) {
    // TODO: Rotor/Hinge rotation will mess things up ...
    // need to fix parentDirection base on rotor angle:
    // valid angles 0,90,180 and 270 +- 20 (0 = -20 to 20, 90 = 70 to 110, ..)
    // maybe i can use block.Top.Orientation ?

    const Base6Directions.Direction Forward = Base6Directions.Direction.Forward;   //  X
    const Base6Directions.Direction Backward = Base6Directions.Direction.Backward; // -X
    const Base6Directions.Direction Left = Base6Directions.Direction.Left;         // -Y
    const Base6Directions.Direction Right = Base6Directions.Direction.Right;       //  Y
    const Base6Directions.Direction Up = Base6Directions.Direction.Up;             //  Z
    const Base6Directions.Direction Down = Base6Directions.Direction.Down;         // -Z

    Base6Directions.Direction[][] lookup = {
        /* Forward  */ new[] { Forward, Backward, Left, Right, Up, Down },// todo: fix those
        /* Backward */ new[] { Forward, Backward, Left, Right, Up, Down },// todo: fix those
        /* Left     */ new[] { Forward, Backward, Left, Right, Up, Down },// todo: fix those
        /* Right    */ new[] { Forward, Backward, Left, Right, Up, Down },// todo: fix those
        /* Up       */ new[] { Forward, Backward, Left, Right, Up, Down },// no orientation change here
        /* Down     */ new[] { Backward, Forward, Right, Left, Down, Up },// flip everything
    };

    return lookup[(int)parentDirection][(int)direction];
}

Color GetAxisColor(string axis) {
    switch (axis) {
        case "X":  return Color.Red;
        case "-X": return Color.Aqua;
        case "Y":  return Color.Green;
        case "-Y": return Color.Fuchsia;
        case "Z":  return Color.Blue;
        case "-Z": return Color.Yellow;
        default:   return Color.White;
    }
}

// convert direction to axis
string GetAxis(Base6Directions.Direction direction) {
    switch (direction) {
        case Base6Directions.Direction.Forward:  return "X";
        case Base6Directions.Direction.Backward: return "-X";
        case Base6Directions.Direction.Left:     return "Y";
        case Base6Directions.Direction.Right:    return "-Y";
        case Base6Directions.Direction.Up:       return "Z";
        case Base6Directions.Direction.Down:     return "-Z";
        default:                                 throw new Exception(); // will never happen, but compiler requires it ...
    }
}

List<BlockData> GetBlockData(Dictionary<IMyMechanicalConnectionBlock, List<IMyMechanicalConnectionBlock>> relevantBlocks) {
    var lights = new List<IMyLightingBlock>();
    GridTerminalSystem.GetBlocksOfType(lights);

    var list = new List<BlockData>();

    foreach (var pair in relevantBlocks) {
        var light = lights.Where(o => o.CustomName.StartsWith(pair.Key.CustomName)).ToList();

        list.Add(
            new BlockData {
                Block = pair.Key,
                Parents = pair.Value,
                Lights = light,
            }
        );
    }

    return list;
}

// relevant blocks are blocks on root`s sub grid or on any relevant block sub grid
Dictionary<IMyMechanicalConnectionBlock, List<IMyMechanicalConnectionBlock>> ResolveRelevantBlocks(IMyMechanicalConnectionBlock root) {
    // valid grid is  grid that is sub grid of root block
    // grid -> list of blocks that connects grid to root
    var validGrids = new Dictionary<IMyCubeGrid, List<IMyMechanicalConnectionBlock>> {
        { root.TopGrid, new List<IMyMechanicalConnectionBlock> { root } },
    };

    Action<IMyCubeGrid, IMyMechanicalConnectionBlock> addValidGrid = (grid, block) => {
        List<IMyMechanicalConnectionBlock> list;
        if (!validGrids.TryGetValue(grid, out list)) {
            list = new List<IMyMechanicalConnectionBlock>();
            validGrids.Add(grid, list);
        }

        list.Add(block);
    };

    var blocks = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(blocks, o => o is IMyPistonBase || o is IMyMotorStator);

    var validBlocks = new Dictionary<IMyMechanicalConnectionBlock, List<IMyMechanicalConnectionBlock>> { { root, null } };

    bool found;
    do {
        found = false;

        foreach (var block in blocks) {
            // skip already validated blocks (so i dont need to modify blocks list inside foreach)
            if (validBlocks.ContainsKey(block)) {
                continue;
            }

            List<IMyMechanicalConnectionBlock> parents;
            if (validGrids.TryGetValue(block.CubeGrid, out parents)) {
                // block is on valid grid
                addValidGrid(block.TopGrid, block);
                validBlocks.Add(block, parents);
                found = true;
            } else if (validGrids.TryGetValue(block.TopGrid, out parents)) {
                // block head is on valid grid
                addValidGrid(block.CubeGrid, block);
                validBlocks.Add(block, parents);
                found = true;
            }
        }
    } while (found);

    return validBlocks;
}

class BlockData
{
    public IMyMechanicalConnectionBlock Block;

    public List<IMyLightingBlock>              Lights;
    public List<IMyMechanicalConnectionBlock> Parents;
}
    }
}
