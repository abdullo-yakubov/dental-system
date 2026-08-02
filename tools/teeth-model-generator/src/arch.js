// Решатель углов установки зубов по дуге, §5.5 промта на 3D-одонтограмму.
// Угол первого зуба выбирается так, чтобы центральные резцы касались по средней линии;
// каждый следующий — минимальным углом, дающим хорду между центрами не меньше
// полусуммы md соседей + 0.15 мм зазора.

function archPoint(theta, a, b) {
    return [a * Math.sin(theta), -b * Math.cos(theta)];
}

function chordLength(t1, t2, a, b) {
    const [x1, z1] = archPoint(t1, a, b);
    const [x2, z2] = archPoint(t2, a, b);
    return Math.hypot(x2 - x1, z2 - z1);
}

/**
 * @param {number[]} mdSizes мезио-дистальные размеры зубов квадранта, 1..8 (или 1..5 для молочных)
 * @param {{a:number,b:number}} arch полуоси дуги (мм)
 * @returns {number[]} углы установки в радианах, той же длины, что mdSizes
 */
export function solveArchAngles(mdSizes, arch) {
    const { a, b } = arch;
    const n = mdSizes.length;
    const angles = new Array(n);

    const t1Ratio = mdSizes[0] * 1.02 / (2 * a);
    angles[0] = Math.asin(Math.min(1, Math.max(-1, t1Ratio)));

    for (let i = 1; i < n; i++) {
        const dMin = (mdSizes[i - 1] + mdSizes[i]) / 2 + 0.15;
        let lo = angles[i - 1];
        let hi = angles[i - 1] + 0.05;
        // Хорда монотонно растёт с увеличением угла в рабочем диапазоне дуги —
        // расширяем hi, пока не гарантированно перекроем требуемую хорду.
        let guard = 0;
        while (chordLength(angles[i - 1], hi, a, b) < dMin && guard < 400) {
            hi += 0.02;
            guard++;
        }
        for (let iter = 0; iter < 100; iter++) {
            const mid = (lo + hi) / 2;
            if (chordLength(angles[i - 1], mid, a, b) < dMin) lo = mid;
            else hi = mid;
        }
        angles[i] = hi;
    }

    return angles;
}

export function solveArchAnglesDegrees(mdSizes, arch) {
    return solveArchAngles(mdSizes, arch).map(rad => rad * 180 / Math.PI);
}

export { archPoint, chordLength };
