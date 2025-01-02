import { Viewer } from '/lib/photo-sphere-viewer/core/index.module.js';
import { EquirectangularVideoAdapter } from '/lib/photo-sphere-viewer/equirectangular-video-adapter/index.module.js';
import { VideoPlugin } from '/lib/photo-sphere-viewer/video-plugin/index.module.js';

let videoViewer = null;

export class DOMCleanup {
    static observer;

    static createObserver() {
        const target = document.querySelector('#video-viewer');

        this.observer = new MutationObserver(function (mutations) {
            const targetRemoved = mutations.some(function (mutation) {
                const nodes = Array.from(mutation.removedNodes);
                return nodes.indexOf(target) !== -1;
            });

            if (targetRemoved) {
                // Cleanup
                destroyPanoVideoViewer();

                // Disconnect and delete MutationObserver
                this.observer && this.observer.disconnect();
                delete this.observer;
            }
        });

        this.observer.observe(target.parentNode, { childList: true });
    }
}

export function createPanoVideoViewer(url, width, height) {
    if (videoViewer === null) {
        videoViewer = new Viewer({
            container: document.querySelector('#video-viewer'),
            adapter: [EquirectangularVideoAdapter, {
                autoplay: true,
                muted: true,
            }],
            plugins: [VideoPlugin],
            panorama: {
                source: url,
                data: {
                    fullWidth: width,
                    fullHeight: height,
                    croppedWidth: width,
                    croppedHeight: height / 2
                }
            },
            defaultZoomLvl: 0,
            defaultPitch: 0.75,
        });

        DOMCleanup.createObserver();
    }
}

export function destroyPanoVideoViewer() {
    if (videoViewer !== null) {
        try {
            videoViewer.destroy();
        } catch { }
        videoViewer = null;
    }
}