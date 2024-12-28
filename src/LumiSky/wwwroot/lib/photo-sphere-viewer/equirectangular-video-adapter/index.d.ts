import { AbstractAdapter, Viewer, TextureData, PanoData, PanoramaPosition, Position } from '@photo-sphere-viewer/core';
import { VideoTexture, Mesh, BufferGeometry, Material, SphereGeometry, MeshBasicMaterial } from 'three';

type AbstractVideoPanorama = {
    source: string | MediaStream | HTMLVideoElement;
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
type AbstractVideoTextureData = TextureData<VideoTexture>;
/**
 * Base video adapters class
 */
declare abstract class AbstractVideoAdapter<TPanorama extends AbstractVideoPanorama, TData, TMesh extends AbstractVideoMesh> extends AbstractAdapter<TPanorama, TData, VideoTexture, TMesh> {
    static readonly supportsDownload = false;
    protected abstract readonly config: AbstractVideoAdapterConfig;
    private video;
    constructor(viewer: Viewer);
    init(): void;
    destroy(): void;
    supportsPreload(): boolean;
    supportsTransition(): boolean;
    loadTexture(panorama: AbstractVideoPanorama): Promise<AbstractVideoTextureData>;
    protected switchVideo(texture: VideoTexture): void;
    setTextureOpacity(mesh: TMesh, opacity: number): void;
    disposeTexture({ texture }: AbstractVideoTextureData): void;
    disposeMesh(mesh: AbstractVideoMesh): void;
    private __removeVideo;
    private __videoLoadPromise;
    private __videoBufferPromise;
}

/**
 * Configuration of an equirectangular video
 */
type EquirectangularVideoPanorama = AbstractVideoPanorama & {
    data?: PanoData | ((image: HTMLVideoElement) => PanoData);
};
type EquirectangularVideoAdapterConfig = AbstractVideoAdapterConfig & {
    /**
     * number of faces of the sphere geometry, higher values may decrease performances
     * @default 64
     */
    resolution?: number;
};

type EquirectangularVideoMesh = Mesh<SphereGeometry, MeshBasicMaterial>;
type EquirectangularVideoTextureData = TextureData<VideoTexture, EquirectangularVideoPanorama, PanoData>;
/**
 * Adapter for equirectangular videos
 */
declare class EquirectangularVideoAdapter extends AbstractVideoAdapter<EquirectangularVideoPanorama, PanoData, EquirectangularVideoMesh> {
    static readonly id = "equirectangular-video";
    static readonly VERSION: string;
    protected readonly config: EquirectangularVideoAdapterConfig;
    private adapter;
    constructor(viewer: Viewer, config: EquirectangularVideoAdapterConfig);
    destroy(): void;
    textureCoordsToSphericalCoords(point: PanoramaPosition, data: PanoData): Position;
    sphericalCoordsToTextureCoords(position: Position, data: PanoData): PanoramaPosition;
    loadTexture(panorama: EquirectangularVideoPanorama, _?: boolean, newPanoData?: any): Promise<EquirectangularVideoTextureData>;
    createMesh(panoData: PanoData): EquirectangularVideoMesh;
    setTexture(mesh: EquirectangularVideoMesh, { texture }: EquirectangularVideoTextureData): void;
}

export { EquirectangularVideoAdapter, type EquirectangularVideoAdapterConfig, type EquirectangularVideoPanorama };
