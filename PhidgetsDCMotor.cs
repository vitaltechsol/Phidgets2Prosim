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
		string prosimDatmRefBwd;
		string prosimDatmRefFwd;
		double targetVelFwd = 1;
		double targetVelBwd = 1;
		double currentVel = 0;
		bool isPaused = false;
		private bool isMotorMoving = false;
		private System.Timers.Timer pulsateTimer;

		public bool pulsateMotor { get; set; } = false;
		public int PulsateMotorInterval { get; set; } = 550;
		public int PulsateMotorIntervalPause { get; set; } = 200;
		private int pulseIntervalPauseReduced = 0;

		private double[] range = new double[] { -1, 1 };

		/// <summary>
		/// Logical input range [min, max]. Defaults to [-1, 1].
		/// Example: set to [0, 1] to use 0 = full back, 0.5 = stop, 1 = full forward.
		/// </summary>
		public double[] Range
		{
			get => range;
			set
			{
				if (value == null || value.Length != 2)
					throw new ArgumentException("Range must be an array with two elements: [min, max]");
				if (value[0] == value[1])
					throw new ArgumentException("Range min and max cannot be the same.");
				range = value;
			}
		}

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

				DataRef dataRef = new DataRef(prosimDataRefFwd, 100, connection);
				DataRef dataRef2 = new DataRef(prosimDataRefBwd, 100, connection);

				dataRef.onDataChange += DataRef_onDataChange;
				dataRef2.onDataChange += DataRef_onDataChange;
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

				if (dataRef.name == prosimDatmRefFwd && !isPaused)
				{
					if (value)
					{
						currentVel = MapToPhysical(targetVelFwd * -1);
						StartMotor(currentVel);
					}
					else
					{
						StopMotorTemporary(0.5);
					}
				}

				if (dataRef.name == prosimDatmRefBwd && !isPaused)
				{
					if (value)
					{
						currentVel = MapToPhysical(targetVelBwd);
						StartMotor(currentVel);
					}
					else
					{
						StopMotorTemporary(-0.5);
					}
				}
			}
			catch (Exception ex)
			{
				currentVel = 0;
				dcMotor.TargetVelocity = 0;
				Debug.WriteLine(ex.ToString());
				Debug.WriteLine("value " + dataRef.value);
			}
		}

		private void StartMotor(double velocity)
		{
			isMotorMoving = true;
			dcMotor.TargetVelocity = velocity;
			if (pulsateMotor && pulsateTimer == null)
			{
				pulsateTimer = new System.Timers.Timer();
				pulsateTimer.Interval = PulsateMotorInterval;
				pulsateTimer.Elapsed += PulsateMotor;
				pulsateTimer.Start();
			}
		}

		private void StopMotorTemporary(double coastVelocity)
		{
			isMotorMoving = false;
			if (pulsateTimer != null)
			{
				pulsateTimer.Stop();
				pulsateTimer.Dispose();
				pulsateTimer = null;
			}
			currentVel = 0;
			dcMotor.TargetVelocity = coastVelocity;
			Thread.Sleep(200);
			dcMotor.TargetVelocity = currentVel;
		}

		/// <summary>
		/// Converts a logical velocity in [Range[0], Range[1]] to physical [-1,1]
		/// </summary>
		private double MapToPhysical(double logical)
		{
			double rMin = range[0];
			double rMax = range[1];
			return -1 + ((logical - rMin) / (rMax - rMin)) * 2;
		}

		public void changeTargetFwdVelocity(double val) => targetVelFwd = val;
		public void changeTargetBwdVelocity(double val) => targetVelBwd = val;
		public void changeTargetVelocity(double val) { targetVelFwd = val; targetVelBwd = val; }

		public void pause(bool isPaused)
		{
			this.isPaused = isPaused;
			dcMotor.TargetVelocity = isPaused ? 0 : currentVel;
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

		private async void PulsateMotor(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (isMotorMoving)
			{
				dcMotor.TargetVelocity = 0;
				await Task.Delay(pulseIntervalPauseReduced);
				pulseIntervalPauseReduced = Math.Max(0, pulseIntervalPauseReduced - 30);
				dcMotor.TargetVelocity = currentVel;
			}
		}
	}
}
