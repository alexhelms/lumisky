import '/lib/video-js/dist/video.min.js';

const playerId = 'player';

export class DOMCleanup {
    static observer;

    static createObserver() {
        const target = document.querySelector(playerId);

        this.observer = new MutationObserver(function (mutations) {
            const targetRemoved = mutations.some(function (mutation) {
                const nodes = Array.from(mutation.removedNodes);
                return nodes.indexOf(target) !== -1;
            });

            if (targetRemoved) {
                // Cleanup
                disposePlayer(playerId);

                // Disconnect and delete MutationObserver
                this.observer && this.observer.disconnect();
                delete this.observer;
            }
        });

        this.observer.observe(target.parentNode, { childList: true });
    }
}

export function loadPlayer(videoUrl) {
    disposePlayer(playerId)
    videojs(playerId, {
        autoplay: true,
        controls: true,
        loop: true,
        preload: 'auto',
        playbackRates: [0.5, 1, 1.5, 2],
        sources: [{
            src: videoUrl,
            type: 'video/mp4',
        }],
    });
}

export function disposePlayer(id) {
    const player = videojs.getPlayer(id);
    if (player) {
        player.dispose();
    }
}