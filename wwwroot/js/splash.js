// splash.js

import * as THREE from '/lib/three/build/three.module.js';
import { RoomEnvironment } from '/lib/three/examples/jsm/environments/RoomEnvironment.js';
import { OrbitControls } from '/lib/three/examples/jsm/controls/OrbitControls.js';
import { GLTFLoader } from '/lib/three/examples/jsm/loaders/GLTFLoader.js';
import { KTX2Loader } from '/lib/three/examples/jsm/loaders/KTX2Loader.js';
import { MeshoptDecoder } from '/lib/three/examples/jsm/libs/meshopt_decoder.module.js';

// DOM elements
const splashScreen = document.getElementById('splashScreen');
const mainContent = document.getElementById('mainContent');
const progressBarFill = document.getElementById('progressBarFill');
const threeContainer = document.getElementById('threeContainer');
const splashMessage = document.getElementById('splashMessage');

// Minimum time (ms) to keep splash
const minSplashDuration = 6000;
const splashStartTime = Date.now();

// Track loading progress
let actualProgress = 0;

// Check if user has visited before
const hasVisited = localStorage.getItem('hasVisited');
if (hasVisited) {
  hideSplash();
} else {
  showSplash();

  // For first-time visitors, attempt to play audio:
    // Localized splash message
    const localizedSplashMessage = window.localizedSplashMessage || "Grab Your Coffee For Learning";
    typeAnimation(splashMessage, localizedSplashMessage, 100);
    function typeAnimation(element, text, speed = 100) {
      let index = 0;
      element.textContent = "";
      const interval = setInterval(() => {
        if (index < text.length) {
          element.textContent += text.charAt(index);
          index++;
        } else {
          clearInterval(interval);
        }
      }, speed);
    }
    
    

  // Start a timer to update the progress bar
  const progressTimer = setInterval(() => {
    const timeProgress = Math.min((Date.now() - splashStartTime) / minSplashDuration, 1);
    const combinedProgress = Math.max(timeProgress, actualProgress);
    progressBarFill.style.width = (combinedProgress * 100) + '%';
  }, 100);

  // Initialize Three.js scene
  initThreeJS(progressTimer);
}

/* ------------------------------
   SPLASH SCREEN DISPLAY LOGIC
------------------------------ */
function showSplash() {
  splashScreen.style.display = 'flex';
  mainContent.style.display = 'none';
}

function hideSplash() {
  splashScreen.style.display = 'none';
  mainContent.style.display = 'block';
}

function endSplash() {

  hideSplash();
  localStorage.setItem('hasVisited', 'true');
}

/* ------------------------------
   SOUND TOGGLE HANDLER
------------------------------ */


/* ------------------------------
   THREE.JS INITIALIZATION
------------------------------ */
function initThreeJS(progressTimer) {

  // Create scene and renderer
  const scene = new THREE.Scene();
  const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
  renderer.setSize(threeContainer.clientWidth, threeContainer.clientHeight);
  threeContainer.appendChild(renderer.domElement);

  // Setup environment using RoomEnvironment
  const environment = new RoomEnvironment();
  const pmremGenerator = new THREE.PMREMGenerator(renderer);
  scene.environment = pmremGenerator.fromScene(environment).texture;
  scene.background = new THREE.Color(0xbbbbbb);

  // Create camera
  const camera = new THREE.PerspectiveCamera(50, threeContainer.clientWidth / threeContainer.clientHeight, 0.1, 1000);
  // We’ll reposition the camera after loading the model

  // Add OrbitControls for debugging (optional)
  const controls = new OrbitControls(camera, renderer.domElement);

  // Helpers (grid + axes) for reference
  const gridHelper = new THREE.GridHelper(10, 10);
  scene.add(gridHelper);
  const axesHelper = new THREE.AxesHelper(5);
  scene.add(axesHelper);

  // Basic lighting
  const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
  scene.add(ambientLight);
  const dirLight = new THREE.DirectionalLight(0xffffff, 0.6);
  dirLight.position.set(5, 10, 7.5);
  scene.add(dirLight);

  // Setup decoders for compressed textures
  const ktx2Loader = new KTX2Loader().setTranscoderPath('/lib/three/examples/jsm/libs/basis/').detectSupport(renderer);

  // GLTFLoader
  const loader = new GLTFLoader();
  loader.setPath('/models/');
  loader.setKTX2Loader(ktx2Loader);
  loader.setMeshoptDecoder(MeshoptDecoder);

  // Load the coffee mug model
  loader.load(
    'coffeemat.glb',
    (gltf) => {
      console.log("Model loaded successfully");
      const model = gltf.scene;
      scene.add(model);

      // Compute bounding box to center + scale the model
      const box = new THREE.Box3().setFromObject(model);
      const center = box.getCenter(new THREE.Vector3());
      const size = box.getSize(new THREE.Vector3());
      const maxDim = Math.max(size.x, size.y, size.z);

      // Scale the model to a desired size
      const desiredSize = 90;
      const scaleFactor = desiredSize / maxDim;
      model.scale.multiplyScalar(scaleFactor);

      // Recompute bounding box after scaling
      box.setFromObject(model);
      box.getCenter(center);

      // Center the model at origin
      model.position.sub(center);

      // Now, we revolve the CAMERA around the mug in the animation loop
      let angle = 0;
      const radius = 250; // distance from the mug center
      const cameraHeight = -20; // how high above the mug center the camera is
      // Set an initial vantage
      camera.position.set(center.x + radius, center.y + cameraHeight, center.z);

      function animate() {
        requestAnimationFrame(animate);

        // Increase angle each frame to revolve the camera around the mug
        angle += 0.005; // adjust rotation speed as desired

        // Move camera in a circle on the XZ plane
        camera.position.x = center.x + radius * Math.cos(angle);
        camera.position.z = center.z + radius * Math.sin(angle);
        camera.position.y = center.y + cameraHeight;

        // Always look at the mug
        camera.lookAt(center);

        renderer.render(scene, camera);
      }
      animate();

      // Keep splash for at least minSplashDuration
      const elapsed = Date.now() - splashStartTime;
      if (elapsed < minSplashDuration) {
        const remainingTime = minSplashDuration - elapsed;
        setTimeout(() => {
          clearInterval(progressTimer);
          endSplash();
        }, remainingTime);
      } else {
        clearInterval(progressTimer);
        endSplash();
      }
    },
    (xhr) => {
      if (xhr.lengthComputable) {
        actualProgress = xhr.loaded / xhr.total;
      }
    },
    (error) => {
      console.error("Error loading 3D model:", error);
    }
  );

  // Handle window resizing
  window.addEventListener('resize', onWindowResize);
  function onWindowResize() {
    camera.aspect = threeContainer.clientWidth / threeContainer.clientHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(threeContainer.clientWidth, threeContainer.clientHeight);
  }
}
