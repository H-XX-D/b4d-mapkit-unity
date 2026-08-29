# Blox 4 Dead Map Kit for Unity

Build levels in Unity the way you normally would, then mark up the gameplay.
The kit reads your scene and writes a map file the game loads.

Nothing about your usual workflow changes. Prefabs, ProBuilder, imported art,
Asset Store packs, your own meshes, all of it works. The kit sits on top and
only cares about a handful of markers you add.

---

## Install

Package Manager ▸ **Add package from git URL**:

```
https://github.com/H-XX-D/b4d-mapkit-unity.git
```

Unity 2021.3 or newer. To pin a release, add a tag: `...unity.git#v1.5.0`.

Unity locks a git package to the commit it first resolved, so use **Update** in
Package Manager to pull newer versions.

---

## Quick start

Five steps. You can stop after step 3 and already be playtesting the layout.

### 1. Build the level normally

Open a scene and lay it out however you like. Drag in prefabs, block it out with
ProBuilder, drop in a warehouse from the Asset Store. Do not think about the
kit yet.

### 2. Set up the campaign

**GameObject ▸ Blox 4 Dead ▸ Set Up Campaign In This Scene**

This adds a campaign root and marks everything already in the scene as
**reference scenery**, meaning your art is never exported and never leaves
Unity. It is there so you can see what you are doing.

Select the campaign root and set:

- **Id**: a name, lower case with underscores, e.g. `cold_storage`
- **Index**: which campaign slot it replaces. Use **2**, **3** or **4**.
- **Theme**: leave as `slaughterhouse` for now, it picks the material palette.

### 3. Draw zones over the walkable space

**GameObject ▸ Blox 4 Dead ▸ Zone**, then drag the box by its faces to cover a
room or corridor. Or select some art and use **Zone From Selection** to fit a
zone to it automatically.

This is the one concept that matters, so it gets its own section below.

### 4. Bring your collision across

**Select your level art ▸ GameObject ▸ Blox 4 Dead ▸ Blocking Props From
Colliders**

A level built normally already has colliders on it. This turns them into solids
the game blocks on. Box colliders keep their rotation; anything else uses its
bounds. Triggers are skipped.

The results are invisible in the game. They only block movement.

### 5. Add the gameplay

Each chapter needs one **Objective** and one **Gate**, both from
**GameObject ▸ Blox 4 Dead**. Optionally scatter **Fuel Barrels** and
**Drop Hazards**.

Then open **Window ▸ Blox 4 Dead ▸ Map Kit** and press **Check map**. It lists
everything still wrong or worth knowing, and every entry has a Select button
that jumps you to the object responsible.

---

## Zones are the map

The one thing to understand.

The walkable area is the union of the zones. **Where two zones overlap by more
than 1.5 metres, that overlap is the doorway between them.** Navigation,
spawning, objective placement and the game's own interior decoration are all
derived from the zone list.

So authoring a map is mostly drawing boxes over the space you want players to
walk through, and making sure connected rooms overlap.

A zone that overlaps nothing else can never be entered. That is an error, and
the exporter refuses to write the file. Zones are drawn in the scene view, and
an unconnected one is drawn **red**.

Zones do not have to match your art exactly. They describe where players can
walk, not what the room looks like.

---

## Components

All under **GameObject ▸ Blox 4 Dead**. They land near the scene view camera and
parent themselves to the campaign root.

| Component | What it is |
| --- | --- |
| **Campaign** | One per scene. Id, slot, theme. |
| **Zone** | A room or corridor. Drag by the faces. |
| **Objective** | A chapter's device, with its stations and cart destination. |
| **Gate** | The checkpoint door that objective opens. |
| **Fuel Barrel** | Goes up when shot. |
| **Drop Hazard** | A load on a cable that falls when shot. |
| **Prop** | Procedural set dressing built from numbers. |
| **Model Prop** | A real mesh, from a `.glb`. |
| **Reference Scenery** | Art for authoring only. Never exported. |

## Tools

| Tool | What it does |
| --- | --- |
| **Set Up Campaign In This Scene** | Adds the root, marks existing art as reference. |
| **Zone From Selection** | Fits a zone to the selected art, roof from its height. |
| **Fit Zone To Selection** | Resizes an existing zone to cover art selected with it. |
| **Blocking Props From Colliders** | Turns existing Unity colliders into solids. |
| **Blocking Prop From Selection** | One solid fitted to the selected art's bounds. |
| **Fit Collider To Renderers** | Takes a prop's collider extents from its art. |
| **Mark As Reference Scenery** | Keeps art out of the export. |
| **Bake Selection To glb** | Bakes art into a mesh the game can render. |

---

## Do I need to bake anything?

**No.** Reference scenery plus blocking props gets you a playable, correctly
shaped level. The game draws it with its own materials and its own decoration.
That is the fastest path and it is the one to use while you are still finding
out whether a layout is fun.

Bake only when you want a specific mesh to appear in the game.
**Bake Selection To glb** does it in one step: it bakes the meshes and materials
into a `.glb` in your project, adds a Model Prop pointing at it, and fits the
collider.

Two defaults matter for shipping, both on: **inline in map** carries the mesh
inside the page rather than as a separate file, and **strip names** removes the
mesh, node and material names that identify which pack a mesh came from.

Materials going black is the classic failure here, and the baker guards against
it. Unity has no single albedo property: Standard uses `_Color`, URP uses
`_BaseColor`, HDRP uses `_BaseColorMap`, Shader Graph can use anything. An
exporter that reads only one gets nothing and writes black, which loads without
error and looks like a lighting bug. The baker probes all of them, and when it
still cannot find a base colour it bakes plain grey and names the material and
shader that defeated it.

See [LICENSING.md](LICENSING.md) before baking purchased art. Short version:
shipping inside a Discord Activity is a good position, and the only thing worth
checking is whether a pack explicitly forbids web builds.

---

## Later: getting the map into the game

Not needed while you are building. When you want to walk around it:

**Window ▸ Blox 4 Dead ▸ Map Kit ▸ Export JSON**, then **drag the exported
`.json` onto the game window**. That is the whole step. The game is a downloaded
HTML file you open straight from disk, so there is no server and no build.

The export refuses to write a file if the map has errors, so a file that comes
out will load. Edit, export over the same file, drag it on again.

[PLAYTESTING.md](PLAYTESTING.md) has the detail, including which campaign slots
can be replaced.

---

## Reference

- [PLAYTESTING.md](PLAYTESTING.md), getting a map into the game.
- [VERIFICATION.md](VERIFICATION.md), what is proven and what is not. Read this
  first if you are the one testing the kit itself.
- [LICENSING.md](LICENSING.md), what you can ship.
- `schema/b4d-campaign.schema.json`, the map format.
- `schema/examples/example_depot.json`, a small map that passes every check.

### Type checking without Unity

```
./Tools~/compile-check/check.sh
```

Compiles every script against reference stubs of the Unity API, so a machine
with no Unity still catches type errors. Needs the .NET SDK
(`brew install dotnet`). Unity ignores the folder because its name ends in `~`.

### Coordinates

Unity's axes match the game's directly, in metres. Nothing is converted. Scene X
and Z are the game's X and Z, and rotation about Y carries over.

### Procedural prop types

Set dressing described by numbers rather than meshes, so the game builds it and
the map file stays small.

`box`, `cylinder`, `grid`, `chainLine`, `carcassRows`, `lightPole`, `vat`,
`pipeRun`, and `model` for a mesh from the map's asset table.

### What the checker looks for

Errors, which block export:

- a zone that overlaps no other zone
- two zones sharing a name, or a zone with no name, area or floor material
- two objectives claiming the same chapter
- an escort objective with no cart destination
- a solid prop with a zero sized collider
- a drop hazard whose cable anchor is at or below its load
- a model prop with no glb, or two files sharing one asset key

Warnings, which do not:

- an objective outside every zone, which the game relocates at load
- a hazard outside every zone, which can therefore never fire
- a breakers objective with no window
- a gate count that does not match the objective count

---

## Licence

MIT, copyright H-XX-D. See [LICENSE.md](LICENSE.md).

The Unreal edition exports the identical format:
[b4d-mapkit-unreal](https://github.com/H-XX-D/b4d-mapkit-unreal).
