// --- Custom draggable scrollbar (modulestrip) ---
window.initModulesScrollbar = (stripSelector, thumbSelector) => {
    const strip = document.querySelector(stripSelector);
    const thumb = document.querySelector(thumbSelector);

    if (!strip || !thumb) {
        console.warn("Scrollbar init failed", { strip, thumb });
        return;
    }

    const track = thumb.parentElement;

    function updateThumb() {
        const trackMax = track.offsetWidth - thumb.offsetWidth;
        const scrollMax = strip.scrollWidth - strip.clientWidth;

        if (scrollMax <= 0) {
            thumb.style.left = "0px";
            return;
        }

        const ratio = strip.scrollLeft / scrollMax;
        thumb.style.left = (ratio * trackMax) + "px";
    }

    strip.addEventListener("scroll", updateThumb);

    // thumb dragging
    let isDragging = false;
    let startX = 0;
    let startLeft = 0;

    thumb.addEventListener("mousedown", (e) => {
        isDragging = true;
        startX = e.clientX;
        startLeft = parseFloat(thumb.style.left) || 0;
        document.body.style.userSelect = "none";
    });

    document.addEventListener("mousemove", (e) => {
        if (!isDragging) return;

        const dx = e.clientX - startX;
        const trackMax = track.offsetWidth - thumb.offsetWidth;
        let newLeft = Math.min(Math.max(startLeft + dx, 0), trackMax);
        thumb.style.left = newLeft + "px";

        const ratio = newLeft / trackMax;
        strip.scrollLeft = ratio * (strip.scrollWidth - strip.clientWidth);
    });

    document.addEventListener("mouseup", () => {
        if (isDragging) {
            isDragging = false;
            document.body.style.userSelect = "";
        }
    });

    updateThumb();
};

// --- Carousel (modulestrip) ---
window.scrollStrip = (selector, direction) => {
    const container = document.querySelector(selector);
    if (!container) return;

    const cards = Array.from(container.querySelectorAll(".module-card"));
    if (!cards.length) return;

    const cardWidth = cards[0].offsetWidth + 24;
    const currentIndex = Math.round(container.scrollLeft / cardWidth);

    let newIndex = currentIndex + direction;
    newIndex = Math.max(0, Math.min(newIndex, cards.length - 1));

    container.scrollTo({
        left: newIndex * cardWidth,
        behavior: "smooth"
    });

    cards.forEach((c, i) => c.classList.toggle("active", i === newIndex));
};

// --- Carousel snap (modulestrip) ---
window.initCarouselSnap = (selector) => {
    const container = document.querySelector(selector);
    if (!container) return;

    let isScrolling;
    container.addEventListener("scroll", () => {
        clearTimeout(isScrolling);
        isScrolling = setTimeout(() => {
            const cards = Array.from(container.querySelectorAll(".module-card"));
            if (!cards.length) return;

            const cardWidth = cards[0].offsetWidth + 24;
            const index = Math.round(container.scrollLeft / cardWidth);

            container.scrollTo({
                left: index * cardWidth,
                behavior: "smooth"
            });

            cards.forEach((c, i) => c.classList.toggle("active", i === index));
        }, 120);
    });
};

// --- Scroll newly added module into center ---
window.scrollModuleIntoCenter = (stripSelector, index) => {
    const strip = document.querySelector(stripSelector);
    if (!strip) return;

    const cards = Array.from(strip.querySelectorAll(".module-card"));
    if (!cards.length || index < 0 || index >= cards.length) return;

    const card = cards[index];
    const stripRect = strip.getBoundingClientRect();
    const cardRect = card.getBoundingClientRect();

    const offset = (cardRect.left + cardRect.width / 2) - (stripRect.left + stripRect.width / 2);

    strip.scrollBy({
        left: offset,
        behavior: "smooth"
    });

    cards.forEach((c, i) => c.classList.toggle("active", i === index));
};

// --- Custom draggable scrollbar (navstrip) ---
window.initNavstripScrollbar = (stripSelector, thumbSelector) => {
    const strip = document.querySelector(stripSelector);
    const thumb = document.querySelector(thumbSelector);
    if (!strip || !thumb) return;

    const track = thumb.parentElement;

    function updateThumb() {
        const trackMax = track.offsetWidth - thumb.offsetWidth;
        const scrollMax = strip.scrollWidth - strip.clientWidth;
        if (scrollMax <= 0) {
            thumb.style.left = "0px";
            return;
        }
        const ratio = strip.scrollLeft / scrollMax;
        thumb.style.left = (ratio * trackMax) + "px";
    }

    strip.addEventListener("scroll", updateThumb);
    window.addEventListener("resize", updateThumb);

    let isDragging = false;
    let startX = 0;
    let startLeft = 0;

    thumb.addEventListener("mousedown", (e) => {
        isDragging = true;
        startX = e.clientX;
        startLeft = parseFloat(thumb.style.left) || 0;
        document.body.style.userSelect = "none";
    });

    document.addEventListener("mousemove", (e) => {
        if (!isDragging) return;
        const dx = e.clientX - startX;
        const trackMax = track.offsetWidth - thumb.offsetWidth;
        let newLeft = Math.min(Math.max(startLeft + dx, 0), trackMax);
        thumb.style.left = newLeft + "px";

        const ratio = newLeft / trackMax;
        strip.scrollLeft = ratio * (strip.scrollWidth - strip.clientWidth);
    });

    document.addEventListener("mouseup", () => {
        if (isDragging) {
            isDragging = false;
            document.body.style.userSelect = "";
        }
    });

    // draggable strip
    let isStripDragging = false;
    let stripStartX, stripScrollLeft;

    strip.addEventListener("mousedown", (e) => {
        isStripDragging = true;
        strip.classList.add("dragging");
        stripStartX = e.pageX - strip.offsetLeft;
        stripScrollLeft = strip.scrollLeft;
    });

    strip.addEventListener("mouseleave", () => {
        isStripDragging = false;
        strip.classList.remove("dragging");
    });

    strip.addEventListener("mouseup", () => {
        isStripDragging = false;
        strip.classList.remove("dragging");
    });

    strip.addEventListener("mousemove", (e) => {
        if (!isStripDragging) return;
        e.preventDefault();
        const x = e.pageX - strip.offsetLeft;
        const walk = (x - stripStartX) * 1.2;
        strip.scrollLeft = stripScrollLeft - walk;
    });

    updateThumb();
};

// --- Center clicked course in navstrip ---
window.scrollCourseIntoCenter = (stripSelector, elementId) => {
    const strip = document.querySelector(stripSelector);
    const element = document.getElementById(elementId);
    if (!strip || !element) return;

    const stripRect = strip.getBoundingClientRect();
    const elRect = element.getBoundingClientRect();

    const offset = (elRect.left + elRect.width / 2) - (stripRect.left + stripRect.width / 2);

    strip.scrollBy({
        left: offset,
        behavior: "smooth"
    });
};