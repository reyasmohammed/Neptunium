﻿using Hqub.MusicBrainz.API.Entities;
using Kukkii;
using Neptunium.Data;
using Neptunium.Managers.Songs.Metadata_Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers.Songs
{
    public class SongMetadataManager
    {
        private ISongMetadataSource MusicBrainz { get; set; } = new MusicBrainzMetadataSource();
        private ISongMetadataSource ITunes { get; set; } = new ITunesMetadataSource();

        internal async Task<MusicBrainzSongMetadata> GetMusicBrainzDataAsync(string track, string artist)
        {
            MusicBrainzSongMetadata metadata = new MusicBrainzSongMetadata();

            var albumData = await MusicBrainz.TryFindAlbumAsync(track, artist);
            if (albumData != null)
            {
                metadata.Album = albumData;
            }

            if (albumData != null)
            {
                //get the artist via artist id
                metadata.Artist = await MusicBrainz.GetArtistAsync(albumData.ArtistID);
            }
            else
            {
                metadata.Artist = await MusicBrainz.TryFindArtistAsync(artist);
            }

            return metadata;
        }

        internal async Task<ITunesSongMetadata> GetITunesDataAsync(string track, string artist)
        {
            ITunesSongMetadata metadata = new ITunesSongMetadata();

            var albumData = await ITunes.TryFindAlbumAsync(track, artist);
            if (albumData != null)
            {
                metadata.Album = albumData;
            }

            if (albumData != null)
            {
                //get the artist via artist id
                metadata.Artist = await ITunes.GetArtistAsync(albumData.ArtistID);
            }
            else
            {
                metadata.Artist = await ITunes.TryFindArtistAsync(artist);
            }

            return metadata;
        }
    }
}
