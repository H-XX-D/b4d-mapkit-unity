# Blox 4 Dead Map Kit for Unity

Author campaign maps in the Unity scene view and export them as JSON the game
loads directly. No engine code changes to add a map.

```
Unity scene  ──►  campaign JSON  ──►  playable map
```

The Unreal edition exports the identical format:
[b4d-mapkit-unreal](https://github.com/H-XX-D/b4d-mapkit-unreal).

## Install

Package Manager ▸ **Add package from git URL**:

```
https://github.com/H-XX-D/b4d-mapkit-unity.git
```

Or clone it into `Packages/com.blox4dead.mapkit`. Unity 2021.3 or newer.

Pin a release by adding a tag: `...b4d-mapkit-unity.git#v1.3.0`. Unity locks a
git package to the commit it first resolved, so use **Update** in Package
Manager to move forward, or delete the entry from `Packages/packages-lock.json`.

## Start here

- [PLAYTESTING.md](PLAYTESTING.md), getting a map you edited into the game.
- [VERIFICATION.md](VERIFICATION.md), what is proven and what is not. Worth
  reading first if you are the one testing this in Unity.
- [LICENSING.md](LICENSING.md), what you can ship, and the Asset Store caveat.

## Zones are the map

The walkable area is the union of the zones. Where two zones overlap by more
than 1.5 metres, that overlap is the doorway between them. Navigation, spawning,
objective placement and the interior architecture pass are all derived from the
zone list, so authoring a map is mostly drawing boxes and naming them.

A zone that overlaps nothing else can never be entered. That is an error and the
exporter refuses to write the file.

## Components

Add them from **GameObject ▸ Blox 4 Dead**. They land under the campaign root
and near the scene view camera.

| Component | What it is |
| --- | --- |
| B4D Campaign | One per scene. Holds the id, slot, theme and extraction. |
| B4D Zone | A room or corridor, dragged by the face with a box handle. |
| B4D Objective | A chapter's device, with its nodes and cart destination. |
| B4D Gate | The checkpoint door that objective opens. |
| B4D Barrel | A barrel that goes up when shot. |
| B4D Drop Hazard | A heavy load on a cable that falls when shot. |
| B4D Prop | Procedural set dressing. Pick a type, fill in its numbers. |
| B4D Model Prop | A real mesh, supplied as a `.glb`. |
| B4D Reference Scenery | Art for authoring only. Never exported. |

Open **Window ▸ Blox 4 Dead ▸ Map Kit** to check, export or import a map. Every
problem in the list has a Select button that pings the object responsible.

Zones draw as boxes in the scene view, and a zone connected to nothing is drawn
red, since that is the one mistake that makes a map unplayable.

## Building with Unity art

You do not have to design against grey boxes. Drop in whatever scenery you
like, a warehouse, a crate pack, anything from the Asset Store, and build the
map against it.

Mark it with **Mark As Reference Scenery**. Reference scenery is never exported
and never reaches the game, so nothing is redistributed. Its ground footprint
draws in the scene view so you can tell what a zone still has to cover.

Then derive the map data from what you built:

| Tool | What it does |
| --- | --- |
| **Zone From Selection** | Creates a zone fitted to the art, roof from its height. |
| **Fit Zone To Selection** | Resizes an existing zone to cover the art beside it. |
| **Blocking Prop From Selection** | Turns the art into a solid players collide with. |
| **Fit Collider To Renderers** | Takes collider extents from the art beneath. |
| **Bake Selection To glb** | Bakes the art into a mesh the game can render. |

The art stays in Unity. Only the boxes and the gameplay objects travel.

### Shipping the art itself

**Bake Selection To glb** bakes the meshes and materials into a `.glb` inside
your project, adds a **B4D Model Prop** pointing at it, and fits the collider.
The exporter then carries the file into the map, either inline as base64
(keeping the game a single self contained page) or as a separate file copied to
an `assets` folder beside the map.

The baker handles mesh filters and skinned meshes (frozen in their current
pose), one primitive per submesh, per submesh materials, and base colour
textures read back through a render target so compressed and non-readable
imports work without touching their import settings.

**On materials going black.** Unity has no single albedo property. Built-in
Standard calls it `_Color` and `_MainTex`, URP calls it `_BaseColor` and
`_BaseMap`, HDRP uses `_BaseColorMap`, and Shader Graph can call it anything. An
exporter reading only one of those gets nothing back and writes a
`baseColorFactor` of black with no texture, which loads without any error and
looks like a lighting bug. The baker probes all of these, and when it still
cannot find a base colour it bakes a plain grey and names the material and
shader that defeated it, because obviously unfinished beats silently black.

Check the licence before shipping purchased art. See
[LICENSING.md](LICENSING.md).

## Type checking without Unity

```
./Tools~/compile-check/check.sh
```

Compiles every script against reference stubs of the Unity API, at the language
version Unity 2021.3 accepts, so a machine with no Unity on it still catches
type errors. Needs the .NET SDK (`brew install dotnet`). Unity ignores the
folder because its name ends in `~`.

## Coordinates

Unity's axes match the game's directly, in metres, so nothing is converted.
Scene X and Z are the game's X and Z, and rotation about Y carries over.

## The format

`schema/b4d-campaign.schema.json` is the specification.
`schema/examples/example_depot.json` is a small map that passes every check.

```json
{
  "schema": 1,
  "id": "example_depot",
  "index": 2,
  "theme": "slaughterhouse",
  "zones": [
    { "name": "depot", "x": 0, "z": -12, "halfX": 30, "halfZ": 26,
      "floor": "tile", "roof": 9, "lampY": 8.4 }
  ],
  "objectives": [
    { "chapter": 1, "x": 0, "z": -12, "label": "RESTART THE DEPOT PUMPS",
      "verb": "PUMPS", "kind": "pumps", "type": "signal", "duration": 20 }
  ]
}
```

### Prop types

Procedural set dressing is described by numbers rather than meshes, so the game
builds it and a map file stays small.

| Type | What it builds |
| --- | --- |
| `box`, `cylinder` | A single solid, optionally blocking |
| `grid` | A repeating grid of sub-props, e.g. pen rails |
| `chainLine` | An overhead rail with hanging chains and hooks |
| `carcassRows` | Rows of hanging carcasses on alternating offsets |
| `lightPole` | A pole with a light on top |
| `vat` | An open topped vessel with a surface disc |
| `pipeRun` | A straight run of stepped horizontal pipes |
| `model` | A mesh from the map's asset table |

Adding a procedural type means a builder in the game, a row in
`B4DPropSchema.Fields`, and the name in the schema enum. No new component is
needed: the inspector drives its fields off that table.

### What gets checked before export

Errors, which block the export:

- a zone that overlaps no other zone
- two zones sharing a name, or a zone with no name, area or floor material
- two objectives claiming the same chapter
- an escort objective with no cart destination
- a solid prop with a zero sized collider
- a drop hazard whose cable anchor is at or below its load
- a model prop with no glb, or two different files sharing one asset key

Warnings, which do not:

- an objective outside every zone (the game relocates it at load)
- a hazard outside every zone (nothing relocates those, so it can never fire)
- a breakers objective with no window
- a gate count that does not match the objective count

That last pair caught real defects in an existing hand authored campaign: two
fuel barrels and a falling carcass rack sitting outside the walkable zones,
left behind when the zones were moved and the set dressing was not.

## Notes

Duplicating a simple prop around the level is cheap in the map file: props that
differ only in where they stand are collapsed into a single entry with an `at`
list on export, and expanded back into individual objects on import.

Model props cannot be imported back from a map file. A base64 blob cannot be
turned into a project asset, so import skips them and says so.

## Licence

MIT. See [LICENSE.md](LICENSE.md) and [LICENSING.md](LICENSING.md).
