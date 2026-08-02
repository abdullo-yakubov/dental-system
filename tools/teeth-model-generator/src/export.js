// Сборка glTF-документа из процедурной геометрии и запись .glb + meta.json, §6/§9 промта.
import { Document, NodeIO } from '@gltf-transform/core';
import * as THREE from 'three';
import { solveArchAngles } from './arch.js';
import { buildToothGeometry, mirrorGeometry, buildGumGeometry } from './geometry.js';

// Один зуб-инстанс на итоговой модели: квадрант (FDI, 1-4), позиция в квадранте (1..N).
function fdiQuadrants(dentition) {
    // FDI: 1=верх-право,2=верх-лево,3=низ-лево,4=низ-право; молочные квадранты 5-8 той же схемой.
    return dentition === 'permanent'
        ? [{ q: 1, jaw: 'upper', side: -1, mirror: false }, { q: 2, jaw: 'upper', side: 1, mirror: true },
        { q: 3, jaw: 'lower', side: 1, mirror: false }, { q: 4, jaw: 'lower', side: -1, mirror: true }]
        : [{ q: 5, jaw: 'upper', side: -1, mirror: false }, { q: 6, jaw: 'upper', side: 1, mirror: true },
        { q: 7, jaw: 'lower', side: 1, mirror: false }, { q: 8, jaw: 'lower', side: -1, mirror: true }];
}

let seed = 20260731 >>> 0;
function rnd() { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296 - 0.5; }

/**
 * @param {object} spec
 *   dentition: 'permanent'|'deciduous'
 *   upperTeeth, lowerTeeth: массивы записей из anatomy.js (позиции 1..N, одна сторона)
 *   archUpper, archLower: {a,b}
 * @returns {{ document: Document, meta: object, stats: object }}
 */
export function buildDentitionModel(spec) {
    seed = 20260731 >>> 0; // детерминированный сид на каждый прогон, §5.6
    const { dentition, upperTeeth, lowerTeeth, archUpper, archLower } = spec;

    const anglesUpper = solveArchAngles(upperTeeth.map(t => t.md), archUpper);
    const anglesLower = solveArchAngles(lowerTeeth.map(t => t.md), archLower);

    const doc = new Document();
    doc.createBuffer();
    const scene = doc.createScene('Scene');
    const material = doc.createMaterial('DentalEnamel')
        .setBaseColorFactor([1, 1, 1, 1])
        .setRoughnessFactor(0.30)
        .setMetallicFactor(0);

    const gumMaterial = doc.createMaterial('Gingiva')
        .setBaseColorFactor([1, 1, 1, 1])
        .setRoughnessFactor(0.55)
        .setMetallicFactor(0)
        .setDoubleSided(true);

    const metaTeeth = [];
    let totalVerts = 0, totalTris = 0;

    // Геометрия строится один раз на позицию (не на каждый зуб): базовая + зеркальная.
    const geomCache = new Map(); // `${jaw}-${position}` -> { base, mirrored }
    const getGeom = (jaw, o) => {
        const key = `${jaw}-${o.position}`;
        let entry = geomCache.get(key);
        if (!entry) {
            entry = { base: buildToothGeometry(o, jaw) };
            geomCache.set(key, entry);
        }
        return entry;
    };

    for (const { q, jaw, side, mirror } of fdiQuadrants(dentition)) {
        const teeth = jaw === 'upper' ? upperTeeth : lowerTeeth;
        const angles = jaw === 'upper' ? anglesUpper : anglesLower;
        const arch = jaw === 'upper' ? archUpper : archLower;

        for (let i = 0; i < teeth.length; i++) {
            const o = teeth[i];
            const th = angles[i];
            const spee = 0.022 * i * i; // кривая Шпее
            const over = (jaw === 'upper' && o.position <= 3) ? 1.1 : 0; // перекрытие фронтальных верхних

            const entry = getGeom(jaw, o);
            let geo = entry.base;
            if (mirror) {
                if (!entry.mirrored) entry.mirrored = mirrorGeometry(entry.base);
                geo = entry.mirrored;
            }

            const x = side * arch.a * Math.sin(th) + rnd() * 0.25;
            const y = (jaw === 'upper' ? o.ch - over : -o.ch) + spee + rnd() * 0.15;
            const z = -arch.b * Math.cos(th) + rnd() * 0.25;
            const rotX = (jaw === 'upper' ? -o.tilt : o.tilt) + rnd() * 0.03;
            const rotY = Math.atan2(side * Math.sin(th), -Math.cos(th)) + rnd() * 0.03;
            const rotZ = (jaw === 'upper' ? Math.PI : 0) + rnd() * 0.03;

            const euler = new THREE.Euler(rotX, rotY, rotZ, 'YXZ');
            const quat = new THREE.Quaternion().setFromEuler(euler);

            const fdi = q * 10 + o.position;
            const meshName = `tooth_${fdi}`;

            const posArr = geo.attributes.position.array;
            const norArr = geo.attributes.normal.array;
            const colArr = geo.attributes.color.array;
            const idxArr = geo.index.array;

            const posAcc = doc.createAccessor().setType('VEC3').setArray(new Float32Array(posArr));
            const norAcc = doc.createAccessor().setType('VEC3').setArray(new Float32Array(norArr));
            const colAcc = doc.createAccessor().setType('VEC3').setArray(new Float32Array(colArr));
            const idxAcc = doc.createAccessor().setType('SCALAR')
                .setArray(posArr.length / 3 > 65535 ? new Uint32Array(idxArr) : new Uint16Array(idxArr));

            const prim = doc.createPrimitive()
                .setAttribute('POSITION', posAcc)
                .setAttribute('NORMAL', norAcc)
                .setAttribute('COLOR_0', colAcc)
                .setIndices(idxAcc)
                .setMaterial(material);
            const mesh = doc.createMesh(meshName).addPrimitive(prim);
            const node = doc.createNode(meshName)
                .setMesh(mesh)
                .setTranslation([x, y, z])
                .setRotation([quat.x, quat.y, quat.z, quat.w]);
            scene.addChild(node);

            totalVerts += posArr.length / 3;
            totalTris += idxArr.length / 3;

            const bb = geo.boundingBox;
            metaTeeth.push({
                fdi,
                quadrant: q,
                position: o.position,
                jaw,
                mesh: meshName,
                mesialAxis: mirror ? '-X' : '+X',
                type: o.type,
                surfaces: surfacesFor(o),
                localBounds: {
                    min: [bb.min.x, bb.min.y, bb.min.z],
                    max: [bb.max.x, bb.max.y, bb.max.z]
                }
            });
        }
    }

    // Десна: строится по одной стороне квадранта дуги (не зеркалится — профиль симметричен
    // относительно средней линии по построению) для верхней и нижней челюсти.
    for (const jaw of ['upper', 'lower']) {
        const teeth = jaw === 'upper' ? upperTeeth : lowerTeeth;
        const angles = jaw === 'upper' ? anglesUpper : anglesLower;
        const arch = jaw === 'upper' ? archUpper : archLower;
        const geo = buildGumGeometry(teeth, angles, arch, jaw);

        const posArr = geo.attributes.position.array;
        const norArr = geo.attributes.normal.array;
        const colArr = geo.attributes.color.array;
        const idxArr = geo.index.array;

        const posAcc = doc.createAccessor().setType('VEC3').setArray(new Float32Array(posArr));
        const norAcc = doc.createAccessor().setType('VEC3').setArray(new Float32Array(norArr));
        const colAcc = doc.createAccessor().setType('VEC3').setArray(new Float32Array(colArr));
        const idxAcc = doc.createAccessor().setType('SCALAR')
            .setArray(posArr.length / 3 > 65535 ? new Uint32Array(idxArr) : new Uint16Array(idxArr));

        const prim = doc.createPrimitive()
            .setAttribute('POSITION', posAcc)
            .setAttribute('NORMAL', norAcc)
            .setAttribute('COLOR_0', colAcc)
            .setIndices(idxAcc)
            .setMaterial(gumMaterial);
        const meshName = jaw === 'upper' ? 'gum_upper' : 'gum_lower';
        const mesh = doc.createMesh(meshName).addPrimitive(prim);
        const node = doc.createNode(meshName).setMesh(mesh);
        scene.addChild(node);

        totalVerts += posArr.length / 3;
        totalTris += idxArr.length / 3;
    }

    const meta = {
        units: 'mm',
        dentition,
        generatedAt: new Date().toISOString(),
        generatorVersion: '1.0.0',
        teeth: metaTeeth
    };

    const archAnglesDegrees = {
        upper: anglesUpper.map(r => r * 180 / Math.PI),
        lower: anglesLower.map(r => r * 180 / Math.PI)
    };

    return { document: doc, meta, stats: { totalVerts, totalTris }, archAnglesDegrees };
}

function surfacesFor(o) {
    return o.position <= 3 ? ['M', 'D', 'I', 'B', 'L'] : ['M', 'D', 'O', 'B', 'L'];
}

export async function writeGlb(document, outPath) {
    const io = new NodeIO();
    await io.write(outPath, document);
}
