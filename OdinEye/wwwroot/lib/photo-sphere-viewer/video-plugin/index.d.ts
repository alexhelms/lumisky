import { ExtendedPosition, AbstractConfigurablePlugin, utils, Viewer, TypedEvent } from '@photo-sphere-viewer/core';

type VideoKeypoint = {
    position: ExtendedPosition;
    time: number;
};
type VideoPluginConfig = {
    /**
     * displays a progressbar on top of the navbar
     * @default true
     */
    progressbar?: boolean;
    /**
     * displays a big "play" button in the center of the viewer
     * @default true
     */
    bigbutton?: boolean;
    /**
     * defines autorotate timed keypoints
     */
    keypoints?: VideoKeypoint[];
};

/**
 * Controls a video adapter
 */
declare class VideoPlugin extends AbstractConfigurablePlugin<VideoPluginConfig, VideoPluginConfig, never, VideoPluginEvents> {
    static readonly id = "video";
    static readonly VERSION: string;
    static readonly configParser: utils.ConfigParser<VideoPluginConfig, VideoPluginConfig>;
    static readonly readonlyOptions: string[];
    private readonly state;
    private video?;
    private progressbar?;
    private overlay?;
    private autorotate?;
    private markers?;
    constructor(viewer: Viewer, config: VideoPluginConfig);
    private __bindVideo;
    private __onKeyPress;
    /**
     * Returns the durection of the video
     */
    getDuration(): number;
    /**
     * Returns the current time of the video
     */
    getTime(): number;
    /**
     * Returns the play progression of the video
     */
    getProgress(): number;
    /**
     * Returns if the video is playing
     */
    isPlaying(): boolean;
    /**
     * Returns the video volume
     */
    getVolume(): number;
    /**
     * Starts or pause the video
     */
    playPause(): void;
    /**
     * Starts the video if paused
     */
    play(): void;
    /**
     * Pauses the cideo if playing
     */
    pause(): void;
    /**
     * Sets the volume of the video
     */
    setVolume(volume: number): void;
    /**
     * (Un)mutes the video
     * @param [mute] - toggle if undefined
     */
    setMute(mute?: boolean): void;
    /**
     * Changes the current time of the video
     */
    setTime(time: number): void;
    /**
     * Changes the progression of the video
     */
    setProgress(progress: number): void;
    /**
     * Changes the keypoints
     * @throws {@link PSVError} if the configuration is invalid
     */
    setKeypoints(keypoints?: VideoKeypoint[]): void;
    private __configureAutorotate;
    private __autorotate;
    private __autorotateNext;
}

/**
 * @event Triggered when the video starts playing or is paused
 */
declare class PlayPauseEvent extends TypedEvent<VideoPlugin> {
    readonly playing: boolean;
    static readonly type = "play-pause";
    type: 'play-pause';
}
/**
 * @event Triggered when the video volume changes
 */
declare class VolumeChangeEvent extends TypedEvent<VideoPlugin> {
    readonly volume: number;
    static readonly type = "volume-change";
    type: 'volume-change';
}
/**
 * @event Triggered when the video play progression changes
 */
declare class ProgressEvent extends TypedEvent<VideoPlugin> {
    readonly time: number;
    readonly duration: number;
    readonly progress: number;
    static readonly type = "progress";
    type: 'progress';
}
/**
 * @event Triggered when the video buffer changes
 */
declare class BufferEvent extends TypedEvent<VideoPlugin> {
    readonly maxBuffer: number;
    static readonly type = "buffer";
    type: 'buffer';
}
type VideoPluginEvents = PlayPauseEvent | VolumeChangeEvent | ProgressEvent | BufferEvent;

type events_BufferEvent = BufferEvent;
declare const events_BufferEvent: typeof BufferEvent;
type events_PlayPauseEvent = PlayPauseEvent;
declare const events_PlayPauseEvent: typeof PlayPauseEvent;
type events_ProgressEvent = ProgressEvent;
declare const events_ProgressEvent: typeof ProgressEvent;
type events_VideoPluginEvents = VideoPluginEvents;
type events_VolumeChangeEvent = VolumeChangeEvent;
declare const events_VolumeChangeEvent: typeof VolumeChangeEvent;
declare namespace events {
  export { events_BufferEvent as BufferEvent, events_PlayPauseEvent as PlayPauseEvent, events_ProgressEvent as ProgressEvent, type events_VideoPluginEvents as VideoPluginEvents, events_VolumeChangeEvent as VolumeChangeEvent };
}

export { type VideoKeypoint, VideoPlugin, type VideoPluginConfig, events };
