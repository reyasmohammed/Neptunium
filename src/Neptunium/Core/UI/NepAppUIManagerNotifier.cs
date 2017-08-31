﻿using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Neptunium.Core.Media.Metadata;
using System;
using Neptunium.Core.Stations;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerNotifier
    {
        private ToastNotifier toastNotifier = null;
        private TileUpdater tileUpdater = null;
        internal NepAppUIManagerNotifier()
        {
            toastNotifier = ToastNotificationManager.CreateToastNotifier();
            tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
        }

        public void ShowSongToastNotification(ExtendedSongMetadata metaData)
        {
            ToastContent content = new ToastContent()
            {
                Launch = "now-playing",
                Audio = new ToastAudio()
                {
                    Silent = true,
                },
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = metaData.Track,
                                HintStyle = AdaptiveTextStyle.Title
                            },

                            new AdaptiveText()
                            {
                                Text = metaData.Artist,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },

                            new AdaptiveText()
                            {
                                Text = metaData.StationPlayedOn,
                                HintStyle = AdaptiveTextStyle.Caption
                            },
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = !string.IsNullOrWhiteSpace(metaData.Album?.AlbumCoverUrl) ? metaData.Album?.AlbumCoverUrl : metaData.StationLogo.ToString(),
                        }
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = "song-notif";
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }

        internal void ShowErrorToastNotification(StationStream stream, string title, string message)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title,
                                HintStyle = AdaptiveTextStyle.Title
                            },

                            new AdaptiveText()
                            {
                                Text = message,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = stream.ParentStation?.StationLogoUrl.ToString(),
                        }
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = "error";
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }
    }
}