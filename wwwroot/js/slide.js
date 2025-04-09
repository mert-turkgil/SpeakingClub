document.addEventListener("DOMContentLoaded", function () {
    // Get the number of slides
    const slideCount = document.querySelectorAll('.alpha-carousel .swiper-slide').length;

    const swiper = new Swiper('.alpha-carousel', {
        loop: slideCount >= 3, // Only enable loop if there are 3 or more slides
        effect: 'fade',
        autoplay: {
            delay: 8000,
            disableOnInteraction: false,
        },
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
        },
        pagination: {
            el: '.swiper-pagination',
            clickable: true,
            renderBullet: function (index, className) {
                const imagePath = window.carouselImages[index];
                return `<span class="${className}" style="background-image:url('${imagePath}')"></span>`;
            }
        },
        on: {
            init: animateSlide,
            slideChangeTransitionStart: resetAnimations,
            slideChangeTransitionEnd: animateSlide
        }
    });

    function resetAnimations() {
        gsap.set('.slide-title, .slide-description, .slide-button', { opacity: 0, y: 50 });
    }

    function animateSlide() {
        gsap.timeline()
            .fromTo('.swiper-slide-active .slide-title', 
                { y: 50, opacity: 0 }, 
                { y: 0, opacity: 1, duration: 1, ease: "power3.out" })
            .fromTo('.swiper-slide-active .slide-description', 
                { y: 50, opacity: 0 }, 
                { y: 0, opacity: 1, duration: 1, delay: -0.6, ease: "power3.out" })
            .fromTo('.swiper-slide-active .slide-button', 
                { y: 50, opacity: 0 }, 
                { y: 0, opacity: 1, duration: 1, delay: -0.6, ease: "power3.out" });
    }
});
