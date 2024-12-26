/*!
 * Photo Sphere Viewer / Equirectangular Video Adapter 5.11.4
 * @copyright 2015-2024 Damien "Mistic" Sorel
 * @licence MIT (https://opensource.org/licenses/MIT)
 */

// src/EquirectangularVideoAdapter.ts
import { EquirectangularAdapter, utils } from "@photo-sphere-viewer/core";

// ../shared/AbstractVideoAdapter.ts
import { AbstractAdapter, PSVError } from "@photo-sphere-viewer/core";
import { VideoTexture } from "three";

// ../shared/video-utils.ts
function createVideo({
  src,
  withCredentials,
  muted,
  autoplay
}) {
  const video = document.createElement("video");
  video.crossOrigin = withCredentials ? "use-credentials" : "anonymous";
  video.loop = true;
  video.playsInline = true;
  video.autoplay = autoplay;
  video.muted = muted;
  video.preload = "metadata";
  if (src instanceof MediaStream) {
    video.srcObject = src;
  } else {
    video.src = src;
  }
  return video;
}

// ../shared/AbstractVideoAdapter.ts
var AbstractVideoAdapter = class extends AbstractAdapter {
  constructor(viewer) {
    super(viewer);
  }
  init() {
    super.init();
    this.viewer.needsContinuousUpdate(true);
  }
  destroy() {
    this.__removeVideo();
    super.destroy();
  }
  supportsPreload() {
    return false;
  }
  supportsTransition() {
    return false;
  }
  async loadTexture(panorama) {
    if (typeof panorama !== "object" || !panorama.source) {
      return Promise.reject(new PSVError("Invalid panorama configuration, are you using the right adapter?"));
    }
    if (!this.viewer.getPlugin("video")) {
      return Promise.reject(new PSVError("Video adapters require VideoPlugin to be loaded too."));
    }
    const video = panorama.source instanceof HTMLVideoElement ? panorama.source : createVideo({
      src: panorama.source,
      withCredentials: this.viewer.config.withCredentials,
      muted: this.config.muted,
      autoplay: false
    });
    await this.__videoLoadPromise(video);
    const texture = new VideoTexture(video);
    return { panorama, texture };
  }
  switchVideo(texture) {
    let currentTime;
    let duration;
    let paused = !this.config.autoplay;
    let muted = this.config.muted;
    let volume = 1;
    if (this.video) {
      ({ currentTime, duration, paused, muted, volume } = this.video);
    }
    this.__removeVideo();
    this.video = texture.image;
    if (this.video.duration === duration) {
      this.video.currentTime = currentTime;
    }
    this.video.muted = muted;
    this.video.volume = volume;
    if (!paused) {
      this.video.play();
    }
  }
  setTextureOpacity(mesh, opacity) {
    mesh.material.opacity = opacity;
    mesh.material.transparent = opacity < 1;
  }
  disposeTexture({ texture }) {
    texture.dispose();
  }
  disposeMesh(mesh) {
    mesh.geometry.dispose();
    mesh.material.dispose();
  }
  __removeVideo() {
    if (this.video) {
      this.video.pause();
      this.video.remove();
      delete this.video;
    }
  }
  __videoLoadPromise(video) {
    return new Promise((resolve, reject) => {
      const onLoaded = () => {
        if (this.video && video.duration === this.video.duration) {
          resolve(this.__videoBufferPromise(video, this.video.currentTime));
        } else {
          resolve();
        }
        video.removeEventListener("loadedmetadata", onLoaded);
      };
      const onError = (err) => {
        reject(err);
        video.removeEventListener("error", onError);
      };
      video.addEventListener("loadedmetadata", onLoaded);
      video.addEventListener("error", onError);
    });
  }
  __videoBufferPromise(video, currentTime) {
    return new Promise((resolve) => {
      function onBuffer() {
        const buffer = video.buffered;
        for (let i = 0, l = buffer.length; i < l; i++) {
          if (buffer.start(i) <= video.currentTime && buffer.end(i) >= video.currentTime) {
            video.pause();
            video.removeEventListener("buffer", onBuffer);
            video.removeEventListener("progress", onBuffer);
            resolve();
            break;
          }
        }
      }
      video.currentTime = Math.min(currentTime + 2e3, video.duration);
      video.muted = true;
      video.addEventListener("buffer", onBuffer);
      video.addEventListener("progress", onBuffer);
      video.play();
    });
  }
};
AbstractVideoAdapter.supportsDownload = false;

// src/EquirectangularVideoAdapter.ts
var getConfig = utils.getConfigParser({
  resolution: 64,
  autoplay: false,
  muted: false
});
var EquirectangularVideoAdapter = class extends AbstractVideoAdapter {
  constructor(viewer, config) {
    super(viewer);
    this.config = getConfig(config);
    this.adapter = new EquirectangularAdapter(this.viewer, {
      resolution: this.config.resolution
    });
  }
  destroy() {
    this.adapter.destroy();
    delete this.adapter;
    super.destroy();
  }
  textureCoordsToSphericalCoords(point, data) {
    return this.adapter.textureCoordsToSphericalCoords(point, data);
  }
  sphericalCoordsToTextureCoords(position, data) {
    return this.adapter.sphericalCoordsToTextureCoords(position, data);
  }
  async loadTexture(panorama, _, newPanoData) {
    const { texture } = await super.loadTexture(panorama);
    const video = texture.image;
    if (panorama.data) {
      newPanoData = panorama.data;
    }
    if (typeof newPanoData === "function") {
      newPanoData = newPanoData(video);
    }
    const panoData = utils.mergePanoData(video.videoWidth, video.videoHeight, newPanoData);
    return { panorama, texture, panoData };
  }
  createMesh(panoData) {
    return this.adapter.createMesh(panoData);
  }
  setTexture(mesh, { texture }) {
    mesh.material.map = texture;
    this.switchVideo(texture);
  }
};
EquirectangularVideoAdapter.id = "equirectangular-video";
EquirectangularVideoAdapter.VERSION = "5.11.4";
export {
  EquirectangularVideoAdapter
};
//# sourceMappingURL=index.module.js.map