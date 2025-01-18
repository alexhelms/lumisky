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

const panoDataProvider = (image, xmpData) => {
    const panoData = {
        fullWidth: image.width,
        fullHeight: image.height,
        croppedX: 0,
        croppedY: 0,
        croppedWidth: image.width,
        croppedHeight: image.height / 2,
        isEquirectangular: true,
    };
    return panoData;
};

export function createPanoViewer() {
    if (viewer === null) {
        viewer = new Viewer({
            container: document.querySelector('#pano-viewer'),
            adapter: EquirectangularAdapter,
            panoData: panoDataProvider,
            defaultZoomLvl: 0,
            defaultPitch: 0.75,
        });
        
        DOMCleanup.createObserver();
    }
}

export async function updatePanoViewer(url) {
    if (viewer === null) {
        createPanoViewer();
    }
    await viewer.setPanorama(url, {
        options: {
            showLoader: false,
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