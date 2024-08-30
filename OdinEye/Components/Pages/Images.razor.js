import { Viewer, EquirectangularAdapter } from '@photo-sphere-viewer/core';

let viewer = null;

export function createPanoViewer() {
    if (viewer === null) {
        viewer = new Viewer({
            container: document.querySelector('#pano-viewer'),
            adapter: [EquirectangularAdapter, {
                backgroundColor: '#000000'
            }],
            defaultZoomLvl: 0
        });
    }
}

export async function updatePanoViewer(url, width, height) {
    if (viewer !== null) {
        await viewer.setPanorama(url, {
            panoData: {
                fullWidth: width,
                fullHeight: height,
                croppedWidth: width,
                croppedHeight: height / 2
            }
        });
    }
}

export function destroyPanoViewer() {
    if (viewer !== null) {
        viewer.destroy();
        viewer = null;
    }
}