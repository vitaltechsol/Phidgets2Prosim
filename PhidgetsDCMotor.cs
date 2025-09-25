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

		public double[] Range
		{
			get => range;
			set
			{
				if (value == null || value.Length != 2)
					throw new ArgumentException("Range must be a double[2], e.g., new[] { -1, 1 } or new[] { 0, 1 }.");
				if (value[0] == value[1])
					throw new ArgumentException("Range min and max cannot be the same.");

				// normalize order
				double a = value[0], b = value[1];
				range = a <= b ? new double[] { a, b } : new double[] { b, a };
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

				// Set ProSim dataref
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

			Debug.WriteLine("trim name " + dataRef.name);
			// var name = dataRef.name;
			try
			{
				var value = Convert.ToBoolean(dataRef.value);

				Debug.WriteLine(dataRef.name);
				Debug.WriteLine(value);

				pulseIntervalPauseReduced = PulsateMotorIntervalPause;

				if (dataRef.name == prosimDatmRefFwd && !isPaused)
				{
					if (value == true)
					{
						currentVel = targetVelFwd * -1;

						isMotorMoving = true;
						dcMotor.TargetVelocity = currentVel;
						if (pulsateMotor)
						{
							if (pulsateTimer == null)
							{
								pulsateTimer = new System.Timers.Timer();
								pulsateTimer.Interval = PulsateMotorInterval;
								pulsateTimer.Elapsed += PulsateMotor;
								pulsateTimer.Start();
							}
						}
					}
					else
					{
						isMotorMoving = false;
						if (pulsateTimer != null)
						{
							pulsateTimer.Stop();
							pulsateTimer.Dispose();
							pulsateTimer = null;
						}
						currentVel = 0;
						dcMotor.TargetVelocity = 0.5;
						Thread.Sleep(200);
						dcMotor.TargetVelocity = currentVel;
					}
				}

				if (dataRef.name == prosimDatmRefBwd && !isPaused)
				{
					if (value == true)
					{
						currentVel = targetVelBwd;
						isMotorMoving = true;
						dcMotor.TargetVelocity = currentVel;
						if (pulsateMotor)
						{
							if (pulsateTimer == null)
							{
								pulsateTimer = new System.Timers.Timer();
								pulsateTimer.Interval = PulsateMotorInterval;
								pulsateTimer.Elapsed += PulsateMotor;
								pulsateTimer.Start();
							}
						}
					}
					else
					{
						isMotorMoving = false;
						if (pulsateTimer != null)
						{
							pulsateTimer.Stop();
							pulsateTimer.Dispose();
							pulsateTimer = null;
						}
						currentVel = 0;
						dcMotor.TargetVelocity = -0.5;
						Thread.Sleep(200);
						dcMotor.TargetVelocity = currentVel;
					}
				}
			}
			catch (Exception ex)
			{
				// Stop motor
				currentVel = 0;
				dcMotor.TargetVelocity = 0;
				Debug.WriteLine(ex.ToString());
				Debug.WriteLine("value " + dataRef.value);
			}

		}

		public void changeTargetFwdVelocity(double val)
		{
			targetVelFwd = val;
		}

		public void changeTargetBwdVelocity(double val)
		{
			targetVelBwd = val;
		}

		public void changeTargetVelocity(double val)
		{
			targetVelFwd = val;
			targetVelBwd = val;
		}

		public void pause(bool isPaused)
		{
			this.isPaused = isPaused;
			if (isPaused == true)
			{
				dcMotor.TargetVelocity = 0;
			}
			else
			{
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

		/// Map a logical velocity in [Range[0], Range[1]] to physical [-1, 1].
		private double MapToPhysical(double logical)
		{
			double rMin = range[0], rMax = range[1];
			double t = (logical - rMin) / (rMax - rMin); // 0..1
			double physical = -1 + (t * 2);              // -1..1
			if (double.IsNaN(physical) || double.IsInfinity(physical)) return 0;
			return Math.Max(-1, Math.Min(1, physical));  // clamp
		}

		private async void PulsateMotor(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (isMotorMoving)
			{
				dcMotor.TargetVelocity = 0;
				await Task.Delay(pulseIntervalPauseReduced);
				pulseIntervalPauseReduced -= 30;
				if (pulseIntervalPauseReduced < 0) { pulseIntervalPauseReduced = 0; }
				dcMotor.TargetVelocity = currentVel;
			}
		}
	}
}

