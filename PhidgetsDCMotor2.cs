using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsDCMotor2 : MotorBase
    {
        // ---- ProSim refs (optional) -----------------------------------------
        public string RefTurnOn { get; set; } = string.Empty;
        public string RefCurrentPos { get; set; } = string.Empty; // if you also mirror position back
        public string RefTargetPos { get; set; } = string.Empty;  // used by UseRefTarget()

        // ---- Motor direction helpers ----------------------------------------
        public bool Reversed { get; set; } = false;
		public double CurrentLimit { get; set; }

		private readonly DCMotor _motor = new DCMotor();

        public PhidgetsDCMotor2(
            int deviceSerialNumber,
            int hubPort,
            ProSimConnect connection,
            MotorTuningOptions options = null
            ) : base(options)
        {
            Connection = connection;
            Serial = deviceSerialNumber;
            HubPort = hubPort;
            Channel = 0; // default DC motor channel
        }

        public override async Task InitializeAsync()
        {
            try
            {
                await base.InitializeAsync();

                _motor.HubPort = HubPort;
                _motor.Channel = Channel;
                if (HubPort >= 0)
                {
                    _motor.HubPort = HubPort;
                    _motor.IsRemote = true;
                }
                _motor.DeviceSerialNumber = Serial;
                _motor.Open(5000);
                if (Acceleration <= 0) Acceleration = 50;
                _motor.Acceleration = Acceleration;
				_motor.CurrentLimit = 4;
				

				SendInfoLog($"DCMotor Connected: serial={Serial} hub={HubPort} ch={Channel} accel={Acceleration}");
            }
            catch (Exception ex)
            {
                SendErrorLog($"Open Fail for DC Motor {Serial}: {HubPort} ch={Channel}");
                SendErrorLog(ex.ToString());
            }
        }

        protected override void ApplyVelocity(double velocity)
        {
            // Clamp and apply direction
            double v = Math.Max(-1.0, Math.Min(1.0, velocity));
            if (Reversed) v = -v;

            if (!_motor.Attached) return;

            _motor.TargetVelocity = v;
            Debug.WriteLine($"[ApplyVelocity] v={v:F3} (reversed={Reversed})");
        }

        // ---- Hook ProSim target ref to controller ---------------------------

        /// <summary>Subscribe to a ProSim data ref; on change, move to mapped target.</summary>
        public void UseRefTarget(string refTargetName)
        {
            if (string.IsNullOrWhiteSpace(refTargetName)) return;

            DataRef dataRef = new DataRef(refTargetName, 100, Connection);
            dataRef.onDataChange += DataRef_onTargetChange;
            SendInfoLog($"Subscribed to target ref: {refTargetName}");
        }

        private async void DataRef_onTargetChange(DataRef dataRef)
        {
            try
            {
                var value = Convert.ToDouble(dataRef.value);
                Debug.WriteLine($"Target ref name:{dataRef.name} - Moving to: {dataRef.value} ");

                await OnTargetMoving(
                    movingTo: value
                );
            }
            catch (Exception ex)
            {
                // Stop motor safely
                ApplyVelocity(0);
                SendErrorLog($"DataRef_onTargetChange error: {ex}\nvalue={dataRef.value}");
            }
        }
    }
}