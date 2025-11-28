using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsDCMotor : MotorBase
    {
        // ---- ProSim refs (optional) -----------------------------------------
        public string RefTurnOn { get; set; } = string.Empty;
        public string RefCurrentPos { get; set; } = string.Empty; // if you also mirror position back
        public string RefTargetPos { get; set; } = string.Empty;  // used by UseRefTarget()
        public double CurrentVelocity { get; set; } = 0;

        private bool isPaused = false;

        // ---- Motor direction helpers ----------------------------------------
        public bool Reversed { get; set; } = false;
		public bool Centered01Input { get; set; } = false;
		public double CurrentLimit { get; set; }

        public double TargetBrakingStrength { get; set; }

        private readonly DCMotor _motor = new DCMotor();

        public PhidgetsDCMotor(
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
            TargetBrakingStrength = 1;
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
				_motor.CurrentLimit = (CurrentLimit > 0) ? CurrentLimit : 4;
                _motor.TargetBrakingStrength = TargetBrakingStrength;



                SendInfoLog($"DCMotor Connected: serial={Serial} hub={HubPort} ch={Channel} accel={Acceleration}");
            }
            catch (Exception ex)
            {
                SendErrorLog($"Open Fail for DC Motor {Serial}: {HubPort} ch={Channel}");
                SendErrorLog(ex.ToString());
            }
        }

		/*        protected override void ApplyVelocity(double velocity)
				{
					// Clamp and apply direction
					double v = Math.Max(-1.0, Math.Min(1.0, velocity));
					if (Reversed) v = -v;

					if (!_motor.Attached) return;

					_motor.TargetVelocity = v;
					CurrentVelocity = v;
					Debug.WriteLine($"[ApplyVelocity] v={v:F3} (reversed={Reversed})");
				}
		*/


		protected override void ApplyVelocity(double velocity)
		{
			double v;

			if (Centered01Input)
			{
				// Expect input in [0,1] with 0.5 = stop.
				double x = Math.Max(0.0, Math.Min(1.0, velocity)); 
                // clamp to [0,1]
				// Map [0,1] with center 0.5 back to [-1,1]:
				// 0   -> -1, 0.5 -> 0, 1 -> +1
				v = (x - 0.5) * 2.0;
			}
			else
			{
				// Standard Phidgets-style: already in [-1,1]
				v = Math.Max(-1.0, Math.Min(1.0, velocity));
			}

			if (Reversed) v = -v;

			if (!_motor.Attached) return;

			_motor.TargetVelocity = v;
			CurrentVelocity = v;
			Debug.WriteLine($"[ApplyVelocity] v={v:F3} (reversed={Reversed}, centered01={Centered01Input})");
		}



		/*       public void SetTargetVelocity(double velocity)
			   {
				   // Clamp and apply direction
				   double v = Math.Max(-1.0, Math.Min(1.0, velocity));
				   if (Reversed) v = -v;

				   if (!_motor.Attached) return;

				   _motor.TargetVelocity = v;
				   CurrentVelocity = v;
				   Debug.WriteLine($"[SetTargetVelocity] v={v:F3} (reversed={Reversed})");
			   }
		*/

		public void SetTargetVelocity(double velocity)
		{
			double v;

			if (Centered01Input)
			{
				//
				// In centered-0..1 mode:
				//   0.0 = backwards
				//   0.5 = stop
				//   1.0 = forwards
				//
				// SEND RAW 0..1 DIRECTLY TO PHIDGETS
				//
				v = Math.Max(0.0, Math.Min(1.0, velocity));
			}
			else
			{
				//
				// Normal [-1,1] mode
				//
				v = Math.Max(-1.0, Math.Min(1.0, velocity));
				if (Reversed) v = -v;
			}

			if (!_motor.Attached) return;

			_motor.TargetVelocity = v;
			CurrentVelocity = v;
			Debug.WriteLine(
				$"[SetTargetVelocity] v={v:F3} (reversed={Reversed}, centered01={Centered01Input})"
			);
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

        public void Pause(bool isPaused)
        {
            this.isPaused = isPaused;
            if (isPaused == true)
            {
                _motor.TargetVelocity = 0;
            }
            else
            {
                _motor.TargetVelocity = CurrentVelocity;
            }
        }
    }
}