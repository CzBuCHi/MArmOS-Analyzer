﻿// ReSharper disable IdentifierTypo

namespace IngameScript
{
    // just testing place to compare expected and generated configurations ...
    internal static class Testing
    {
        internal static void Test() {
            // expected configuration - defined in https://steamcommunity.com/sharedfiles/filedetails/?id=2096696897

            new Rotor("R1Z", "Z");  // The base rotor, I don't count its solid as I feel it's irrelevant, since nothing moves.
            new SolidLG(0, 1, 1);      // Solid marked Yellow. From the head of Rotor R1Z to the head of Rotor R2Y - X 0, Y 1, Z 1 - One Up, one to the left.

            new Rotor("R2Y", "Y");  // Rotor facing left, so Y axis orientation. 
            new Piston("P1Z", "Z"); // Pistons are just declared, but their solid retracted shape needs to be included in the solid
            new Piston("P2Z", "Z"); // between rotors.

            new SolidLG(0, -2, 6); // Solid marked black. This solid is counted starting with the head of rotor R2Y, but I'm lookind towards 
            // where it finishes with the head of rotor R3-Y, so I'm going up 6 (2 conveyor tubes & 2 pistons, 
            // Two to the right, none forward, so X 0, Y -2, Z 6 .

            new Rotor("R3-Y", "-Y"); // Another rotor, but this time it's facing right = axis -Y.
            new Piston("P3X", "X");  // Yey, another set of pistons. These are easy. Just tell MArmOS which way they're facing.
            new Piston("P4X", "X");

            new SolidLG(5, -1);       // Marked with green. Same thing as the solid between R2Y and R3-Y.
            new Rotor("R4X", "X", 1); // Starting with OriMode: 1 to get this rotor to respond to mouse & Q&E.
            new SolidLG(1, 1);        //  it's an L shaped tube that's leading to the other rotor. We're counting from the tip of the rotor R4X here, so
            // it's one forward and one to the left. X 1, Y 1, Z 0.

            new Rotor("R5Y", "Y", 1); // This rotor is handled by mouse input, as it's OriMode: 1 - 3 axis of freedom on the
            // working end will help you "fly" the arm like a ship.
            new SolidLG(0, 1, -1); // Marked blue: The solid leading from the head of R5Y to the head of R6-Z. In my original version of this arm,
            // I wrote 1.1 on the Y axis, as I have the rotor set to 20 cm displacement... it's actually 1.08... but I'm not sure
            // it even matters that much in the grand scheme of things, so for simplicity's sake, it's one to the left and one
            // down - X 0, Y 1, Z -1.
            new Rotor("R6-Z", "-Z", 1); // Last rotor, but we still have the tip of the arm after it, I'm gonna declare that too.
            new SolidLG(0, 0, -1);      // Marked red: the tip of the arm, and where the camera is placed, facing "forward" - forward is on the X axis
            // when building, but I guess that's clear by now.


            // generated by script
            new Rotor(Name: "R1Z", Axis: "Z", OriMode: 0, Home: 1);
            new SolidLG(-1, 1, 0);

            new Rotor(Name: "R2Y", Axis: "Y", OriMode: 0, Home: 359);
            new Piston(Name: "P1Z", Axis: "-Y");
            new Piston(Name: "P2Z", Axis: "Z"); // how the heck piston changes axis???

            new SolidLG(-1, 1, 0);

            new Rotor(Name: "R3-Y", Axis: "Y", OriMode: 0);
            new Piston(Name: "P3X", Axis: "-X");
            new Piston(Name: "P4X", Axis: "Z");

            new SolidLG(0, 1, 0);
            new Rotor(Name: "R4X", Axis: "Z", OriMode: 0);
            new SolidLG(0, 1, -1);

            new Rotor(Name: "R5Y", Axis: "X", OriMode: 0);
            new SolidLG(1, 1, 0);

            new Rotor(Name: "R6-Z", Axis: "-Y", OriMode: 0);
        }
    }

    // fake classes with same constructors as MArmOS so i can paste MArmOS configuration into Test method and compiler wont complain
    internal class SolidLG
    {
        public SolidLG(int X = 0, int Y = 0, int Z = 0) {
        }
    }

    internal class SolidSG
    {
        public SolidSG(int X = 0, int Y = 0, int Z = 0) {
        }
    }

    internal class Piston
    {
        public Piston
        (
            string Name = null,
            string Axis = null,
            bool Override = false,
            double MaxSpeed = 0,
            double SoftMaxLimit = 0,
            double SoftMinLimit = 0,
            double Home = 0
        ) {
        }
    }

    internal class Rotor
    {
        public Rotor
        (
            string Name = null,
            string Axis = null,
            double OriMode = 0,
            bool Override = false,
            double MaxSpeed = 0,
            double Offset = 0,
            double SoftMaxLimit = 0,
            double SoftMinLimit = 0,
            double Home = 0
        ) {
        }
    }
}
