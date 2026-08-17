/*  Generates WebApp/wwwroot/js/island-geometry.js — the single source of truth for
 *  the island playground's board shape and its predefined layouts.
 *
 *      node tools/island-geometry/build.mjs
 *
 *  ── The board is board.svg, beside this script ──────────────────────────────
 *  The outline emitted here is the path in board.svg — same corners, same big organic
 *  lobes and bays. No synthetic profile replaces it.
 *
 *  board.svg is a 60°/120° RHOMBUS: four sides of ~84 units, three of them cut to
 *  interlock and the fourth the ocean coast. Measured off the trace at CORNERS below:
 *
 *      |AB| 84.00   |BC| 84.41   |CD| 84.40 (coast)   |DA| 83.73
 *      angles  A 60.0   B 120.0   C 59.6   D 120.4
 *
 *  Getting the corners right is the whole game. The outline has a pronounced bend part-way
 *  along the coast, and taking that for a corner splits the boundary in the wrong place:
 *  it reports sides of 84 / 84 / 69 / 103 with angles 55.9 / 120 / 70.4 / 113.7, leaves
 *  only two sides able to mate, and makes every join miss a 60° step by ~4°. If the numbers
 *  above ever come out like that again, the corner indices have drifted.
 *
 *  ── Which sides join ────────────────────────────────────────────────────────
 *  Two sides are flush when, laid against each other, their profiles coincide — which needs
 *  each profile to be odd-symmetric about its own midpoint. Departures on the raw trace:
 *
 *      A→B   0.03 units      interlock
 *      B→C   0.53 units      interlock
 *      D→A   0.27 units      interlock
 *      C→D  11.98 units      the coast — deliberately not an interlock
 *
 *  So all three land sides interlock as drawn, in all nine ordered combinations, at slide 0
 *  (corner to corner). The generator's only correction is to put the three onto ONE shared
 *  profile and one shared length, and to snap the quad to an exact rhombus, so the fits are
 *  exact rather than a half-unit out. Every point moves by well under a unit.
 *
 *  Because the rhombus is exactly 60/120, the relative rotation of every join is an exact
 *  multiple of 60°: 180° for a side onto its own kind, and 0/±60/±120/240 for the others.
 *  That is what makes the two 60° rotate buttons sufficient to reach every possible join.
 *
 *  ── The layouts ─────────────────────────────────────────────────────────────
 *  Plain data: layouts.json, beside this script, in the island playground's own save format.
 *  To correct one, build it in the playground, save it, and paste the arrangement in — the
 *  placements and seams come from the same code that loads them back, so what you saw is
 *  what ships.
 *
 *  All 25 have been checked against the printed layouts; the ones that needed rebuilding
 *  carry a note recording what was verified. The answers are held directly rather than
 *  derived — a solver can score a wrong island as a good fit, and the printed layouts are
 *  the only authority anyway.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(HERE, '../..');
const SRC_SVG = path.join(HERE, 'board.svg');
const OUT_JS = path.join(REPO, 'src/SpiritIslandCompanion/WebApp/wwwroot/js/island-geometry.js');
const THUMB_DIR = path.join(REPO, 'src/SpiritIslandCompanion/WebApp/wwwroot/img/layouts/generated');

const LAYOUTS_FILE = path.join(HERE, 'layouts.json');
const MAX_BOARDS = 6;          // one per player at a full table; the extra board stops at five players

// ── tunables ────────────────────────────────────────────────────────────────
const SIDE = 84;            // every side of the rhombus, in board units
const CORNERS = [0, 1990, 3996, 6016];   // A B C D as sample indices into the traced outline
const COAST = 2;            // C→D is the ocean side
const LAND = [0, 1, 3];     // the three interlocking sides
const FLUSH_TOL = 0.35;     // units; below this a contact counts as flush
const MIN_CONTACT = 30;     // units of shared side before a contact is worth offering
const STEP = 0.55;          // outline sampling step, board units
const COAST_BAND = 9.5;     // width of the shallow-water band drawn inside the coast
const SAMPLES = 8000;       // outline samples read from the SVG
const NAME = ['AB', 'BC', 'CD', 'DA'];

// How a board is painted in a picture of an island. Held here rather than at each drawing
// site because there are now two of them — the thumbnails below and the server-side one —
// and a published layout sitting next to a player's own shape must not be a different island.
const ART = { land: '#e2d6b4', edge: '#6b563d', edgeWidth: 1.2, shallow: '#4a90c4', margin: 4 };

// ── read board.svg ──────────────────────────────────────────────────────────
function loadTraced(file, M) {
  const d = fs.readFileSync(file, 'utf8').match(/\sd="([^"]+)"/)[1];
  const nums = d.replace(/([MCZz])/g, ' $1 ').trim().split(/[\s,]+/);
  let i = 0, cmd = null, cur = null, start = null; const segs = []; const rd = () => parseFloat(nums[i++]);
  while (i < nums.length) {
    const t = nums[i];
    if (/^[MCZz]$/.test(t)) { cmd = t; i++; if (cmd === 'Z' || cmd === 'z') { segs.push(['L', cur, start]); continue; } }
    if (cmd === 'M') { const x = rd(), y = rd(); cur = [x, y]; start = cur; cmd = 'L'; }
    else if (cmd === 'C') { const c1 = [rd(), rd()], c2 = [rd(), rd()], p = [rd(), rd()]; segs.push(['C', cur, c1, c2, p]); cur = p; }
    else break;
  }
  const bez = (p0, p1, p2, p3, t) => { const u = 1 - t; return [
    u*u*u*p0[0] + 3*u*u*t*p1[0] + 3*u*t*t*p2[0] + t*t*t*p3[0],
    u*u*u*p0[1] + 3*u*u*t*p1[1] + 3*u*t*t*p2[1] + t*t*t*p3[1]]; };
  const raw = [];
  for (const s of segs) {
    if (s[0] === 'C') for (let k = 0; k < 40; k++) raw.push(bez(s[1], s[2], s[3], s[4], k / 40));
    else { const [, a, b] = s; for (let k = 0; k < 8; k++) raw.push([a[0]+(b[0]-a[0])*k/8, a[1]+(b[1]-a[1])*k/8]); }
  }
  const cum = [0];
  for (let k = 1; k <= raw.length; k++) { const p = raw[k-1], q = raw[k % raw.length]; cum.push(cum[k-1] + Math.hypot(q[0]-p[0], q[1]-p[1])); }
  const total = cum[raw.length], out = [];
  for (let k = 0, j = 0; k < M; k++) {
    const s = total * k / M; while (cum[j+1] < s) j++;
    const f = (s - cum[j]) / (cum[j+1] - cum[j]), p = raw[j], q = raw[(j+1) % raw.length];
    out.push([p[0] + (q[0]-p[0])*f, p[1] + (q[1]-p[1])*f]);
  }
  return out;
}
const traced = loadTraced(SRC_SVG, SAMPLES);
const chordOf = n => {
  const a = traced[CORNERS[n]], b = traced[CORNERS[(n+1) % 4]];
  return { len: Math.hypot(b[0]-a[0], b[1]-a[1]), dir: Math.atan2(b[1]-a[1], b[0]-a[0]) };
};
const RAW = [0,1,2,3].map(n => chordOf(n));
const SCALE = SIDE / (RAW.reduce((a, e) => a + e.len, 0) / 4);
console.log('board.svg as traced:');
console.log('  sides ' + RAW.map((e, n) => `${NAME[n]} ${(e.len*SCALE).toFixed(2)}`).join('   '));

/** Side n of the trace as perp offset (board units) against normalised position along its chord. */
function tracedProfile(n, N = 1400) {
  const k0 = CORNERS[n], k1 = CORNERS[(n+1) % 4];
  const a = traced[k0], b = traced[k1 % SAMPLES];
  const L = Math.hypot(b[0]-a[0], b[1]-a[1]);
  const ux = (b[0]-a[0])/L, uy = (b[1]-a[1])/L;
  const span = ((k1 - k0) + SAMPLES) % SAMPLES, al = [], pe = [];
  for (let s = 0; s <= span; s++) {
    const p = traced[(k0+s) % SAMPLES], dx = p[0]-a[0], dy = p[1]-a[1];
    al.push((dx*ux + dy*uy) / L); pe.push((-dx*uy + dy*ux) * SCALE);
  }
  for (let i = 1; i < al.length; i++) if (al[i] <= al[i-1]) al[i] = al[i-1] + 1e-9;
  const grid = [];
  for (let s = 0; s <= N; s++) {
    const t = s / N;
    let lo = 0, hi = al.length - 1;
    while (hi - lo > 1) { const m = (lo+hi) >> 1; if (al[m] <= t) lo = m; else hi = m; }
    grid.push(pe[lo] + (pe[hi]-pe[lo]) * ((t - al[lo]) / (al[hi] - al[lo] || 1)));
  }
  grid[0] = 0; grid[N] = 0;
  return { N, grid };
}
const TP = [0,1,2,3].map(n => tracedProfile(n));
const sampleT = (p, t) => { const f = Math.max(0, Math.min(p.N, t * p.N)), i = Math.floor(f);
  return p.grid[i] + (p.grid[Math.min(i+1, p.N)] - p.grid[i]) * (f - i); };

console.log('  departure from the odd symmetry a join needs:');
for (let n = 0; n < 4; n++) {
  let mx = 0;
  for (let s = 0; s <= 400; s++) mx = Math.max(mx, Math.abs(sampleT(TP[n], s/400) + sampleT(TP[n], 1 - s/400)) / 2);
  console.log(`    ${NAME[n]}  ${mx.toFixed(2)} units${n === COAST ? '   (the coast)' : ''}`);
}
if (Math.min(...LAND.map(n => RAW[n].len * SCALE)) < SIDE * 0.95)
  throw new Error('a land side is far from ' + SIDE + ' — the corner indices have drifted');

// ── one shared profile for the three interlocking sides ─────────────────────
const N_PROF = TP[0].N;
const mating = [];
for (let s = 0; s <= N_PROF; s++)
  mating.push(LAND.reduce((a, n) => a + sampleT(TP[n], s / N_PROF), 0) / LAND.length);
for (let s = 0; s <= N_PROF / 2; s++) {                     // make it exactly odd
  const o = (mating[s] - mating[N_PROF - s]) / 2;
  mating[s] = o; mating[N_PROF - s] = -o;
}
const matingShift = Math.max(...LAND.map(n => {
  let mx = 0;
  for (let s = 0; s <= N_PROF; s++) mx = Math.max(mx, Math.abs(mating[s] - sampleT(TP[n], s / N_PROF)));
  return mx;
}));
console.log(`  three interlocking sides unified: worst point movement ${matingShift.toFixed(3)} units`);

// ── the corners, snapped to an exact 60/120 rhombus ─────────────────────────
// Sides run at d0, d0+60°, d0+180°, d0+240°; d0 is the circular mean of the four traced
// directions with those offsets removed, so the correction is spread evenly.
const OFFSETS = [0, 60, 180, 240].map(d => d * Math.PI / 180);
const d0 = Math.atan2(
  RAW.reduce((a, e, n) => a + Math.sin(e.dir - OFFSETS[n]), 0),
  RAW.reduce((a, e, n) => a + Math.cos(e.dir - OFFSETS[n]), 0));
const ideal = [[0, 0]];
for (let n = 0; n < 3; n++) {
  const th = d0 + OFFSETS[n];
  ideal.push([ideal[n][0] + SIDE * Math.cos(th), ideal[n][1] + SIDE * Math.sin(th)]);
}
// line the ideal quad up with the traced one
const mean = pts => [pts.reduce((a,p) => a+p[0], 0)/pts.length, pts.reduce((a,p) => a+p[1], 0)/pts.length];
const tracedV = CORNERS.map(k => [traced[k][0]*SCALE, traced[k][1]*SCALE]);
const [mi, mt] = [mean(ideal), mean(tracedV)];
const shifted = ideal.map(p => [p[0] - mi[0] + mt[0], p[1] - mi[1] + mt[1]]);
const drift = Math.max(...shifted.map((p, n) => Math.hypot(p[0]-tracedV[n][0], p[1]-tracedV[n][1])));
console.log(`  corners snapped to an exact 60/120 rhombus: worst corner moved ${drift.toFixed(3)} units`);

const ox = Math.min(...shifted.map(p => p[0])) - 4, oy = Math.min(...shifted.map(p => p[1])) - 4;
const V = shifted.map(p => [+(p[0]-ox).toFixed(6), +(p[1]-oy).toFixed(6)]);
const EDGE_KIND = [0,1,2,3].map(n => n === COAST ? 'coast' : 'mate');
const PROFILE = [0,1,2,3].map(n => n === COAST ? { N: TP[n].N, grid: TP[n].grid } : { N: N_PROF, grid: mating });
const at = (n, x) => sampleT(PROFILE[n], x / SIDE);
const LEN = n => Math.hypot(V[(n+1)%4][0] - V[n][0], V[(n+1)%4][1] - V[n][1]);

// ── outline ─────────────────────────────────────────────────────────────────
const OUTLINE = [], EDGE_START = [0];
for (let n = 0; n < 4; n++) {
  const P = V[n], Q = V[(n+1) % 4], L = LEN(n);
  const ux = (Q[0]-P[0])/L, uy = (Q[1]-P[1])/L;
  const N = Math.max(40, Math.round(L / STEP));
  for (let s = 0; s < N; s++) {
    const x = L * s / N, w = at(n, x);
    OUTLINE.push([P[0] + ux*x - uy*w, P[1] + uy*x + ux*w]);
  }
  EDGE_START.push(OUTLINE.length);
}

// ── measure the contacts this shape supports ────────────────────────────────
function contactError(i, j, s) {
  const Li = LEN(i), Lj = LEN(j);
  const lo = Math.max(0, Li - s - Lj), hi = Math.min(Li, Li - s);
  if (hi - lo < MIN_CONTACT) return null;
  let mx = 0;
  for (let x = lo; x <= hi; x += 0.25) mx = Math.max(mx, Math.abs(at(i, x) + at(j, (Li - s) - x)));
  return { mx, overlap: hi - lo };
}
const CONTACTS = [];
console.log('\ncontacts (host <- guest), and the turn the guest needs:');
for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) {
  const found = [];
  for (let s = -(LEN(j) - MIN_CONTACT); s <= LEN(i) - MIN_CONTACT; s += 0.05) {
    const r = contactError(i, j, s);
    if (r && r.mx < FLUSH_TOL) found.push({ s, ...r });
  }
  const kept = [];
  for (const f of found) {
    const near = kept.find(k => Math.abs(k.s - f.s) < 4);
    if (!near) kept.push(f); else if (f.mx < near.mx) Object.assign(near, f);
  }
  // The sweep lands on its own step grid, which leaves the emitted slide up to half a step
  // off the true optimum — enough to seat a board a thousandth of a unit out. Refine each
  // survivor so the shipped slide is the exact one.
  for (const k of kept) {
    let lo = k.s - 0.06, hi = k.s + 0.06;
    for (let it = 0; it < 40; it++) {
      const a = lo + (hi - lo) / 3, b = hi - (hi - lo) / 3;
      const ra = contactError(i, j, a), rb = contactError(i, j, b);
      if (!ra || !rb) break;
      if (ra.mx <= rb.mx) hi = b; else lo = a;
    }
    const s = (lo + hi) / 2, r = contactError(i, j, s);
    if (r && r.mx <= k.mx) { k.s = s; k.mx = r.mx; k.overlap = r.overlap; }
    // The mating profile is odd by construction and all mating sides are the same length, so
    // the exact slide is exactly 0. Refining each pair on its own leaves them a fraction of a
    // thousandth apart, which does not cancel around a loop of four boards and leaves a
    // 2x2 arrangement very slightly inconsistent. Snap to the analytic value.
    if (Math.abs(k.s) < 0.05) {
      const exact = contactError(i, j, 0);
      if (exact && exact.mx < FLUSH_TOL) { k.s = 0; k.mx = exact.mx; k.overlap = exact.overlap; }
    }
  }
  for (const k of kept) {
    // The guest's rotation relative to the host. Exact by construction: the contacts are
    // measured on the snapped outline, whose edges run at 60° offsets, so every turn is a
    // multiple of 60 — any tracing drift is already reported by the corner-snap line above.
    let turn = (OFFSETS[i] + Math.PI - OFFSETS[j]) * 180 / Math.PI;
    turn = ((turn % 360) + 360) % 360;
    CONTACTS.push({ he: i, ge: j, s: +k.s.toFixed(9), gap: +k.mx.toFixed(6) });
    console.log(`  ${NAME[i]} <- ${NAME[j]}  slide ${k.s.toFixed(2).padStart(7)}  gap ${k.mx.toFixed(4)}`
              + `  overlap ${k.overlap.toFixed(0)}  turn ${turn.toFixed(1)}°`);
  }
}
if (!CONTACTS.length) throw new Error('no flush contacts — check the corner indices');
console.log(`  => ${CONTACTS.length} contacts across sides ${[...new Set(CONTACTS.map(c => c.he))].map(n => NAME[n]).join(', ')}`);

// ── shallow water along the coast ───────────────────────────────────────────
// Emitted as the coast line itself, to be drawn as a thick stroke clipped to the land. The
// obvious alternative — offsetting the coast inwards and filling the ribbon between — is a
// naive polygon offset: around the bays the offset curve crosses itself, and the nonzero
// fill turns those crossings into a blocky staircase along the inner edge. Stroking follows
// the curve exactly, joins round, and cannot self-intersect; the clip trims the outer half.
const COAST_PATHS = [{ start: EDGE_START[COAST], end: EDGE_START[COAST + 1] }].map(({ start, end }) => {
  const pts = [];
  for (let i = start; i <= end; i++) pts.push(OUTLINE[i % OUTLINE.length]);
  return 'M' + pts.map(p => `${p[0].toFixed(2)} ${p[1].toFixed(2)}`).join('L');
});

// ── predefined layouts ──────────────────────────────────────────────────────
const rotp = (p, a) => [p[0]*Math.cos(a) - p[1]*Math.sin(a), p[0]*Math.sin(a) + p[1]*Math.cos(a)];
const xf = (p, Tf) => { const r = rotp(p, Tf.th); return [r[0]+Tf.tx, r[1]+Tf.ty]; };
const bbox = pts => [Math.min(...pts.map(p => p[0])), Math.min(...pts.map(p => p[1])),
                     Math.max(...pts.map(p => p[0])), Math.max(...pts.map(p => p[1]))];
const BB = bbox(OUTLINE);
const CENTER = [+((BB[0]+BB[2])/2).toFixed(4), +((BB[1]+BB[3])/2).toFixed(4)];
const norm = a => { a %= 2*Math.PI; if (a > Math.PI) a -= 2*Math.PI; if (a < -Math.PI) a += 2*Math.PI; return a; };
const HULL = OUTLINE.filter((_, k) => k % 7 === 0);

const layouts = {};

/** A stored {rot,x,y} placement as the affine transform the rest of this file works in. */
const boardTf = b => { const th = b.rot * Math.PI/180, r = rotp(CENTER, th);
  return { th, tx: b.x - r[0], ty: b.y - r[1] }; };

/** Every layout, as a board count and the seams between them. See the header. */
const LAYOUT_DEFS = JSON.parse(fs.readFileSync(LAYOUTS_FILE, 'utf8'));

/** Where a board lands when its side `ge` is laid flush along the host segment P→Q. */
function contactTf(ge, P, Q) {
  const L = Math.hypot(Q[0]-P[0], Q[1]-P[1]);
  const ux = (Q[0]-P[0])/L, uy = (Q[1]-P[1])/L;
  const a = V[ge], b = V[(ge+1)%4];
  const th = Math.atan2(-uy, -ux) - Math.atan2(b[1]-a[1], b[0]-a[0]);
  const r = rotp(a, th);
  return { th, tx: P[0] + ux*L - r[0], ty: P[1] + uy*L - r[1] };
}
/** World endpoints of side k of a placed board. */
const sideOf = (Tf, k) => [xf(V[k], Tf), xf(V[(k+1)%4], Tf)];

/**
 * The island walked out from board 0, seam by seam.
 *
 * Every seam joins two sides corner to corner, so the join fixes one board completely
 * relative to the other and the whole island follows from the graph — which is what an
 * island physically is. Storing positions as well would only add a second description of
 * the same thing, free to disagree with the first.
 */
function placementsFrom(id, def) {
  const adj = Array.from({ length: def.boards }, () => []);
  for (const s of def.seams) {
    adj[s.a].push({ to: s.b, mySide: s.ae, theirSide: s.be });
    // Corner-to-corner contact is symmetric, so a seam is walkable from either end.
    adj[s.b].push({ to: s.a, mySide: s.be, theirSide: s.ae });
  }
  const Ts = new Array(def.boards);
  // How the island as a whole is turned. The seams fix every board relative to its
  // neighbours but say nothing about which way the finished island faces, and that shows —
  // in the layout tile, and in how it lands when dropped into the sea.
  Ts[0] = { th: (def.rot ?? 0) * Math.PI / 180, tx: 0, ty: 0 };
  for (const queue = [0]; queue.length; ) {
    const at = queue.shift();
    for (const e of adj[at]) {
      if (Ts[e.to]) continue;
      const [P, Q] = sideOf(Ts[at], e.mySide);
      Ts[e.to] = contactTf(e.theirSide, P, Q);
      queue.push(e.to);
    }
  }
  const orphan = Ts.findIndex(T => !T);
  if (orphan >= 0)
    throw new Error(`layouts.json: ${id} board ${orphan} is joined to nothing — the island is in pieces`);
  return Ts;
}

/** A layout is taken as given, so it gets no second opinion — anything wrong with it ships.
 *  Refuse the ways it can be wrong that a picture would not show. */
function checkLayout(id, def) {
  const fail = why => { throw new Error(`layouts.json: ${id} ${why}`); };
  if (!Number.isInteger(def?.boards) || def.boards < 1) fail('needs a board count');
  if (def.boards > MAX_BOARDS) fail(`covers ${def.boards} boards; an island holds at most ${MAX_BOARDS}`);
  if (!Array.isArray(def.seams)) fail('has no seams array');
  if (def.boards > 1 && def.seams.length < def.boards - 1)
    fail(`has ${def.seams.length} seams for ${def.boards} boards — too few to join them into one island`);
  const used = new Set();
  for (const s of def.seams) {
    if (!(s.a >= 0 && s.a < def.boards && s.b >= 0 && s.b < def.boards && s.a !== s.b))
      fail(`has a seam between boards that aren't both there (${s.a}–${s.b})`);
    if (!(LAND.includes(s.ae) && LAND.includes(s.be)))
      fail(`has a seam on a side that cannot join (${NAME[s.ae]}–${NAME[s.be]}; ${NAME[COAST]} is the coast)`);
    // One side, one seam: a side laid against two boards at once is not a thing the pieces do.
    for (const k of [s.a * 4 + s.ae, s.b * 4 + s.be]) {
      if (used.has(k)) fail(`joins board ${Math.floor(k / 4)} side ${NAME[k % 4]} to two boards at once`);
      used.add(k);
    }
  }
}

// Every predefined arrangement is turned so its first board sits on an exact 60° step.
//
// The board is a 60°/120° rhombus and every contact it can make is within ~4.1° of a 60° step,
// so a board that has only ever been turned with the playground's 60° rotate control can join
// any island whose own rotation is a multiple of 60 — and can join none of the others. The
// reference table carries the arbitrary angles the diagrams were measured at (Coastline sat at
// 85°), which put every side of those layouts permanently out of reach. Turning the whole
// island is a rigid motion: it changes nothing about which boards touch where.
const TURN = Math.PI / 3;   // 60°, the board's rotational symmetry step
const snapIslandRotation = Ts => {
  if (!Ts.length) return Ts;
  const delta = Math.round(Ts[0].th / TURN) * TURN - Ts[0].th;
  return Ts.map(T => {
    const t = rotp([T.tx, T.ty], delta);
    return { th: T.th + delta, tx: t[0], ty: t[1] };
  });
};
/** How far a board's rotation sits from the nearest 60° step, in degrees. */
const offStep = th => Math.abs(norm(th - Math.round(th / TURN) * TURN)) * 180 / Math.PI;

console.log('\nlayouts (seams / boards-1, worst ° off a 60 step):');
for (const [id, def] of Object.entries(LAYOUT_DEFS)) {
  checkLayout(id, def);
  // Turned so the island sits on an exact 60° step, then centred on its own bounding box.
  // Both are rigid motions: nothing about which boards touch where is changed.
  const Ts = snapIslandRotation(placementsFrom(id, def));
  const [x0, y0, x1, y1] = bbox(Ts.flatMap(Tf => HULL.map(p => xf(p, Tf))));
  const cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
  layouts[id] = {
    // A board only ever sits on a 60° step, so emit the step it is on: one of 0, 60 … 300,
    // rather than the tail of floating-point noise the trigonometry leaves behind (-179.999999)
    // or whichever turn of the circle the sum happened to land on (-240 for 120).
    boards: Ts.map(Tf => { const c = xf(CENTER, Tf);
      return { x: +(c[0]-cx).toFixed(6), y: +(c[1]-cy).toFixed(6),
               rot: ((Math.round(Tf.th * 180/Math.PI / 60) * 60) % 360 + 360) % 360 }; }),
    bonds: def.seams.map(s => ({ ...s, s: 0 })),
  };
  const worstOff = Math.max(...Ts.map(T => offStep(T.th)));
  console.log(`  ${id.padEnd(20)} ${String(def.seams.length).padStart(2)}/${def.boards-1}`
            + `   ${worstOff.toFixed(1)}°` + (worstOff > 6 ? ' << UNREACHABLE by a rotated board' : '')
            + (def.note ? `\n${' '.repeat(24)}${def.note}` : ''));
}

// ── emit ────────────────────────────────────────────────────────────────────
const pathOf = p => 'M' + p.map(q => `${q[0].toFixed(2)} ${q[1].toFixed(2)}`).join('L') + 'Z';
const js = `// GENERATED by tools/island-geometry/build.mjs — do not edit by hand.
//
// board.svg is a 60°/120° rhombus: four ~${SIDE}-unit sides, three cut to interlock and one the
// ocean coast. The outline below is that traced path; the three interlocking sides share one
// profile and the quad is snapped to an exact rhombus (worst point movement
// ${matingShift.toFixed(2)} units) so the fits are exact. See the generator header.

/** One board in its own coordinates. Corners A, B, C, D run clockwise on screen. */
export const BOARD = {
  corners: ${JSON.stringify(V)},
  /** per side A→B, B→C, C→D, D→A. 'coast' cannot join anything. */
  edges: ${JSON.stringify(EDGE_KIND.map((kind, n) => ({ kind, len: +LEN(n).toFixed(4) })))},
  /** rotation reference point in board coordinates */
  center: ${JSON.stringify(CENTER)},
  /** where each side starts in outlinePoints (5 entries; the last is the length) */
  edgeStart: ${JSON.stringify(EDGE_START)},
  outline: ${JSON.stringify(pathOf(OUTLINE))},
  outlinePoints: ${JSON.stringify(OUTLINE.map(p => [+p[0].toFixed(2), +p[1].toFixed(2)]))},
  /** the coast line — stroke it at coastWidth and clip to the outline to get the
   *  shallow-water band (see the generator note on why this is not a filled ribbon) */
  coast: ${JSON.stringify(COAST_PATHS)},
  coastWidth: ${COAST_BAND * 2},
  /** Every join this shape can make, measured off the outline above: guest side \`ge\`
   *  sits flush on host side \`he\` when the guest's start corner is at (Lhost − s) along
   *  the host chord. \`gap\` is the worst separation along the seam, in board units. */
  contacts: ${JSON.stringify(CONTACTS)},
};

/** Official arrangements, expressed as flush contacts so every seam is exact.
 *  x/y is where the board's local centre lands; rot is degrees clockwise.
 *  bonds: { a, ae, b, be, s } — board a's side ae holds board b's side be at slide s. */
export const LAYOUTS = ${JSON.stringify(layouts, null, 1)};
`;
fs.mkdirSync(path.dirname(OUT_JS), { recursive: true });
fs.writeFileSync(OUT_JS, js);
console.log(`\nwrote ${path.relative(REPO, OUT_JS)} (${(js.length/1024).toFixed(1)} kB)`);

// ── layout thumbnails ───────────────────────────────────────────────────────
fs.mkdirSync(THUMB_DIR, { recursive: true });
for (const [id, def] of Object.entries(layouts)) {
  const pts = def.boards.flatMap(b => HULL.map(p => xf(p, boardTf(b))));
  const [x0, y0, x1, y1] = bbox(pts);
  const body = def.boards.map(b => {
    const Tf = boardTf(b);
    const d = 'M' + OUTLINE.map(p => { const q = xf(p, Tf); return `${q[0].toFixed(1)} ${q[1].toFixed(1)}`; }).join('L') + 'Z';
    const r = rotp(CENTER, Tf.th);
    const local = `translate(${(b.x - r[0]).toFixed(2)} ${(b.y - r[1]).toFixed(2)}) rotate(${b.rot.toFixed(3)})`;
    return `<g><path d="${d}" fill="${ART.land}" stroke="${ART.edge}" stroke-width="${ART.edgeWidth}" stroke-linejoin="round"/>` +
           `<g transform="${local}" clip-path="url(#thumbClip)">${COAST_PATHS.map(c => `<path d="${c}" fill="none" stroke="${ART.shallow}" stroke-width="${COAST_BAND*2}" stroke-linejoin="round"/>`).join('')}</g></g>`;
  }).join('');
  fs.writeFileSync(path.join(THUMB_DIR, `${id}.svg`),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${(x0-ART.margin).toFixed(1)} ${(y0-ART.margin).toFixed(1)} ${(x1-x0+ART.margin*2).toFixed(1)} ${(y1-y0+ART.margin*2).toFixed(1)}">` +
    `<title>${id} layout</title>` +
    `<defs><clipPath id="thumbClip" clipPathUnits="userSpaceOnUse">` +
    `<path d="${pathOf(OUTLINE)}"/></clipPath></defs>${body}</svg>\n`);
}
console.log(`wrote ${Object.keys(layouts).length} thumbnails to ${path.relative(REPO, THUMB_DIR)}`);

// ── the board itself, for islands only the server knows ─────────────────────
// A published layout can ship as a picture; a player's own shape cannot — it exists only as
// rows in their library. So the same board goes out a third time, as C# the app draws with,
// and a hand-built island ends up on screen looking like every other layout.
//
// Emitted at every STRIDE-th outline point. The stride has to divide the number of samples a
// side carries: a seam pairs point k of one side with point N-k of the other, so a stride that
// divides N lands on the same places along both halves of a join and the seam stays flush —
// one that doesn't leaves a hairline of sea running down the middle of the island. At 84 units
// to a side, three samples apart is 1.65 units, well under a pixel at the sizes these are
// drawn, and it keeps the markup a third of the size (it is inlined per picture).
// Checked at every corner, not just on the first side: the stride walks the outline end to end,
// so a corner it steps over leaves the side after it sampled out of step with the side before.
const STRIDE = 3;
if (EDGE_START.some(k => k % STRIDE))
  throw new Error(`stride ${STRIDE} does not divide the outline at every corner (${EDGE_START.join(', ')}) — seams would open up`);
const decimate = (from, to) => {
  const pts = [];
  for (let i = from; i <= to; i += STRIDE) pts.push(OUTLINE[i % OUTLINE.length]);
  return 'M' + pts.map(p => `${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join('L');
};
const OUT_CS = path.join(REPO, 'src/SpiritIslandCompanion/WebApp/Components/Shared/IslandBoardArt.g.cs');
const hullPts = HULL.map(p => `(${p[0].toFixed(2)}, ${p[1].toFixed(2)})`);
const hullRows = Array.from({ length: Math.ceil(hullPts.length / 8) },
  (_, r) => '        ' + hullPts.slice(r * 8, r * 8 + 8).join(', ') + ',').join('\n');
const cs = `// GENERATED by tools/island-geometry/build.mjs — do not edit by hand.
//
// The board as it is drawn in a picture of an island, so the app can draw an arrangement it
// has never seen — a shape from a player's own library — exactly as the shipped thumbnails in
// wwwroot/img/layouts/generated draw the published ones. Same trace, same colours, same
// framing; the outline is sampled every ${STRIDE} points, which is invisible at these sizes and a
// third of the markup. See the generator header for the geometry itself.

namespace WebApp.Components.Shared;

/// <summary>One board, in its own coordinates, ready to be stamped into an SVG.</summary>
public static class IslandBoardArt
{
    /// <summary>The board's edge, closed — the land, and the shape the shallows are clipped to.</summary>
    public const string Outline = ${JSON.stringify(decimate(0, OUTLINE.length - STRIDE) + 'Z')};

    /// <summary>The ocean side on its own. Stroked at <see cref="CoastWidth"/> and clipped to
    /// <see cref="Outline"/> it becomes the shallow-water band; see the generator on why this
    /// is a stroked line and not a filled ribbon.</summary>
    public const string Coast = ${JSON.stringify(decimate(EDGE_START[COAST], EDGE_START[COAST + 1]))};

    public const double CoastWidth = ${COAST_BAND * 2};

    /// <summary>The point a board turns about, in board coordinates — a stored placement's
    /// x/y says where this lands.</summary>
    public const double CenterX = ${CENTER[0]};
    public const double CenterY = ${CENTER[1]};

    /// <summary>Every seventh outline point: enough of the edge to frame an island by, without
    /// carrying all ${OUTLINE.length} of them. The shipped thumbnails are framed off the same
    /// points, so a custom island is cropped exactly like a published one.</summary>
    public static readonly (double X, double Y)[] Hull =
    [
${hullRows}
    ];

    /// <summary>Breathing room around the island, in board units.</summary>
    public const double Margin = ${ART.margin};

    public const string Land = "${ART.land}";
    public const string Edge = "${ART.edge}";
    public const double EdgeWidth = ${ART.edgeWidth};
    public const string Shallow = "${ART.shallow}";
}
`;
fs.writeFileSync(OUT_CS, cs);
console.log(`wrote ${path.relative(REPO, OUT_CS)} (${(cs.length/1024).toFixed(1)} kB)`);
