// Построение меша зуба и десны по алгоритму §5.1-5.7 промта на 3D-одонтограмму.
// three.js используется только для сборки BufferGeometry/расчёта нормалей —
// в остальном это чистая математика без WebGL/DOM.
import * as THREE from 'three';
import { rootTips } from './anatomy.js';

// Разрешение сетки. Подобрано так, чтобы 32 постоянных зуба + 2 десны укладывались
// в бюджет ≤120000 треугольников / ≤4МБ (см. index.js --check).
export const SEG = 36;           // сегментов по окружности сечения
const RJ_SINGLE = 10;            // колец одиночного корня
const RJ_MULTI = 6;              // колец ствола многокорневого зуба
const ROOT_CONE_RINGS = 7;       // колец на отдельный корень многокорневого зуба
const CJ = 11;                   // колец коронки (шейка -> площадка)
const KJ = 6;                    // колец жевательной площадки (скаты к центру)
const GUM_SECTIONS = 140;        // сечений дёсенного профиля вдоль дуги

const sgnPow = (v, k) => (v < 0 ? -1 : 1) * Math.pow(Math.abs(v), k);
const mix = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
// glTF трактует атрибут COLOR_0 как линейный цвет (spec: "linear space"), а Three.js
// GLTFLoader не конвертирует его в рабочее цветовое пространство — поэтому исходные
// sRGB hex-значения переводятся в linear здесь же, при экспорте, чтобы модель выглядела
// так, как задумано в §5.4/§5.7, а не светлее из-за пропущенной гамма-коррекции.
const srgbToLinear = c => c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
const hex = h => [srgbToLinear(((h >> 16) & 255) / 255), srgbToLinear(((h >> 8) & 255) / 255), srgbToLinear((h & 255) / 255)];

// §5.4: послойный цвет как в реставрационной керамике.
const C_NECK = hex(0xE6D4AD), C_BODY = hex(0xF3EEE1), C_EDGE = hex(0xD6E0E1);
const C_ROOT = hex(0xD9CBAF), C_APEX = hex(0xB4A387);

function crownColor(u) {
    // u — доля высоты коронки (0 у шейки, 1 у режущего края/площадки), деление на трети.
    if (u < 1 / 3) return mix(C_NECK, C_BODY, u / (1 / 3));
    if (u < 2 / 3) return C_BODY;
    return mix(C_BODY, C_EDGE, (u - 2 / 3) / (1 / 3));
}
function rootColor(v) {
    // v — доля длины корня от шейки (0) до верхушки (1).
    return mix(C_ROOT, C_APEX, Math.min(1, v));
}

// Расстояние от точки до отрезка — основа рельефа жевательной поверхности, §5.2.
function distSeg(px, pz, x1, z1, x2, z2) {
    const dx = x2 - x1, dz = z2 - z1, L = dx * dx + dz * dz;
    let t = L ? ((px - x1) * dx + (pz - z1) * dz) / L : 0;
    t = t < 0 ? 0 : t > 1 ? 1 : t;
    const qx = x1 + t * dx - px, qz = z1 + t * dz - pz;
    return Math.sqrt(qx * qx + qz * qz);
}

function skin(rings, tipA, tipB, pos, col, idx) {
    const n = rings[0].length, base = pos.length / 3;
    for (const ring of rings) for (const p of ring) {
        pos.push(p[0], p[1], p[2]);
        col.push(p[3][0], p[3][1], p[3][2]);
    }
    for (let r = 0; r < rings.length - 1; r++) for (let i = 0; i < n; i++) {
        const j = (i + 1) % n;
        const a = base + r * n + i, b = base + r * n + j, c = base + (r + 1) * n + j, d = base + (r + 1) * n + i;
        idx.push(a, b, c, a, c, d);
    }
    if (tipA) {
        const t = pos.length / 3;
        pos.push(tipA[0], tipA[1], tipA[2]); col.push(tipA[3][0], tipA[3][1], tipA[3][2]);
        for (let i = 0; i < n; i++) idx.push(t, base + ((i + 1) % n), base + i);
    }
    if (tipB) {
        const first = base + (rings.length - 1) * n, t = pos.length / 3;
        pos.push(tipB[0], tipB[1], tipB[2]); col.push(tipB[3][0], tipB[3][1], tipB[3][2]);
        for (let i = 0; i < n; i++) idx.push(first + i, first + ((i + 1) % n), t);
    }
}

/**
 * Тело зуба (корень+коронка) в локальных координатах: +X мезиально, +Z вестибулярно,
 * +Y к режущему краю/площадке. Начало координат — центр окклюзионной/шеечной границы.
 */
function toothBody(o, pos, col, idx) {
    const rx = o.md / 2, rz = o.bl / 2, k = 2 / o.pw;
    const single = o.roots === 1;
    const rootLen = single ? o.rl : o.rl * 0.34;
    let maxC = 0; for (const c of o.cusps) maxC = Math.max(maxC, c[4]);
    const H = o.ch / (1 + maxC);
    const maxH = maxC * H;

    const relief = (x, z) => {
        let h = 0;
        for (const c of o.cusps) {
            const d = distSeg(x, z, c[0] * rx, c[1] * rz, c[2] * rx, c[3] * rz);
            const t = d / (c[5] * Math.min(rx, rz));
            if (t < 1) { const b = 1 - t * t; h = Math.max(h, c[4] * H * b * b); }
        }
        return h;
    };

    const ring = (y, sx, sz, elev, micro, colorFn) => {
        const out = [];
        for (let i = 0; i < SEG; i++) {
            const th = (i / SEG) * Math.PI * 2;
            let mx = sx, mz = sz;
            if (micro) {
                const m = 1 + 0.007 * Math.sin(y * 7.5 + th * 3) + 0.005 * Math.cos(th * 11); // перикиматии
                mx *= m; mz *= m;
            } else {
                const m = 1 + 0.035 * Math.cos(2 * th); // продольное уплощение корня
                mx *= m; mz *= m;
            }
            const x = rx * mx * sgnPow(Math.sin(th), k), z = rz * mz * sgnPow(Math.cos(th), k);
            let yy = y, dark = 1;
            if (elev > 0) {
                const e = relief(x, z);
                yy += e * elev;
                dark = 0.74 + 0.26 * Math.min(1, e / (maxH * 0.62)); // дно фиссур темнее
            }
            const c = colorFn(yy, dark);
            out.push([x, yy, z, c]);
        }
        return out;
    };

    const rootColorFn = (y) => rootColor(-y / rootLen);
    const crownColorFn = (y, dark) => {
        const c = crownColor(Math.min(1, y / H));
        return [c[0] * dark, c[1] * dark, c[2] * dark];
    };

    const rings = [];
    const RJ = single ? RJ_SINGLE : RJ_MULTI;
    for (let j = 0; j <= RJ; j++) {
        const v = 1 - j / RJ;
        let s = single ? 0.88 * (1 - 0.95 * Math.pow(v, 1.3))
            : j === 0 ? 0.12 : j === 1 ? 0.46 : 0.88 * (1 - 0.52 * Math.pow(v, 1.6));
        const r = ring(-rootLen * v, s, s, 0, false, rootColorFn);
        if (single) { const bend = 0.11 * o.rl * Math.pow(v, 2.1); for (const p of r) p[0] += bend; }
        rings.push(r);
    }
    for (let j = 1; j <= CJ; j++) {
        const u = j / CJ;
        const w = 0.86 + 0.62 * u - 0.68 * u * u;
        const ramp = u < 0.78 ? 0 : Math.pow((u - 0.78) / 0.22, 2);
        rings.push(ring(H * u, w, w * (1 - o.squash * u * u), ramp, true, crownColorFn));
    }
    for (let j = 1; j < KJ; j++) {
        const rr = 1 - j / KJ, w = 0.80 * (1 - 0.99 * Math.pow(1 - rr, 1.5));
        rings.push(ring(H, w, w * (1 - o.squash), 1, true, crownColorFn));
    }

    const apex = [single ? 0.11 * o.rl : 0, -rootLen * 1.03, 0, rootColor(1)];
    const tip = [0, H + relief(0, 0), 0, crownColorFn(H + relief(0, 0), 0.74)];
    skin(rings, apex, tip, pos, col, idx);
}

function rootCone(len, r0, tipX, tipZ, yTop, pos, col, idx) {
    const rings = [];
    for (let j = 0; j <= ROOT_CONE_RINGS; j++) {
        const v = j / ROOT_CONE_RINGS, cx = tipX * Math.pow(v, 1.5), cz = tipZ * Math.pow(v, 1.5);
        const r = r0 * (1 - 0.90 * Math.pow(v, 1.1)), out = [];
        for (let i = 0; i < SEG; i++) {
            const th = (i / SEG) * Math.PI * 2, m = 1 + 0.05 * Math.cos(2 * th + 1);
            const y = yTop - len * v;
            out.push([cx + r * m * Math.sin(th), y, cz + r * m * Math.cos(th), rootColor(Math.min(1, -y / (len + Math.abs(yTop))))]);
        }
        rings.unshift(out);
    }
    const tip = [tipX, yTop - len * 1.04, tipZ, rootColor(1)];
    skin(rings, tip, null, pos, col, idx);
}

/**
 * Строит геометрию одного зуба (корень(и) + коронка) в локальных координатах.
 * @param {object} o запись из anatomy.js (md,bl,ch,rl,roots,pw,squash,cusps,...)
 * @param {'upper'|'lower'} jaw
 */
export function buildToothGeometry(o, jaw) {
    const pos = [], col = [], idx = [];
    toothBody(o, pos, col, idx);
    const tips = rootTips(o, jaw);
    if (tips) {
        const r0 = 0.30 * Math.min(o.md, o.bl);
        for (const t of tips) rootCone(o.rl * 0.80, r0, t[0], t[1], -o.rl * 0.20, pos, col, idx);
    }
    return finalize(pos, col, idx);
}

function finalize(pos, col, idx) {
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
    geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
    geo.setIndex(idx);
    geo.computeVertexNormals();
    geo.computeBoundingBox();
    return geo;
}

/** Зеркальная копия по X — распределение мезиальной/дистальной стороны на другую половину дуги, §5.6. */
export function mirrorGeometry(geo) {
    const m = geo.clone();
    const p = m.attributes.position.array, n = m.attributes.normal.array;
    for (let i = 0; i < p.length; i += 3) { p[i] = -p[i]; n[i] = -n[i]; }
    const ix = m.index.array;
    for (let i = 0; i < ix.length; i += 3) { const t = ix[i + 1]; ix[i + 1] = ix[i + 2]; ix[i + 2] = t; }
    m.attributes.position.needsUpdate = true;
    m.attributes.normal.needsUpdate = true;
    m.computeBoundingBox();
    return m;
}

/* ---------------- десна и альвеолярный гребень, §5.7 ---------------- */
const C_MARGIN = hex(0xD99C96), C_GUM = hex(0xC4736F), C_BONE = hex(0xE0D3BA);

/**
 * @param {object[]} teeth 8 (или 5) записей квадранта в порядке позиций 1..N
 * @param {number[]} angles углы установки (рад), той же длины, что teeth
 * @param {{a:number,b:number}} arch
 * @param {'upper'|'lower'} jaw
 */
export function buildGumGeometry(teeth, angles, arch, jaw) {
    const g = jaw === 'upper' ? 1 : -1;
    const { a, b } = arch;
    const angDeg = angles.map(r => r * 180 / Math.PI);
    const maxAngDeg = angDeg[angDeg.length - 1];

    const near = (tAbs) => {
        if (tAbs <= angDeg[0]) return { bl: teeth[0].bl, ch: teeth[0].ch, d: 1 - tAbs / angDeg[0], k: 0 };
        if (tAbs >= maxAngDeg) return { bl: teeth[teeth.length - 1].bl, ch: teeth[teeth.length - 1].ch, d: Math.min(1, (tAbs - maxAngDeg) / 7), k: teeth.length - 1 };
        let i = 0; while (i < angDeg.length - 2 && tAbs > angDeg[i + 1]) i++;
        const f = (tAbs - angDeg[i]) / (angDeg[i + 1] - angDeg[i]);
        return {
            bl: teeth[i].bl + (teeth[i + 1].bl - teeth[i].bl) * f,
            ch: teeth[i].ch + (teeth[i + 1].ch - teeth[i].ch) * f,
            d: 1 - Math.abs(f - 0.5) * 2, k: i
        };
    };

    const t0 = -(maxAngDeg + 14), t1 = maxAngDeg + 14;
    const rings = [], pos = [], col = [], idx = [];
    for (let s = 0; s <= GUM_SECTIONS; s++) {
        const t = t0 + (t1 - t0) * s / GUM_SECTIONS, r = t * Math.PI / 180;
        const cx = a * Math.sin(r), cz = -b * Math.cos(r);
        const nx = b * Math.sin(r), nz = -a * Math.cos(r);
        const L = Math.hypot(nx, nz) || 1, ux = nx / L, uz = nz / L;
        const p = near(Math.abs(t));
        const pap = (p.k < 3 ? 2.3 : 1.3) * Math.pow(p.d, 1.7); // сосочек между зубами
        const yM = g * (p.ch - 0.7) - g * pap;
        const wIn = 0.40 * p.bl, wOut = 0.44 * p.bl + 1.5; // наружная/внутренняя кромка следуют bl
        const prof = [[wIn, 0], [wOut, 1.1], [wOut + 0.6, 4.5], [wOut - 0.6, 13.5], [wOut * 0.55, 17], [0, 18],
        [-wOut * 0.55, 17], [-wOut + 0.6, 13.5], [-wOut - 0.4, 4.5], [-wOut, 1.1], [-wIn, 0]];
        const ring = prof.map(q => {
            const y = yM + g * q[1], d = q[1];
            let c = d < 1.4 ? mix(C_MARGIN, C_GUM, d / 1.4)
                : d < 7 ? C_GUM
                    : mix(C_GUM, C_BONE, Math.min(1, (d - 7) / 6));
            return [cx + ux * q[0], y, cz + uz * q[0], c];
        });
        rings.push(ring);
    }
    const mid = r => {
        let x = 0, y = 0, z = 0; for (const p of r) { x += p[0]; y += p[1]; z += p[2]; }
        const n = r.length; return [x / n, y / n, z / n, C_BONE];
    };
    skin(rings, mid(rings[0]), mid(rings[rings.length - 1]), pos, col, idx);
    return finalize(pos, col, idx);
}
