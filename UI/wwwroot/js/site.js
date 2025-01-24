// Import ResizeObserver from the installed polyfill
import ResizeObserver from 'resize-observer-polyfill';

// Function to observe and resize a single element
function observeAndResizeElement(element, aspectRatio = 16 / 9, maxHeightPercentage = 0.75) {
    const resizeObserver = new ResizeObserver(entries => {
        entries.forEach(entry => {
            const width = entry.contentRect.width;
            const desiredHeight = Math.min(window.innerHeight * maxHeightPercentage, width / aspectRatio);
            element.style.height = `${desiredHeight}px`;
        });
    });

    resizeObserver.observe(element);
}

// Observe the main carousel
const carousel = document.querySelector("#myCarousel");
if (carousel) {
    observeAndResizeElement(carousel);
}

// Observe specific nested divs by their unique IDs
const uniqueDivs = ["carousel-caption-1", "carousel-caption-2", "carousel-caption-3"];
uniqueDivs.forEach(id => {
    const element = document.querySelector(`#${id}`);
    if (element) {
        observeAndResizeElement(element, 4 / 3, 0.5); // Example: 4:3 aspect ratio, 50% viewport height
    }
});
