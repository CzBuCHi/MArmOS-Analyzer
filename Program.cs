using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
// ReSharper disable IdentifierTypo
// ReSharper disable PartialTypeWithSinglePart

namespace IngameScript
{
    internal partial class Program : MyGridProgram
    {
// script to place inside programmable block is in between #if #endif


// too lazy to create second project for utility script ...
#if RENAME_SCRIPT

// simple script, that will rename all rotor/hinge/piston sub grids to "[NAME_OF_ROTOR]`s Grid"
public void Main(string argument) {
    var blocks = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(blocks, o => o is IMyPistonBase || o is IMyMotorStator);
    foreach (var block in blocks) {
        block.TopGrid.CustomName = block.CustomName + "`s Grid";
    }
}

#else

// call this from ui with name of root rotor/hinge/piston block as argument ...
public void Main(string argument) {
    var root = GridTerminalSystem.GetBlockWithName(argument) as IMyMechanicalConnectionBlock;
    if (root == null) {
        Echo("Root block named '" + argument + "' not found");
        return;
    }

    // resolves relevant blocks

    // parents arent use now, but i may need them to resolve axis ...
    Dictionary<IMyMechanicalConnectionBlock, IMyMechanicalConnectionBlock> parents;
    List<IMyMechanicalConnectionBlock> blocks;
    FindArmBlocks(root, out parents, out blocks);

    var commands = new List<string>();

    foreach (var block in blocks) {
        Echo(block.CustomName + " on " + block.CubeGrid.CustomName);

        if (block != root && block is IMyMotorStator) {
            commands.Add(BuildSolid(block));
        }

        var axis = GetAxis(block);

        if (block is IMyPistonBase) {
            IMyPistonBase piston = (IMyPistonBase)block;
            commands.Add(BlockConfiguration("Piston", block.CustomName, axis, GetPosition(piston)));
        }

        if (block is IMyMotorStator) {
            IMyMotorStator motor = (IMyMotorStator)block;
            commands.Add( BlockConfiguration("Rotor", block.CustomName, axis, GetAngle(motor)));
        }
    }

    // save generated commands to programmable blocks Custom Data field
    Me.CustomData = string.Join("\r\n", commands);
}


/*
 * to my understanding those two solids have same definition: new SolidLG(7, 2, 0)
 * which are coordinates of second rotor in first rotor sub grid ...
 *
 *  O: right facing rotor
 *  X: armor block
 *
 *    1234567
 *   OX      
 *    X      
 *    XXXXXXO
 *          
 *   OX XXX  
 *    X X XX 
 *    XXX  XO
 *
 *   
 */
string BuildSolid(IMyMechanicalConnectionBlock block) {
    var type = block.CubeGrid.GridSizeEnum == MyCubeSize.Small ? "SolidSG" : "SolidLG";
    var x = block.Position.X;
    var y = block.Position.Y;
    var z = block.Position.Z;

    Echo(block.CustomName + "(" + x + ", " + y + ", " + z + ")");
    return "    new " + type + "(" + x + ", " + y + ", " + z + ");";
}


// this will get all rotors, hinges and pistons that are on sub-grid of root block
void FindArmBlocks(IMyMechanicalConnectionBlock root, out Dictionary<IMyMechanicalConnectionBlock, IMyMechanicalConnectionBlock> parents, out List<IMyMechanicalConnectionBlock> blocks) {

    // TODO: what if grid have multiple parents? (aka parallel hinges, etc)

    // block -> parent block
    parents = new Dictionary<IMyMechanicalConnectionBlock, IMyMechanicalConnectionBlock> { { root, null } };
    blocks = new List<IMyMechanicalConnectionBlock> { root };

    // get all rotors, hinges and pistons on programmable block grid and sub grids
    var candidates = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(candidates, o => o is IMyPistonBase || o is IMyMotorStator);

    // grid -> parent block
    var knownGrids = new Dictionary<IMyCubeGrid, IMyMechanicalConnectionBlock> { { root.TopGrid, root } };
    
    bool found;
    do {
        found = false;

        foreach (var block in candidates) {
            if (blocks.Contains(block)) {
                continue;
            }

            // check if block grid is known sub grid of root
            IMyMechanicalConnectionBlock parent;
            if (knownGrids.TryGetValue(block.CubeGrid, out parent)) {
                blocks.Add(block);
                knownGrids.Add(block.TopGrid, block);
                parents.Add(block, parent);
                found = true;
            }

            // TODO: deal with reverse placed blocks (aka head first)
        }

        // when new block found its sub grid needs to be checked too ...
    } while (found);
}

// convert block orientation to axis
string GetAxis(IMyMechanicalConnectionBlock block) {
    // block.Orientation.Forward is not important here (tested by placing 4 pistons with 0, 90, 180 and 270 deg rotation in each direction)
    switch (block.Orientation.Up) {
        case Base6Directions.Direction.Forward:  return "X";
        case Base6Directions.Direction.Backward: return "-X";
        case Base6Directions.Direction.Left:     return "Y";
        case Base6Directions.Direction.Right:    return "-Y";
        case Base6Directions.Direction.Up:       return "Z";
        case Base6Directions.Direction.Down:     return "-Z";
        default:                                 return "???"; // this will never happen, but compiler is happy
    }
}

// returns piston position
double GetPosition(IMyPistonBase piston) {
    // rounding so i dont have too crazy values in config ...
    return Math.Round(piston.CurrentPosition, 2);
}

// returns angle in 0 to 359 deg for rotor and -90 to 90 for hinge
double GetAngle(IMyMotorStator motor) {
    var angle = Math.Round(180 / Math.PI * motor.Angle, 0);

    // correct angle to 0-359
    angle = (angle + 360) % 360;

    // TODO: Find better way to distinguish hinge from rotor than this ...
    var isHinge = motor.CustomName.Contains("Hinge");
    if (isHinge && angle > 90) {
        // for hinge use range -90 to 90 instead of 0 to 90 and 270 to 360
        angle -= 360;
    }

    return angle;
}

// generate configuration
string BlockConfiguration(string type, string name, string axis, double home) {
    var ret = "    new " + type + "(Name: \"" + name + "\", Axis: \"" + axis + "\"";

    if (type == "Rotor") {
        ret += ", OriMode: 0";
    }

    if (home != 0) {
        ret += ", Home: " + home;
    }

    return ret + ");";
}

#endif


    }
}
