using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace B4D
{
    public class B4DBakeOptions
    {
        [Tooltip("Longest edge a baked texture may have. Larger textures are scaled down.")]
        public int maxTextureSize = 1024;

        [Tooltip("Multiplies every vertex. The game works in metres, as Unity does, so leave at 1 unless the source art is mis-scaled.")]
        public float scale = 1f;

        public bool includeVertexColors = false;
        public bool includeTextures = true;

        [Tooltip("Drop mesh, node and material names from the baked file. The game never reads them, and they are the part that identifies which pack a mesh came from. On by default for shipping.")]
        public bool stripNames = true;
    }

    public class B4DBakeReport
    {
        public List<string> warnings = new List<string>();
        public int meshes, primitives, materials, textures, triangles, bytes;

        public string Summary()
            => $"{primitives} primitive(s) from {meshes} mesh(es), {triangles} triangles, "
             + $"{materials} material(s), {textures} texture(s), {bytes / 1024}KB";
    }

    /// Bakes Unity meshes into a glTF binary the game can read.
    ///
    /// The failure this is built to avoid: an engine's own glTF export resolves a
    /// material through its shader graph, and anything it cannot reduce collapses
    /// to a constant. The result loads without error and renders solid black with
    /// no textures at all, which is easy to mistake for bad lighting. So the
    /// material pass here probes the property names real shaders actually use,
    /// falls back to a visible grey rather than black, and reports every material
    /// it could not resolve by name.
    public static class B4DGltfBaker
    {
        // Unity has no single "albedo" property. Standard, URP, HDRP and most
        // Shader Graph materials each name it differently, and reading the wrong
        // one gives black.
        static readonly string[] ColorProperties =
        {
            "_BaseColor",       // URP Lit, most Shader Graph
            "_Color",           // Built-in Standard
            "_BaseColorFactor",
            "_MainColor",
            "_TintColor",
            "_UnlitColor"
        };

        static readonly string[] TextureProperties =
        {
            "_BaseMap",         // URP Lit
            "_MainTex",         // Built-in Standard
            "_BaseColorMap",    // HDRP Lit
            "_AlbedoMap",
            "_MainTexture",
            "_Albedo"
        };

        static readonly string[] MetallicProperties = { "_Metallic", "_MetallicFactor" };
        static readonly string[] RoughnessProperties = { "_Roughness", "_RoughnessFactor" };
        static readonly string[] SmoothnessProperties = { "_Smoothness", "_Glossiness" };

        // ------------------------------------------------------------------
        // entry point
        // ------------------------------------------------------------------

        public static byte[] Bake(GameObject root, B4DBakeOptions options, out B4DBakeReport report)
        {
            options = options ?? new B4DBakeOptions();
            report = new B4DBakeReport();
            if (root == null) throw new ArgumentNullException(nameof(root));

            var bin = new MemoryStream();
            var gltf = new GltfDocument();
            var toRoot = root.transform.worldToLocalMatrix;

            // Everything is flattened into the root's space. A prop is a handful of
            // meshes, so a node hierarchy would only add ways to get it wrong.
            var primitives = new List<Dictionary<string, object>>();
            var materialIndices = new Dictionary<Material, int>();

            foreach (var source in CollectMeshes(root, report))
            {
                var mesh = source.mesh;
                var matrix = toRoot * source.transform.localToWorldMatrix;
                report.meshes++;

                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var uvs = mesh.uv;
                var colors = options.includeVertexColors ? mesh.colors : null;

                if (vertices == null || vertices.Length == 0)
                {
                    report.warnings.Add($"\"{source.transform.name}\" gave no vertices. Enable Read/Write on the model import settings.");
                    continue;
                }

                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var indices = mesh.GetTriangles(sub);
                    if (indices.Length == 0) continue;

                    var material = sub < source.materials.Length ? source.materials[sub] : null;
                    var primitive = BuildPrimitive(
                        gltf, bin, vertices, normals, uvs, colors, indices, matrix, options);
                    if (primitive == null) continue;

                    if (material != null)
                    {
                        if (!materialIndices.TryGetValue(material, out var materialIndex))
                        {
                            materialIndex = BuildMaterial(gltf, bin, material, options, report);
                            materialIndices[material] = materialIndex;
                        }
                        primitive["material"] = materialIndex;
                    }

                    primitives.Add(primitive);
                    report.primitives++;
                    report.triangles += indices.Length / 3;
                }
            }

            if (primitives.Count == 0)
                throw new InvalidOperationException("nothing to bake: the selection has no readable mesh geometry");

            report.materials = materialIndices.Count;

            gltf.meshes.Add(new Dictionary<string, object> { ["primitives"] = primitives });
            var node = new Dictionary<string, object> { ["mesh"] = 0 };
            if (!options.stripNames) node["name"] = root.name;
            gltf.nodes.Add(node);
            gltf.scenes.Add(new Dictionary<string, object> { ["nodes"] = new List<object> { 0 } });

            gltf.stripNames = options.stripNames;
            var bytes = Package(gltf, bin.ToArray());
            report.bytes = bytes.Length;

            // A malformed file loads as an invisible or black prop, which is the
            // exact failure this baker exists to prevent. Check before writing.
            var problem = Verify(bytes);
            if (problem != null)
                throw new InvalidOperationException($"the baked file failed its own check: {problem}");

            return bytes;
        }

        // ------------------------------------------------------------------
        // geometry
        // ------------------------------------------------------------------

        struct MeshSource
        {
            public Mesh mesh;
            public Transform transform;
            public Material[] materials;
        }

        static List<MeshSource> CollectMeshes(GameObject root, B4DBakeReport report)
        {
            var sources = new List<MeshSource>();

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(false))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
                sources.Add(new MeshSource
                {
                    mesh = filter.sharedMesh,
                    transform = filter.transform,
                    materials = renderer.sharedMaterials
                });
            }

            foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!skinned.enabled || skinned.sharedMesh == null) continue;
                // Freeze the current pose: the game has no skinning for props.
                var baked = new Mesh();
                skinned.BakeMesh(baked);
                sources.Add(new MeshSource
                {
                    mesh = baked,
                    transform = skinned.transform,
                    materials = skinned.sharedMaterials
                });
                report.warnings.Add($"\"{skinned.name}\" is skinned and was frozen in its current pose.");
            }

            return sources;
        }

        static Dictionary<string, object> BuildPrimitive(
            GltfDocument gltf, MemoryStream bin,
            Vector3[] vertices, Vector3[] normals, Vector2[] uvs, Color[] colors,
            int[] indices, Matrix4x4 matrix, B4DBakeOptions options)
        {
            // Only the vertices this submesh touches, renumbered from zero, so one
            // submesh never drags the whole mesh's vertex buffer along with it.
            var remap = new Dictionary<int, int>();
            var order = new List<int>();
            foreach (var index in indices)
            {
                if (remap.ContainsKey(index)) continue;
                remap[index] = order.Count;
                order.Add(index);
            }

            var count = order.Count;
            var positions = new float[count * 3];
            var outNormals = normals != null && normals.Length == vertices.Length ? new float[count * 3] : null;
            var outUvs = uvs != null && uvs.Length == vertices.Length ? new float[count * 2] : null;
            var outColors = colors != null && colors.Length == vertices.Length ? new float[count * 4] : null;

            var min = new float[] { float.MaxValue, float.MaxValue, float.MaxValue };
            var max = new float[] { float.MinValue, float.MinValue, float.MinValue };

            for (var i = 0; i < count; i++)
            {
                var v = matrix.MultiplyPoint3x4(vertices[order[i]]) * options.scale;
                positions[i * 3] = v.x; positions[i * 3 + 1] = v.y; positions[i * 3 + 2] = v.z;
                for (var c = 0; c < 3; c++)
                {
                    var value = c == 0 ? v.x : c == 1 ? v.y : v.z;
                    if (value < min[c]) min[c] = value;
                    if (value > max[c]) max[c] = value;
                }

                if (outNormals != null)
                {
                    var n = matrix.MultiplyVector(normals[order[i]]).normalized;
                    outNormals[i * 3] = n.x; outNormals[i * 3 + 1] = n.y; outNormals[i * 3 + 2] = n.z;
                }
                if (outUvs != null)
                {
                    var uv = uvs[order[i]];
                    outUvs[i * 2] = uv.x;
                    // Unity's texture origin is bottom left, glTF's is top left.
                    outUvs[i * 2 + 1] = 1f - uv.y;
                }
                if (outColors != null)
                {
                    var c = colors[order[i]];
                    outColors[i * 4] = c.r; outColors[i * 4 + 1] = c.g;
                    outColors[i * 4 + 2] = c.b; outColors[i * 4 + 3] = c.a;
                }
            }

            var triangles = new int[indices.Length];
            for (var i = 0; i < indices.Length; i++) triangles[i] = remap[indices[i]];

            // Unity is left handed and glTF is right handed. Rather than guess at a
            // blanket winding flip, make each triangle agree with the normals it was
            // given: that is correct whichever way the source was wound, and it is
            // the difference between a solid prop and one you can see straight
            // through from the front.
            OrientTrianglesToNormals(triangles, positions, outNormals);

            var primitive = new Dictionary<string, object>();
            var attributes = new Dictionary<string, object>
            {
                ["POSITION"] = gltf.AddFloatAccessor(bin, positions, 3, "VEC3", min, max)
            };
            if (outNormals != null) attributes["NORMAL"] = gltf.AddFloatAccessor(bin, outNormals, 3, "VEC3", null, null);
            if (outUvs != null) attributes["TEXCOORD_0"] = gltf.AddFloatAccessor(bin, outUvs, 2, "VEC2", null, null);
            if (outColors != null) attributes["COLOR_0"] = gltf.AddFloatAccessor(bin, outColors, 4, "VEC4", null, null);

            primitive["attributes"] = attributes;
            primitive["indices"] = gltf.AddIndexAccessor(bin, triangles, count);
            primitive["mode"] = 4;
            return primitive;
        }

        /// Flips any triangle whose winding disagrees with its own vertex normals.
        static void OrientTrianglesToNormals(int[] triangles, float[] positions, float[] normals)
        {
            if (normals == null) return;
            for (var t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                var ax = positions[a * 3]; var ay = positions[a * 3 + 1]; var az = positions[a * 3 + 2];
                var bx = positions[b * 3]; var by = positions[b * 3 + 1]; var bz = positions[b * 3 + 2];
                var cx = positions[c * 3]; var cy = positions[c * 3 + 1]; var cz = positions[c * 3 + 2];

                // face normal from the winding, by the right hand rule glTF assumes
                var ux = bx - ax; var uy = by - ay; var uz = bz - az;
                var vx = cx - ax; var vy = cy - ay; var vz = cz - az;
                var fx = uy * vz - uz * vy;
                var fy = uz * vx - ux * vz;
                var fz = ux * vy - uy * vx;

                var nx = normals[a * 3] + normals[b * 3] + normals[c * 3];
                var ny = normals[a * 3 + 1] + normals[b * 3 + 1] + normals[c * 3 + 1];
                var nz = normals[a * 3 + 2] + normals[b * 3 + 2] + normals[c * 3 + 2];

                if (fx * nx + fy * ny + fz * nz < 0f)
                {
                    triangles[t + 1] = c;
                    triangles[t + 2] = b;
                }
            }
        }

        // ------------------------------------------------------------------
        // materials
        // ------------------------------------------------------------------

        static int BuildMaterial(GltfDocument gltf, MemoryStream bin, Material material,
            B4DBakeOptions options, B4DBakeReport report)
        {
            var pbr = new Dictionary<string, object>();
            var json = new Dictionary<string, object> { ["pbrMetallicRoughness"] = pbr };
            // Names are useful while authoring and are pure liability in a shipped
            // file: they are what identifies the pack a mesh came from. The reader
            // never looks at them.
            if (!options.stripNames) json["name"] = material.name;

            var colorProperty = FirstPresent(material, ColorProperties);
            var textureProperty = FirstPresent(material, TextureProperties);

            var color = colorProperty != null ? material.GetColor(colorProperty) : Color.white;
            Texture texture = null;
            if (options.includeTextures && textureProperty != null) texture = material.GetTexture(textureProperty);

            if (colorProperty == null && textureProperty == null)
            {
                // This is the black-and-textureless case. A neutral grey is obviously
                // unfinished; black looks like a lighting bug and hides the problem.
                color = new Color(0.72f, 0.72f, 0.72f, 1f);
                report.warnings.Add(
                    $"material \"{material.name}\" (shader \"{material.shader.name}\") exposes no base colour or " +
                    "texture property this baker recognises, so it was baked as plain grey. Assign a simpler " +
                    "material, or bake the look down to a texture first.");
            }
            else if (colorProperty != null && color.maxColorComponent <= 0.001f && texture == null)
            {
                color = new Color(0.72f, 0.72f, 0.72f, 1f);
                report.warnings.Add(
                    $"material \"{material.name}\" resolved to black with no texture, which is almost always a " +
                    "shader whose colour lives somewhere this baker cannot see. Baked as plain grey instead.");
            }

            // glTF colour factors are linear.
            var linear = color.linear;
            pbr["baseColorFactor"] = new List<object> { linear.r, linear.g, linear.b, color.a };

            var metallic = FirstPresent(material, MetallicProperties);
            pbr["metallicFactor"] = metallic != null ? material.GetFloat(metallic) : 0f;

            var roughness = FirstPresent(material, RoughnessProperties);
            if (roughness != null)
            {
                pbr["roughnessFactor"] = material.GetFloat(roughness);
            }
            else
            {
                var smoothness = FirstPresent(material, SmoothnessProperties);
                pbr["roughnessFactor"] = smoothness != null ? 1f - material.GetFloat(smoothness) : 0.85f;
            }

            if (texture is Texture2D texture2D)
            {
                var png = EncodeTexture(texture2D, options.maxTextureSize, report);
                if (png != null)
                {
                    var index = gltf.AddImage(bin, png, "image/png");
                    pbr["baseColorTexture"] = new Dictionary<string, object> { ["index"] = index };
                    report.textures++;
                }
            }
            else if (options.includeTextures && textureProperty != null && texture == null)
            {
                report.warnings.Add($"material \"{material.name}\" has a texture slot but nothing assigned to it.");
            }

            if (color.a < 1f)
            {
                json["alphaMode"] = "BLEND";
            }

            gltf.materials.Add(json);
            return gltf.materials.Count - 1;
        }

        static string FirstPresent(Material material, string[] candidates)
        {
            foreach (var name in candidates)
            {
                if (material.HasProperty(name)) return name;
            }
            return null;
        }

        /// Reads a texture's pixels regardless of whether it was imported readable
        /// or is stored compressed, by rendering it into a temporary target first.
        /// Calling EncodeToPNG directly fails on most imported textures.
        static byte[] EncodeTexture(Texture2D source, int maxSize, B4DBakeReport report)
        {
            var width = source.width;
            var height = source.height;
            if (maxSize > 0 && Mathf.Max(width, height) > maxSize)
            {
                var factor = (float)maxSize / Mathf.Max(width, height);
                width = Mathf.Max(1, Mathf.RoundToInt(width * factor));
                height = Mathf.Max(1, Mathf.RoundToInt(height * factor));
            }

            RenderTexture target = null;
            Texture2D readable = null;
            var previous = RenderTexture.active;
            try
            {
                target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(source, target);
                RenderTexture.active = target;

                readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply();
                return readable.EncodeToPNG();
            }
            catch (Exception e)
            {
                report.warnings.Add($"texture \"{source.name}\" could not be read: {e.Message}");
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        // ------------------------------------------------------------------
        // container
        // ------------------------------------------------------------------

        static byte[] Package(GltfDocument gltf, byte[] bin)
        {
            var json = gltf.ToJson(bin.Length);
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
            var jsonPad = (4 - (jsonBytes.Length % 4)) % 4;
            var binPad = (4 - (bin.Length % 4)) % 4;

            var total = 12 + 8 + jsonBytes.Length + jsonPad + (bin.Length > 0 ? 8 + bin.Length + binPad : 0);
            var output = new MemoryStream(total);
            var writer = new BinaryWriter(output);

            writer.Write(0x46546C67u);              // "glTF"
            writer.Write(2u);
            writer.Write((uint)total);

            // Chunk lengths include their padding, as the specification requires.
            writer.Write((uint)(jsonBytes.Length + jsonPad));
            writer.Write(0x4E4F534Au);              // "JSON"
            writer.Write(jsonBytes);
            for (var i = 0; i < jsonPad; i++) writer.Write((byte)0x20);   // spaces

            if (bin.Length > 0)
            {
                writer.Write((uint)(bin.Length + binPad));
                writer.Write(0x004E4942u);          // "BIN"
                writer.Write(bin);
                for (var i = 0; i < binPad; i++) writer.Write((byte)0);
            }

            writer.Flush();
            return output.ToArray();
        }

        /// Walks the finished bytes the way a reader will, so a transcription slip
        /// surfaces here rather than as an invisible prop in the game.
        public static string Verify(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 20) return "the file is too short to be a glb";
            if (BitConverter.ToUInt32(bytes, 0) != 0x46546C67u) return "the magic number is wrong";
            if (BitConverter.ToUInt32(bytes, 4) != 2u) return "the version is not 2";
            if (BitConverter.ToUInt32(bytes, 8) != (uint)bytes.Length) return "the declared length does not match the file";

            var offset = 12;
            var sawJson = false;
            while (offset + 8 <= bytes.Length)
            {
                var length = (int)BitConverter.ToUInt32(bytes, offset);
                var type = BitConverter.ToUInt32(bytes, offset + 4);
                if (length < 0 || offset + 8 + length > bytes.Length) return "a chunk runs past the end of the file";
                if (length % 4 != 0) return "a chunk length is not padded to four bytes";
                if (type == 0x4E4F534Au) sawJson = true;
                offset += 8 + length;
            }
            if (offset != bytes.Length) return "the chunks do not fill the file exactly";
            return sawJson ? null : "there is no json chunk";
        }

        // ------------------------------------------------------------------
        // minimal glTF document
        // ------------------------------------------------------------------

        class GltfDocument
        {
            public bool stripNames;
            public List<Dictionary<string, object>> accessors = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> bufferViews = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> meshes = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> materials = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> images = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> textures = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> nodes = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> scenes = new List<Dictionary<string, object>>();

            /// Pads the blob so the next view starts on a four byte boundary, which
            /// the specification requires of every accessor.
            static void Align(MemoryStream bin)
            {
                while (bin.Length % 4 != 0) bin.WriteByte(0);
            }

            int AddBufferView(MemoryStream bin, byte[] data, int? target)
            {
                Align(bin);
                var offset = (int)bin.Length;
                bin.Write(data, 0, data.Length);
                var view = new Dictionary<string, object>
                {
                    ["buffer"] = 0,
                    ["byteOffset"] = offset,
                    ["byteLength"] = data.Length
                };
                if (target.HasValue) view["target"] = target.Value;
                bufferViews.Add(view);
                return bufferViews.Count - 1;
            }

            public int AddFloatAccessor(MemoryStream bin, float[] values, int components, string type, float[] min, float[] max)
            {
                var bytes = new byte[values.Length * 4];
                Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
                var view = AddBufferView(bin, bytes, 34962);   // ARRAY_BUFFER

                var accessor = new Dictionary<string, object>
                {
                    ["bufferView"] = view,
                    ["componentType"] = 5126,                  // FLOAT
                    ["count"] = values.Length / components,
                    ["type"] = type
                };
                // POSITION must carry bounds; readers and viewers rely on them.
                if (min != null) accessor["min"] = min.Select(v => (object)v).ToList();
                if (max != null) accessor["max"] = max.Select(v => (object)v).ToList();
                accessors.Add(accessor);
                return accessors.Count - 1;
            }

            public int AddIndexAccessor(MemoryStream bin, int[] indices, int vertexCount)
            {
                byte[] bytes;
                int componentType;
                // Sixteen bit indices halve the file, but only while they can address
                // every vertex.
                if (vertexCount <= 65535)
                {
                    componentType = 5123;                      // UNSIGNED_SHORT
                    bytes = new byte[indices.Length * 2];
                    for (var i = 0; i < indices.Length; i++)
                    {
                        var value = (ushort)indices[i];
                        bytes[i * 2] = (byte)(value & 0xFF);
                        bytes[i * 2 + 1] = (byte)(value >> 8);
                    }
                }
                else
                {
                    componentType = 5125;                      // UNSIGNED_INT
                    bytes = new byte[indices.Length * 4];
                    Buffer.BlockCopy(indices, 0, bytes, 0, bytes.Length);
                }

                var view = AddBufferView(bin, bytes, 34963);   // ELEMENT_ARRAY_BUFFER
                accessors.Add(new Dictionary<string, object>
                {
                    ["bufferView"] = view,
                    ["componentType"] = componentType,
                    ["count"] = indices.Length,
                    ["type"] = "SCALAR"
                });
                return accessors.Count - 1;
            }

            public int AddImage(MemoryStream bin, byte[] png, string mimeType)
            {
                var view = AddBufferView(bin, png, null);
                images.Add(new Dictionary<string, object>
                {
                    ["bufferView"] = view,
                    ["mimeType"] = mimeType
                });
                textures.Add(new Dictionary<string, object> { ["source"] = images.Count - 1 });
                return textures.Count - 1;
            }

            public string ToJson(int binLength)
            {
                var root = new Dictionary<string, object>
                {
                    ["asset"] = stripNames
                        ? new Dictionary<string, object> { ["version"] = "2.0" }
                        : new Dictionary<string, object>
                        {
                            ["version"] = "2.0",
                            ["generator"] = "Blox 4 Dead Map Kit"
                        },
                    ["scene"] = 0,
                    ["scenes"] = scenes.Cast<object>().ToList(),
                    ["nodes"] = nodes.Cast<object>().ToList(),
                    ["meshes"] = meshes.Cast<object>().ToList(),
                    ["accessors"] = accessors.Cast<object>().ToList(),
                    ["bufferViews"] = bufferViews.Cast<object>().ToList(),
                    ["buffers"] = new List<object>
                    {
                        new Dictionary<string, object> { ["byteLength"] = binLength }
                    }
                };
                if (materials.Count > 0) root["materials"] = materials.Cast<object>().ToList();
                if (images.Count > 0) root["images"] = images.Cast<object>().ToList();
                if (textures.Count > 0) root["textures"] = textures.Cast<object>().ToList();
                return B4DJson.Write(root, false);
            }
        }
    }
}
