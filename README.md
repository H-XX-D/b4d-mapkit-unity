# Blox 4 Dead Map Kit for Unity

Author campaign maps in the Unity scene view and export them as JSON the game
loads directly. No engine code changes to add a map.

```
Unity scene  ──►  campaign JSON  ──►  playable map
```

The Unreal edition of the same kit exports the identical format:
[b4d-mapkit-unreal](https://github.com/H-XX-D/b4d-mapkit-unreal).

## Install

Package Manager ▸ **Add package from git URL**:

```
https://github.com/H-XX-D/b4d-mapkit-unity.git
```

Or clone it into `Packages/com.blox4dead.mapkit`. Unity 2021.3 or newer.

## Use

Add pieces from **GameObject ▸ Blox 4 Dead**. They land under the campaign root
and near the scene view camera, ready to drag into place.

| Component | What it is |
| --- | --- |
| B4D Campaign | One per scene. Holds the id, slot, theme and extraction. |
| B4D Zone | A room or corridor, dragged by the face with a box handle. |
| B4D Objective | A chapter's device, with its nodes and cart destination. |
| B4D Gate | The checkpoint door that objective opens. |
| B4D Barrel | A barrel that goes up when shot. |
| B4D Drop Hazard | A heavy load on a cable that falls when shot. |
| B4D Prop | Set dressing. Pick a type and the inspector shows its fields. |

Open **Window ▸ Blox 4 Dead ▸ Map Kit** to check the map, export it, or import
an existing JSON file back into the scene for editing. Every problem in the list
has a Select button that pings the object responsible.

Zones draw as boxes in the scene view, and a zone connected to nothing is drawn
red, since that is the one mistake that makes a map unplayable.

## Building with Unity art

You do not have to design a level against grey boxes. Drop in whatever scenery
you like, a warehouse, a crate pack, anything from the Asset Store, and build
the map against it.

Mark it with **GameObject ▸ Blox 4 Dead ▸ Mark As Reference Scenery**. Reference
scenery is never exported and never reaches the game, so nothing is
redistributed and no licence is stretched. It is there so you can see what you
are doing. Its ground footprint draws in the scene view so you can tell what a
zone still has to cover.

Then derive the map data from what you built:

| Tool | What it does |
| --- | --- |
| **Zone From Selection** | Creates a zone fitted to the selected art, with the roof taken from its height. |
| **Fit Zone To Selection** | Resizes an existing zone to cover the art selected alongside it. |
| **Blocking Prop From Selection** | Turns the selected art into a solid the players collide with. |
| **Fit Collider To Renderers** | Takes a prop's collider extents from the art beneath it. |

The art stays in Unity. Only the boxes and the gameplay objects travel.

### Shipping the art itself

If you do want a mesh in the game, use **B4D Model Prop**. Point it at a `.glb`
file and the exporter carries it into the map, either inline as base64 (keeping
the game a single self contained page) or as a separate file copied to an
`assets` folder beside the map.

The game reads a practical subset of glTF: indexed triangles, normals, texture
coordinates, vertex colours, and PBR base colour with an embedded texture.
Anything it cannot read falls back to a plain box, and the collider comes from
the map data either way, so the collision players feel is identical whether or
not the art loaded.

Check the licence first. Plenty of Asset Store packs allow use in a built game
but forbid redistributing the source mesh, and a `.glb` served to a browser is
about as redistributable as it gets. Reference scenery exists for that case.

## Coordinates

Unity's axes match the game's directly, in metres, so nothing is converted.
Scene X and Z are the game's X and Z, and rotation about Y carries over.

## The format

A map is one JSON document. `schema/b4d-campaign.schema.json` is the full
specification and `schema/examples/example_depot.json` is a small map that
passes every check, useful as a starting point.

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

### Zones are the map

The walkable area is the union of the zones. Where two zones overlap by more
than 1.5 metres, that overlap is the doorway between them. Navigation,
spawning, objective placement and the interior architecture pass are all
derived from the zone list, so authoring a map is mostly drawing boxes and
naming them.

A zone that overlaps nothing else can never be entered. That is an error and
the exporter refuses to write the file.

### What gets checked before export

Errors, which block the export:

- a zone that overlaps no other zone
- two zones sharing a name, or a zone with no name, area or floor material
- two objectives claiming the same chapter
- an escort objective with no cart destination
- a solid prop with a zero sized collider
- a drop hazard whose cable anchor is at or below its load

Warnings, which do not block it:

- an objective outside every zone (the game relocates it to the nearest clear
  spot at load, so it plays, just not where you drew it)
- a hazard outside every zone (nothing relocates those, so it can never be
  shot or triggered)
- a breakers objective with no window, so its switches never have to line up
- a gate count that does not match the objective count

That last pair is worth taking seriously. Running these rules over an existing
hand-authored campaign turned up two fuel barrels and a falling carcass rack
sitting outside the walkable zones, left behind when the zones were moved and
the set dressing was not.

## Prop types

Set dressing is described by numbers rather than meshes, so the game builds it
procedurally and a map file stays small.

| Type | What it builds |
| --- | --- |
| `box`, `cylinder` | A single solid, optionally blocking |
| `grid` | A repeating grid of sub-props, e.g. pen rails |
| `chainLine` | An overhead rail with hanging chains and hooks |
| `carcassRows` | Rows of hanging carcasses on alternating offsets |
| `lightPole` | A pole with a light on top |
| `vat` | An open topped vessel with a surface disc |
| `pipeRun` | A straight run of stepped horizontal pipes |

Adding a type means adding a builder in the game, a row to the field table in
this plugin, and the name to the schema enum. No new class is needed: the
editor drives its fields off that table.

## Updating

Unity pins a git package to the exact commit it first resolved, recorded in
`Packages/packages-lock.json`, so it will not pick up new work on its own. To
move to the latest:

- Package Manager, select the package, then **Update**, or
- remove the entry for `com.blox4dead.mapkit` from `Packages/packages-lock.json`
  and reopen the project.

Pin a specific release instead by adding a tag to the URL:

```
https://github.com/H-XX-D/b4d-mapkit-unity.git#v1.1.0
```

## Notes

Duplicating a simple prop around the level is cheap in the map file: props that
differ only in where they stand are collapsed into a single entry with an `at`
list on export, and expanded back into individual objects on import.

## Licence

MIT. See [LICENSE.md](LICENSE.md).
