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

// Timing settings
const baseMinSplashDuration = 6000; // Base time in ms for splash to show
const extraDelayAfterTyping = 8000; // Extra delay after typewriter finishes (ms)
const splashStartTime = Date.now();

// Flags for waiting conditions
let actualProgress = 0;
let modelLoaded = false;
let modelError = false;
let typewriterFinished = false;
let typewriterExtraDelayFinished = false;

// Check if user has visited before
const hasVisited = localStorage.getItem('hasVisited');
if (hasVisited) {
  hideSplash();
} else {
  showSplash();

  // Use localized message from global variable (set in _Layout.cshtml)
  const localizedSplashMessage = window.localizedSplashMessage || "Brew up your curiosity... The adventure begins now!";
  
  // Run typewriter animation (returns a promise when finished)
  typeAnimation(splashMessage, localizedSplashMessage, 100).then(() => {
    typewriterFinished = true;
    // After typewriter finishes, start extra delay timer
    setTimeout(() => {
      typewriterExtraDelayFinished = true;
    }, extraDelayAfterTyping);
  });

  // Start a timer to update the progress bar
  const progressTimer = setInterval(() => {
    // Update progress bar based on time relative to baseMinSplashDuration and loading progress
    const timeProgress = Math.min((Date.now() - splashStartTime) / baseMinSplashDuration, 1);
    const combinedProgress = Math.max(timeProgress, actualProgress);
    progressBarFill.style.width = (combinedProgress * 100) + '%';

    // Only end splash if model is loaded AND typewriter has finished AND extra delay has passed.
    if (modelLoaded && typewriterFinished && typewriterExtraDelayFinished) {
      clearInterval(progressTimer);
      endSplash();
      setTimeout(() => {
        window.location.replace(window.location.href);
        // window.location.reload(); 
      }, 5000);
    }
    if (modelError) {
      splashMessage.textContent = "Oops! We couldn't brew your coffee. Please check your connection or reload the page.";
      clearInterval(progressTimer);
    }
  }, 100);

  // Initialize Three.js scene (this updates modelLoaded/modelError)
  initThreeJS();
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
   TYPE ANIMATION (Promise-Based)
------------------------------ */
function typeAnimation(element, text, speed = 100) {
  return new Promise((resolve) => {
    let index = 0;
    element.textContent = "";
    const interval = setInterval(() => {
      if (index < text.length) {
        element.textContent += text.charAt(index);
        index++;
      } else {
        clearInterval(interval);
        resolve();
      }
    }, speed);
  });
}

/* ------------------------------
   THREE.JS INITIALIZATION
------------------------------ */
function initThreeJS() {
  // Create scene and renderer
  const scene = new THREE.Scene();
  const renderer = new THREE.WebGLRenderer({ 
    alpha: true, 
    antialias: window.innerWidth > 768, // Disable antialiasing on mobile for performance
    powerPreference: 'high-performance'
  });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // Limit pixel ratio for performance
  renderer.setSize(threeContainer.clientWidth, threeContainer.clientHeight);
  threeContainer.appendChild(renderer.domElement);

  // Setup environment using RoomEnvironment
  const environment = new RoomEnvironment();
  const pmremGenerator = new THREE.PMREMGenerator(renderer);
  scene.environment = pmremGenerator.fromScene(environment).texture;
  scene.background = new THREE.Color(0xbbbbbb);

  // Create camera with responsive FOV
  const isMobile = window.innerWidth <= 768;
  const fov = isMobile ? 60 : 50; // Wider FOV on mobile for better view
  const camera = new THREE.PerspectiveCamera(fov, threeContainer.clientWidth / threeContainer.clientHeight, 0.1, 1000);

  // Optional: OrbitControls for debugging
  const controls = new OrbitControls(camera, renderer.domElement);

  // Helpers for reference (grid and axes)
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

  // GLTFLoader configuration
  const loader = new GLTFLoader();
  loader.setPath('/models/');
  loader.setKTX2Loader(ktx2Loader);
  loader.setMeshoptDecoder(MeshoptDecoder);

  // Load the coffee mug model
  loader.load(
    'coffeemat.glb',
    (gltf) => {
      console.log("Model loaded successfully");
      modelLoaded = true;
      const model = gltf.scene;
      scene.add(model);

      // Center and scale the model (responsive for mobile)
      const box = new THREE.Box3().setFromObject(model);
      const center = box.getCenter(new THREE.Vector3());
      const size = box.getSize(new THREE.Vector3());
      const maxDim = Math.max(size.x, size.y, size.z);
      const isMobile = window.innerWidth <= 768;
      const desiredSize = isMobile ? 70 : 90; // Smaller on mobile
      const scaleFactor = desiredSize / maxDim;
      model.scale.multiplyScalar(scaleFactor);
      box.setFromObject(model);
      box.getCenter(center);
      model.position.sub(center);

      // Start scene animation
      animate(camera, center, renderer, scene);

      // Ensure splash remains for at least the base duration if model loads quickly.
      const elapsed = Date.now() - splashStartTime;
      if (elapsed < baseMinSplashDuration) {
        setTimeout(() => {
          // Don't end splash until extra delay after typewriter is complete as well
          if (modelLoaded && typewriterFinished && typewriterExtraDelayFinished) {
            endSplash();
          }
        }, baseMinSplashDuration - elapsed);
      } else {
        if (modelLoaded && typewriterFinished && typewriterExtraDelayFinished) {
          endSplash();
        }
      }
    },
    (xhr) => {
      if (xhr.lengthComputable) {
        actualProgress = xhr.loaded / xhr.total;
      }
    },
    (error) => {
      console.error("Error loading 3D model:", error);
      modelError = true;
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

// Animation loop for Three.js scene
function animate(camera, center, renderer, scene) {
  let angle = 0;
  const isMobile = window.innerWidth <= 768;
  const radius = isMobile ? 200 : 250; // Closer on mobile
  const cameraHeight = isMobile ? -10 : -20; // Less vertical offset on mobile
  function loop() {
    requestAnimationFrame(loop);
    angle += 0.005; // Adjust rotation speed as desired
    camera.position.x = center.x + radius * Math.cos(angle);
    camera.position.z = center.z + radius * Math.sin(angle);
    camera.position.y = center.y + cameraHeight;
    camera.lookAt(center);
    renderer.render(scene, camera);
  }
  loop();
}
