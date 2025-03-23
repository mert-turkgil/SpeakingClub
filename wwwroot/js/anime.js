import * as THREE from '/lib/three/build/three.module.js';
import { GLTFLoader } from '/lib/three/examples/jsm/loaders/GLTFLoader.js';
import { DRACOLoader } from '/lib/three/examples/jsm/loaders/DRACOLoader.js';

// Container and Scene setup
const canvas = document.getElementById('three-canvas');

const renderer = new THREE.WebGLRenderer({ 
    canvas, alpha: true, antialias: true 
});
renderer.setPixelRatio(window.devicePixelRatio);
renderer.shadowMap.enabled = true;

// Camera setup
const camera = new THREE.PerspectiveCamera(35, canvas.clientWidth / canvas.clientHeight, 0.1, 100);
camera.position.set(0, 7, 20);
camera.lookAt(0, 4, 2);

// Define the two target positions
const initialLookAt = new THREE.Vector3(0, 4, 2);
const houseLookAt = new THREE.Vector3(-5, 4, 6);
const currentLookAtTarget = new THREE.Vector3().copy(initialLookAt);

// Update function to interpolate lookAt based on scroll position
function updateCameraLookAt() {
    const threshold = 500; // Adjust as needed
    const scrollY = window.scrollY;
    const progress = THREE.MathUtils.clamp(scrollY / threshold, 0, 1);
    currentLookAtTarget.copy(initialLookAt).lerp(houseLookAt, progress);
}

// Listen to scroll events to update the target
window.addEventListener("scroll", updateCameraLookAt);

// Responsive resize function
const resize = () => {
    const w = canvas.clientWidth, h = canvas.clientHeight;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
};
window.addEventListener('resize', resize);
resize();

// Scene and fog
const scene = new THREE.Scene();
scene.fog = new THREE.Fog(0xFFF5E4, 10, 50);

// Lights setup
const ambient = new THREE.AmbientLight(0xffffff, 0.2);
scene.add(ambient);

const sunLight = new THREE.DirectionalLight(0xffffff, 1.2);
sunLight.castShadow = true;
scene.add(sunLight);

const moonLight = new THREE.DirectionalLight(0xaaaaff, 0.3);
moonLight.castShadow = true;
scene.add(moonLight);

// DRACOLoader & GLTFLoader setup
const dracoLoader = new DRACOLoader();
dracoLoader.setDecoderPath('/lib/three/examples/jsm/libs/draco/');
const loader = new GLTFLoader().setDRACOLoader(dracoLoader);

let house, sun, moon;

// Load forest house (with Draco)
loader.load('/models/forest_house.glb', (gltf) => {
    house = gltf.scene;
    house.castShadow = true;
    house.receiveShadow = true;
    house.scale.setScalar(2);
    scene.add(house);
}, undefined, console.error);

// Load sun and moon
loader.load('/models/sun.glb', (gltf) => {
    sun = gltf.scene;
    scene.add(sun);
}, undefined, console.error);

loader.load('/models/the_moon_sharp.glb', (gltf) => {
    moon = gltf.scene;
    scene.add(moon);
}, undefined, console.error);

// RGB reflection hourly
function rgbReflection() {
    const colors = [0xff0000, 0x00ff00, 0x0000ff];
    const color = colors[new Date().getHours() % 3];
    const rgbLight = new THREE.PointLight(color, 1.5, 20);
    rgbLight.position.set(0, 5, 0);
    scene.add(rgbLight);
    setTimeout(() => {
        scene.remove(rgbLight);
        rgbLight.dispose(); // Clean up the light
    }, 15000);
}
rgbReflection();
setInterval(rgbReflection, 3600000);

// Animation loop
const clock = new THREE.Clock();
function animate() {
    requestAnimationFrame(animate);
    resize();
    updateCameraLookAt(); // Update camera lookAt target
    camera.lookAt(currentLookAtTarget);

    if (sun && moon) {
        const hour = new Date().getHours() + new Date().getMinutes() / 60;
        const angle = (hour / 24) * Math.PI * 2;
        const radius = 25;

        sun.position.set(radius * Math.cos(angle), radius * Math.sin(angle), 0);
        moon.position.set(radius * Math.cos(angle + Math.PI), radius * Math.sin(angle + Math.PI), 0);

        sunLight.position.copy(sun.position);
        moonLight.position.copy(moon.position);

        // Fix light intensity calculation
        sunLight.intensity = Math.max(0, Math.sin(angle)) * 1.2; // Adjust intensity based on sun position
        moonLight.intensity = Math.max(0, Math.sin(angle + Math.PI)) * 0.3; // Adjust intensity based on moon position

        const dayFactor = THREE.MathUtils.clamp(sun.position.y / radius, 0, 1);
        const skyColor = new THREE.Color(0xFFF5E4).lerp(new THREE.Color(0x000022), 1 - dayFactor);
        scene.fog.color.copy(skyColor);

        const isikContainer = document.querySelector('#Isik');
        if (isikContainer) {
            isikContainer.style.background = `linear-gradient(to top, #FFF5E4, ${skyColor.getStyle()})`;
        }
    }

    renderer.render(scene, camera);
}

animate();