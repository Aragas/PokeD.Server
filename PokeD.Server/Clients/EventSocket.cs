using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace PokeD.Server.Clients
{
    public abstract class SocketEvent : EventArgs
    {
        public Socket Socket { get; set; }

        public SocketEvent(Socket socket) { Socket = socket; }
    }

    public delegate void SocketConnectedEventArgs(SocketConnectedArgs args);
    public class SocketConnectedArgs : SocketEvent
    {
        public SocketConnectedArgs(Socket socket) : base(socket) { }
    }

    public delegate void SocketDataReceivedEventArgs(SocketDataReceivedArgs args);
    public class SocketDataReceivedArgs : SocketEvent
    {
        public byte[] Data { get; set; }

        public SocketDataReceivedArgs(Socket socket, byte[] data) : base(socket) { Data = data; }
    }

    public delegate void SocketDisconnectedEventArgs(SocketDisconnectedArgs args);
    public class SocketDisconnectedArgs : SocketEvent
    {
        public string Reason { get; set; }

        public SocketDisconnectedArgs(Socket socket, string reason) : base(socket) { Reason = reason; }
    }

    /*
    Inspired by 'Umby24', Project 'Managed Sockets'
    */
    public class EventSocket
    {
        public Socket Socket { get; }
        
        public event SocketConnectedEventArgs Connected;
        public event SocketDataReceivedEventArgs DataReceived;
        public event SocketDisconnectedEventArgs Disconnected;

        private const int ConnectTimeout = 10000;
        private const int ReadBufferSize = 16 * 4096;

        private readonly byte[] _readBuffer = new byte[ReadBufferSize];

        
        public EventSocket(Socket socket) { Socket = socket; }  


        public void Connect(string endpoint, ushort port)
        {
            if (Socket.Connected)
                Disconnect("Connect() Called");

            IAsyncResult handle = Socket.BeginConnect(endpoint, port, ConnectCallback, null);
            if (handle.AsyncWaitHandle.WaitOne(ConnectTimeout)) // -- Handle connection timeouts
            {
                try { Socket.BeginReceive(_readBuffer, 0, ReadBufferSize, 0, ReceiveCallback, null); }
                catch (Exception e) when (e is SocketException || e is IOException) { Disconnect($"Socket exception occured: {e.HResult}; InnerException: {e.InnerException?.HResult}"); }
            }


            Socket.Close();
            throw new TimeoutException("Failed to connect to the server");
        }
        public void Disconnect() => Disconnect("Disconnect() Called");
        private void Disconnect(string reason)
        {
            if (!Socket.Connected)
                return;

            Socket.Disconnect(false);

            Disconnected?.Invoke(new SocketDisconnectedArgs(Socket, reason));
        }



        #region Callbacks
        private void ConnectCallback(IAsyncResult ar)
        {
            Socket.EndConnect(ar); // -- End the connection event..
            //IsConnected = true; // -- Flag the system as connected
            Socket.BeginReceive(_readBuffer, 0, ReadBufferSize, 0, ReceiveCallback, null); /* Begin reading data */

            Connected?.Invoke(new SocketConnectedArgs(Socket));
            // Task.Run(() => Connected(new SocketConnectedArgs(this))); // -- Trigger the socket connected event.
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            int received;

            try { received = Socket.EndReceive(ar); }
            catch (ObjectDisposedException) { return; /* Socket closed by client */ }
            catch (Exception e) when (e is SocketException || e is IOException) { Disconnect($"Socket exception occured: {e.HResult}; InnerException: {e.InnerException?.HResult}"); return; }

            if (received == 0) { Disconnect("Connection closed by remote host"); return; /* Socket Disconnected */ }

            var newMem = new byte[received];
            Buffer.BlockCopy(_readBuffer, 0, newMem, 0, received); // -- Copy the received data so the end user can use it however they wish


            DataReceived?.Invoke(new SocketDataReceivedArgs(Socket, newMem)); // -- Call the data received event. (Unblocks immediately, async).


            try { if (Socket.Connected) Socket.BeginReceive(_readBuffer, 0, ReadBufferSize, 0, ReceiveCallback, null); /* Read again! */ }
            catch { Disconnect("Socket closing"); }
        }
        #endregion Callbacks
    }
}
