/*!
 * Photo Sphere Viewer / Equirectangular Video Adapter 5.10.0
 * @copyright 2015-2024 Damien "Mistic" Sorel
 * @licence MIT (https://opensource.org/licenses/MIT)
 */
"use strict";
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __export = (target, all) => {
  for (var name in all)
    __defProp(target, name, { get: all[name], enumerable: true });
};
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

// src/index.ts
var src_exports = {};
__export(src_exports, {
  EquirectangularVideoAdapter: () => EquirectangularVideoAdapter
});
module.exports = __toCommonJS(src_exports);

// src/EquirectangularVideoAdapter.ts
var import_core2 = require("@photo-sphere-viewer/core");
var import_three2 = require("three");

// ../shared/AbstractVideoAdapter.ts
var import_core = require("@photo-sphere-viewer/core");
var import_three = require("three");

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
var AbstractVideoAdapter = class extends import_core.AbstractAdapter {
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
  loadTexture(panorama) {
    if (typeof panorama !== "object" || !panorama.source) {
      return Promise.reject(new import_core.PSVError("Invalid panorama configuration, are you using the right adapter?"));
    }
    if (!this.viewer.getPlugin("video")) {
      return Promise.reject(new import_core.PSVError("Video adapters require VideoPlugin to be loaded too."));
    }
    const video = createVideo({
      src: panorama.source,
      withCredentials: this.viewer.config.withCredentials,
      muted: this.config.muted,
      autoplay: false
    });
    return this.__videoLoadPromise(video).then(() => {
      const texture = new import_three.VideoTexture(video);
      return { panorama, texture };
    });
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
  disposeTexture(textureData) {
    textureData.texture.dispose();
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
var getConfig = import_core2.utils.getConfigParser(
  {
    resolution: 64,
    autoplay: false,
    muted: false
  },
  {
    resolution: (resolution) => {
      if (!resolution || !import_three2.MathUtils.isPowerOfTwo(resolution)) {
        throw new import_core2.PSVError("EquirectangularTilesAdapter resolution must be power of two");
      }
      return resolution;
    }
  }
);
var EquirectangularVideoAdapter = class extends AbstractVideoAdapter {
  constructor(viewer, config) {
    super(viewer);
    this.config = getConfig(config);
    this.SPHERE_SEGMENTS = this.config.resolution;
    this.SPHERE_HORIZONTAL_SEGMENTS = this.SPHERE_SEGMENTS / 2;
  }
  destroy() {
    this.adapter?.destroy();
    delete this.adapter;
    super.destroy();
  }
  textureCoordsToSphericalCoords(point, data) {
    return this.getAdapter().textureCoordsToSphericalCoords(point, data);
  }
  sphericalCoordsToTextureCoords(position, data) {
    return this.getAdapter().sphericalCoordsToTextureCoords(position, data);
  }
  loadTexture(panorama) {
    return super.loadTexture(panorama).then(({ texture }) => {
      const video = texture.image;
      const panoData = {
        isEquirectangular: true,
        fullWidth: video.videoWidth,
        fullHeight: video.videoHeight,
        croppedWidth: video.videoWidth,
        croppedHeight: video.videoHeight,
        croppedX: 0,
        croppedY: 0,
        poseHeading: 0,
        posePitch: 0,
        poseRoll: 0
      };
      return { panorama, texture, panoData };
    });
  }
  createMesh() {
    const geometry = new import_three2.SphereGeometry(
      import_core2.CONSTANTS.SPHERE_RADIUS,
      this.SPHERE_SEGMENTS,
      this.SPHERE_HORIZONTAL_SEGMENTS,
      -Math.PI / 2
    ).scale(-1, 1, 1);
    const material = new import_three2.MeshBasicMaterial();
    return new import_three2.Mesh(geometry, material);
  }
  setTexture(mesh, textureData) {
    mesh.material.map = textureData.texture;
    this.switchVideo(textureData.texture);
  }
  /**
   * @internal
   */
  getAdapter() {
    if (!this.adapter) {
      this.adapter = new import_core2.EquirectangularAdapter(this.viewer, {
        interpolateBackground: false,
        resolution: this.config.resolution
      });
    }
    return this.adapter;
  }
};
EquirectangularVideoAdapter.id = "equirectangular-video";
EquirectangularVideoAdapter.VERSION = "5.10.0";
// Annotate the CommonJS export names for ESM import in node:
0 && (module.exports = {
  EquirectangularVideoAdapter
});
//# sourceMappingURL=index.cjs.map