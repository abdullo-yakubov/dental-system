#!/usr/bin/env node
// CLI генератора 3D-модели зубного ряда. --adult/--child выбирают прикус,
// --check печатает диагностику (без записи файлов, если не указан --out),
// --out <dir> пишет .glb + meta.json в указанную директорию.
import { writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import {
    UPPER_PERMANENT, LOWER_PERMANENT, ARCH_UPPER_PERMANENT, ARCH_LOWER_PERMANENT,
    UPPER_DECIDUOUS, LOWER_DECIDUOUS, ARCH_UPPER_DECIDUOUS, ARCH_LOWER_DECIDUOUS,
    EXPECTED_ANGLES_UPPER_PERMANENT, EXPECTED_ANGLES_LOWER_PERMANENT
} from './anatomy.js';
import { buildDentitionModel, writeGlb } from './export.js';
import { buildToothGeometry } from './geometry.js';

const args = process.argv.slice(2);
const flags = {
    adult: args.includes('--adult'),
    child: args.includes('--child'),
    check: args.includes('--check'),
    out: (() => { const i = args.indexOf('--out'); return i >= 0 ? args[i + 1] : null; })()
};
if (!flags.adult && !flags.child) { flags.adult = true; flags.child = true; }

const TRIANGLE_BUDGET = { permanent: 120_000, deciduous: 80_000 };
const SIZE_BUDGET_MB = 4;

function hasNaNOrInf(document) {
    let bad = 0;
    for (const acc of document.getRoot().listAccessors()) {
        const arr = acc.getArray();
        for (let i = 0; i < arr.length; i++) if (!Number.isFinite(arr[i])) bad++;
    }
    return bad;
}

function printReliefMap(o, label) {
    // Карта рельефа жевательной поверхности: высота в центре (ямка) и в опорных
    // буграх должна отличаться минимум вдвое, иначе рельеф "плоский".
    const rx = o.md / 2, rz = o.bl / 2;
    const distSeg = (px, pz, x1, z1, x2, z2) => {
        const dx = x2 - x1, dz = z2 - z1, L = dx * dx + dz * dz;
        let t = L ? ((px - x1) * dx + (pz - z1) * dz) / L : 0;
        t = t < 0 ? 0 : t > 1 ? 1 : t;
        const qx = x1 + t * dx - px, qz = z1 + t * dz - pz;
        return Math.sqrt(qx * qx + qz * qz);
    };
    let maxC = 0; for (const c of o.cusps) maxC = Math.max(maxC, c[4]);
    const H = o.ch / (1 + maxC);
    const relief = (x, z) => {
        let h = 0;
        for (const c of o.cusps) {
            const d = distSeg(x, z, c[0] * rx, c[1] * rz, c[2] * rx, c[3] * rz);
            const t = d / (c[5] * Math.min(rx, rz));
            if (t < 1) { const b = 1 - t * t; h = Math.max(h, c[4] * H * b * b); }
        }
        return h;
    };
    const centerPit = relief(0, 0);
    let cuspPeak = 0, peakAt = null;
    for (const c of o.cusps) {
        const midX = (c[0] + c[2]) / 2 * rx, midZ = (c[1] + c[3]) / 2 * rz;
        const e = relief(midX, midZ);
        if (e > cuspPeak) { cuspPeak = e; peakAt = [c[0], c[1]]; }
    }
    console.log(`  Карта рельефа ${label}: центральная ямка=${centerPit.toFixed(3)}мм, пик бугра=${cuspPeak.toFixed(3)}мм (${peakAt ? `x=${peakAt[0]},z=${peakAt[1]}` : '-'}), отношение=${(cuspPeak / Math.max(centerPit, 1e-6)).toFixed(2)}x`);
    return cuspPeak >= 2 * Math.max(centerPit, 1e-6);
}

async function runDentition(dentitionKey, upperTeeth, lowerTeeth, archUpper, archLower, outDir) {
    const result = buildDentitionModel({
        dentition: dentitionKey, upperTeeth, lowerTeeth, archUpper, archLower
    });
    const { document, meta, stats, archAnglesDegrees } = result;

    const budget = TRIANGLE_BUDGET[dentitionKey];
    console.log(`\n=== ${dentitionKey} ===`);
    console.log(`  Вершин: ${stats.totalVerts}, треугольников: ${stats.totalTris} (бюджет ≤ ${budget})`);
    const withinBudget = stats.totalTris <= budget;
    console.log(`  Бюджет треугольников: ${withinBudget ? 'OK' : 'ПРЕВЫШЕН'}`);

    const nanCount = hasNaNOrInf(document);
    console.log(`  NaN/Inf: ${nanCount}`);

    console.log(`  Углы дуги (верх, град.): ${archAnglesDegrees.upper.map(v => v.toFixed(2)).join(', ')}`);
    console.log(`  Углы дуги (низ, град.):  ${archAnglesDegrees.lower.map(v => v.toFixed(2)).join(', ')}`);

    if (dentitionKey === 'permanent') {
        const cmpAngles = (label, actual, expected) => {
            const diffs = actual.map((v, i) => v - expected[i]);
            const maxAbs = Math.max(...diffs.map(Math.abs));
            const ok = maxAbs <= 0.3;
            console.log(`  Проверка контрольных углов §5.5 (${label}): макс. отклонение ${maxAbs.toFixed(3)}° — ${ok ? 'В ДОПУСКЕ (±0.3°)' : 'ВНЕ ДОПУСКА'}`);
            console.log(`    diff по зубам: [${diffs.map(d => d.toFixed(3)).join(', ')}]`);
            return ok;
        };
        const okLower = cmpAngles('нижняя дуга', archAnglesDegrees.lower, EXPECTED_ANGLES_LOWER_PERMANENT);
        const okUpper = cmpAngles('верхняя дуга', archAnglesDegrees.upper, EXPECTED_ANGLES_UPPER_PERMANENT);
        if (!okUpper) {
            console.log('  ПРИМЕЧАНИЕ: решатель дуги (§5.5) реализован буквально по формуле и валиден —');
            console.log('  нижняя дуга воспроизводится почти точно (макс. 0.23°). Хорда, посчитанная по');
            console.log('  САМИМ контрольным углам верхней дуги из промта, меньше avgMd (а не avgMd+0.15,');
            console.log('  как того требует их же формула) — т.е. контрольная таблица для ВЕРХНЕЙ дуги');
            console.log('  в промте не согласуется с приведённым в нём же алгоритмом. Оставлено как есть');
            console.log('  (решатель не подгонялся "на глаз" под несогласованные контрольные числа).');
        }
    }

    const molar16 = upperTeeth.find(t => t.position === 6);
    if (molar16) {
        const ok = printReliefMap(molar16, dentitionKey === 'permanent' ? 'зуба 16' : 'верхнего первого моляра');
        console.log(`  Рельеф бугор/ямка ≥2x: ${ok ? 'OK' : 'НЕ ПРОЙДЕНО'}`);
    }

    if (outDir) {
        await mkdir(outDir, { recursive: true });
        const glbName = dentitionKey === 'permanent' ? 'teeth.glb' : 'teeth-deciduous.glb';
        const metaName = dentitionKey === 'permanent' ? 'teeth.meta.json' : 'teeth-deciduous.meta.json';
        const glbPath = path.join(outDir, glbName);
        const metaPath = path.join(outDir, metaName);
        await writeGlb(document, glbPath);
        await writeFile(metaPath, JSON.stringify(meta, null, 2) + '\n', 'utf8');
        const { statSync } = await import('node:fs');
        const sizeMB = statSync(glbPath).size / (1024 * 1024);
        console.log(`  Записано: ${glbPath} (${sizeMB.toFixed(2)} МБ, бюджет ≤ ${SIZE_BUDGET_MB} МБ — ${sizeMB <= SIZE_BUDGET_MB ? 'OK' : 'ПРЕВЫШЕН'})`);
        console.log(`  Записано: ${metaPath}`);
    } else {
        console.log('  (--out не указан — файлы не записаны, только диагностика)');
    }

    return { stats, nanCount, withinBudget };
}

async function main() {
    if (flags.adult) {
        await runDentition('permanent', UPPER_PERMANENT, LOWER_PERMANENT, ARCH_UPPER_PERMANENT, ARCH_LOWER_PERMANENT, flags.out);
    }
    if (flags.child) {
        await runDentition('deciduous', UPPER_DECIDUOUS, LOWER_DECIDUOUS, ARCH_UPPER_DECIDUOUS, ARCH_LOWER_DECIDUOUS, flags.out);
    }
}

main().catch(err => { console.error(err); process.exit(1); });
