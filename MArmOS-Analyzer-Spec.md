# MArmOS Analyzer Specification

## Goal

Create second Space Engineers programmable block script that scans player's mechanical arm and generates code for `MArmOS_Configuration()` in `MArmOS`.

Target user flow:

1. Player builds arm and one programmable block.
2. Player folds arm into desired resting / home pose.
3. Player loads analyzer script into that programmable block.
4. Player runs analyzer script and passes root joint name.
5. Analyzer generates configuration code.
6. Player loads `MArmOS` into same programmable block.
7. Player pastes generated code into `MArmOS_Configuration()`.
8. Arm should work without manual geometry math.

## In-Game Compiler Constraints

Analyzer implementation must target the Space Engineers programmable block compiler, not normal desktop C# only.

Local project files are only development tooling:

- `src/MArmOS-Analyzer.csproj` exists so the script can be edited and locally compiled in Visual Studio
- that `.csproj` is not part of the final programmable block script
- final in-game script is extracted manually from `src/Program.cs`

Practical constraints:

- only the code between the two `// >>>>>>>>>>>>>` markers in `src/Program.cs` is pasted into the programmable block
- comments inside that marker region are pasted too, so that area may also hold in-script usage notes
- script source has a hard size limit of about `100000` characters
- game uses a custom compiler / validator pipeline, not standard Roslyn behavior alone
- project should stay compatible with `C# 7.3`
- any referenced type not on the programmable block whitelist will fail compilation, even if local project build succeeds

Implementation implications:

- prefer small helper methods and compact data flow
- avoid adding dependencies that are not already known-safe in programmable block scripts
- avoid newer language features than `C# 7.3`
- desktop compile success is necessary but not sufficient; in-game compile compatibility is the real target

## Runtime Scheduling Option

Programmable block scripts can request periodic execution from inside `Program()`:

```csharp
Runtime.UpdateFrequency = UpdateFrequency.Update10;
```

Useful meaning:

- `Update10` runs `Main(...)` every 10th simulation tick
- this can split expensive scanning across time instead of doing all work in one run
- analyzer may use this later for staged graph scan, staged geometry scan, or chunked debug output

Reference in repo:

- `Wheels.cs` uses `Program()` plus periodic `Main(string arg, UpdateType updateSource)` scheduling as a real in-game example

Design implication for analyzer:

- if one-shot scan gets too heavy, switch to a small state machine driven by periodic updates
- keep idle update frequency off when no background work is pending

## MArmOS Model

`MArmOS_Configuration()` defines arm as ordered hardware declarations.

Supported hardware relevant for analyzer v1:

- `Rotor`
- `Piston`
- `SolidLG`
- `SolidSG`
- optional `UserControl`

Important behavior in `MArmOS.cs`:

- Hardware constructors auto-append to `DefaultArm` in declaration order.
- In "simple" usage, user writes flat ordered list of `new ...` lines.
- `+` and `*` operators exist for advanced manual assembly, but analyzer v1 will not target them.

## Meaning Of `Solid`

`SolidLG(x, y, z)` / `SolidSG(x, y, z)` do **not** describe occupied block shape.

They describe only net displacement:

- start point in local grid coordinates
- end point in same local grid coordinates

Two differently shaped grids are equivalent to MArmOS if they have same start and end coordinates.

Implications:

- Analyzer does not need to scan armor block layout.
- Analyzer only needs relative offsets between successive mechanical joints.
- Piston body shape is not modeled separately; only resulting joint-to-joint displacement matters.

## Space Engineers Geometry Assumptions

### Grid Basics

- Grid is rigid collection of cubes sharing one transform.
- Large grid cube size is `2.5 m`.
- Small grid cube size is `0.5 m`.
- Large cube = `5 x 5 x 5` small cubes.
- Mechanical joints can connect grids of different sizes.

### Mechanical Joints

Relevant joint types:

- rotor / hinge
- piston

Useful conceptual model:

- joint base is on parent grid
- joint head creates child grid
- analyzer walks arm by following grid transitions

### Head / Attachment Notes

Known assumptions from discussion:

- Head grid origin is stable relative to mechanical head, regardless of rotor angle / piston extension.
- Joint geometry includes attachment-side offsets. Raw block position alone is not always enough to describe head-to-head displacement unless anchor offsets are accounted for.
- Example: rotor facing `X` with base at `[x,y,z]` has attached cube on parent side at `[x+1,y,z]`, and attached cube on head side at `[1,0,0]` in head coordinates.

Practical implication:

- Analyzer must reason in terms of joint anchors / head-to-head displacement, not occupied voxel path.

## Pose Assumption

Analyzer scans current arm pose and treats it as canonical MArmOS home pose.

Required user behavior:

- Player places arm into desired folded / resting pose before running analyzer.

Why:

- `Solid` offsets depend on current relative joint transforms.
- Rotor axes in effective local chain also depend on current configuration.
- Attempting to recover some abstract "straight" or "zeroed" arm model is out of scope.

Therefore analyzer should emit current live values as home values:

- rotor / hinge angle -> `Home`
- piston extension -> `Home`

Generated configuration should be self-consistent for the pose used during scan.

## Scope For Analyzer V1

Analyzer v1 should support:

- single serial arm chain only
- rotors / hinges
- pistons
- current-pose-as-home workflow
- output to `Me.CustomData`

Analyzer v1 should not support:

- mixed large / small grids
- parallel branches
- explicit advanced `+` / `*` arm assembly generation
- hydraulic joints
- rotor wheels
- automatic inference of `OriMode`
- reconstruction of arm independent of current pose

## Chain Definition

Serial chain is ordered list of mechanical joints reachable from chosen root by following child grids.

For v1:

- use root mechanical block passed by user
- walk from root toward tip using mechanical graph
- ignore unrelated mechanical blocks not on chosen serial chain

Parallel / ambiguous branches are not supported in v1 and should produce clear warning or error.

## Output Format

Default output target:

- `Me.CustomData`

Reference example assets in this repo:

- screenshot: `example.png`
- expected hand-written config: `Example.cs`

For that example:

- root joint is the base rotor mounted on the white large-grid base
- root rotor axis is `Z` because it is up-facing
- the white base grid is partially embedded in the ice lake
- no leading `Solid` is emitted before the root rotor

Generated code shape:

```csharp
new Rotor("R1", "Z", Home: 0);
new SolidLG(0, 1, 1);
new Piston("P1", "Z", Home: 0);
new SolidLG(0, -2, 6);
new Rotor("R2", "-Y", Home: 90);
```

Notes:

- Output should be simple ordered declarations.
- No advanced manual arm assembly in v1.
- No trailing `Solid` after final joint by default.
- Optional `new UserControl();` may be emitted later, but is not required for core geometry output.

## Solid Generation Rules

### Between Joints

For each adjacent pair of joints:

- start = current joint head reference point
- end = next joint head reference point
- compute net displacement in current child-grid local coordinates
- emit `SolidLG` or `SolidSG` based on relevant grid size

Important:

- Use net head-to-head offset only.
- Ignore winding path of cubes between joints.

### After Final Joint

Default v1 behavior:

- emit no trailing `Solid`

Reason:

- MArmOS can end with rotor or piston.
- Any rigid shape after final joint usually adds no control value.
- Script cannot actuate anything beyond last joint.

Trailing solid may be added later as optional feature for display / pose-reference use, but is not part of v1.

## Axis Rules

MArmOS local axis convention:

- `X` = Forward
- `Y` = Left
- `Z` = Up
- negatives allowed: `-X`, `-Y`, `-Z`

For analyzer:

- determine each joint axis in parent-local MArmOS coordinates
- do not infer axis from shape size
- use block orientation / mechanical head direction logic

Open issue:

- exact anchor and orientation handling across rotated parent joints must be validated in implementation.

## Home Value Rules

Analyzer should emit live home values from current pose:

- `Rotor` / `Hinge`: current angle in degrees
- `Piston`: current extension in meters

This is because scanned pose is intended home pose.

## Mixed Grid Size Rules

Analyzer should work in meters first, then convert to grid units:

- large grid: divide by `2.5`
- small grid: divide by `0.5`

Need to support chains where grid size changes across mechanical connections.

Fractional values are acceptable when exact offset is not integer number of cubes.

## Non-Goals / Known Hard Problems

- Detecting correct serial path in presence of parallel pistons / rotors
- Recovering idealized arm shape independent of live pose
- Determining user intent for wrist `OriMode: 1`
- Exact support for all rotor subtypes without anchor verification

## Recommended Implementation Plan

### Step 1: Graph Resolver

Build serial mechanical graph:

- find root mechanical block by name
- collect reachable mechanical joints
- map parent grid -> child grid transitions
- detect unsupported branch cases

### Step 2: Joint Data

For each joint in chain, resolve:

- block type
- block name
- parent grid size
- child grid size
- axis string
- current home value

### Step 3: Solid Data

For each pair of adjacent joints:

- compute head-to-head net displacement
- express in current child-grid local coordinates
- convert to LG / SG units
- emit `SolidLG` / `SolidSG`

### Step 4: Code Generator

Write ordered code lines into `Me.CustomData`.

### Step 5: Debug Output

During development, print intermediate data:

- joint order
- grid names
- grid sizes
- axis
- home value
- local deltas

## Open Questions For Later

- Exact per-subtype anchor offsets for:
  - 1x1x1 large rotor
  - 1x1x1 small rotor
  - 3x3x3 small advanced rotor
  - pistons
- Best way to resolve axis across already-rotated parent joints
- Whether root joint needs special-case handling for preceding solid
- Whether to emit optional `UserControl`
- Whether to support optional trailing solid for reference point only

## Current Conclusion

Specification is stable enough to implement analyzer v1 with this contract:

- scan current folded pose
- treat current pose as home pose
- output simple ordered MArmOS configuration
- support single serial chain only
- emit joints and in-between solids only

Concrete validation case now available in repo:

- `example.png` shows an in-game arm in folded pose
- `Example.cs` shows the intended MArmOS declaration for that exact arm
- analyzer output should eventually match that declaration shape, especially:
  - first joint is up-facing root rotor on the white base grid
  - first emitted axis is `Z`
  - first `Solid` starts after the root rotor, not before it
