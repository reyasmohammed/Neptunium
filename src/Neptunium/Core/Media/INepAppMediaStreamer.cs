﻿using Windows.Media.Playback;
using Neptunium.Core.Stations;
using System.Threading.Tasks;
using System;
using Windows.Media.Core;
using Neptunium.Core.Media.Metadata;
using System.Threading;
using UWPShoutcastMSS.Streaming;

namespace Neptunium.Media
{
    public class MediaStreamerMetadataChangedEventArgs : EventArgs
    {
        internal MediaStreamerMetadataChangedEventArgs(SongMetadata meta)
        {
            Metadata = meta;
        }

        public SongMetadata Metadata { get; private set; }
    }

    public class MediaStreamerStationInfoAcquiredEventArgs : EventArgs
    {
        internal MediaStreamerStationInfoAcquiredEventArgs(ServerStationInfo serverStationInfo)
        {
            StationInfo = serverStationInfo;
        }

        public ServerStationInfo StationInfo { get; private set; }
    }

    internal interface INepAppMediaStreamer : IDisposable
    {
        void InitializePlayback(MediaPlayer player);
        Task TryConnectAsync(StationStream stream);

        double Volume { get; set; }
        StationItem StationPlaying { get; }
        bool IsPlaying { get; }
        bool IsOpen { get; }
        MediaPlayer Player { get; }
        MediaSource StreamMediaSource { get; }
        SongMetadata SongMetadata { get; }

        void Play();
        void Pause();

        bool PollConnection();

        event EventHandler<MediaStreamerMetadataChangedEventArgs> MetadataChanged;
    }

    public abstract class BasicNepAppMediaStreamer : INepAppMediaStreamer
    {
        private SemaphoreSlim volumeLock = null;

        public MediaSource StreamMediaSource { get; protected set; }
        public virtual bool IsOpen { get { return (bool)StreamMediaSource?.IsOpen; } }

        public bool IsPlaying { get; protected set; }

        public MediaPlayer Player { get; protected set; }

        public StationItem StationPlaying { get; protected set; }

        public SongMetadata SongMetadata { get; private set; }

        public BasicNepAppMediaStreamer()
        {
            volumeLock = new SemaphoreSlim(1);
        }

        public double Volume
        {
            get
            {
                if (Player == null) throw new InvalidOperationException();
                return (double)Player.Volume;
            }

            set
            {
                if (Player == null) throw new InvalidOperationException();
                Player.Volume = value;
            }
        }

        public event EventHandler<MediaStreamerMetadataChangedEventArgs> MetadataChanged;
        public event EventHandler<MediaStreamerStationInfoAcquiredEventArgs> StationInfoAcquired;

        protected void RaiseMetadataChanged(SongMetadata metadata)
        {
            this.SongMetadata = metadata;
            MetadataChanged?.Invoke(this, new MediaStreamerMetadataChangedEventArgs(metadata));
        }

        protected void RaiseStationInfoAcquired(ServerStationInfo serverStationInfo)
        {
            if (serverStationInfo != null)
            {
                StationInfoAcquired?.Invoke(this, new MediaStreamerStationInfoAcquiredEventArgs(serverStationInfo));
            }
        }

        public abstract void InitializePlayback(MediaPlayer player);
        public abstract Task TryConnectAsync(StationStream stream);

        public virtual void Dispose()
        {
            if (Player != null)
            {
                Player.Dispose();
                Player = null;
            }

            if (StreamMediaSource != null)
            {
                StreamMediaSource.Dispose();
            }

            volumeLock.Dispose();
        }

        public void Play()
        {
            if (Player == null) throw new InvalidOperationException();

            Player.Play();
        }

        public void Pause()
        {
            if (Player == null) throw new InvalidOperationException();

            Player.Pause();
        }

        public async Task FadeVolumeDownToAsync(double value)
        {
            if (value > Player.Volume) throw new ArgumentOutOfRangeException(nameof(value), actualValue: value, message: "Out of range.");

            if (value == Player.Volume) return;

            await volumeLock.WaitAsync();

            var initial = Player.Volume;
            for (double x = initial; x > value; x -= .01)
            {
                await Task.Delay(25);
                Player.Volume = x;
            }

            volumeLock.Release();
        }
        public async Task FadeVolumeUpToAsync(double value)
        {
            if (value < Player.Volume) throw new ArgumentOutOfRangeException(nameof(value), actualValue: value, message: "Out of range.");

            if (value == Player.Volume) return;

            await volumeLock.WaitAsync();

            var initial = Player.Volume;
            for (double x = initial; x < value; x += .01)
            {
                await Task.Delay(25);
                Player.Volume = x;
            }

            volumeLock.Release();
        }

        public virtual bool PollConnection()
        {
            return IsOpen;
        }
    }
}