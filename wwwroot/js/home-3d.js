// ============================================================================
// home-3d.js — Three.js on the home page (ES module).
//
// 1. Highlights slider: draws the slide images on a WebGL plane. Slides change
//    with a liquid, noise-driven wipe in the slide direction (coral edge, small
//    colour split), drift with a slow zoom, and ripple around the pointer.
//    home.js runs the slider itself and fires "hp:slide"; without WebGL the
//    CSS transition is used.
// 2. Clinical band: a small 3D still life (speech bubble with a cross, a typing
//    bubble, a capsule, a ring, spheres) behind the frosted cards. Objects are
//    placed in pixels relative to the layout, so it adapts to every width.
//
// Three.js (~180 KB gzipped) is imported only after the page has loaded and a
// section is close to the viewport. Rendering stops while off-screen or in a
// background tab. "Reduce motion" gets still frames; "Save data" gets no 3D.
// ============================================================================

const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const saveData = !!(navigator.connection && navigator.connection.saveData);

const webglAvailable = (() => {
    try {
        const canvas = document.createElement('canvas');
        return !!(canvas.getContext('webgl2') || canvas.getContext('webgl'));
    } catch (e) {
        return false;
    }
})();

let threePromise = null;
const loadThree = () => (threePromise ??= import('three'));

function afterLoad(fn) {
    const idle = window.requestIdleCallback || ((cb) => setTimeout(cb, 200));
    if (document.readyState === 'complete') idle(fn, { timeout: 2000 });
    else window.addEventListener('load', () => idle(fn, { timeout: 2000 }), { once: true });
}

function whenNear(el, fn) {
    if (!('IntersectionObserver' in window)) { fn(); return; }
    const io = new IntersectionObserver((entries) => {
        if (entries.some((e) => e.isIntersecting)) { io.disconnect(); fn(); }
    }, { rootMargin: '300px 0px' });
    io.observe(el);
}

// Calls step(now) every frame while el is on screen and the tab is visible.
function animateWhileVisible(el, step) {
    let visible = false;
    let raf = 0;
    const loop = (now) => { raf = requestAnimationFrame(loop); step(now); };
    const update = () => {
        const run = visible && !document.hidden;
        if (run && !raf) raf = requestAnimationFrame(loop);
        else if (!run && raf) { cancelAnimationFrame(raf); raf = 0; }
    };
    new IntersectionObserver(([entry]) => { visible = entry.isIntersecting; update(); }).observe(el);
    document.addEventListener('visibilitychange', update);
}

const easeInOutCubic = (t) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);
const easeOutBack = (t) => { const c = 1.6; return 1 + (c + 1) * Math.pow(t - 1, 3) + c * Math.pow(t - 1, 2); };
const clamp01 = (v) => Math.min(1, Math.max(0, v));

// ----------------------------------------------------------------------------
// 1. Slider
// ----------------------------------------------------------------------------
const SLIDER_VERTEX = /* glsl */ `
varying vec2 vUv;
void main() {
    vUv = uv;
    gl_Position = vec4(position.xy, 0.0, 1.0);
}`;

const SLIDER_FRAGMENT = /* glsl */ `
uniform sampler2D uTexA;
uniform sampler2D uTexB;
uniform vec2 uSizeA;
uniform vec2 uSizeB;
uniform vec2 uRes;
uniform float uProgress;
uniform float uDir;
uniform float uTime;
uniform float uZoomA;
uniform float uZoomB;
uniform vec2 uMouse;
uniform float uHover;
varying vec2 vUv;

// 2D simplex noise: Ian McEwan, Ashima Arts (MIT)
vec3 permute(vec3 x) { return mod(((x * 34.0) + 1.0) * x, 289.0); }
float snoise(vec2 v) {
    const vec4 C = vec4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
    vec2 i = floor(v + dot(v, C.yy));
    vec2 x0 = v - i + dot(i, C.xx);
    vec2 i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    vec4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = mod(i, 289.0);
    vec3 p = permute(permute(i.y + vec3(0.0, i1.y, 1.0)) + i.x + vec3(0.0, i1.x, 1.0));
    vec3 m = max(0.5 - vec3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
    m = m * m;
    m = m * m;
    vec3 x = 2.0 * fract(p * C.www) - 1.0;
    vec3 h = abs(x) - 0.5;
    vec3 ox = floor(x + 0.5);
    vec3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
    vec3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

// object-fit: cover, plus a zoom around the centre
vec2 cover(vec2 uv, vec2 size, float zoom) {
    float rs = uRes.x / uRes.y;
    float ri = size.x / size.y;
    vec2 s = vec2(min(rs / ri, 1.0), min(ri / rs, 1.0));
    return (uv - 0.5) * s / zoom + 0.5;
}

void main() {
    vec2 uv = vUv;

    // ripple and a little parallax around the pointer
    vec2 d = uv - uMouse;
    d.x *= uRes.x / uRes.y;
    float dist = length(d);
    float wave = sin(dist * 28.0 - uTime * 3.0) * exp(-dist * 5.0) * 0.006 * uHover;
    uv += (dist > 0.0001 ? d / dist : vec2(0.0)) * wave;
    uv += (uMouse - 0.5) * 0.012 * uHover;

    // liquid wipe: a sweep in the slide direction broken up by noise
    float n = snoise(uv * 2.2 + vec2(uTime * 0.05, 0.0)) * 0.5 + 0.5;
    float sweep = uDir > 0.0 ? uv.x : 1.0 - uv.x;
    float field = mix(n, sweep, 0.55);
    float w = 0.22;
    float p = uProgress;
    float m = smoothstep(field - w, field, p * (1.0 + w) - w);

    // the outgoing image is pushed away, the incoming one settles into place
    vec2 push = vec2(uDir * 0.08, 0.03) * (n - 0.5);
    vec3 a = texture2D(uTexA, cover(uv + push * p * 2.0, uSizeA, uZoomA)).rgb;
    vec2 uvB = cover(uv - push * (1.0 - p) * 2.0, uSizeB, uZoomB);
    float shift = (1.0 - p) * p * 0.015 * uDir;
    vec3 b = vec3(
        texture2D(uTexB, uvB + vec2(shift, 0.0)).r,
        texture2D(uTexB, uvB).g,
        texture2D(uTexB, uvB - vec2(shift, 0.0)).b
    );

    vec3 color = mix(a, b, m);
    // coral glow on the liquid edge
    float edge = m * (1.0 - m) * 4.0;
    color += vec3(1.0, 0.58, 0.55) * edge * 0.28 * step(0.001, p) * step(p, 0.999);
    gl_FragColor = vec4(color, 1.0);
}`;

async function initSlider(slider) {
    const stage = slider.querySelector('.hp-gl-slider__stage');
    const canvas = slider.querySelector('.hp-gl-slider__canvas');
    const images = Array.from(slider.querySelectorAll('.hp-gl-slide__img'));
    if (!stage || !canvas || images.length === 0) return;

    const THREE = await loadThree();
    let renderer;
    try {
        renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance' });
    } catch (e) {
        console.warn('Slider WebGL unavailable, using the CSS transition:', e);
        return;
    }
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    // Textures are sampled and written as-is (sRGB in, sRGB out), so colours match the <img>
    renderer.outputColorSpace = THREE.LinearSRGBColorSpace;

    const blank = new THREE.DataTexture(new Uint8Array([27, 20, 17, 255]), 1, 1);
    blank.needsUpdate = true;
    const uniforms = {
        uTexA: { value: blank },
        uTexB: { value: blank },
        uSizeA: { value: new THREE.Vector2(1, 1) },
        uSizeB: { value: new THREE.Vector2(1, 1) },
        uRes: { value: new THREE.Vector2(1, 1) },
        uProgress: { value: 0 },
        uDir: { value: 1 },
        uTime: { value: 0 },
        uZoomA: { value: 1 },
        uZoomB: { value: 1 },
        uMouse: { value: new THREE.Vector2(0.5, 0.5) },
        uHover: { value: 0 }
    };
    const scene = new THREE.Scene();
    const camera = new THREE.Camera();
    scene.add(new THREE.Mesh(
        new THREE.PlaneGeometry(2, 2),
        new THREE.ShaderMaterial({ uniforms, vertexShader: SLIDER_VERTEX, fragmentShader: SLIDER_FRAGMENT, depthTest: false, depthWrite: false })
    ));

    // One texture per slide, from the size the browser picked out of srcset
    const loader = new THREE.TextureLoader();
    const textures = new Map();
    const load = (i) => {
        if (!textures.has(i)) {
            const img = images[i];
            textures.set(i, new Promise((resolve, reject) => {
                loader.load(img.currentSrc || img.src, (texture) => {
                    texture.minFilter = THREE.LinearFilter;
                    texture.magFilter = THREE.LinearFilter;
                    texture.generateMipmaps = false;
                    resolve(texture);
                }, undefined, reject);
            }));
        }
        return textures.get(i);
    };
    const sizeOf = (texture) => new THREE.Vector2(texture.image.naturalWidth || texture.image.width, texture.image.naturalHeight || texture.image.height);

    let current = parseInt(slider.getAttribute('data-active'), 10) || 0;
    let firstTexture;
    try {
        firstTexture = await load(current);
    } catch (e) {
        return;
    }
    uniforms.uTexA.value = firstTexture;
    uniforms.uSizeA.value.copy(sizeOf(firstTexture));

    const resize = () => {
        const width = stage.clientWidth;
        const height = stage.clientHeight;
        renderer.setSize(width, height, false);
        uniforms.uRes.value.set(width * renderer.getPixelRatio(), height * renderer.getPixelRatio());
    };
    resize();
    new ResizeObserver(resize).observe(stage);

    const interval = parseInt(slider.getAttribute('data-interval'), 10) || 7000;
    const zoomSpan = interval + 1600;
    let shownAt = performance.now();
    let transition = null;
    let requested = current;

    const finish = () => {
        uniforms.uTexA.value = uniforms.uTexB.value;
        uniforms.uSizeA.value.copy(uniforms.uSizeB.value);
        uniforms.uProgress.value = 0;
        shownAt = transition.start;
        current = transition.to;
        transition = null;
    };

    slider.addEventListener('hp:slide', async (event) => {
        const { to, direction } = event.detail;
        requested = to;
        let texture;
        try {
            texture = await load(to);
        } catch (e) {
            return;
        }
        if (requested !== to) return; // a newer slide was asked for while this one loaded
        if (transition) finish();
        uniforms.uTexB.value = texture;
        uniforms.uSizeB.value.copy(sizeOf(texture));
        uniforms.uDir.value = direction < 0 ? -1 : 1;
        transition = { start: performance.now(), duration: reduceMotion ? 450 : 1600, to };
        load((to + 1) % images.length).catch(() => {});
    });
    load((current + 1) % images.length).catch(() => {});

    const pointer = new THREE.Vector2(0.5, 0.5);
    let hover = 0;
    stage.addEventListener('pointermove', (e) => {
        const r = stage.getBoundingClientRect();
        pointer.set((e.clientX - r.left) / r.width, 1 - (e.clientY - r.top) / r.height);
        hover = e.pointerType === 'mouse' ? 1 : 0;
    });
    stage.addEventListener('pointerleave', () => { hover = 0; });

    const startedAt = performance.now();
    const step = (now) => {
        uniforms.uTime.value = (now - startedAt) / 1000;
        if (transition) {
            const p = clamp01((now - transition.start) / transition.duration);
            uniforms.uProgress.value = easeInOutCubic(p);
            uniforms.uZoomB.value = reduceMotion ? 1 : 1 + 0.06 * clamp01((now - transition.start) / zoomSpan);
            if (p >= 1) finish();
        }
        uniforms.uZoomA.value = reduceMotion ? 1 : 1 + 0.06 * clamp01((now - shownAt) / zoomSpan);
        uniforms.uMouse.value.lerp(pointer, 0.08);
        uniforms.uHover.value += ((reduceMotion ? 0 : hover) - uniforms.uHover.value) * 0.06;
        renderer.render(scene, camera);
    };

    step(performance.now());
    slider.classList.add('has-webgl');
    animateWhileVisible(stage, step);
}

// ----------------------------------------------------------------------------
// 2. Clinical band
// ----------------------------------------------------------------------------
async function initClinic(container) {
    const band = container.parentElement;
    const THREE = await loadThree();
    const { RoomEnvironment } = await import('three/addons/environments/RoomEnvironment.js');

    let renderer;
    try {
        renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: 'high-performance' });
    } catch (e) {
        console.warn('3D scene unavailable:', e);
        return;
    }
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.75));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.05;
    renderer.setClearColor(0x000000, 0);
    container.appendChild(renderer.domElement);

    const scene = new THREE.Scene();
    const pmrem = new THREE.PMREMGenerator(renderer);
    scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
    pmrem.dispose();

    const camera = new THREE.PerspectiveCamera(30, 1, 0.1, 100);
    camera.position.set(0, 0, 18);

    // A warm key light and a coral rim light so the shapes read on the dark band
    const key = new THREE.DirectionalLight(0xfff1e6, 1.3);
    key.position.set(-4, 6, 8);
    const rim = new THREE.DirectionalLight(0xff9494, 2.4);
    rim.position.set(6, -2, -6);
    scene.add(key, rim);

    const material = {
        coral: new THREE.MeshPhysicalMaterial({ color: 0xff8f87, roughness: 0.32, clearcoat: 1, clearcoatRoughness: 0.12, sheen: 0.5, sheenColor: new THREE.Color(0xffd1d1) }),
        cream: new THREE.MeshPhysicalMaterial({ color: 0xfff3ea, roughness: 0.38, clearcoat: 0.8, clearcoatRoughness: 0.2 }),
        white: new THREE.MeshPhysicalMaterial({ color: 0xffffff, roughness: 0.22, clearcoat: 1 }),
        deep: new THREE.MeshPhysicalMaterial({ color: 0xe05a5a, roughness: 0.3, metalness: 0.15, clearcoat: 1 }),
        ink: new THREE.MeshPhysicalMaterial({ color: 0x3a2c28, roughness: 0.45, clearcoat: 0.6 }),
        pink: new THREE.MeshPhysicalMaterial({ color: 0xffc9c4, roughness: 0.25, clearcoat: 1 })
    };

    // Speech bubble: a rounded rectangle with a tail, extruded with soft bevels.
    // The body is centred on the origin; the tail hangs below it.
    const bubbleGeometry = (w, h, r, tail) => {
        const s = new THREE.Shape();
        const x = -w / 2;
        const y = -h / 2;
        s.moveTo(x + r, y);
        if (tail === 'left') { s.lineTo(x + r + 0.15, y); s.lineTo(x + r - 0.3, y - 0.75); s.lineTo(x + r + 0.95, y); }
        if (tail === 'right') { s.lineTo(x + w - r - 0.95, y); s.lineTo(x + w - r + 0.3, y - 0.75); s.lineTo(x + w - r - 0.15, y); }
        s.lineTo(x + w - r, y);
        s.absarc(x + w - r, y + r, r, -Math.PI / 2, 0, false);
        s.lineTo(x + w, y + h - r);
        s.absarc(x + w - r, y + h - r, r, 0, Math.PI / 2, false);
        s.lineTo(x + r, y + h);
        s.absarc(x + r, y + h - r, r, Math.PI / 2, Math.PI, false);
        s.lineTo(x, y + r);
        s.absarc(x + r, y + r, r, Math.PI, Math.PI * 1.5, false);
        const g = new THREE.ExtrudeGeometry(s, { depth: 0.55, bevelEnabled: true, bevelThickness: 0.22, bevelSize: 0.2, bevelSegments: 8, curveSegments: 24 });
        g.translate(0, 0, -0.275);
        return g;
    };
    const FRONT = 0.275 + 0.22; // z of a bubble's front face

    const crossGeometry = (length, arm) => {
        const s = new THREE.Shape();
        const h = arm / 2;
        const l = length / 2;
        s.moveTo(-h, l); s.lineTo(h, l); s.lineTo(h, h); s.lineTo(l, h); s.lineTo(l, -h); s.lineTo(h, -h);
        s.lineTo(h, -l); s.lineTo(-h, -l); s.lineTo(-h, -h); s.lineTo(-l, -h); s.lineTo(-l, h); s.lineTo(-h, h);
        s.closePath();
        const g = new THREE.ExtrudeGeometry(s, { depth: 0.2, bevelEnabled: true, bevelThickness: 0.08, bevelSize: 0.07, bevelSegments: 4 });
        g.translate(0, 0, -0.1);
        return g;
    };

    // Items are placed in CSS pixels relative to the band (see place()), so the layout follows the page
    const items = [];
    const add = (object, worldWidth, place, options = {}) => {
        const holder = new THREE.Group();
        holder.add(object);
        scene.add(holder);
        items.push({
            holder,
            object,
            worldWidth,
            place,
            phase: Math.random() * Math.PI * 2,
            rest: options.rest || { x: 0, y: 0 },
            spin: options.spin ?? 1,
            parallax: options.parallax ?? 1,
            update: options.update,
            home: new THREE.Vector3(),
            scale: 1
        });
    };

    // Speech bubble with a medical cross
    const careBubble = new THREE.Group();
    careBubble.add(new THREE.Mesh(bubbleGeometry(3.2, 2.2, 0.75, 'left'), material.coral));
    const cross = new THREE.Mesh(crossGeometry(1.2, 0.42), material.white);
    cross.position.z = FRONT + 0.1;
    careBubble.add(cross);
    add(careBubble, 3.6, (L) =>
        L.mode === 'desktop' ? { x: L.right - 210, y: 190, px: 250 }
            : L.mode === 'tablet' ? { x: L.right - 70, y: 110, px: 170 }
                : { x: L.W - 108, y: 86, px: 150 },
    { rest: { x: -0.1, y: -0.35 }, parallax: 1.3 });

    // Cream bubble with "typing" dots
    const chatBubble = new THREE.Group();
    chatBubble.add(new THREE.Mesh(bubbleGeometry(2.4, 1.5, 0.6, 'right'), material.cream));
    const dots = [-0.55, 0, 0.55].map((x) => {
        const dot = new THREE.Mesh(new THREE.SphereGeometry(0.19, 24, 16), material.ink);
        dot.position.set(x, 0, FRONT + 0.1);
        chatBubble.add(dot);
        return dot;
    });
    // Desktop: in the free space between the intro text and the big bubble; tablet: tucked behind the right card
    add(chatBubble, 2.8, (L) =>
        L.mode === 'desktop' ? { x: L.right - 470, y: 250, px: 150 }
            : L.mode === 'tablet' ? { x: L.right - 30, y: L.H * 0.66, px: 130 }
                : { x: 84, y: 94, px: 100 },
    {
        rest: { x: 0.1, y: 0.4 },
        update: (t) => dots.forEach((dot, i) => { dot.position.y = Math.max(0, Math.sin(t * 5 - i * 0.7)) * 0.2; })
    });

    // Two-tone capsule
    const capsule = new THREE.Group();
    const capsuleHalf = (mat, sign) => {
        const half = new THREE.Group();
        const radius = 0.42;
        const length = 0.9;
        const body = new THREE.Mesh(new THREE.CylinderGeometry(radius, radius, length / 2, 40, 1, true), mat);
        body.position.y = (sign * length) / 4;
        const cap = new THREE.Mesh(new THREE.SphereGeometry(radius, 40, 20, 0, Math.PI * 2, sign > 0 ? 0 : Math.PI / 2, Math.PI / 2), mat);
        cap.position.y = (sign * length) / 2;
        half.add(body, cap);
        return half;
    };
    capsule.add(capsuleHalf(material.coral, 1), capsuleHalf(material.cream, -1));
    capsule.rotation.z = 0.8;
    add(capsule, 1.5, (L) =>
        L.mode === 'mobile' ? { x: L.W - 42, y: L.H - 90, px: 70 } : { x: L.left + L.contentW * 0.5, y: L.H - 78, px: 110 },
    { rest: { x: 0.2, y: 0 }, spin: 1.6, parallax: 0.6 });

    // Ring
    const ring = new THREE.Mesh(new THREE.TorusGeometry(0.55, 0.16, 24, 64), material.deep);
    add(ring, 1.42, (L) =>
        L.mode === 'desktop' ? { x: L.right - 38, y: 72, px: 70 }
            : L.mode === 'tablet' ? { x: L.right + 4, y: 48, px: 56 }
                : { x: L.W - 30, y: 40, px: 44 },
    { rest: { x: 0.6, y: 0.3 }, parallax: 1.6, update: (t) => { ring.rotation.z = t * 0.4; } });

    // Spheres
    const sphere = (mat) => new THREE.Mesh(new THREE.SphereGeometry(1, 32, 16), mat);
    add(sphere(material.pink), 2, (L) => (L.mode === 'mobile' ? { x: L.W * 0.5, y: 46, px: 22 } : { x: L.left + L.contentW * 0.6, y: 96, px: 34 }), { parallax: 1.8 });
    add(sphere(material.coral), 2, (L) => (L.mode === 'mobile' ? { x: 22, y: L.H - 62, px: 40 } : { x: L.left - 28, y: L.H - 140, px: 56 }), { parallax: 0.8 });
    add(sphere(material.cream), 2, (L) => (L.mode === 'mobile' ? null : { x: L.right + 28, y: L.H - 72, px: 40 }), { parallax: 1.2 });
    add(sphere(material.pink), 2, (L) => (L.mode === 'mobile' ? null : { x: L.left + L.contentW * 0.26, y: L.H * 0.56, px: 26 }), { parallax: 1.4 });

    const layout = () => {
        const W = container.clientWidth;
        const H = container.clientHeight;
        if (!W || !H) return;
        renderer.setSize(W, H, false);
        camera.aspect = W / H;
        camera.updateProjectionMatrix();
        const viewHeight = 2 * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2)) * camera.position.z;
        const unit = viewHeight / H; // world units per CSS pixel on the z = 0 plane
        const contentW = Math.min(1180, W - 32);
        const left = (W - contentW) / 2;
        const L = { W, H, left, right: left + contentW, contentW, mode: W >= 992 ? 'desktop' : W >= 768 ? 'tablet' : 'mobile' };
        items.forEach((item) => {
            const spot = item.place(L);
            item.holder.visible = !!spot;
            if (!spot) return;
            item.home.set((spot.x - W / 2) * unit, (H / 2 - spot.y) * unit, 0);
            item.scale = (spot.px * unit) / item.worldWidth;
        });
    };

    const pointer = new THREE.Vector2();
    const tilt = new THREE.Vector2();
    band.addEventListener('pointermove', (e) => {
        if (e.pointerType !== 'mouse') return;
        const r = band.getBoundingClientRect();
        pointer.set((e.clientX - r.left) / r.width - 0.5, (e.clientY - r.top) / r.height - 0.5);
    });
    band.addEventListener('pointerleave', () => pointer.set(0, 0));

    let firstFrame = null;
    const step = (now) => {
        if (firstFrame === null) firstFrame = now;
        const t = (now - firstFrame) / 1000;
        tilt.lerp(pointer, 0.05);
        items.forEach((item, i) => {
            if (!item.holder.visible) return;
            const appear = reduceMotion ? 1 : easeOutBack(clamp01((t - 0.15 - i * 0.08) / 0.9));
            const bob = reduceMotion ? 0 : Math.sin(t * 0.9 + item.phase) * 0.12;
            item.holder.position.set(
                item.home.x + tilt.x * 0.45 * item.parallax,
                item.home.y + bob - tilt.y * 0.3 * item.parallax,
                item.home.z
            );
            item.holder.scale.setScalar(Math.max(0.0001, item.scale * appear));
            const sway = reduceMotion ? 0 : 1;
            item.object.rotation.y = item.rest.y + sway * Math.sin(t * 0.45 + item.phase) * 0.35 * item.spin + tilt.x * 0.6;
            item.object.rotation.x = item.rest.x + sway * Math.sin(t * 0.35 + item.phase) * 0.15 + tilt.y * 0.35;
            if (item.update && !reduceMotion) item.update(t);
        });
        renderer.render(scene, camera);
    };

    layout();
    new ResizeObserver(() => {
        layout();
        if (reduceMotion) step(performance.now());
    }).observe(container);

    if (reduceMotion) step(performance.now());
    else animateWhileVisible(container, step);
}

// ----------------------------------------------------------------------------
if (webglAvailable && !saveData) {
    afterLoad(() => {
        const slider = document.querySelector('[data-slider]');
        if (slider && slider.querySelectorAll('.hp-gl-slide').length > 0) {
            whenNear(slider, () => initSlider(slider).catch((e) => console.warn('Slider WebGL disabled:', e)));
        }
        const clinic = document.querySelector('[data-clinic-3d]');
        if (clinic) {
            whenNear(clinic, () => initClinic(clinic).catch((e) => console.warn('3D scene disabled:', e)));
        }
    });
}
