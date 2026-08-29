#!/usr/bin/env node
// Says what is actually inside a .glb, and names the specific reasons a viewer
// would show it untextured.
//
//   node Tools~/glb-inspect/inspect.mjs path/to/model.glb
//
// Written for the case where a bake looks fine in Unity and arrives untextured
// in Blender. It reads the file the same way a viewer does and reports the
// chain a texture has to survive: material to texture to image to bytes, plus
// the UVs the mesh has to supply for any of it to land.
import fs from 'node:fs';

const path = process.argv[2];
if (!path) {
  console.error('usage: node inspect.mjs <file.glb>');
  process.exit(2);
}

const buffer = fs.readFileSync(path);
const view = new DataView(buffer.buffer, buffer.byteOffset, buffer.byteLength);

function fail(message) {
  console.error(`NOT READABLE: ${message}`);
  process.exit(1);
}

if (buffer.length < 12) fail('the file is too short to be a glb');
if (view.getUint32(0, true) !== 0x46546c67) fail('the magic number is not "glTF"');
const version = view.getUint32(4, true);
if (version !== 2) fail(`glb version ${version}, expected 2`);
const declared = view.getUint32(8, true);
if (declared !== buffer.length) {
  console.log(`WARNING  the header declares ${declared} bytes but the file is ${buffer.length}`);
}

let offset = 12, json = null, bin = null;
const chunks = [];
while (offset + 8 <= buffer.length) {
  const length = view.getUint32(offset, true);
  const type = view.getUint32(offset + 4, true);
  const start = offset + 8;
  if (start + length > buffer.length) fail(`a chunk at ${offset} runs past the end of the file`);
  chunks.push({ type, length, padded: length % 4 === 0 });
  if (type === 0x4e4f534a) json = JSON.parse(new TextDecoder().decode(buffer.subarray(start, start + length)));
  if (type === 0x004e4942) bin = buffer.subarray(start, start + length);
  offset = start + length + ((4 - (length % 4)) % 4);
}
if (!json) fail('there is no json chunk');

const problems = [];
const notes = [];

console.log(`file      ${path}`);
console.log(`size      ${(buffer.length / 1024).toFixed(1)}KB`);
console.log(`generator ${(json.asset && json.asset.generator) || '(stripped)'}`);
chunks.forEach((c, i) => {
  if (!c.padded) problems.push(`chunk ${i} declares a length of ${c.length}, which is not padded to four bytes. Blender and three.js reject this.`);
});
if (json.extensionsRequired && json.extensionsRequired.length) {
  problems.push(`the file requires extensions most viewers will not have: ${json.extensionsRequired.join(', ')}`);
}

const images = json.images || [];
const textures = json.textures || [];
const materials = json.materials || [];
const meshes = json.meshes || [];
const bufferViews = json.bufferViews || [];

console.log(`\ncontents  ${meshes.length} mesh(es), ${materials.length} material(s), ${textures.length} texture(s), ${images.length} image(s)`);

// ---- images -------------------------------------------------------------
console.log('\nimages');
if (images.length === 0) {
  console.log('  none. Every material in this file is a flat colour.');
  notes.push('There are no images at all, so the bake never got a texture out of Unity. '
    + 'That is a Unity side problem, not a Blender one: check the bake report for a warning '
    + 'naming the material, and confirm the texture is assigned to the property the shader '
    + 'actually uses (_BaseMap on URP, _MainTex on Built-in, _BaseColorMap on HDRP).');
}
const PNG = [0x89, 0x50, 0x4e, 0x47];
const JPG = [0xff, 0xd8, 0xff];
images.forEach((image, i) => {
  const bits = [`image ${i}`];
  if (image.uri) {
    bits.push(image.uri.startsWith('data:') ? 'inline data uri' : `external file "${image.uri}"`);
    if (!image.uri.startsWith('data:')) {
      problems.push(`image ${i} points at an external file "${image.uri}". If that file is not beside the glb, every viewer shows it untextured.`);
    }
  } else if (image.bufferView !== undefined) {
    const v = bufferViews[image.bufferView];
    if (!v) { problems.push(`image ${i} references buffer view ${image.bufferView}, which does not exist`); return; }
    const start = v.byteOffset || 0;
    const bytes = bin ? bin.subarray(start, start + v.byteLength) : null;
    bits.push(`${(v.byteLength / 1024).toFixed(1)}KB`, image.mimeType || '(no mimeType)');
    if (!bytes || bytes.length === 0) {
      problems.push(`image ${i} has no bytes behind it`);
    } else {
      const isPng = PNG.every((b, k) => bytes[k] === b);
      const isJpg = JPG.every((b, k) => bytes[k] === b);
      if (!isPng && !isJpg) {
        problems.push(`image ${i} does not start with a PNG or JPEG signature, so the bytes are not a readable image`);
      } else {
        bits.push(isPng ? 'valid PNG header' : 'valid JPEG header');
        if (isPng && image.mimeType !== 'image/png') problems.push(`image ${i} is a PNG but declares "${image.mimeType}"`);
        if (isJpg && image.mimeType !== 'image/jpeg') problems.push(`image ${i} is a JPEG but declares "${image.mimeType}"`);
      }
      if (!image.mimeType) {
        problems.push(`image ${i} has no mimeType. It is required when an image lives in a buffer view, and Blender skips images without it.`);
      }
    }
  } else {
    problems.push(`image ${i} has neither a uri nor a bufferView, so there is nothing to load`);
  }
  console.log('  ' + bits.join('  '));
});

// ---- textures -----------------------------------------------------------
if (textures.length) {
  console.log('\ntextures');
  textures.forEach((texture, i) => {
    if (texture.source === undefined) {
      problems.push(`texture ${i} has no source, so it points at no image`);
      console.log(`  texture ${i}  NO SOURCE`);
    } else if (!images[texture.source]) {
      problems.push(`texture ${i} points at image ${texture.source}, which does not exist`);
      console.log(`  texture ${i}  BROKEN -> image ${texture.source}`);
    } else {
      console.log(`  texture ${i}  -> image ${texture.source}${texture.sampler !== undefined ? `, sampler ${texture.sampler}` : ', no sampler (defaults apply)'}`);
    }
  });
}

// ---- materials ----------------------------------------------------------
console.log('\nmaterials');
const texturedMaterials = new Set();
materials.forEach((material, i) => {
  const pbr = material.pbrMetallicRoughness || {};
  const factor = pbr.baseColorFactor || [1, 1, 1, 1];
  const hasTexture = !!pbr.baseColorTexture;
  if (hasTexture) texturedMaterials.add(i);
  const name = material.name ? `"${material.name}"` : '(name stripped)';
  console.log(`  material ${i} ${name}  baseColor [${factor.map(n => n.toFixed(2)).join(', ')}]  ${hasTexture ? `texture ${pbr.baseColorTexture.index}` : 'no texture'}`);

  if (hasTexture && !textures[pbr.baseColorTexture.index]) {
    problems.push(`material ${i} uses texture ${pbr.baseColorTexture.index}, which does not exist`);
  }
  const rgbMax = Math.max(factor[0], factor[1], factor[2]);
  if (hasTexture && rgbMax <= 0.02) {
    problems.push(`material ${i} has a texture but a near black baseColorFactor [${factor.slice(0, 3).map(n => n.toFixed(3)).join(', ')}]. `
      + 'The factor multiplies the texture, so the result renders black even though the image is present. '
      + 'This is the classic "textures are missing" report.');
  }
  if (!hasTexture && rgbMax <= 0.02) {
    notes.push(`material ${i} is flat black with no texture, which usually means the bake could not read the shader's colour.`);
  }
});

// ---- meshes -------------------------------------------------------------
console.log('\nmeshes');
meshes.forEach((mesh, m) => {
  (mesh.primitives || []).forEach((primitive, p) => {
    const attributes = Object.keys(primitive.attributes || {});
    const material = primitive.material;
    console.log(`  mesh ${m} primitive ${p}  [${attributes.join(', ')}]  material ${material === undefined ? '(none)' : material}`);

    if (material !== undefined && texturedMaterials.has(material) && !attributes.includes('TEXCOORD_0')) {
      problems.push(`mesh ${m} primitive ${p} uses textured material ${material} but has no TEXCOORD_0. `
        + 'Without texture coordinates there is nowhere to put the image, so it imports untextured. '
        + 'The source mesh in Unity has no UVs.');
    }
    if (!attributes.includes('NORMAL')) {
      notes.push(`mesh ${m} primitive ${p} has no normals, so lighting is generated from the faces.`);
    }
  });
});

// ---- verdict ------------------------------------------------------------
console.log('');
if (problems.length === 0) {
  console.log('VERDICT   the texture chain in this file is intact.');
  if (images.length === 0) {
    console.log('          But it carries no images, so it was always going to look flat.');
  } else {
    console.log('          If Blender still shows no texture, check the viewport shading mode:');
    console.log('          Solid mode ignores textures. Switch to Material Preview or Rendered.');
  }
} else {
  console.log(`VERDICT   ${problems.length} problem(s) that would show as missing textures:\n`);
  problems.forEach((p, i) => console.log(`  ${i + 1}. ${p}\n`));
}
if (notes.length) {
  console.log('notes');
  notes.forEach(n => console.log(`  - ${n}`));
}
process.exit(problems.length ? 1 : 0);
