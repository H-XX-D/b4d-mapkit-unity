# What has been verified, and what has not

Written for whoever opens this in Unity first. It was authored without a Unity
install, so this says exactly where to expect trouble rather than pretending
everything is proven.

## Verified

**The whole package type checks.** Every script compiles clean against
reference stubs of the Unity API, at the language version Unity 2021.3 accepts.

```
./Tools~/compile-check/check.sh      # needs: brew install dotnet
```

That check is itself tested: injecting a wrong type, a misspelled member, a
changed signature and a syntax error each produced errors, and removing them
returned to zero. So a clean run means something.

**Every asset has a meta file**, 30 assets and 30 unique GUIDs, which is what a
package installed under `Packages/` needs since Unity cannot generate them
there.

**The glb byte layout the baker writes is readable by the game.** A file built
to the baker's exact rules, including four byte buffer view alignment, padded
chunk lengths, the 16 to 32 bit index switch and an embedded PNG, loads
correctly in the reader the game ships. Deliberately breaking the alignment, the
padding or the triangle winding is caught.

**The game side round trips.** Zones, objectives, gates, hazards and props go
from map data to a built world identically to the original hand written level,
mesh for mesh and collider for collider.

## Not verified

**Nothing has been run inside Unity.** No scene, no inspector, no bake.

The stubs are the weak point. They were written from the documented API, so a
signature that differs subtly from the real one would let a genuine error
through. If something fails to compile in Unity, that is where to look first.

Specific places worth testing early:

| Area | Why it is worth a look |
| --- | --- |
| `B4DGltfBaker.EncodeTexture` | Blits through a `RenderTexture` to handle non-readable and compressed textures. The format and sRGB flags are the fiddly part. |
| `B4DGltfBaker.CollectMeshes` | `mesh.vertices` on a mesh imported without Read/Write. It should report rather than throw, but that path is untested. |
| Material property probing | The name lists cover Standard, URP, HDRP and common Shader Graph. A pipeline that names albedo something else bakes grey and warns. Tell me the property name and it takes one line to add. |
| `B4DSceneGizmos` zone handle | `BoxBoundsHandle` resizing writes back to both the component and the transform. The centre offset maths deserves a look. |
| `B4DExporter.UnderReference` | Uses `GetComponentInParent`, which includes the object itself. Intentional, but confirm reference scenery really is excluded. |
| Menu paths | Everything is under `GameObject ▸ Blox 4 Dead` and `Window ▸ Blox 4 Dead`. Priorities were guessed. |

## Known limitations

- Skinned meshes are frozen in their current pose. The game has no skinning for
  props.
- Only the base colour map is baked. No normal, metallic, roughness, emissive or
  occlusion maps.
- One UV channel, `TEXCOORD_0`. Lightmap UVs are dropped.
- Meshes are flattened into one node. No hierarchy is preserved in the glb.
- Negative or mirrored scales will transform normals incorrectly, because the
  normal transform uses the matrix rather than its inverse transpose.
- Model props cannot be imported back from a map file. A base64 blob cannot be
  turned back into a project asset, so import skips them and says so.

## Reporting something

Useful in a report:

- The Unity version and render pipeline, since most of the risk above is
  pipeline specific.
- The full console error with the file and line.
- Whether `./Tools~/compile-check/check.sh` passes. If it passes and Unity does
  not, a stub is wrong, which is quick to fix.

## Layout

```
Runtime/          components that live in a scene
Editor/           tools, exporter, importer, validator, baker
schema/           the map format and an example map
Tools~/           the compile check; Unity ignores folders ending in ~
```
