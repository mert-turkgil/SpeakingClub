import * as THREE from '/lib/three/build/three.module.js';
import { GLTFLoader } from '/lib/three/examples/jsm/loaders/GLTFLoader.js';
import { DRACOLoader } from '/lib/three/examples/jsm/loaders/DRACOLoader.js';
import { OrbitControls } from '/lib/three/examples/jsm/controls/OrbitControls.js';
import { gsap } from 'https://cdn.skypack.dev/gsap';
import { GUI } from 'https://cdn.jsdelivr.net/npm/dat.gui@0.7.9/build/dat.gui.module.js';

/* ===============================
   SETUP: Renderer, Scene & Camera
=============================== */
const canvas = document.getElementById('three-canvas');
const overlayText = document.getElementById('overlayText');

const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
renderer.setSize(canvas.clientWidth, canvas.clientHeight);
renderer.shadowMap.enabled = true;

const scene = new THREE.Scene();
scene.fog = new THREE.Fog(0xFFF5E4, 20, 2000);

const camera = new THREE.PerspectiveCamera(
  50,
  canvas.clientWidth / canvas.clientHeight,
  0.1,
  2000
);
camera.position.set(0, 50, 200);
camera.lookAt(new THREE.Vector3(0, 0, 0));

const controls = new OrbitControls(camera, renderer.domElement);
controls.enabled = false; // Disable mouse controls—GUI only
controls.target.set(0, 0, 0);
controls.update();

/* ===============================
   SKY GRADIENT (Custom Shader)
=============================== */
const vertexShader = `
  varying vec3 vWorldPosition;
  void main() {
    vec4 worldPosition = modelMatrix * vec4(position, 1.0);
    vWorldPosition = worldPosition.xyz;
    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  }
`;

const fragmentShader = `
  uniform vec3 topColor;
  uniform vec3 bottomColor;
  uniform float offset;
  uniform float exponent;
  varying vec3 vWorldPosition;
  void main() {
    float h = normalize(vWorldPosition + offset).y;
    gl_FragColor = vec4(mix(bottomColor, topColor, pow(max(h, 0.0), exponent)), 1.0);
  }
`;

const uniforms = {
  topColor: { value: new THREE.Color(0x87CEEB) },
  bottomColor: { value: new THREE.Color(0xFFF5E4) },
  offset: { value: 33 },
  exponent: { value: 0.6 }
};
scene.fog.color.copy(uniforms.bottomColor.value);

const skyGeo = new THREE.SphereGeometry(4000, 32, 15);
const skyMat = new THREE.ShaderMaterial({
  uniforms: uniforms,
  vertexShader: vertexShader,
  fragmentShader: fragmentShader,
  side: THREE.BackSide
});
const sky = new THREE.Mesh(skyGeo, skyMat);
scene.add(sky);

/* ===============================
   LOAD HOUSE MODEL
=============================== */
const dracoLoader = new DRACOLoader();
dracoLoader.setDecoderPath('/lib/three/examples/jsm/libs/draco/');
const gltfLoader = new GLTFLoader();
gltfLoader.setDRACOLoader(dracoLoader);
gltfLoader.setPath('/models/');
gltfLoader.load('forest_house.glb', (gltf) => {
  const house = gltf.scene;
  house.scale.set(4, 4, 4);
  house.traverse((child) => {
    if (child.isMesh) {
      child.castShadow = true;
      child.receiveShadow = true;
    }
  });
  house.position.set(0, 40, 175);
  scene.add(house);
}, undefined, (error) => {
  console.error('Error loading house model:', error);
});

/* ===============================
   SUN & MOON LIGHTS & VISUALS
=============================== */
const celestialRadius = 600;

const sunLight = new THREE.DirectionalLight(0xffffff, 1.0);
sunLight.castShadow = true;
scene.add(sunLight);

const moonLight = new THREE.DirectionalLight(0x8888ff, 0.5);
moonLight.castShadow = true;
scene.add(moonLight);

const sunSphere = new THREE.Mesh(
  new THREE.SphereGeometry(30, 16, 16),
  new THREE.MeshBasicMaterial({ color: 0xF39C12 })
);
scene.add(sunSphere);

const moonSphere = new THREE.Mesh(
  new THREE.SphereGeometry(20, 16, 16),
  new THREE.MeshBasicMaterial({ color: 0x34495E })
);
scene.add(moonSphere);

/* ===============================
   DAY-NIGHT CYCLE & SKY UPDATE
=============================== */
function updateCelestialPositions() {
  const now = new Date();
  const hours = now.getHours() + now.getMinutes() / 60;
  
  if (hours >= 6 && hours < 18) {
    const dayPhase = (hours - 6) / 12;
    const angle = dayPhase * Math.PI;
    const sunX = celestialRadius * Math.cos(angle);
    const sunY = celestialRadius * Math.sin(angle);
    sunLight.position.set(sunX, sunY, 0);
    sunLight.intensity = 1;
    sunSphere.position.set(sunX, sunY, 0);
    sunSphere.visible = true;
    
    moonLight.position.set(0, -celestialRadius, 0);
    moonLight.intensity = 0;
    moonSphere.visible = false;
    
    let topColor;
    const dawn = new THREE.Color(0xFAD6A5);
    const midday = new THREE.Color(0x87CEEB);
    if (dayPhase <= 0.5) {
      topColor = dawn.clone().lerp(midday, dayPhase / 0.5);
    } else {
      topColor = midday.clone().lerp(dawn, (dayPhase - 0.5) / 0.5);
    }
    uniforms.topColor.value.copy(topColor);
    
  } else {
    sunLight.intensity = 0;
    sunSphere.visible = false;
    
    moonLight.position.set(0, celestialRadius, 0);
    moonLight.intensity = 0.6;
    moonSphere.position.set(0, celestialRadius, 0);
    moonSphere.visible = true;
    
    uniforms.topColor.value.set(0x0b0b2f);
  }
}
updateCelestialPositions();
setInterval(updateCelestialPositions, 60000);

/* ===============================
   ANIMATION LOOP & RESPONSIVENESS
=============================== */
function animate() {
  requestAnimationFrame(animate);
  renderer.render(scene, camera);
}
animate();

window.addEventListener('resize', () => {
  const width = canvas.clientWidth;
  const height = canvas.clientHeight;
  renderer.setSize(width, height, false);
  camera.aspect = width / height;
  camera.updateProjectionMatrix();
});

/* ===============================
   CAMERA CONTROLS GUI (dat.GUI) INSIDE HERO SECTION
=============================== */
// Create the dat.GUI panel and append it to the container with id "Isik"
const gui = new GUI({ autoPlace: false });
const guiContainer = document.getElementById('Isik');
guiContainer.appendChild(gui.domElement);

// Style the GUI panel so it appears at top-right of the container.
gui.domElement.style.position = 'absolute';
gui.domElement.style.top = '10px';
gui.domElement.style.right = '10px';
gui.domElement.style.zIndex = '99999';
gui.domElement.style.width = '200px';
gui.domElement.style.maxHeight = '70vh';
gui.domElement.style.overflowY = 'auto';

const cameraFolder = gui.addFolder('Camera');
const cameraSettings = {
  posX: camera.position.x,
  posY: camera.position.y,
  posZ: camera.position.z,
  rotX: THREE.MathUtils.radToDeg(camera.rotation.x),
  rotY: THREE.MathUtils.radToDeg(camera.rotation.y),
  rotZ: THREE.MathUtils.radToDeg(camera.rotation.z)
};
cameraFolder.add(cameraSettings, 'posX', -1000, 1000, 1).name('Pos X').onChange(val => {
  camera.position.x = val;
});
cameraFolder.add(cameraSettings, 'posY', -1000, 1000, 1).name('Pos Y').onChange(val => {
  camera.position.y = val;
});
cameraFolder.add(cameraSettings, 'posZ', 10, 1000, 1).name('Pos Z').onChange(val => {
  camera.position.z = val;
});
cameraFolder.add(cameraSettings, 'rotX', -180, 180, 1).name('Rot X').onChange(val => {
  camera.rotation.x = THREE.MathUtils.degToRad(val);
});
cameraFolder.add(cameraSettings, 'rotY', -180, 180, 1).name('Rot Y').onChange(val => {
  camera.rotation.y = THREE.MathUtils.degToRad(val);
});
cameraFolder.add(cameraSettings, 'rotZ', -180, 180, 1).name('Rot Z').onChange(val => {
  camera.rotation.z = THREE.MathUtils.degToRad(val);
});
cameraFolder.open();

/* ===============================
   DOUBLE-CLICK: MOVE CAMERA CLOSER
=============================== */
canvas.addEventListener('dblclick', () => {
  gsap.to(camera.position, {
    duration: 1,
    x: 0,
    y: 50,
    z: 120,
    ease: "power2.out",
    onUpdate: () => {
      camera.updateProjectionMatrix();
    }
  });
});

/* ===============================
   SCROLL-TRIGGERED OVERLAY TEXT ANIMATION
=============================== */
window.addEventListener('scroll', () => {
  if (window.scrollY > 100) {
    gsap.to(overlayText, {
      duration: 1,
      left: "50%",
      top: "50%",
      transform: "translate(-50%, -50%)",
      opacity: 1,
      ease: "power3.out"
    });
    // Keep the original innerHTML intact.
  } else {
    gsap.to(overlayText, {
      duration: 0.5,
      left: "0",
      top: "50%",
      transform: "translate(0, -50%)",
      opacity: 0.5,
      ease: "power3.in"
    });
  }
});
