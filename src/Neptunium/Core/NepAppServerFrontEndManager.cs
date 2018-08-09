﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;
using static Neptunium.NepApp;

namespace Neptunium.Core
{
    public class NepAppServerFrontEndManager : INotifyPropertyChanged, INepAppFunctionManager
    {
        public const int ServerPortNumber = 8806; //netsh advfirewall firewall add rule name="Open port 8806" dir=in action=allow protocol=TCP localport=8806

        /// <summary>
        /// From INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public class NepAppServerFrontEndManagerDataReceivedEventArgs : EventArgs
        {
            public string Data { get; private set; }
            internal NepAppServerFrontEndManagerDataReceivedEventArgs(string data)
            {
                Data = data;
            }
        }

        public bool IsInitialized { get; private set; }
        public IEnumerable<IPAddress> LocalEndPoints { get; private set; }

        public event EventHandler<NepAppServerFrontEndManagerDataReceivedEventArgs> DataReceived;

        private List<Tuple<StreamSocket,DataReader,DataWriter>> connections = new List<Tuple<StreamSocket, DataReader, DataWriter>>();
        private StreamSocketListener listener = null;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;

            listener = new StreamSocketListener();
            listener.ConnectionReceived += Listener_ConnectionReceived;
            await listener.BindServiceNameAsync(ServerPortNumber.ToString());

            LocalEndPoints = NepApp.Network.GetLocalIPAddresses();
            RaisePropertyChanged(nameof(LocalEndPoints));

            IsInitialized = true;
        }

        private void SongManager_PreSongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {
            List<Tuple<StreamSocket, DataReader, DataWriter>> connectionsToRemove = new List<Tuple<StreamSocket, DataReader, DataWriter>>();

            foreach (Tuple<StreamSocket, DataReader, DataWriter> tup in connections)
            {
                try
                {
                    tup.Item3.WriteString("MEDIA" + NepAppServerClient.MessageTypeSeperator + e.Metadata.ToString());
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset
                        || ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionAborted)
                    {
                        connectionsToRemove.Add(tup);
                    }
                }
            }

            foreach (Tuple<StreamSocket, DataReader, DataWriter> tup in connectionsToRemove)
            {
                try
                {
                    tup.Item3.Dispose();
                    tup.Item2.Dispose();
                    tup.Item1.Dispose();
                }
                catch (Exception) { }

                connections.Remove(tup);
            }

            connectionsToRemove.Clear();
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            DataReader reader = new DataReader(args.Socket.InputStream);
            DataWriter writer = new DataWriter(args.Socket.OutputStream);

            reader.InputStreamOptions = InputStreamOptions.Partial;

            var socketTup = new Tuple<StreamSocket, DataReader, DataWriter>(args.Socket, reader, writer);
            connections.Add(socketTup);

            while (true)
            {
                uint available = await reader.LoadAsync(50);

                if (available > 0)
                {
                    var data = reader.ReadString(available);
                    data = data.Trim();

                    DataReceived?.Invoke(this, new NepAppServerFrontEndManagerDataReceivedEventArgs(data));
                }
                else
                {
                    lock(connections)
                    {
                        connections.Remove(socketTup);
                    }

                    break;
                }
            }
        }

        private void CleanUp()
        {
            if (!IsInitialized) return;

            //clean up
            listener.Dispose();

            LocalEndPoints = null;
            RaisePropertyChanged(nameof(LocalEndPoints));

            NepApp.SongManager.PreSongChanged -= SongManager_PreSongChanged;

            IsInitialized = false;
        }

        public class NepAppServerClient : IDisposable, INotifyPropertyChanged
        {
            public const char MessageTypeSeperator = '|';

            private StreamSocket tcpClient = null;
            private DataWriter dataWriter = null;

            public bool IsConnected { get; private set; }

            public NepAppServerClient()
            {
                tcpClient = new StreamSocket();
            }

            public async Task TryConnectAsync(IPAddress address)
            {
                await tcpClient.ConnectAsync(new Windows.Networking.HostName(address.ToString()), ServerPortNumber.ToString());
                dataWriter = new DataWriter(tcpClient.OutputStream);

                IsConnected = true;
                RaisePropertyChanged(nameof(IsConnected));
            }

            public void AskServerToStreamStation(Stations.StationItem station)
            {
                if (IsConnected)
                {
                    dataWriter.WriteString("PLAY" + MessageTypeSeperator + station.Name);
                }
            }

            public void AskServerToStop()
            {
                if (IsConnected)
                {
                    dataWriter.WriteString("STOP" + MessageTypeSeperator);
                }
            }


            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                        dataWriter.Dispose();
                        tcpClient.Dispose();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~NepAppServerClient() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion


            /// <summary>
            /// From INotifyPropertyChanged
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
            {
                if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}