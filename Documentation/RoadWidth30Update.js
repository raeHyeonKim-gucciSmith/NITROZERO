// One-time, scene-preserving width correction for the shared moon-road meshes.
// Run with --check first, then --apply. Does not open or save Unity scenes.
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const ratio = 30 / 35;
const lightGuid = '62ae067da95c5414486b42f2c078b5ad';
const targetLightOffset = 14.55; // center of the shoulder between the white edge line (14.143 m) and 15 m asphalt edge
const targetLightHeight = 0.08; // clear the road mesh instead of z-fighting with it
const roadAssets = {
  avoid: ['AvoidMissile_AsphaltRoad.asset', 'AvoidMissile_AsphaltRoad_EdgeLines.asset', 'AvoidMissile_AsphaltRoad_EdgeDust.asset'],
  boost: ['BoostOn_AsphaltRoad.asset', 'BoostOn_AsphaltRoad_EdgeLines.asset', 'BoostOn_AsphaltRoad_EdgeDust.asset'],
  dome: ['DomeExit_AsphaltRoad.asset', 'DomeExit_AsphaltRoad_EdgeLines.asset', 'DomeExit_AsphaltRoad_EdgeDust.asset'],
  racing: ['Racing_DriftRoad.asset', 'Racing_EdgeLines.asset', 'Racing_EdgeDust.asset'],
};
const scenes = {
  avoid: ['Assets/Scenes/avoidMissile.unity', 'Assets/Scenes/CarTest.unity', 'Assets/01RAEHYEON/RHScenes/RH_avoidMissile.unity', 'Assets/01RAEHYEON/RHScenes/RH_CarTest.unity', 'Assets/03DAMIN/DM_avoidMissile.unity', 'Assets/03DAMIN/DM_avoidMissile_Cinematic.unity'],
  boost: ['Assets/Scenes/boostOn.unity', 'Assets/01RAEHYEON/RHScenes/RH_boostMode.unity', 'Assets/01RAEHYEON/RHScenes/RH_inside.unity', 'Assets/02YUJEONG/boostOn_Yujeong.unity', 'Assets/02YUJEONG/boostOn_Yujeong_1.unity'],
  dome: ['Assets/Scenes/domeInTheMoon.unity', 'Assets/01RAEHYEON/RHScenes/RH_domeInTheMoon.unity', 'Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity'],
  racing: ['Assets/Scenes/racing.unity', 'Assets/01RAEHYEON/RHScenes/RH_racing.unity'],
};
const changes = new Map();
const verifying = process.argv.includes('--verify');
const realigning = process.argv.includes('--realign');
const fmt = n => String(Number(n.toFixed(6)));
const xy = b => {
  const m = b.match(/m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/);
  return m && {x: +m[1], y: +m[2], z: +m[3]};
};
const scale = b => {
  const m = b.match(/m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/);
  return m && {x: +m[1], y: +m[2], z: +m[3]};
};
function meshData(asset) {
  const p = path.join(root, 'Assets/Terrain', asset);
  const text = fs.readFileSync(p, 'utf8');
  const n = +(text.match(/m_VertexCount: (\d+)/) || [])[1];
  const hex = (text.match(/_typelessdata: ([0-9a-f]+)/) || [])[1];
  if (!n || !hex) throw Error('Cannot read mesh: ' + asset);
  const bytes = Buffer.from(hex, 'hex');
  if (bytes.length !== n * 32) throw Error('Unexpected vertex stride: ' + asset);
  const vertices = Array.from({length: n}, (_, i) => ({
    x: bytes.readFloatLE(i * 32), y: bytes.readFloatLE(i * 32 + 4), z: bytes.readFloatLE(i * 32 + 8)
  }));
  return {p, text, bytes, vertices};
}
function widthOf(vertices) {
  return Math.hypot(vertices[0].x - vertices[1].x, vertices[0].z - vertices[1].z);
}
function resizeMesh(asset, group) {
  const m = meshData(asset);
  if (m.vertices.length % group) throw Error('Bad section count: ' + asset);
  if (group === 2 && Math.abs(widthOf(m.vertices) - 35) > .02) throw Error('Road is not 35 m: ' + asset);
  for (let i = 0; i < m.vertices.length; i += group) {
    const a = m.vertices[i], b = m.vertices[i + group - 1];
    const cx = (a.x + b.x) / 2, cz = (a.z + b.z) / 2;
    for (let j = 0; j < group; j++) {
      const v = m.vertices[i + j];
      v.x = cx + (v.x - cx) * ratio;
      v.z = cz + (v.z - cz) * ratio;
      m.bytes.writeFloatLE(v.x, (i + j) * 32);
      m.bytes.writeFloatLE(v.z, (i + j) * 32 + 8);
    }
  }
  const lo = {x: Infinity, y: Infinity, z: Infinity}, hi = {x: -Infinity, y: -Infinity, z: -Infinity};
  for (const v of m.vertices) for (const k of ['x', 'y', 'z']) {
    lo[k] = Math.min(lo[k], v[k]); hi[k] = Math.max(hi[k], v[k]);
  }
  const center = {}, extent = {};
  for (const k of ['x', 'y', 'z']) {center[k] = (lo[k] + hi[k]) / 2; extent[k] = (hi[k] - lo[k]) / 2;}
  let text = m.text.replace(/_typelessdata: [0-9a-f]+/, '_typelessdata: ' + m.bytes.toString('hex'));
  let bounds = 0;
  text = text.replace(/m_Center: \{x: [^}]+\}\r?\n([ \t]*)m_Extent: \{x: [^}]+\}/g, (_, indent) => {
    bounds++;
    return `m_Center: {x: ${fmt(center.x)}, y: ${fmt(center.y)}, z: ${fmt(center.z)}}\n${indent}m_Extent: {x: ${fmt(extent.x)}, y: ${fmt(extent.y)}, z: ${fmt(extent.z)}}`;
  });
  if (bounds !== 2) throw Error('Expected two bounds in ' + asset + ', got ' + bounds);
  changes.set(m.p, text);
  return m.vertices;
}
function nearest(points, p) {
  let best = null;
  for (let i = 0; i + 1 < points.length; i++) {
    const a = points[i], b = points[i + 1], dx = b.x-a.x, dz = b.z-a.z;
    const len2 = dx*dx + dz*dz;
    const rawT = ((p.x-a.x)*dx + (p.z-a.z)*dz)/len2;
    const t = Math.max(i === 0 ? -.2 : 0, Math.min(i === points.length-2 ? 1.2 : 1, rawT));
    const x = a.x+t*dx, z = a.z+t*dz;
    const nx = -dz/Math.sqrt(len2), nz = dx/Math.sqrt(len2);
    const signed = (p.x-x)*nx + (p.z-z)*nz;
    const d2 = (p.x-x)**2 + (p.z-z)**2;
    if (!best || d2 < best.d2) best = {x,z,nx,nz,signed,d2};
  }
  return best;
}
function adjustScene(relative, centerline, kind) {
  const p = path.join(root, relative), source = fs.readFileSync(p, 'utf8');
  let blocks = source.split(/(?=^--- !u!)/m);
  const transforms = new Map(), lightObjects = new Set();
  for (const b of blocks) {
    const go = b.match(/^--- !u!1 &(\d+)/);
    if (go && b.includes('m_Name: "\\uCC28')) lightObjects.add(go[1]);
    const tr = b.match(/^--- !u!4 &(\d+)/);
    if (tr && b.includes('m_GameObject:')) transforms.set(tr[1], {
      position: xy(b), scale: scale(b), parent: (b.match(/m_Father: \{fileID: (\d+)\}/) || [])[1] || '0'
      , rotation: (b.match(/m_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}/) || []).slice(1).map(Number)
    });
  }
  const parentCounts = new Map();
  for (const b of blocks) {
    const go = (b.match(/m_GameObject: \{fileID: (\d+)\}/) || [])[1];
    if (/^--- !u!4 &\d+/.test(b) && lightObjects.has(go)) {
      const parent = (b.match(/m_Father: \{fileID: (\d+)\}/) || [])[1];
      if (parent && parent !== '0') parentCounts.set(parent, (parentCounts.get(parent) || 0) + 1);
    }
    if (b.includes(`m_SourcePrefab: {fileID: 100100000, guid: ${lightGuid}`)) {
      const parent = (b.match(/m_TransformParent: \{fileID: (\d+)\}/) || [])[1];
      if (parent && parent !== '0') parentCounts.set(parent, (parentCounts.get(parent) || 0) + 1);
    }
  }
  const guideParent = [...parentCounts].sort((a,b) => b[1]-a[1])[0]?.[0];
  function world(id) {
    if (id === '0') return {x:0,y:0,z:0,sx:1,sy:1,sz:1};
    const t = transforms.get(id);
    if (!t || !t.position || !t.scale) throw Error('Missing light parent ' + id + ' in ' + relative);
    if (t.rotation.length !== 4 || Math.abs(t.rotation[0])+Math.abs(t.rotation[1])+Math.abs(t.rotation[2])+Math.abs(t.rotation[3]-1) > .0001)
      throw Error('Rotated guide-light parent needs explicit world conversion: ' + relative);
    const up = world(t.parent);
    return {x:up.x+t.position.x*up.sx, y:up.y+t.position.y*up.sy,
      z:up.z+t.position.z*up.sz, sx:up.sx*t.scale.x, sy:up.sy*t.scale.y, sz:up.sz*t.scale.z};
  }
  let moved = 0, left = 0, right = 0, displaced = 0, maxError = 0;
  let minHeightGap = Infinity, maxHeightGap = -Infinity;
  function target(local, parentId) {
    const t = world(parentId);
    const heightGap = t.y+local.y*t.sy-centerline[0].y;
    if (verifying && Math.abs(heightGap-targetLightHeight) > .01)
      throw Error(`Guide light height is ${heightGap.toFixed(3)} m above asphalt in ${relative}, expected ${targetLightHeight}`);
    minHeightGap = Math.min(minHeightGap, heightGap);
    maxHeightGap = Math.max(maxHeightGap, heightGap);
    const pos = {x:t.x+local.x*t.sx, z:t.z+local.z*t.sz};
    const n = nearest(centerline, pos), d = Math.sqrt(n.d2);
    if (verifying && Math.abs(d-targetLightOffset) > .02)
      throw Error(`Guide light is ${d.toFixed(3)} m from centerline in ${relative}, expected ${targetLightOffset}`);
    if (d < 6 || d > 100) throw Error(`Unexpected light distance ${d.toFixed(2)} in ${relative}`);
    if (d > 13) displaced++;
    const side = Math.sign(n.signed);
    if (side < 0) left++; else right++;
    let low = 0, high = 30;
    for (let i = 0; i < 24; i++) {
      const mid = (low+high)/2;
      const check = {x:n.x+n.nx*side*mid, z:n.z+n.nz*side*mid};
      if (Math.sqrt(nearest(centerline, check).d2) < targetLightOffset) low = mid;
      else high = mid;
    }
    const offset = (low+high)/2;
    const x = n.x+n.nx*side*offset;
    const z = n.z+n.nz*side*offset;
    maxError = Math.max(maxError, Math.abs(Math.sqrt(nearest(centerline, {x,z}).d2) - targetLightOffset));
    moved++;
    return {x:(x-t.x)/t.sx, y:(centerline[0].y+targetLightHeight-t.y)/t.sy, z:(z-t.z)/t.sz};
  }
  blocks = blocks.map(b => {
    const go = (b.match(/m_GameObject: \{fileID: (\d+)\}/) || [])[1];
    if (/^--- !u!4 &\d+/.test(b) && lightObjects.has(go)) {
      const parent = (b.match(/m_Father: \{fileID: (\d+)\}/) || [])[1];
      if (parent && parent === guideParent) {
        const pos = xy(b), next = target(pos, parent);
        return b.replace(/m_LocalPosition: \{x: [^}]+\}/,
          `m_LocalPosition: {x: ${fmt(next.x)}, y: ${fmt(next.y)}, z: ${fmt(next.z)}}`);
      }
    }
    if (b.includes(`m_SourcePrefab: {fileID: 100100000, guid: ${lightGuid}`)) {
      const parent = (b.match(/m_TransformParent: \{fileID: (\d+)\}/) || [])[1];
      const x = (b.match(/propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)/) || [])[1];
      const y = (b.match(/propertyPath: m_LocalPosition\.y\r?\n\s+value: ([^\r\n]+)/) || [])[1];
      const z = (b.match(/propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)/) || [])[1];
      if (parent && parent === guideParent && x && y && z) {
        const next = target({x:+x,y:+y,z:+z},parent);
        return b.replace(/(propertyPath: m_LocalPosition\.x\r?\n\s+value: )[^\r\n]+/, `$1${fmt(next.x)}`)
          .replace(/(propertyPath: m_LocalPosition\.y\r?\n\s+value: )[^\r\n]+/, `$1${fmt(next.y)}`)
          .replace(/(propertyPath: m_LocalPosition\.z\r?\n\s+value: )[^\r\n]+/, `$1${fmt(next.z)}`);
      }
    }
    return b;
  });
  if (kind === 'avoid' && (verifying || process.argv.includes('--normalize-avoid-stations'))) {
    const sides = {left: [], right: []};
    for (let i = 0; i < blocks.length; i++) {
      const b = blocks[i], go = (b.match(/m_GameObject: \{fileID: (\d+)\}/) || [])[1];
      if (/^--- !u!4 &\d+/.test(b) && lightObjects.has(go)) {
        const parent = (b.match(/m_Father: \{fileID: (\d+)\}/) || [])[1];
        if (parent !== guideParent) continue;
        const pos = xy(b);
        sides[pos.z < 0 ? 'left' : 'right'].push({i, x:pos.x, direct:true});
      } else if (b.includes(`m_SourcePrefab: {fileID: 100100000, guid: ${lightGuid}`)) {
        const parent = (b.match(/m_TransformParent: \{fileID: (\d+)\}/) || [])[1];
        if (parent !== guideParent) continue;
        const x = +(b.match(/propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)/) || [])[1];
        const z = +(b.match(/propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)/) || [])[1];
        sides[z < 0 ? 'left' : 'right'].push({i,x,direct:false});
      }
    }
    for (const side of Object.values(sides)) {
      side.sort((a,b) => a.x-b.x);
      if (side.length !== 70) throw Error('Avoid guide-light row does not have 70 posts: '+relative);
      for (let i = 0; i < side.length; i++) {
        const desired = -1998 + i*(3996/69);
        const item = side[i];
        if (verifying && Math.abs(item.x-desired) > .01)
          throw Error(`Uneven/outside guide-light station ${i} at ${item.x.toFixed(2)} in ${relative}`);
        if (!verifying && process.argv.includes('--normalize-avoid-stations')) {
          blocks[item.i] = item.direct
            ? blocks[item.i].replace(/(m_LocalPosition: \{x: )[^,]+/, (_,prefix) => prefix+fmt(desired))
            : blocks[item.i].replace(/(propertyPath: m_LocalPosition\.x\r?\n\s+value: )[^\r\n]+/, (_,prefix) => prefix+fmt(desired));
        }
      }
    }
  }
  let text = blocks.join('');
  if (verifying && (/Guide \(35m/.test(text) || /flatWidth: 35(?=\r?\n)/.test(text)))
    throw Error('Old 35 m guide remains in ' + relative);
  text = text.replace(/Guide \(35m/g, 'Guide (30m');
  text = text.replace(/flatWidth: 35(?=\r?\n)/g, 'flatWidth: 30');
  text = text.replace(/m_LocalScale: \{x: (?:4000|6000|3925), y: 1, z: 35\}/g,
    m => m.replace('z: 35}', 'z: 30}'));
  if (process.argv.includes('--repair-endpoints') &&
      (relative.includes('avoidMissile') || relative.includes('CarTest'))) {
    text = text.replace(/m_LocalPosition: \{x: -2000, y: (-29\.647|-29\.652), z: (-?16\.499999)\}/g,
      (_, y, z) => `m_LocalPosition: {x: -2004, y: ${y}, z: ${z}}`);
  }
  if (text !== source) changes.set(p, text);
  console.log(relative, `lights=${moved} left=${left} right=${right} heightAboveRoad=${Number.isFinite(minHeightGap) ? minHeightGap.toFixed(3)+'..'+maxHeightGap.toFixed(3) : 'n/a'}m maxOffsetError=${maxError.toFixed(3)}`);
  if (moved && Math.abs(left-right) > 1) throw Error('Unpaired guide lights in ' + relative);
}

const centerlines = {};
for (const [kind, assets] of Object.entries(roadAssets)) {
  const road = (verifying || realigning) ? meshData(assets[0]).vertices : resizeMesh(assets[0], 2);
  if (verifying || realigning) {
    for (let i = 0; i < road.length; i += 2)
      if (Math.abs(Math.hypot(road[i].x-road[i+1].x, road[i].z-road[i+1].z)-30) > .002)
        throw Error('Road width is not 30 m: ' + assets[0]);
  }
  centerlines[kind] = Array.from({length: road.length/2}, (_,i) => ({
    x:(road[i*2].x+road[i*2+1].x)/2,
    y:(road[i*2].y+road[i*2+1].y)/2,
    z:(road[i*2].z+road[i*2+1].z)/2
  }));
  if (!verifying && !realigning) {resizeMesh(assets[1], 4); resizeMesh(assets[2], 4);}
}
for (const [kind, paths] of Object.entries(scenes)) for (const p of paths) adjustScene(p, centerlines[kind], kind);
if (verifying) { console.log('All road widths and guide-light offsets verified.'); process.exit(0); }
console.log(`Validated ${changes.size} files. ${process.argv.includes('--apply') ? 'Applying.' : 'Dry run only.'}`);
if (process.argv.includes('--apply')) for (const [p,text] of changes) fs.writeFileSync(p,text);
