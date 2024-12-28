import { Viewer, EquirectangularAdapter } from '/lib/photo-sphere-viewer/core/index.module.js';

let viewer = null;

export function createPanoViewer() {
    if (viewer === null) {
        viewer = new Viewer({
            container: document.querySelector('#pano-viewer'),
            adapter: [EquirectangularAdapter, {
                backgroundColor: '#000000'
            }],
            defaultZoomLvl: 0,
            defaultPitch: 0.75,
        });
    }
}

export async function updatePanoViewer(url, width, height) {
    if (viewer !== null) {
        try {
            await viewer.setPanorama(url, {
                panoData: {
                    fullWidth: width,
                    fullHeight: height,
                    croppedWidth: width,
                    croppedHeight: height / 2
                },
            });
        } catch (error) {
            console.error(error);
        }
    }
}

export function destroyPanoViewer() {
    if (viewer !== null) {
        viewer.destroy();
        viewer = null;
    }
}