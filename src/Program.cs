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

// >>>>>>>>>>>>>
// Only code between these two markers is intended for the in-game programmable block.
// This file is the real script source; the local .csproj is only a Visual Studio helper and is not pasted into the game.
// Comments in this region are included in the final pasted script, so they can also serve as user instructions.
// Keep this region compatible with the Space Engineers script compiler:
// - hard script size limit is about 100k characters
// - custom compiler / whitelist checks apply
// - stay within C# 7.3 features
// - using a non-whitelisted type can fail in-game even if local build passes
// Heavy work can be split over time via Program() + Runtime.UpdateFrequency if one-shot scanning becomes too large.

// call this from ui with name of root rotor/hinge/piston block as argument ...
public void Main(string argument) {
    var root = GridTerminalSystem.GetBlockWithName(argument) as IMyMechanicalConnectionBlock;
    if (root == null) {
        Echo("Root block named '" + argument + "' not found");
        return;
    }

    var commands = new List<string>();

    // TODO: generate commands

    // save generated commands to programmable blocks Custom Data field
    Me.CustomData = string.Join("\r\n", commands);
}



// >>>>>>>>>>>>>
    }
}
