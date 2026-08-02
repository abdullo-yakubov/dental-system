// 3D-одонтограмма (разделы 8/17.3/19.7/21.5 ТЗ). Геометрия зубов и десны НЕ строится в
// браузере: она процедурно сгенерирована один раз офлайн инструментом
// tools/teeth-model-generator и закоммичена как wwwroot/models/teeth[-deciduous].glb —
// этот модуль только грузит .glb через GLTFLoader и рисует сцену. Three.js и его аддоны
// (GLTFLoader/OrbitControls/BufferGeometryUtils) вендорены в wwwroot/lib/three — без CDN,
// приложение работает полностью офлайн.
//
// Примечание по интеграции: §9 промта описывает import map (`<script type="importmap">`)
// для подключения "three"/"three/addons/". В этом проекте единственный существующий тег
// <script type="importmap"> в index.html зарезервирован тулчейном Blazor WebAssembly для
// его собственной карты модулей рантайма — переопределять его контент рискованно (можно
// сломать загрузку WASM), а второй <script type="importmap"> надёжно поддерживается не
// всеми браузерами. Поэтому вендорные аддоны используют относительные ES-импорты
// (`../../three.module.js`) вместо bare-спецификатора "three" — функционально это даёт
// тот же результат (полностью локальные модули, без сети), просто без import map.
window.dentalOdontogram3d = (function () {
    let THREE, GLTFLoader, OrbitControls;
    const scenes = new Map(); // canvasId -> viewer state

    async function loadThree() {
        if (THREE) return true;
        try {
            THREE = await import('../lib/three/three.module.js');
            ({ GLTFLoader } = await import('../lib/three/addons/loaders/GLTFLoader.js'));
            ({ OrbitControls } = await import('../lib/three/addons/controls/OrbitControls.js'));
            return true;
        } catch (e) {
            console.error('Не удалось загрузить three.js', e);
            return false;
        }
    }

    function webglAvailable() {
        try {
            const canvas = document.createElement('canvas');
            return !!(window.WebGLRenderingContext &&
                (canvas.getContext('webgl2') || canvas.getContext('webgl')));
        } catch (e) {
            return false;
        }
    }

    /* ---------------- модель: грузится один раз на прикус, кэшируется ---------------- */
    const modelCache = new Map(); // 'permanent'|'deciduous' -> Promise<{ scene, meta, toothCount }>

    function loadModel(dentition, modelUrl, metaUrl) {
        const key = dentition;
        if (modelCache.has(key)) return modelCache.get(key);

        const promise = (async () => {
            const loader = new GLTFLoader();
            const [gltf, meta] = await Promise.all([
                loader.loadAsync(modelUrl),
                fetch(metaUrl).then(r => {
                    if (!r.ok) throw new Error(`meta ${metaUrl}: HTTP ${r.status}`);
                    return r.json();
                })
            ]);
            const metaByFdi = new Map(meta.teeth.map(t => [t.fdi, t]));
            return { scene: gltf.scene, meta: metaByFdi, toothCount: meta.teeth.length };
        })();

        modelCache.set(key, promise);
        return promise;
    }

    // Абсолютные от корня пути (не относительные): fetch/GLTFLoader резолвят URL
    // относительно текущего адреса SPA-маршрута (например /patients/123), а не
    // относительно расположения этого JS-модуля — относительный путь здесь был бы
    // неверен на любой странице, кроме корневой. index.html объявляет <base href="/" />.
    function defaultUrls(dentition) {
        return dentition === 'deciduous'
            ? { modelUrl: '/models/teeth-deciduous.glb', metaUrl: '/models/teeth-deciduous.meta.json' }
            : { modelUrl: '/models/teeth.glb', metaUrl: '/models/teeth.meta.json' };
    }

    /* ---------------- цвета статусов, §8 ---------------- */
    // material.color работает как множитель поверх послойной раскраски вершин (COLOR_0),
    // поэтому статус тонирует зуб, не убивая естественную светотень эмали. Ключи —
    // ровно имена DentalLocal.Shared.Enums.ToothStateType (Blazor передаёт status.ToString()).
    // Цвета Healthy/Caries/Filling/Crown/Implant/RootCanalTreated/Veneer/Cracked/Destroyed —
    // из §8 промта; Pulpitis/Periodontitis/Bridge в промте не описаны (в его таблице статусов
    // нет), оставлены как в прежней реализации Odontogram3D.razor (GetStateColor).
    const STATUS_STYLE = {
        Healthy: { color: 0xffffff },
        Caries: { color: 0x7a4a22, roughness: 0.55 },
        Filling: { color: 0x3e6fb0 },
        Crown: { color: 0xd6a32b, metalness: 0.35, roughness: 0.25 },
        Implant: { color: 0xa8aeb4, metalness: 0.9, roughness: 0.2 },
        RootCanalTreated: { color: 0xc0392b },
        Veneer: { color: 0x7fc4dc, clearcoat: 1.0 },
        Cracked: { color: 0xffffff, emissive: 0xe07a20, emissiveIntensity: 0.35 },
        Destroyed: { color: 0x6e5b45, roughness: 0.8 },
        Pulpitis: { color: 0xb91c1c },
        Periodontitis: { color: 0x7c3aed },
        Bridge: { color: 0xd97706 },
        Extracted: { ghost: true },
        Missing: { ghost: true }
    };

    const SELECTION_EMISSIVE = 0xfbbf24, SELECTION_INTENSITY = 0.55;
    const HOVER_EMISSIVE = 0x38bdf8, HOVER_INTENSITY = 0.35;
    const RAYCAST_THROTTLE_MS = 70;

    function init(canvasId, dotNetRef, options) {
        options = options || {};
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (!webglAvailable()) {
            dotNetRef.invokeMethodAsync('OnError', 'WebGL недоступен');
            return;
        }

        loadThree().then((ok) => {
            if (!ok) {
                dotNetRef.invokeMethodAsync('OnError', 'Не удалось загрузить библиотеку three.js');
                return;
            }
            initScene(canvasId, canvas, dotNetRef, options);
        });
    }

    function initScene(canvasId, canvas, dotNetRef, options) {
        const width = canvas.clientWidth || 800;
        const height = canvas.clientHeight || 480;

        let renderer;
        try {
            renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
        } catch (e) {
            dotNetRef.invokeMethodAsync('OnError', 'Не удалось создать контекст WebGL');
            return;
        }
        renderer.setSize(width, height);
        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2)); // §3 бюджет
        renderer.shadowMap.enabled = !!options.shadows;

        const scene = new THREE.Scene();
        scene.background = options.background ? new THREE.Color(options.background) : null;

        const camera = new THREE.PerspectiveCamera(42, width / height, 0.1, 400);
        const controls = new OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        controls.minDistance = 12;
        controls.maxDistance = 120;

        scene.add(new THREE.HemisphereLight(0xd8e8f4, 0x3a2f28, 0.55));
        const key = new THREE.DirectionalLight(0xfff4e8, 1.15);
        key.position.set(24, 40, 30);
        scene.add(key);
        const fill = new THREE.DirectionalLight(0x9fc0e8, 0.4);
        fill.position.set(-30, 10, -20);
        scene.add(fill);

        const viewTargets = {
            front: { yaw: 0, pitch: 0.06, dist: 70 },
            top: { yaw: 0, pitch: 1.3, dist: 90 },
            left: { yaw: -Math.PI / 2, pitch: 0.15, dist: 70 },
            right: { yaw: Math.PI / 2, pitch: 0.15, dist: 70 },
            reset: { yaw: 0.42, pitch: 0.34, dist: 78 }
        };
        const cam = { yaw: viewTargets.reset.yaw, pitch: viewTargets.reset.pitch, dist: viewTargets.reset.dist };
        const camTarget = { yaw: cam.yaw, pitch: cam.pitch, dist: cam.dist };
        const orbitTarget = new THREE.Vector3(0, 0, -6);
        const orbitTargetGoal = orbitTarget.clone();

        const state = {
            renderer, scene, camera, controls,
            cam, camTarget, orbitTarget, orbitTargetGoal, viewTargets,
            dotNetRef,
            dentition: options.dentition === 'deciduous' ? 'deciduous' : 'permanent',
            loaded: false,
            pendingQueue: [],
            toothMeshes: new Map(),     // fdi -> Mesh (raycast target; hidden if ghost)
            ghostMeshes: new Map(),     // fdi -> Mesh (полупрозрачный "призрак" удалённого зуба)
            meshMeta: new Map(),        // fdi -> meta entry (mesialAxis/surfaces/localBounds/position)
            statusMaterialCache: new Map(), // status -> Material (общий на статус, не на клик)
            statuses: new Map(),        // fdi -> status (текущее состояние для пересборки при setDentition)
            selection: new Set(),
            hoverFdi: null,
            lastRaycastAt: 0,
            jawVisibility: { upper: true, lower: true },
            gumVisible: true,
            animId: 0,
            root: null,       // текущий THREE.Group с зубами/дёснами
            disposables: []   // материалы/призрачные геометрии этого instance (не общая кэш-геометрия)
        };
        scenes.set(canvasId, state);

        loadDentitionInto(state, state.dentition, options).catch(err => {
            console.error(err);
            dotNetRef.invokeMethodAsync('OnError', 'Не удалось загрузить 3D-модель: ' + (err && err.message ? err.message : String(err)));
        });

        function onPointerMove(ev) {
            const now = performance.now();
            if (now - state.lastRaycastAt < RAYCAST_THROTTLE_MS) return;
            state.lastRaycastAt = now;
            const hit = raycastAt(state, ev.clientX, ev.clientY);
            const fdi = hit ? hit.object.userData.fdi : null;
            if (fdi !== state.hoverFdi) {
                state.hoverFdi = fdi;
                applyHighlights(state);
                dotNetRef.invokeMethodAsync('OnToothHovered', fdi);
            }
            renderer.domElement.style.cursor = fdi ? 'pointer' : 'default';
        }

        let downX = 0, downY = 0, moved = 0;
        function onPointerDown(ev) { downX = ev.clientX; downY = ev.clientY; moved = 0; }
        function onPointerMoveForDrag(ev) { moved += Math.abs(ev.clientX - downX) + Math.abs(ev.clientY - downY); downX = ev.clientX; downY = ev.clientY; }
        function onPointerUp(ev) {
            if (moved > 6) return; // это было вращение камеры, не клик
            const hit = raycastAt(state, ev.clientX, ev.clientY);
            if (!hit) return;
            const fdi = hit.object.userData.fdi;
            const meta = state.meshMeta.get(fdi);
            const surfaceCode = meta ? classifySurface(hit, hit.object, meta) : null;

            if (ev.shiftKey) {
                if (state.selection.has(fdi)) state.selection.delete(fdi); else state.selection.add(fdi);
                applyHighlights(state);
                dotNetRef.invokeMethodAsync('OnSelectionChanged', Array.from(state.selection));
            }
            dotNetRef.invokeMethodAsync('OnToothClicked', fdi, surfaceCode);
        }

        renderer.domElement.addEventListener('pointermove', onPointerMove);
        renderer.domElement.addEventListener('pointerdown', onPointerDown);
        renderer.domElement.addEventListener('pointermove', onPointerMoveForDrag);
        renderer.domElement.addEventListener('pointerup', onPointerUp);

        function resize() {
            const w = canvas.clientWidth || width;
            const h = canvas.clientHeight || height;
            renderer.setSize(w, h);
            camera.aspect = w / h;
            camera.updateProjectionMatrix();
        }
        window.addEventListener('resize', resize);

        const lerp = (a, b, t) => a + (b - a) * t;
        function animate() {
            state.animId = requestAnimationFrame(animate);
            cam.yaw = lerp(cam.yaw, camTarget.yaw, 0.14);
            cam.pitch = lerp(cam.pitch, camTarget.pitch, 0.14);
            cam.dist = lerp(cam.dist, camTarget.dist, 0.12);
            orbitTarget.lerp(orbitTargetGoal, 0.14);
            camera.position.set(
                orbitTarget.x + cam.dist * Math.cos(cam.pitch) * Math.sin(cam.yaw),
                orbitTarget.y + cam.dist * Math.sin(cam.pitch),
                orbitTarget.z + cam.dist * Math.cos(cam.pitch) * Math.cos(cam.yaw)
            );
            camera.lookAt(orbitTarget);
            controls.target.copy(orbitTarget);
            controls.update();
            renderer.render(scene, camera);
        }
        animate();

        state._teardown = () => {
            cancelAnimationFrame(state.animId);
            renderer.domElement.removeEventListener('pointermove', onPointerMove);
            renderer.domElement.removeEventListener('pointerdown', onPointerDown);
            renderer.domElement.removeEventListener('pointermove', onPointerMoveForDrag);
            renderer.domElement.removeEventListener('pointerup', onPointerUp);
            window.removeEventListener('resize', resize);
        };
    }

    async function loadDentitionInto(state, dentition, options) {
        const urls = defaultUrls(dentition);
        const modelUrl = options.modelUrl || urls.modelUrl;
        const metaUrl = options.metaUrl || urls.metaUrl;
        const { scene: templateRoot, meta, toothCount } = await loadModel(dentition, modelUrl, metaUrl);

        if (state.root) {
            state.scene.remove(state.root);
            disposeInstance(state);
        }

        // Клонируем граф сцены (three.js по умолчанию делит geometry/material по ссылке —
        // тяжёлые типизированные массивы вершин не копируются и не пересчитываются).
        const root = templateRoot.clone(true);
        state.root = root;
        state.scene.add(root);
        state.toothMeshes.clear();
        state.ghostMeshes.clear();
        state.meshMeta.clear();
        state.statusMaterialCache.clear();
        state.disposables = [];

        root.traverse(obj => {
            if (!obj.isMesh) return;
            const name = obj.name || '';
            const m = /^tooth_(\d+)$/.exec(name);
            if (m) {
                const fdi = parseInt(m[1], 10);
                obj.userData.fdi = fdi;
                obj.material = obj.material.clone(); // независимая перекраска на инстанс
                state.disposables.push(obj.material);
                state.toothMeshes.set(fdi, obj);
                const metaEntry = meta.get(fdi);
                if (metaEntry) state.meshMeta.set(fdi, metaEntry);
            } else if (name === 'gum_upper' || name === 'gum_lower') {
                obj.material = obj.material.clone();
                state.disposables.push(obj.material);
                obj.userData.isGum = true;
                obj.userData.jaw = name === 'gum_upper' ? 'upper' : 'lower';
            }
        });

        applyJawVisibility(state);
        applyGumVisibility(state);
        // Переприменяем ранее выставленные статусы (актуально при setDentition).
        for (const [fdi, status] of state.statuses) applyStatus(state, fdi, status);
        applyHighlights(state);

        state.loaded = true;
        state.dotNetRef.invokeMethodAsync('OnModelLoaded', toothCount);

        const queue = state.pendingQueue;
        state.pendingQueue = [];
        for (const task of queue) task();
    }

    function disposeInstance(state) {
        for (const d of state.disposables) d.dispose();
        for (const [, ghost] of state.ghostMeshes) {
            if (ghost.geometry) ghost.geometry.dispose();
            if (ghost.material) ghost.material.dispose();
        }
    }

    function whenLoaded(state, fn) {
        if (state.loaded) fn(); else state.pendingQueue.push(fn);
    }

    /* ---------------- статусы/материалы, §8 ---------------- */
    function getStatusMaterial(state, status) {
        let mat = state.statusMaterialCache.get(status);
        if (mat) return mat;
        const style = STATUS_STYLE[status] || STATUS_STYLE.Healthy;
        mat = new THREE.MeshPhysicalMaterial({
            vertexColors: true,
            color: new THREE.Color(style.color !== undefined ? style.color : 0xffffff),
            roughness: style.roughness !== undefined ? style.roughness : 0.30,
            metalness: style.metalness !== undefined ? style.metalness : 0,
            clearcoat: style.clearcoat !== undefined ? style.clearcoat : 0.55,
            clearcoatRoughness: 0.22
        });
        if (style.emissive !== undefined) {
            mat.emissive = new THREE.Color(style.emissive);
            mat.emissiveIntensity = style.emissiveIntensity || 0;
        }
        state.statusMaterialCache.set(status, mat);
        state.disposables.push(mat);
        return mat;
    }

    function getGhostMaterial(state) {
        let mat = state.statusMaterialCache.get('__ghost__');
        if (mat) return mat;
        mat = new THREE.MeshPhysicalMaterial({
            color: 0x8a9aa5, transparent: true, opacity: 0.12, depthWrite: false
        });
        state.statusMaterialCache.set('__ghost__', mat);
        state.disposables.push(mat);
        return mat;
    }

    function applyStatus(state, fdi, status) {
        const mesh = state.toothMeshes.get(fdi);
        if (!mesh) return;
        state.statuses.set(fdi, status);
        const style = STATUS_STYLE[status] || STATUS_STYLE.Healthy;

        if (style.ghost) {
            mesh.visible = false;
            let ghost = state.ghostMeshes.get(fdi);
            if (!ghost) {
                ghost = new THREE.Mesh(mesh.geometry, getGhostMaterial(state));
                ghost.position.copy(mesh.position);
                ghost.rotation.copy(mesh.rotation);
                ghost.renderOrder = 1;
                mesh.parent.add(ghost);
                state.ghostMeshes.set(fdi, ghost);
            }
            ghost.visible = true;
        } else {
            mesh.material = getStatusMaterial(state, status);
            const ghost = state.ghostMeshes.get(fdi);
            if (ghost) ghost.visible = false;
        }
        applyJawVisibility(state); // выставляет mesh.visible/ghost.visible по челюсти и статусу
    }

    function setToothStatus(canvasId, fdi, status) {
        const state = scenes.get(canvasId);
        if (!state) return;
        whenLoaded(state, () => applyStatus(state, fdi, status));
    }

    function setStatusBatch(canvasId, items) {
        const state = scenes.get(canvasId);
        if (!state) return;
        whenLoaded(state, () => { for (const it of (items || [])) applyStatus(state, it.fdi, it.status); });
    }

    /* ---------------- подсветка: наведение + мультивыбор ---------------- */
    function applyHighlights(state) {
        for (const [fdi, mesh] of state.toothMeshes) {
            const mat = mesh.material;
            if (!mat) continue;
            if (state.selection.has(fdi)) {
                mat.emissive = mat.emissive || new THREE.Color();
                mat.emissive.set(SELECTION_EMISSIVE);
                mat.emissiveIntensity = SELECTION_INTENSITY;
            } else if (state.hoverFdi === fdi) {
                mat.emissive = mat.emissive || new THREE.Color();
                mat.emissive.set(HOVER_EMISSIVE);
                mat.emissiveIntensity = HOVER_INTENSITY;
            } else {
                const style = STATUS_STYLE[state.statuses.get(fdi)] || {};
                mat.emissive = mat.emissive || new THREE.Color();
                mat.emissive.set(style.emissive !== undefined ? style.emissive : 0x000000);
                mat.emissiveIntensity = style.emissiveIntensity || 0;
            }
        }
    }

    function highlightTooth(canvasId, fdi, on) {
        const state = scenes.get(canvasId);
        if (!state) return;
        whenLoaded(state, () => { state.hoverFdi = on ? fdi : (state.hoverFdi === fdi ? null : state.hoverFdi); applyHighlights(state); });
    }

    function setSelection(canvasId, fdiList) {
        const state = scenes.get(canvasId);
        if (!state) return;
        whenLoaded(state, () => {
            state.selection = new Set(fdiList || []);
            applyHighlights(state);
        });
    }

    /* ---------------- видимость челюстей/дёсен ---------------- */
    function applyJawVisibility(state) {
        for (const [fdi, mesh] of state.toothMeshes) {
            const meta = state.meshMeta.get(fdi);
            const jaw = meta ? meta.jaw : (fdi < 30 || (fdi >= 50 && fdi < 70) ? 'upper' : 'lower');
            const jawOn = jaw === 'upper' ? state.jawVisibility.upper : state.jawVisibility.lower;
            const isGhost = (STATUS_STYLE[state.statuses.get(fdi)] || {}).ghost === true;
            mesh.visible = jawOn && !isGhost;
            const ghost = state.ghostMeshes.get(fdi);
            if (ghost) ghost.visible = jawOn && isGhost;
        }
        if (state.root) {
            state.root.traverse(obj => {
                if (obj.userData && obj.userData.isGum) {
                    const jawOn = obj.userData.jaw === 'upper' ? state.jawVisibility.upper : state.jawVisibility.lower;
                    obj.visible = state.gumVisible && jawOn;
                }
            });
        }
    }

    function applyGumVisibility(state) { applyJawVisibility(state); }

    function setJawVisibility(canvasId, upper, lower) {
        const state = scenes.get(canvasId);
        if (!state) return;
        state.jawVisibility.upper = !!upper;
        state.jawVisibility.lower = !!lower;
        whenLoaded(state, () => applyJawVisibility(state));
    }

    function setGumVisibility(canvasId, visible) {
        const state = scenes.get(canvasId);
        if (!state) return;
        state.gumVisible = !!visible;
        whenLoaded(state, () => applyGumVisibility(state));
    }

    /* ---------------- дентиция ---------------- */
    function setDentition(canvasId, dentition) {
        const state = scenes.get(canvasId);
        if (!state) return;
        const target = dentition === 'deciduous' ? 'deciduous' : 'permanent';
        if (state.dentition === target) return;
        state.dentition = target;
        state.loaded = false;
        loadDentitionInto(state, target, {}).catch(err => {
            console.error(err);
            state.dotNetRef.invokeMethodAsync('OnError', 'Не удалось переключить прикус: ' + (err && err.message ? err.message : String(err)));
        });
    }

    /* ---------------- камера ---------------- */
    function setView(canvasId, view) {
        const state = scenes.get(canvasId);
        if (!state) return;
        const v = state.viewTargets[view] || state.viewTargets.reset;
        state.camTarget.yaw = v.yaw; state.camTarget.pitch = v.pitch; state.camTarget.dist = v.dist;
    }

    function focusTooth(canvasId, fdi) {
        const state = scenes.get(canvasId);
        if (!state) return;
        whenLoaded(state, () => {
            const mesh = state.toothMeshes.get(fdi);
            if (!mesh) return;
            state.orbitTargetGoal.copy(mesh.position);
            state.camTarget.dist = 32;
        });
    }

    /* ---------------- рейкастинг и определение поверхности, §7 ---------------- */
    function raycastAt(state, clientX, clientY) {
        const rect = state.renderer.domElement.getBoundingClientRect();
        const pointer = new THREE.Vector2(
            ((clientX - rect.left) / rect.width) * 2 - 1,
            -((clientY - rect.top) / rect.height) * 2 + 1
        );
        const raycaster = new THREE.Raycaster();
        raycaster.setFromCamera(pointer, state.camera);
        const targets = Array.from(state.toothMeshes.values());
        const hits = raycaster.intersectObjects(targets, false);
        return hits.length ? hits[0] : null;
    }

    function classifySurface(hit, mesh, meta) {
        const local = mesh.worldToLocal(hit.point.clone());
        const [minX, minY, minZ] = meta.localBounds.min;
        const [maxX, maxY, maxZ] = meta.localBounds.max;
        const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2, cz = (minZ + maxZ) / 2;
        const hx = Math.max(1e-6, (maxX - minX) / 2), hy = Math.max(1e-6, (maxY - minY) / 2), hz = Math.max(1e-6, (maxZ - minZ) / 2);

        if (local.y < 0) return null; // клик по корню — открыть историю зуба, как в 2D (§9)

        const n = { x: (local.x - cx) / hx, y: (local.y - cy) / hy, z: (local.z - cz) / hz };
        if (n.y > 0.55) return meta.position <= 3 ? 'I' : 'O';
        if (Math.abs(n.x) >= Math.abs(n.z)) {
            const mesialIsPlusX = meta.mesialAxis !== '-X';
            const isMesial = mesialIsPlusX ? n.x > 0 : n.x < 0;
            return isMesial ? 'M' : 'D';
        }
        return n.z > 0 ? 'B' : 'L';
    }

    /* ---------------- служебное ---------------- */
    function resize(canvasId) {
        const state = scenes.get(canvasId);
        if (!state) return;
        const canvas = state.renderer.domElement;
        const w = canvas.clientWidth, h = canvas.clientHeight;
        state.renderer.setSize(w, h);
        state.camera.aspect = w / h;
        state.camera.updateProjectionMatrix();
    }

    function dispose(canvasId) {
        const state = scenes.get(canvasId);
        if (!state) return;
        if (state._teardown) state._teardown();
        disposeInstance(state);
        state.renderer.dispose();
        scenes.delete(canvasId);
    }

    return {
        init, dispose, resize,
        setDentition, setToothStatus, setStatusBatch,
        highlightTooth, focusTooth, setSelection, setView,
        setJawVisibility, setGumVisibility
    };
})();
