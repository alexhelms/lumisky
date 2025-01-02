import { Viewer, EquirectangularAdapter } from '/lib/photo-sphere-viewer/core/index.module.js';

let viewer = null;

export class DOMCleanup {
    static observer;

    static createObserver() {
        const target = document.querySelector('#pano-viewer');

        this.observer = new MutationObserver(function (mutations) {
            const targetRemoved = mutations.some(function (mutation) {
                const nodes = Array.from(mutation.removedNodes);
                return nodes.indexOf(target) !== -1;
            });

            if (targetRemoved) {
                // Cleanup
                destroyPanoViewer();

                // Disconnect and delete MutationObserver
                this.observer && this.observer.disconnect();
                delete this.observer;
            }
        });

        this.observer.observe(target.parentNode, { childList: true });
    }
}

export function createPanoViewer() {
    if (viewer === null) {
        viewer = new Viewer({
            container: document.querySelector('#pano-viewer'),
            adapter: EquirectangularAdapter,
            defaultZoomLvl: 0,
            defaultPitch: 0.75,
        });
        
        DOMCleanup.createObserver();
    }
}

export async function updatePanoViewer(url, width, height) {
    if (viewer === null) {
        createPanoViewer();
    }
    await viewer.setPanorama(url, {
        panoData: {
            fullWidth: width,
            fullHeight: height,
            croppedWidth: width,
            croppedHeight: height / 2
        },
    });
}

export function destroyPanoViewer() {
    if (viewer !== null) {
        try {
            viewer.destroy();
        } catch { }
        viewer = null;
    }
}