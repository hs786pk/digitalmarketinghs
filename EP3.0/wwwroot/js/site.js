window.earnPlatform = {
    openAd: function (url) {
        const videoId = extractYouTubeId(url);
        const isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);
        const isAndroid = /Android/i.test(navigator.userAgent);
        const isMobile = isIOS || isAndroid;

        if (!videoId || !isMobile) {
            window.open(url, '_blank', 'noopener');
            return;
        }

        const webUrl = `https://www.youtube.com/watch?v=${videoId}`;

        let fallbackTimer = setTimeout(function () {
            window.open(webUrl, '_blank', 'noopener');
        }, 1500);

        const cancelFallback = function () {
            clearTimeout(fallbackTimer);
            window.removeEventListener('blur', cancelFallback);
            document.removeEventListener('visibilitychange', cancelFallback);
        };
        window.addEventListener('blur', cancelFallback);
        document.addEventListener('visibilitychange', cancelFallback);

        if (isAndroid) {
            window.location.href =
                `intent://www.youtube.com/watch?v=${videoId}#Intent;package=com.google.android.youtube;scheme=https;end`;
        } else if (isIOS) {
            window.location.href = `youtube://www.youtube.com/watch?v=${videoId}`;
        }
    },

    // ---------- Top loading bar ----------
    // Called from JS (on link click) to show, and from C# (via NavigationManager.LocationChanged,
    // see NavProgressWatcher.razor) to hide once the new page has actually rendered.
    showNavProgressBar: function () {
        const bar = document.querySelector('.nav-progress-bar');
        if (!bar) return;
        navProgressShownAt = Date.now();
        bar.classList.add('loading');
    },

    hideNavProgressBar: function () {
        const bar = document.querySelector('.nav-progress-bar');
        if (!bar) return;

        const elapsed = Date.now() - navProgressShownAt;
        const remaining = MIN_VISIBLE_MS - elapsed;

        if (remaining > 0) {
            setTimeout(function () {
                bar.classList.remove('loading');
            }, remaining);
        } else {
            bar.classList.remove('loading');
        }
    }
};

function extractYouTubeId(url) {
    const match = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})/);
    return match ? match[1] : null;
}

const MIN_VISIBLE_MS = 350;
let navProgressShownAt = 0;

// Show the bar the instant any in-app link is clicked or a form is submitted —
// this covers the gap before Blazor/the server has even started processing
// the navigation.
document.addEventListener('click', function (e) {
    const link = e.target.closest('a[href]');
    if (!link) return;

    const href = link.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('http') || link.target === '_blank') return;

    window.earnPlatform.showNavProgressBar();
});

document.addEventListener('submit', function () {
    window.earnPlatform.showNavProgressBar();
});

// Full page load (covers static SSR pages like Login/Signup, and browser refresh).
window.addEventListener('load', function () {
    window.earnPlatform.hideNavProgressBar();
});