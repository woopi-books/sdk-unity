# Moho Workflow

How to take a rigged, animated character from Moho into a Unity book. The sample ships the exported files in both
glTF flavours (`.glb` and `.gltf`) and a scene that plays them side by side.

The character is a placeholder made of plain shapes (head, body, two arms, two legs). It has a bone rig and a 2 second
looping idle with a wave, so you can see the whole pipeline without any real artwork in the way.

## Requirements

- Moho Pro 14.4 or newer. The glTF exporter comes with the 14.4 "Side Quest" update (**File > Video Game (glTF)...**).
- **glTFast** (`com.unity.cloud.gltfast`) in your Unity project. The SDK does not depend on it, so add it yourself:
  Package Manager > Unity Registry > Unity glTFast. Tested with 6.20.0 on Unity 6000.6.
  Without it Unity does not know `.glb` / `.gltf` and shows them as plain files.

## What's in the sample

| Path | What it is |
|---|---|
| `Moho/` | The Moho source project and the PSD it was built from. Open them to see how the rig is set up. Keep `Dummy.psd` next to `WorkflowDemo.moho`: the project refers to it by a path relative to the project file. |
| `Art/GLB/WorkflowDemo.glb` | The export as one binary file, textures included. |
| `Art/GLTF/WorkflowDemo.gltf` | The same export as JSON, with the textures as separate PNGs in `WorkflowDemo__gltf/`. |
| `Scenes/MohoWorkflow.unity` | Both characters in one scene. Left is the GLB, right is the GLTF. |

## From Moho to Unity

### 1. Rig it in Moho

1. Import the layered PSD. Moho creates a bone layer with one image layer per PSD layer.
2. Add the bones with the Add Bone tool, start each child bone at the parent's tip, and give every bone a clear name
   (`Hips`, `Spine`, `Head`, `ArmL`, `ArmR`, `LegL`, `LegR`). The names end up in Unity.
3. Bind each image layer to its bone with the Bind Layer tool.
4. Animate the bones. Set the project's **end frame** to the last frame of the animation, because the export covers the
   whole timeline (this sample uses 24 fps and ends at frame 48).

### 2. Export

**File > Video Game (glTF)...** and use these settings:

| Setting | Value | Why |
|---|---|---|
| Output | `export .GLB` (tick `.GLTF` only if you want that flow) | One self-contained file. |
| Bake every frame | On, step 1 or 2 | glTF has no equivalent of Moho's smooth easing, so baking samples the motion and Unity matches what you saw in Moho. |
| Re-Render Textures | None | Uses the original image layers as textures. |
| Texture Scaling | 100% | |
| Mesh Density | 20 | Plenty for flat shapes. Raise it if limbs look blocky when they bend. |
| Z offset factor | 1 | Distance between layers. |
| Flip Z Order | Off | Turn it on if layers come in stacked backwards. |

Export the GLB and the GLTF in two passes into separate folders so the files don't mix.

### 3. Import into Unity

Drop the file into the project. glTFast imports it as a prefab-like asset with:

- one GameObject per bone and per layer (the bone names are kept),
- a mesh, material and texture per image layer,
- an `Animation` component with the clip `Mainline` (Moho's timeline).

A layer bound to a single bone is exported as a rigid mesh parented to its bone, so Unity animates plain transforms
and there are no skinned renderers.

### 4. Put it in a scene

1. Drag the asset into the scene.
2. **Rotate the instance 180 degrees around Y.** glTF and Unity use opposite handedness, so glTFast flips X on import.
   Without the rotation the character is mirrored: the arm that waves on the right in Moho waves on the left in Unity.
3. Set the animation to loop. The imported clip is legacy and does not loop on its own:

   ```csharp
   var anim = GetComponent<Animation>();
   anim.wrapMode = WrapMode.Loop;
   anim.Play("Mainline");
   ```

   The scene sets `playAutomatically` and `wrapMode` on the component instead.
4. The layers sit at the same Z and rely on draw order. If you mix them with other sprites or transparent objects, check
   the sorting.

## GLB or GLTF?

**Use the GLB.** Moho writes the `.gltf` with its data embedded as base64 text, and when you select the `.gltf` in Unity
the importer's report shows:

> JSON embed buffers are slow! consider using glTF binary

The second line in that report, *Overriding naming method to be OriginalUnique (animation requirement)*, is only
informational: glTFast makes the node names unique so the animation clip can find its targets.

| | `.glb` | `.gltf` |
|---|---|---|
| Files | One. | A JSON file plus a folder of PNGs. |
| Size | Smaller, and faster to import. | Larger, and slower because of the embedded base64 data. |
| Textures | Embedded as sub-assets. You re-export to change one. | Plain PNGs you can replace without re-exporting. |
| Moving it around | Copy one file. | Keep the `.gltf` and its texture folder together. |
| In version control | One binary file. | The JSON diffs, but there are more files. |

Pick the `.gltf` only if you need to swap a texture by hand or want to inspect the file. Everything else about the two
is identical, which is what the sample scene shows.

## Notes about this sample

- The placeholder textures were exported from Moho's Demo Mode, so they carry its watermark. Exporting from a licensed
  Moho gives clean textures.
- The exported files here are a snapshot. If you change the rig or the animation in Moho, export again and let Unity
  reimport the file. Keep the file names the same so the references in your scene survive.
