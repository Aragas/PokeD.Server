using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

using Aragas.Network.IO;
using Aragas.Network.Packets;

using PokeD.Core.Extensions;
using PokeD.Server.Modules;

namespace PokeD.Server.Clients
{
    public abstract class StandardClient<TPacketTransmission, TPacketType, TPacketIDType, TSerializer, TDeserializer> : Client
        where TPacketTransmission : PacketTransmission<TPacketType, TPacketIDType, TSerializer, TDeserializer>
        where TPacketType : Packet<TPacketIDType, TSerializer, TDeserializer>
        where TSerializer : PacketSerializer
        where TDeserializer : PacketDeserializer
    {
        protected TPacketTransmission Stream { get; }

        private ConcurrentQueue<TPacketType> PacketsToSend { get; } = new ConcurrentQueue<TPacketType>();

#if DEBUG
        // -- Debug -- //
        private const int QueueSize = 100;
        protected virtual Queue<TPacketType> Received { get; } = new Queue<TPacketType>(QueueSize);
        protected virtual Queue<TPacketType> Sended { get; } = new Queue<TPacketType>(QueueSize);
        // -- Debug -- //
#endif

        private bool IsDisposing { get; set; }

        protected StandardClient(Socket socket, ServerModule serverModule) : base(serverModule)
        {
            Stream = (TPacketTransmission) Activator.CreateInstance(typeof(TPacketTransmission), socket);
        }

        // Bad design, either replace Client with StandardClient or do something else.
        public sealed override void SendPacket<TPacket>(TPacket packet) => SendPacket(packet as TPacketType);
        public virtual void SendPacket(TPacketType packet) => PacketsToSend.Enqueue(packet);

        public sealed override void Update()
        {
            UpdateLock.Reset(); // Signal that the UpdateThread is alive.
            try
            {
                while (!UpdateToken.IsCancellationRequested && Stream.IsConnected)
                {
                    ConnectionLock.Reset(); // Signal that we are handling pending client data.
                    try
                    {
                        while (Stream.TryReadPacket(out var packetToReceive))
                        {
                            HandlePacket(packetToReceive);

#if DEBUG
                            Received.Enqueue(packetToReceive);
                            if (Received.Count >= QueueSize)
                                Received.Dequeue();
#endif
                        }
                        while (PacketsToSend.TryDequeue(out var packetToSend))
                        {
                            Stream.SendPacket(packetToSend);

#if DEBUG
                            Sended.Enqueue(packetToSend);
                            if (Sended.Count >= QueueSize)
                                Sended.Dequeue();
#endif
                        }
                    }
                    finally
                    {
                        ConnectionLock.Set(); // Signal that we are not handling anymore pending client data.
                    }

                    Thread.Sleep(100); // 100 calls per second should not be too often?
                }
            }
            finally
            {
                UpdateLock.Set(); // Signal that the UpdateThread is finished

                if (!UpdateToken.IsCancellationRequested && !Stream.IsConnected) // Leave() if the update cycle stopped unexpectedly
                    LeaveAsync();
            }
        }

        public abstract void HandlePacket(TPacketType packet);

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposing)
            {
                if (disposing)
                {
                    Stream.Dispose();

#if DEBUG
                    Sended.Clear();
                    Received.Clear();
#endif
                }


                IsDisposing = true;
            }
            base.Dispose(disposing);
        }
    }

    public abstract class StandardClient<TServerModule, TPacketTransmission, TPacketType, TPacketIDType, TSerializer, TDeserializer> : StandardClient<TPacketTransmission, TPacketType, TPacketIDType, TSerializer, TDeserializer>
        where TServerModule : ServerModule
        where TPacketTransmission : PacketTransmission<TPacketType, TPacketIDType, TSerializer, TDeserializer>
        where TPacketType : Packet<TPacketIDType, TSerializer, TDeserializer>
        where TSerializer : PacketSerializer
        where TDeserializer : PacketDeserializer
    {
        protected TServerModule Module { get; }

        protected StandardClient(Socket socket, TServerModule serverModule) : base(socket, serverModule)
        {
            Module = serverModule;
        }
    }
}