import { AbstractAdapter, Viewer, TextureData, PanoData, PanoramaPosition, Position } from '@photo-sphere-viewer/core';
import { VideoTexture, Mesh, BufferGeometry, Material, SphereGeometry, MeshBasicMaterial } from 'three';

type AbstractVideoPanorama = {
    source: string | MediaStream;
};
type AbstractVideoAdapterConfig = {
    /**
     * automatically start the video
     * @default false
     */
    autoplay?: boolean;
    /**
     * initially mute the video
     * @default false
     */
    muted?: boolean;
};
type AbstractVideoMesh = Mesh<BufferGeometry, Material>;
type AbstractVideoTexture = TextureData<VideoTexture>;
/**
 * Base video adapters class
 */
declare abstract class AbstractVideoAdapter<TPanorama extends AbstractVideoPanorama, TData> extends AbstractAdapter<TPanorama, VideoTexture, TData> {
    static readonly supportsDownload = false;
    protected abstract readonly config: AbstractVideoAdapterConfig;
    private video;
    constructor(viewer: Viewer);
    init(): void;
    destroy(): void;
    supportsPreload(): boolean;
    supportsTransition(): boolean;
    loadTexture(panorama: AbstractVideoPanorama): Promise<AbstractVideoTexture>;
    protected switchVideo(texture: VideoTexture): void;
    setTextureOpacity(mesh: AbstractVideoMesh, opacity: number): void;
    disposeTexture(textureData: AbstractVideoTexture): void;
    private __removeVideo;
    private __videoLoadPromise;
    private __videoBufferPromise;
}

/**
 * Configuration of an equirectangular video
 */
type EquirectangularVideoPanorama = AbstractVideoPanorama;
type EquirectangularVideoAdapterConfig = AbstractVideoAdapterConfig & {
    /**
     * number of faces of the sphere geometry, higher values may decrease performances
     * @default 64
     */
    resolution?: number;
};

type EquirectangularMesh = Mesh<SphereGeometry, MeshBasicMaterial>;
type EquirectangularTexture = TextureData<VideoTexture, EquirectangularVideoPanorama, PanoData>;
/**
 * Adapter for equirectangular videos
 */
declare class EquirectangularVideoAdapter extends AbstractVideoAdapter<EquirectangularVideoPanorama, PanoData> {
    static readonly id = "equirectangular-video";
    static readonly VERSION: string;
    protected readonly config: EquirectangularVideoAdapterConfig;
    private readonly SPHERE_SEGMENTS;
    private readonly SPHERE_HORIZONTAL_SEGMENTS;
    private adapter;
    constructor(viewer: Viewer, config: EquirectangularVideoAdapterConfig);
    destroy(): void;
    textureCoordsToSphericalCoords(point: PanoramaPosition, data: PanoData): Position;
    sphericalCoordsToTextureCoords(position: Position, data: PanoData): PanoramaPosition;
    loadTexture(panorama: EquirectangularVideoPanorama): Promise<EquirectangularTexture>;
    createMesh(): EquirectangularMesh;
    setTexture(mesh: EquirectangularMesh, textureData: EquirectangularTexture): void;
}

export { EquirectangularVideoAdapter, type EquirectangularVideoAdapterConfig, type EquirectangularVideoPanorama };
