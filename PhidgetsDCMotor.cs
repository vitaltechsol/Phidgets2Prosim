using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Phidgets2Prosim
{
	internal class PhidgetsDCMotor : PhidgetDevice
	{
		// ProSim datarefs
		string prosimDatmRefBwd;
		string prosimDatmRefFwd;

		// Logical target velocities (expressed in the configured Range space)
		double targetVelFwd = 1;  // logical "full forward" in your chosen Range
		double targetVelBwd = 1;  // logical "full backward" in your chosen Range

		// Current PHYSICAL velocity we’re commanding to the Phidgets motor (always in [-1, 1])
		double currentVel = 0;

		bool isPaused = false;
		private bool isMotorMoving = false;
		private System.Timers.Timer pulsateTimer;

		public bool pulsateMotor { get; set; } = false;
		public int PulsateMotorInterval { get; set; } = 550;
		public int PulsateMotorIntervalPause { get; set; } = 200;
		private int pulseIntervalPauseReduced = 0;

		// ---- NEW: Logical input range (e.g., [-1,1] default, or [0,1]) ----
		private double[] range = new double[] { -1, 1 };

		// Logical input range [min, max]. Defaults to [-1, 1].
		// Set to [0, 1] to use 0=full back, 0.5=stop, 1=full forward.
		public double[] Range
		{
			get => (double[])range.Clone();
			set
			{
				if (value == null || value.Length != 2)
					throw new ArgumentException("Range must be a double[2], e.g., new[] { -1, 1 } or new[] { 0, 1 }.");
				if (value[0] == value[1])
					throw new ArgumentException("Range min and max cannot be the same.");

				// normalize/ensure ascending
				var a = value[0];
				var b = value[1];
				range[0] = Math.Min(a, b);
				range[1] = Math.Max(a, b);
			}
		}

		// Phidgets DC motor
		DCMotor dcMotor = new DCMotor();

		public PhidgetsDCMotor(int serial, int hubPort, string prosimDataRefFwd, string prosimDataRefBwd, ProSimConnect connection)
		{
			try
			{
				HubPort = hubPort;
				this.prosimDatmRefBwd = prosimDataRefBwd;
				this.prosimDatmRefFwd = prosimDataRefFwd;

				if (HubPort >= 0)
				{
					dcMotor.HubPort = HubPort;
					dcMotor.IsRemote = true;
				}

				dcMotor.Open(5000);
				dcMotor.DeviceSerialNumber = serial;
				dcMotor.Acceleration = 100;
				dcMotor.TargetBrakingStrength = 1;

				// Subscribe to ProSim dataRefs (forward & backward)
				DataRef dataRefFwd = new DataRef(prosimDataRefFwd, 100, connection);
				DataRef dataRefBwd = new DataRef(prosimDataRefBwd, 100, connection);

				dataRefFwd.onDataChange += DataRef_onDataChange;
				dataRefBwd.onDataChange += DataRef_onDataChange;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				Debug.WriteLine("prosimDatmRefCW " + prosimDataRefFwd);
			}
		}

		private async void DataRef_onDataChange(DataRef dataRef)
		{
			try
			{
				var value = Convert.ToBoolean(dataRef.value);
				pulseIntervalPauseReduced = PulsateMotorIntervalPause;

				// Precompute logical midpoints for “coast” and neutral based on current Range
				double rMin = Range[0];
				double rMax = Range[1];
				double neutralLogical = (rMin + rMax) / 2.0; // logical center (maps to physical 0)
				double coastForwardLogical = neutralLogical + (rMax - neutralLogical) / 2.0; // maps ~ +0.5 physical
				double coastBackwardLogical = neutralLogical - (neutralLogical - rMin) / 2.0; // maps ~ -0.5 physical

				// Forward command
				if (dataRef.name == prosimDatmRefFwd && !isPaused)
				{
					if (value)
					{
						// Your original logic used "targetVelFwd * -1" to set direction.
						// Interpret "targetVelFwd" as a logical magnitude in Range, then invert to go forward.
						double logical = targetVelFwd * -1.0;
						currentVel = MapToPhysical(logical); // physical [-1,1]
						SendInfoLog($"[DCMotor] FWD ON: logical={logical:F3} → physical={currentVel:F3}");
						dcMotor.TargetVelocity = currentVel;
					}
					else
					{
						// Brief coast, then stop at neutral
						isMotorMoving = false;
						StopPulsateIfAny();

						currentVel = MapToPhysical(neutralLogical);
						dcMotor.TargetVelocity = MapToPhysical(coastForwardLogical);
						Thread.Sleep(200);
						dcMotor.TargetVelocity = currentVel;
					}
				}

				// Backward command
				if (dataRef.name == prosimDatmRefBwd && !isPaused)
				{
					if (value)
					{
						double logical = targetVelBwd;     // logical magnitude in Range (backwards)
						currentVel = MapToPhysical(logical); // physical [-1,1]
						SendInfoLog($"[DCMotor] BWD ON: logical={logical:F3} → physical={currentVel:F3}");
						dcMotor.TargetVelocity = currentVel;
					}
					else
					{
						// Brief coast, then stop at neutral
						isMotorMoving = false;
						StopPulsateIfAny();

						currentVel = MapToPhysical(neutralLogical);
						dcMotor.TargetVelocity = MapToPhysical(coastBackwardLogical);
						Thread.Sleep(200);
						dcMotor.TargetVelocity = currentVel;
					}
				}
			}
			catch (Exception ex)
			{
				// Stop motor on error
				currentVel = 0;
				dcMotor.TargetVelocity = 0;
				Debug.WriteLine(ex.ToString());
				Debug.WriteLine("value " + dataRef.value);
			}
		}

		// ---- Public helpers you already had ----
		public void changeTargetFwdVelocity(double val) => targetVelFwd = val;   // logical, in Range
		public void changeTargetBwdVelocity(double val) => targetVelBwd = val;   // logical, in Range
		public void changeTargetVelocity(double val) { targetVelFwd = val; targetVelBwd = val; }

		public void pause(bool isPaused)
		{
			this.isPaused = isPaused;
			if (isPaused)
			{
				// physical stop
				dcMotor.TargetVelocity = 0;
			}
			else
			{
				// resume to last physical velocity
				dcMotor.TargetVelocity = currentVel;
			}
		}

		private async void Open()
		{
			try
			{
				await Task.Run(() => dcMotor.Open(500));
				SendErrorLog("DC Motor Connected " + HubPort + "ch" + Channel);
			}
			catch (Exception ex)
			{
				SendErrorLog("Open failed for DC Motor " + HubPort);
				SendErrorLog(ex.ToString());
			}
		}

		// ---- Private utilities ----

		private void StartMotor(double physicalVelocity)
		{
			isMotorMoving = true;
			dcMotor.TargetVelocity = physicalVelocity;

			if (pulsateMotor && pulsateTimer == null)
			{
				pulsateTimer = new System.Timers.Timer();
				pulsateTimer.Interval = PulsateMotorInterval;
				pulsateTimer.Elapsed += PulsateMotor;
				pulsateTimer.Start();
			}
		}

		private void StopPulsateIfAny()
		{
			if (pulsateTimer != null)
			{
				pulsateTimer.Stop();
				pulsateTimer.Dispose();
				pulsateTimer = null;
			}
		}

		/// <summary>
		/// Map a logical velocity (in [Range[0], Range[1]]) to Phidgets physical [-1, 1].
		/// </summary>
		private double MapToPhysical(double logical)
		{
			double rMin = range[0], rMax = range[1];
			double span = rMax - rMin;
			if (span == 0) return 0;
			double t = (logical - rMin) / span;   // 0..1
			double physical = (t * 2.0) - 1.0;    // -1..1
			if (double.IsNaN(physical) || double.IsInfinity(physical)) return 0;
			return Math.Max(-1.0, Math.Min(1.0, physical));
		}

		private async void PulsateMotor(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (isMotorMoving)
			{
				// brief stop, then resume
				dcMotor.TargetVelocity = 0;
				await Task.Delay(pulseIntervalPauseReduced);
				pulseIntervalPauseReduced = Math.Max(0, pulseIntervalPauseReduced - 30);
				dcMotor.TargetVelocity = currentVel;
			}
		}
	}
}
