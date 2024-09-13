import { Viewer } from '/lib/photo-sphere-viewer/core/index.module.js';
import { EquirectangularVideoAdapter } from '/lib/photo-sphere-viewer/equirectangular-video-adapter/index.module.js';
import { VideoPlugin } from '/lib/photo-sphere-viewer/video-plugin/index.module.js';

let videoViewer = null;

export function createPanoVideoViewer(url) {
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
            },
            defaultZoomLvl: 0,
            defaultPitch: 0.75,
        });
    }
}

export function destroyPanoVideoViewer() {
    if (videoViewer !== null) {
        videoViewer.destroy();
        videoViewer = null;
    }
}