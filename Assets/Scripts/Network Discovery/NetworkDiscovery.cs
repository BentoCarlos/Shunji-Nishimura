using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Transporting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FishNet.Discovery
{
    /// <summary>
    /// A component that advertises a server or searches for servers.
    /// </summary>
    public sealed class NetworkDiscovery : MonoBehaviour
    {
        /// <summary>
        /// A string that differentiates your application/game from others.
        /// <b>Must not</b> be null, empty, or blank.
        /// </summary>
        [SerializeField]
        [Tooltip("A string that differentiates your application/game from others. Must not be null, empty, or blank.")]
        private string secret;

        /// <summary>
        /// The port number used by this <see cref="NetworkDiscovery"/> component.
        /// <b>Must</b> be different from the one used by the <seealso cref="Transport"/>.
        /// </summary>
        [SerializeField]
        [Tooltip("The port number used by this NetworkDiscovery component. Must be different from the one used by the Transport.")]
        private ushort port;

        /// <summary>
        /// How often does this <see cref="NetworkDiscovery"/> component advertises a server or searches for servers.
        /// </summary>
        [SerializeField]
        [Tooltip("How often does this NetworkDiscovery component advertises a server or searches for servers.")]
        private float discoveryInterval;

        /// <summary>
        /// Whether this <see cref="NetworkDiscovery"/> component will automatically start/stop? <b>Setting this to true is recommended.</b>
        /// </summary>
        [SerializeField]
        [Tooltip("Whether this NetworkDiscovery component will automatically start/stop? Setting this to true is recommended.")]
        private bool automatic;

        /// <summary>
        /// The <see cref="UdpClient"/> used to advertise the server.
        /// </summary>
        private UdpClient _serverUdpClient;

        /// <summary>
        /// The <see cref="UdpClient"/> used to search for servers.
        /// </summary>
        private UdpClient _clientUdpClient;

        /// <summary>
        /// Whether this <see cref="NetworkDiscovery"/> component is currently advertising a server or not.
        /// </summary>
        public bool IsAdvertising => _serverUdpClient != null;

        /// <summary>
        /// Whether this <see cref="NetworkDiscovery"/> component is currently searching for servers or not.
        /// </summary>
        public bool IsSearching => _clientUdpClient != null;

        /// <summary>
        /// An <see cref="Action"/> that is invoked by this <seealso cref="NetworkDiscovery"/> component whenever a server is found.
        /// </summary>
        public event Action<IPEndPoint> ServerFoundCallback;

        private void Start()
        {
            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log($"[Discovery] Start() called. automatic={automatic}, IsServer={InstanceFinder.IsServer}, IsClient={InstanceFinder.IsClient}", this);

            if (automatic)
            {
                InstanceFinder.ServerManager.OnServerConnectionState += ServerConnectionStateChangedHandler;

                InstanceFinder.ClientManager.OnClientConnectionState += ClientConnectionStateChangedHandler;

                // Don't start searching immediately - let the connection state handlers manage it
                // The handlers will be called when the server/client starts, and will start search/advertising accordingly

                if (NetworkManager.StaticCanLog(LoggingType.Common))
                    Debug.Log("[Discovery] NetworkDiscovery started in automatic mode. Waiting for connection state changes.", this);
            }
            else
            {
                if (NetworkManager.StaticCanLog(LoggingType.Common))
                    Debug.Log("[Discovery] NetworkDiscovery started but automatic=false. Manual control required.", this);
            }
        }

        private void OnDisable()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedHandler;

            InstanceFinder.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedHandler;

            StopAdvertisingServer();

            StopSearchingForServers();
        }

        private void OnDestroy()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedHandler;

            InstanceFinder.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedHandler;

            StopAdvertisingServer();

            StopSearchingForServers();
        }

        private void OnApplicationQuit()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedHandler;

            InstanceFinder.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedHandler;

            StopAdvertisingServer();

            StopSearchingForServers();
        }

        #region Connection State Handlers

        private void ServerConnectionStateChangedHandler(ServerConnectionStateArgs args)
        {
            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log($"[Discovery] Server connection state changed: {args.ConnectionState}", this);

            if (args.ConnectionState == LocalConnectionState.Starting)
            {
                StopSearchingForServers();
            }
            else if (args.ConnectionState == LocalConnectionState.Started)
            {
                StartAdvertisingServer();
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping)
            {
                StopAdvertisingServer();
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                StartSearchingForServers();
            }
        }

        private void ClientConnectionStateChangedHandler(ClientConnectionStateArgs args)
        {
            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log($"[Discovery] Client connection state changed: {args.ConnectionState}", this);

            if (args.ConnectionState == LocalConnectionState.Starting)
            {
                StopSearchingForServers();
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                StartSearchingForServers();
            }
        }

        #endregion

        #region Server

        /// <summary>
        /// Makes this <see cref="NetworkDiscovery"/> component start advertising a server.
        /// </summary>
        public void StartAdvertisingServer()
        {
            if (!InstanceFinder.IsServer)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning)) Debug.LogWarning("Unable to start advertising server. Server is inactive.", this);

                return;
            }

            // Validate secret first
            if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(secret))
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning))
                    Debug.LogWarning("✗ ERROR: Secret is null, empty, or whitespace! Cannot start server advertising.", this);

                return;
            }

            if (_serverUdpClient != null)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Server is already being advertised.", this);

                return;
            }

            if (port == InstanceFinder.TransportManager.Transport.GetPort())
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning)) Debug.LogWarning("Unable to start advertising server on the same port as the transport.", this);

                return;
            }

            try
            {
                // Bind to all interfaces (0.0.0.0) so it can receive from both localhost and broadcast
                _serverUdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port))
                {
                    EnableBroadcast = true,
                    MulticastLoopback = false,
                };

                Task.Run(AdvertiseServerAsync);

                if (NetworkManager.StaticCanLog(LoggingType.Common))
                    Debug.Log($"[Server Discovery] ✓ Started advertising on port {port} with secret '{secret}'", this);
            }
            catch (Exception ex)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning))
                    Debug.LogWarning($"[Server Discovery] Failed to start advertising: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Makes this <see cref="NetworkDiscovery"/> component <i>immediately</i> stop advertising the server it is currently advertising.
        /// </summary>
        public void StopAdvertisingServer()
        {
            if (_serverUdpClient == null) return;

            _serverUdpClient.Close();

            _serverUdpClient = null;

            if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Stopped advertising server.", this);
        }

        private async void AdvertiseServerAsync()
        {
            if (NetworkManager.StaticCanLog(LoggingType.Common))
            {
                Debug.Log($"[Server Discovery] Secret length: {secret.Length} chars. Content: '{secret}'", this);
                Debug.Log($"[Server Discovery] Server is now advertising on port {port} with secret '{secret}'", this);
            }

            int requestCount = 0;

            while (_serverUdpClient != null)
            {
                try
                {
                    if (NetworkManager.StaticCanLog(LoggingType.Common))
                        Debug.Log($"[Server Discovery] Waiting for discovery requests...", this);

                    UdpReceiveResult result = await _serverUdpClient.ReceiveAsync();

                    string receivedSecret = Encoding.UTF8.GetString(result.Buffer);

                    requestCount++;

                    if (NetworkManager.StaticCanLog(LoggingType.Common))
                        Debug.Log($"[Server Discovery] Request #{requestCount} from {result.RemoteEndPoint}, secret received: '{receivedSecret}'", this);

                    if (receivedSecret == secret)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Server Discovery] ✓ Request #{requestCount} from {result.RemoteEndPoint} - Secret MATCH! Sending response", this);

                        try
                        {
                            byte[] okBytes = BitConverter.GetBytes(true);
                            int sentBytes = await _serverUdpClient.SendAsync(okBytes, okBytes.Length, result.RemoteEndPoint);

                            if (NetworkManager.StaticCanLog(LoggingType.Common))
                                Debug.Log($"[Server Discovery] ✓ Sent {sentBytes} bytes response to {result.RemoteEndPoint}", this);
                        }
                        catch (Exception sendEx)
                        {
                            if (NetworkManager.StaticCanLog(LoggingType.Warning))
                                Debug.LogWarning($"[Server Discovery] Error sending response to {result.RemoteEndPoint}: {sendEx.GetType().Name} - {sendEx.Message}", this);
                        }
                    }
                    else
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Server Discovery] ✗ Request #{requestCount} from {result.RemoteEndPoint} - Secret MISMATCH! Expected '{secret}', got '{receivedSecret}'", this);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Server was stopped
                    break;
                }
                catch (Exception ex)
                {
                    if (NetworkManager.StaticCanLog(LoggingType.Warning))
                        Debug.LogWarning($"[Server Discovery] Error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", this);
                }
            }

            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log("[Server Discovery] Server advertisement stopped.", this);
        }

        #endregion

        #region Client

        /// <summary>
        /// Makes this <see cref="NetworkDiscovery"/> component start searching for servers.
        /// </summary>
        public void StartSearchingForServers()
        {
            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log($"[Discovery] StartSearchingForServers() called. IsServer={InstanceFinder.IsServer}, IsClient={InstanceFinder.IsClient}, _clientUdpClient={(_clientUdpClient != null ? "not null" : "null")}", this);

            if (InstanceFinder.IsServer)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning)) Debug.LogWarning("Unable to start searching for servers. Server is active.", this);

                return;
            }

            if (InstanceFinder.IsClient)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning)) Debug.LogWarning("Unable to start searching for servers. Client is active.", this);

                return;
            }

            if (_clientUdpClient != null)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Already searching for servers.", this);

                return;
            }

            try
            {
                // Create UDP client explicitly bound to any available port on all interfaces
                // This ensures the socket is ready to BOTH send and receive
                IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 0);
                _clientUdpClient = new UdpClient(localEp)
                {
                    EnableBroadcast = true,
                    MulticastLoopback = false,
                };

                var boundEndpoint = _clientUdpClient.Client.LocalEndPoint as IPEndPoint;
                if (NetworkManager.StaticCanLog(LoggingType.Common))
                    Debug.Log($"[Discovery] Client socket BOUND to 0.0.0.0:{boundEndpoint?.Port} (ready to receive)", this);

                Task.Run(SearchForServersAsync);

                if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Started searching for servers.", this);
            }
            catch (Exception ex)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning))
                    Debug.LogWarning($"[Discovery] Error creating UDP client: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Makes this <see cref="NetworkDiscovery"/> component <i>immediately</i> stop searching for servers.
        /// </summary>
        public void StopSearchingForServers()
        {
            if (_clientUdpClient == null) return;

            try
            {
                _clientUdpClient.Close();
            }
            catch (Exception ex)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Common))
                    Debug.Log($"[Discovery] Error closing UDP client: {ex.Message}", this);
            }

            _clientUdpClient = null;

            if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Stopped searching for servers.", this);
        }

        private async void SearchForServersAsync()
        {
            // Validate secret first
            if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(secret))
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning))
                    Debug.LogWarning($"[Discovery] ✗ ERROR: Secret is null, empty, or whitespace! Cannot search for servers.", this);
                return;
            }

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);

            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log($"[Discovery] Secret length: {secret.Length} chars, {secretBytes.Length} bytes. Content: '{secret}'", this);

            if (secretBytes.Length < 3)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning))
                    Debug.LogWarning($"[Discovery] ✗ WARNING: Secret is very short ({secretBytes.Length} bytes)! Make sure it's configured in the Inspector.", this);
            }

            // Try both broadcast and localhost
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            IPEndPoint localhostEndPoint = new IPEndPoint(IPAddress.Loopback, port);

            int attemptCount = 0;

            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log($"[Discovery] Starting server discovery on port {port} with secret '{secret}' ({secretBytes.Length} bytes)", this);

            while (_clientUdpClient != null)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(discoveryInterval));

                    attemptCount++;

                    if (NetworkManager.StaticCanLog(LoggingType.Common))
                        Debug.Log($"[Discovery] Attempt #{attemptCount} - Sending discovery requests...", this);

                    // Try broadcast first
                    try
                    {
                        int sentBytes = await _clientUdpClient.SendAsync(secretBytes, secretBytes.Length, broadcastEndPoint);
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Sent {sentBytes} bytes to broadcast", this);
                    }
                    catch (Exception ex)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Broadcast send failed: {ex.Message}", this);
                    }

                    // Also try localhost (important for testing on same machine)
                    try
                    {
                        int sentBytes = await _clientUdpClient.SendAsync(secretBytes, secretBytes.Length, localhostEndPoint);
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Sent {sentBytes} bytes to localhost", this);
                    }
                    catch (Exception ex)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Localhost send failed: {ex.Message}", this);
                    }

                    // Wait for responses with proper timeout using CancellationToken
                    float timeoutSeconds = 5f;
                    if (NetworkManager.StaticCanLog(LoggingType.Common))
                        Debug.Log($"[Discovery] Waiting for response (timeout: {timeoutSeconds}s)...", this);

                    if (_clientUdpClient == null)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Client was closed while waiting", this);
                        break;
                    }

                    try
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            var receiveTask = _clientUdpClient.ReceiveAsync();
                            UdpReceiveResult result = await receiveTask;

                            if (NetworkManager.StaticCanLog(LoggingType.Common))
                                Debug.Log($"[Discovery] ✓ Received response from {result.RemoteEndPoint}, buffer: {result.Buffer.Length} bytes = {string.Join(",", result.Buffer)}", this);

                            if (result.Buffer.Length > 0)
                            {
                                bool isValid = BitConverter.ToBoolean(result.Buffer, 0);
                                if (NetworkManager.StaticCanLog(LoggingType.Common))
                                    Debug.Log($"[Discovery] Response valid: {isValid}", this);

                                if (isValid)
                                {
                                    if (NetworkManager.StaticCanLog(LoggingType.Common))
                                        Debug.Log($"[Discovery] ✓✓✓ Found server at {result.RemoteEndPoint}", this);

                                    ServerFoundCallback?.Invoke(result.RemoteEndPoint);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Attempt #{attemptCount} - Timeout ({timeoutSeconds}s). No server response. Retrying...", this);
                    }
                    catch (ObjectDisposedException disposedEx)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Common))
                            Debug.Log($"[Discovery] Client socket was closed. Stopping search.", this);
                        break;
                    }
                    catch (Exception receiveEx)
                    {
                        if (NetworkManager.StaticCanLog(LoggingType.Warning))
                            Debug.LogWarning($"[Discovery] Error receiving response: {receiveEx.GetType().Name} - {receiveEx.Message}", this);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Client was closed, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    if (NetworkManager.StaticCanLog(LoggingType.Warning))
                        Debug.LogWarning($"[Discovery] Error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", this);
                }
            }

            if (NetworkManager.StaticCanLog(LoggingType.Common))
                Debug.Log("[Discovery] Server discovery stopped.", this);
        }

        #endregion
    }
}
