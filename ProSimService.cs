using System;
using System.Threading.Tasks;
using ProSimSDK;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Owns the ProSimConnect lifecycle: connect, disconnect, status,
    /// and the simulator.pause DataRef subscription.
    /// All events are raised on a background thread — callers must marshal
    /// back to the UI thread as needed.
    /// </summary>
    internal class ProSimService
    {
        // ── Public events ────────────────────────────────────────────────────────

        /// <summary>Raised (on the connection thread) when ProSim connects.</summary>
        public event Action Connected;

        /// <summary>Raised (on the connection thread) when ProSim disconnects.</summary>
        public event Action Disconnected;

        /// <summary>Raised whenever the simulator.pause state changes.</summary>
        public event Action<bool> SimPauseChanged;

        public event Action<string> InfoLog;
        public event Action<string> ErrorLog;

        // ── State ────────────────────────────────────────────────────────────────

        public bool IsConnected => _connection.isConnected;
        public bool IsPaused    { get; private set; }

        // ── Internals ────────────────────────────────────────────────────────────

        private readonly ProSimConnect _connection = new ProSimConnect();
        private DataRef _pauseDataRef;

        /// <summary>Exposes the raw connection object for Phidgets device constructors.</summary>
        public ProSimConnect Connection => _connection;

        // ── Constructor ──────────────────────────────────────────────────────────

        public ProSimService()
        {
            _connection.onConnect    += OnConnect;
            _connection.onDisconnect += OnDisconnect;
        }

        // ── Connect / disconnect ─────────────────────────────────────────────────

        /// <summary>Initiates an async connection to ProSim at <paramref name="ip"/>.</summary>
        public async Task ConnectAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
            try
            {
                RaiseInfo("ProSim connecting to " + ip + "…");
                await Task.Run(() => _connection.Connect(ip));
            }
            catch (Exception ex)
            {
                RaiseError("Cannot connect to ProSim: " + ex.Message);
            }
        }

        /// <summary>
        /// Tears down input DataRefs and cleans up.
        /// ProSimConnect has no Disconnect() method; the closest equivalent is
        /// to stop all subscribed DataRefs and let the app release the object.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _pauseDataRef = null;   // GC will clean up; no explicit close API
                RaiseInfo("ProSim service disconnected.");
            }
            catch (Exception ex)
            {
                RaiseError("Error during ProSim disconnect: " + ex.Message);
            }
        }

        // ── simulator.pause subscription ─────────────────────────────────────────

        /// <summary>
        /// Subscribes to the simulator.pause DataRef.
        /// Must be called after a successful connect (connection must be live).
        /// </summary>
        public void SubscribePause()
        {
            try
            {
                _pauseDataRef = new DataRef("simulator.pause", 100, _connection);
                _pauseDataRef.onDataChange += OnPauseDataChange;
            }
            catch (Exception ex)
            {
                RaiseError("Error subscribing to simulator.pause: " + ex.Message);
            }
        }

        // ── Private handlers ─────────────────────────────────────────────────────

        private void OnConnect()    => Connected?.Invoke();
        private void OnDisconnect() => Disconnected?.Invoke();

        private void OnPauseDataChange(DataRef dataRef)
        {
            try
            {

                bool paused = Convert.ToBoolean(dataRef.value);
                IsPaused = paused;
                SimPauseChanged?.Invoke(paused);
            }
            catch (Exception ex)
            {
                RaiseError("ERROR: simulator.pause value '" + dataRef.value + "': " + ex.Message);
            }
        }

        private void RaiseInfo(string msg)  => InfoLog?.Invoke(msg);
        private void RaiseError(string msg) => ErrorLog?.Invoke(msg);
    }
}
