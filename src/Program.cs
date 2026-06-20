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

// NOTE: next line is used as marker to automagically  extract script code to Script.txt
// >>>>>>>>>>>>>
/*
MArmOS arm analyzer for Space Engineers programmable block.

Usage:
1. Paste this script into a programmable block.
2. Run it with the root mechanical block name as the argument.
3. Read the generated MArmOS definition from Custom Data.
*/

const string ScriptNewLine = "\r\n";

// Run from terminal with the root rotor / hinge / piston name as argument.
public void Main(string argument) {
    try {
        RunAnalyzer(argument);
    } catch (Exception exc) {
        Echo("Analyzer failed");
        Echo(exc.Message);
    }
}

// Validates input, resolves the arm chain, then writes generated commands to Custom Data.
void RunAnalyzer(string argument) {
    argument = argument == null ? "" : argument.Trim();
    if (argument.Length == 0) {
        Echo("Usage: run with root joint name");
        Echo("Example: 'Arm - Base Rotor'");
        return;
    }

    var root = GridTerminalSystem.GetBlockWithName(argument) as IMyMechanicalConnectionBlock;
    if (root == null) {
        Echo("Root joint not found: " + argument);
        return;
    }

    if (root.TopGrid == null) {
        Echo("Root joint has no attached top grid");
        return;
    }

    var blocks = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(blocks, IsSupportedJoint);

    var chain = ResolveSerialChain(root, blocks);
    var commands = BuildCommands(chain);

    Me.CustomData = string.Join(ScriptNewLine, commands);

    Echo("Analyzer OK");
    Echo("Joints: " + chain.Count);
    Echo("Output lines: " + commands.Count);
    Echo("Saved to Custom Data");

    for (var i = 0; i < chain.Count; i++) {
        Echo((i + 1) + ": " + chain[i].CustomName + " [" + GetJointKind(chain[i]) + "]");
    }
}

// Limits the scan to the mechanical joints that can appear in a MArmOS arm definition.
bool IsSupportedJoint(IMyMechanicalConnectionBlock block) {
    return block is IMyMotorStator || block is IMyPistonBase;
}

// Walks through attached top grids and builds one serial chain starting at the chosen root block.
List<IMyMechanicalConnectionBlock> ResolveSerialChain(IMyMechanicalConnectionBlock root, List<IMyMechanicalConnectionBlock> blocks) {
    var chain = new List<IMyMechanicalConnectionBlock> { root };
    var current = root;

    while (true) {
        var next = FindNextJoint(current, blocks, chain);
        if (next == null) {
            return chain;
        }

        chain.Add(next);
        current = next;

        if (current.TopGrid == null) {
            return chain;
        }
    }
}

// Finds the next supported joint on the current top grid and rejects branches.
IMyMechanicalConnectionBlock FindNextJoint(
    IMyMechanicalConnectionBlock current,
    List<IMyMechanicalConnectionBlock> blocks,
    List<IMyMechanicalConnectionBlock> chain
) {
    var childGrid = current.TopGrid;
    if (childGrid == null) {
        return null;
    }

    IMyMechanicalConnectionBlock found = null;

    for (var i = 0; i < blocks.Count; i++) {
        var block = blocks[i];
        if (block == null || block == current || chain.Contains(block)) {
            continue;
        }

        if (block.CubeGrid != childGrid) {
            continue;
        }

        if (found != null) {
            throw new Exception(
                "Branch detected on grid '" + childGrid.CustomName + "' between '" + found.CustomName + "' and '" + block.CustomName + "'"
            );
        }

        found = block;
    }

    return found;
}

// Emits the final MArmOS command list in chain order.
List<string> BuildCommands(List<IMyMechanicalConnectionBlock> chain) {
    var commands = new List<string>();

    for (var i = 0; i < chain.Count; i++) {
        var current = chain[i];

        if (current is IMyPistonBase) {
            commands.Add(BuildPistonBodySolid(current));
        }

        commands.Add(BuildJointCommand(current));

        if (i + 1 < chain.Count) {
            var solid = BuildSolidCommand(current, chain[i + 1]);
            if (solid != null) {
                commands.Add(solid);
            }
        }
    }

    return commands;
}

// Builds one Rotor(...) or Piston(...) command, including home when it is not zero.
string BuildJointCommand(IMyMechanicalConnectionBlock block) {
    var type = block is IMyPistonBase ? "Piston" : "Rotor";
    var axis = GetAxis(block);
    var home = GetHomeValue(block);
    var command = "new " + type + "(\"" + block.CustomName + "\", \"" + axis + "\"";

    if (home != 0) {
        command += ", Home: " + FormatNumber(home);
    }

    return command + ");";
}

// Builds the solid segment between two joints using the current top-grid coordinate space.
string BuildSolidCommand(IMyMechanicalConnectionBlock current, IMyMechanicalConnectionBlock next) {
    if (current.TopGrid == null || next.TopGrid == null) {
        throw new Exception("Cannot build solid for detached joint");
    }

    if (ShouldSkipSolidBetween(current, next)) {
        return null;
    }

    var childGrid = current.TopGrid;
    var delta = next.TopGrid.WorldMatrix.Translation - childGrid.WorldMatrix.Translation;
    var unit = childGrid.GridSize;

    var x = SnapSolid(Vector3D.Dot(delta, childGrid.WorldMatrix.Forward) / unit);
    var y = SnapSolid(Vector3D.Dot(delta, childGrid.WorldMatrix.Left) / unit);
    var z = SnapSolid(Vector3D.Dot(delta, childGrid.WorldMatrix.Up) / unit);

    if (next is IMyPistonBase) {
        var pistonSolid = GetPistonBodyVector(next);
        x -= pistonSolid.X;
        y -= pistonSolid.Y;
        z -= pistonSolid.Z;
    }

    if (x == 0 && y == 0 && z == 0) {
        return null;
    }

    var type = childGrid.GridSizeEnum == MyCubeSize.Small ? "SolidSG" : "SolidLG";
    return "new " + type + "(" + x + ", " + y + ", " + z + ");";
}

// Stacked pistons along the same axis do not need an extra solid between them.
bool ShouldSkipSolidBetween(IMyMechanicalConnectionBlock current, IMyMechanicalConnectionBlock next) {
    var currentPiston = current as IMyPistonBase;
    var nextPiston = next as IMyPistonBase;
    if (currentPiston == null || nextPiston == null) {
        return false;
    }

    var currentAxis = GetPistonAxis(currentPiston);
    var nextAxis = GetPistonAxis(nextPiston);
    return currentAxis == nextAxis;
}

// Emits the collapsed piston body as a solid placed before the piston command.
string BuildPistonBodySolid(IMyMechanicalConnectionBlock block) {
    if (block.TopGrid == null) {
        throw new Exception("Cannot build piston solid for detached joint");
    }

    var vector = GetPistonBodyVector(block);
    var type = block.TopGrid.GridSizeEnum == MyCubeSize.Small ? "SolidSG" : "SolidLG";
    return "new " + type + "(" + vector.X + ", " + vector.Y + ", " + vector.Z + ");";
}

string GetJointKind(IMyMechanicalConnectionBlock block) {
    return block is IMyPistonBase ? "Piston" : "Rotor";
}

// Dispatches axis detection based on mechanical block type.
string GetAxis(IMyMechanicalConnectionBlock block) {
    if (block is IMyMotorStator) {
        return GetRotorAxis(block);
    }

    if (block is IMyPistonBase) {
        return GetPistonAxis(block);
    }

    throw new Exception("Unsupported joint axis");
}

// Rotors use local Z as spin axis, while hinges use local Y.
string GetRotorAxis(IMyMechanicalConnectionBlock block) {
    var motor = block as IMyMotorStator;
    if (motor != null && IsProbablyHinge(motor)) {
        return GetAxisFromLocalVector(block, new Vector3I(0, 1, 0));
    }

    return GetAxisFromLocalVector(block, new Vector3I(0, 0, 1));
}

// Pistons extend along their local Z axis.
string GetPistonAxis(IMyMechanicalConnectionBlock block) {
    return GetAxisFromLocalVector(block, new Vector3I(0, 0, 1));
}

// Converts a block-local axis vector into a MArmOS axis string in base-grid space.
string GetAxisFromLocalVector(IMyMechanicalConnectionBlock block, Vector3I localAxis) {
    var axis = TransformLocalToBase(block, localAxis);

    if (Math.Abs(axis.X) >= Math.Abs(axis.Y) && Math.Abs(axis.X) >= Math.Abs(axis.Z)) {
        return axis.X >= 0 ? "X" : "-X";
    }

    if (Math.Abs(axis.Y) >= Math.Abs(axis.Z)) {
        return axis.Y >= 0 ? "Y" : "-Y";
    }

    return axis.Z >= 0 ? "Z" : "-Z";
}

// Transforms a local axis vector into parent-grid coordinates using the block orientation basis.
Vector3I TransformLocalToBase(IMyMechanicalConnectionBlock block, Vector3I localAxis) {
    var basisX = GetDirectionVector(block.Orientation.Forward);
    var basisZ = GetDirectionVector(block.Orientation.Up);
    var basisY = new Vector3I(
        -basisX.Y * basisZ.Z + basisX.Z * basisZ.Y,
        -basisX.Z * basisZ.X + basisX.X * basisZ.Z,
        -basisX.X * basisZ.Y + basisX.Y * basisZ.X
    );

    return new Vector3I(
        basisX.X * localAxis.X + basisY.X * localAxis.Y + basisZ.X * localAxis.Z,
        basisX.Y * localAxis.X + basisY.Y * localAxis.Y + basisZ.Y * localAxis.Z,
        basisX.Z * localAxis.X + basisY.Z * localAxis.Y + basisZ.Z * localAxis.Z
    );
}

// Converts a direction enum into a signed unit vector.
Vector3I GetDirectionVector(Base6Directions.Direction direction) {
    switch (direction) {
        case Base6Directions.Direction.Forward:  return new Vector3I(1, 0, 0);
        case Base6Directions.Direction.Backward: return new Vector3I(-1, 0, 0);
        case Base6Directions.Direction.Left:     return new Vector3I(0, 1, 0);
        case Base6Directions.Direction.Right:    return new Vector3I(0, -1, 0);
        case Base6Directions.Direction.Up:       return new Vector3I(0, 0, 1);
        case Base6Directions.Direction.Down:     return new Vector3I(0, 0, -1);
        default: throw new Exception("Unsupported direction");
    }
}

// Reads piston extension or rotor angle and snaps it to stable output values.
double GetHomeValue(IMyMechanicalConnectionBlock block) {
    var piston = block as IMyPistonBase;
    if (piston != null) {
        return Snap(piston.CurrentPosition);
    }

    var motor = block as IMyMotorStator;
    if (motor != null) {
        var angle = NormalizeRotorAngle(motor.Angle * 180d / Math.PI);
        angle = SnapAngleHome(angle);

        if (IsProbablyHinge(motor) && angle > 90) {
            angle -= 360;
        }

        return angle;
    }

    throw new Exception("Unsupported joint type");
}

// Normalizes an angle into the [0, 360) range.
double NormalizeRotorAngle(double angle) {
    while (angle < 0) {
        angle += 360;
    }

    while (angle >= 360) {
        angle -= 360;
    }

    return angle;
}

// Rounds rotor homes to 5-degree steps and collapses near-360 back to zero.
double SnapAngleHome(double angle) {
    angle = Math.Round(angle / 5d, 0) * 5d;
    angle = NormalizeRotorAngle(angle);

    if (angle >= 355) {
        return 0;
    }

    return angle;
}

// Temporary hinge detection based on block name.
bool IsProbablyHinge(IMyMotorStator motor) {
    var name = motor.CustomName;
    return name != null && name.IndexOf("hinge", StringComparison.OrdinalIgnoreCase) >= 0;
}

// Converts a MArmOS axis string back into an integer direction vector.
Vector3I GetAxisVector(string axis) {
    switch (axis) {
        case "X":  return new Vector3I(1, 0, 0);
        case "-X": return new Vector3I(-1, 0, 0);
        case "Y":  return new Vector3I(0, 1, 0);
        case "-Y": return new Vector3I(0, -1, 0);
        case "Z":  return new Vector3I(0, 0, 1);
        case "-Z": return new Vector3I(0, 0, -1);
        default: throw new Exception("Unsupported axis vector");
    }
}

// Represents the collapsed piston body as a 2-block solid along its axis.
Vector3I GetPistonBodyVector(IMyMechanicalConnectionBlock block) {
    var axis = GetAxis(block);
    var vector = GetAxisVector(axis);
    return new Vector3I(vector.X * 2, vector.Y * 2, vector.Z * 2);
}

// Keeps home values readable while preserving non-integer piston positions.
double Snap(double value) {
    value = Math.Round(value, 2);

    var rounded = Math.Round(value, 0);
    if (Math.Abs(value - rounded) < 0.01) {
        return rounded;
    }

    return value;
}

// Solids are emitted as integer cube offsets.
int SnapSolid(double value) {
    return (int)Math.Round(value, 0);
}

// Formats generated numeric text with '.' decimal separator.
string FormatNumber(double value) {
    return value.ToString("0.##").Replace(',', '.');
}

// >>>>>>>>>>>>>
// NOTE: end of script marked by previous line
    }
}
