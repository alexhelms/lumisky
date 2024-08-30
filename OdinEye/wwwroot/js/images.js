import { Viewer } from '@photo-sphere-viewer/core';

export default function createPanoViewer(target) {
    const viewer = new Viewer({
        container: target,
        panorama: 'https://photo-sphere-viewer-data.netlify.app/assets/sphere.jpg',
    });
}