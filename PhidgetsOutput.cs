using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;

namespace Phidgets2Prosim
{
    internal class PhidgetsOutput : PhidgetDevice
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        private CancellationTokenSource blinkCancellation;
        public int BlinkFastIntervalMs { get; set; } = 300; // Default blink interval
        public int BlinkSlowIntervalMs { get; set; } = 600; // Default blink interval
        public int MaxTimeOn { get; set; } = 0;
        public bool Inverse { get; set; } = false;
        public bool IsGate { get; set; }
        public string ProsimDataRefOff { get; set; }
        public int Delay { get; set; } = 0;
        public double ValueOn { get; set; } = 1;
        public double ValueOff { get; set; } = 0;
        public double ValueDim { get; set; } = 0.7;
		private bool _initFromVarDone = false;

		private string _variable;
		public string Variable
		{
			get => _variable;
			set
			{
				_variable = value;
				SendInfoLog($"[WIRE:OUTPUT] Variable property set to '{_variable ?? "<null>"}'");
				EnsureVariableSubscription();   // << subscribe now that we have a name
			}
		}

		private void TryInitFromVariable()
		{
			if (_initFromVarDone) return;

			try
			{
				if (!digitalOutput.IsOpen || !digitalOutput.Attached)  // <-- only act when attached
				{
					SendInfoLog("[SUB:OUTPUT] Device not attached yet; will init on attach.");
					TryInitFromVariable();
					return;
				}

				if (string.IsNullOrWhiteSpace(_variable))
				{
					SendInfoLog("[SUB:OUTPUT] No variable configured; skipping init.");
					return;
				}

				var init = VariableManager.Get(_variable);
				bool on = init != 0;
				if (Inverse) on = !on;

				var duty = on ? ValueOn : ValueOff;
				digitalOutput.DutyCycle = duty;

				SendInfoLog($"[SUB:OUTPUT] Initial state from '{_variable}' = {init} => {(on ? "ON" : "OFF")} (Duty={duty})");
				_initFromVarDone = true;
			}
			catch (Phidget22.PhidgetException ex)
			{
				SendErrorLog("Initial duty set skipped (device not ready)");
				SendErrorLog(ex.ToString());
				// We'll try again on next attach event
			}
			catch (Exception ex)
			{
				SendErrorLog("Initial duty init failed");
				SendErrorLog(ex.ToString());
			}
		}

		private void EnsureVariableSubscription()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(_variable))
				{
					SendInfoLog("[SUB:OUTPUT] Variable is null/empty — output will not follow a variable.");
					return;
				}
				if (_variableSubscription != null) return;

				SendInfoLog($"[SUB:OUTPUT] Subscribing to Variable '{_variable}'");

				_variableSubscription = VariableManager.Subscribe(_variable, (name, val) =>
				{
					try
					{
						bool on = val != 0;
						if (Inverse) on = !on;

						double duty = on ? ValueOn : ValueOff;

						if (digitalOutput.IsOpen && digitalOutput.Attached)
						{
							digitalOutput.BeginSetDutyCycle(duty, ar =>
							{
								try { digitalOutput.EndSetDutyCycle(ar); } catch { /* ignore */ }
							}, null);
						}
						else
						{
							SendInfoLog("[Var→Output] Skipped write (device not attached yet).");
						}

						SendInfoLog($"[Var→Output] {name} = {val} => {(on ? "ON" : "OFF")} (Duty={duty})");
					}
					catch (Exception ex)
					{
						SendErrorLog("Output (Variable) write failed");
						SendErrorLog(ex.ToString());
					}
				});

				// Do not set duty here — let TryInitFromVariable() handle it when attached.
			}
			catch (Exception ex)
			{
				SendErrorLog("EnsureVariableSubscription failed");
				SendErrorLog(ex.ToString());
			}
		}




		private IDisposable _variableSubscription;

		public PhidgetsOutput(int serial, int hubPort, int channel, string prosimDataRef, ProSimConnect connection, bool isGate = false, string prosimDataRefOff = null)
        {
            IsGate = isGate;
            Channel = channel;
            HubPort = hubPort;
            // Set ProSim dataref
            ProsimDataRef = prosimDataRef;
            ProsimDataRefOff = prosimDataRefOff;
            Serial = serial;
      
            try
            {
                if (HubPort >= 0)
                {
                    digitalOutput.HubPort = HubPort;
                    digitalOutput.IsRemote = true;
                    // use -1 for channel when is a IsHubPortDevice
                    digitalOutput.IsHubPortDevice = Channel == -1;
                }
                digitalOutput.Channel = Channel;
                digitalOutput.DeviceSerialNumber = Serial;
                
                Open();

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDataRef, 5, connection);
                dataRef.onDataChange += DataRef_onDataChange;


          
				if (prosimDataRefOff != null)
                {
                    DataRef dataRef2 = new DataRef(prosimDataRefOff, 100, connection);
                    dataRef2.onDataChange += DataRef_onDataChange;
                }

            }
            catch (Exception ex)
            {
                SendErrorLog(ex.ToString());
            }

        }
        public void SyncOpen()
        {
            _ = Open();
        }

        public async Task TurnOn(double dutyCycle)
        {
            try
            {
                if (Delay > 0)
                {
                    var taskDelay = Task.Delay(Delay);
                    await taskDelay;
                }

                digitalOutput.BeginSetDutyCycle(dutyCycle, delegate (IAsyncResult result)
                {
                    digitalOutput.EndSetDutyCycle(result);

                }, null);

                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [ON ({dutyCycle})] | Ref: {ProsimDataRef}");

                // Turn off after specified time(ms)
                if (MaxTimeOn > 0)
                {
                    SendInfoLog("Start OFF Delay " + MaxTimeOn + " for " + ProsimDataRef + " - Channel " + Channel);
                    var taskDelay2 = Task.Delay(MaxTimeOn);
                    await taskDelay2;
                    await TurnOff(ValueOff);
                }
            }
            catch (Exception ex)
            {
                SendErrorLog($">>> Turn On Output Failed, Port [{HubPort}] | Ch [{Channel}]: [OFF ({dutyCycle})] | Ref: {ProsimDataRef}");
                SendErrorLog(ex.Message.ToString());
            }

        }

        public async Task TurnOff(double dutyCycle)
        {
            try
            {
                digitalOutput.BeginSetDutyCycle(dutyCycle, delegate (IAsyncResult result)
                {
                    digitalOutput.EndSetDutyCycle(result);

                }, null);
                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [OFF ({dutyCycle})] | Ref: {ProsimDataRef}");

            }
            catch (Exception ex)
            {
                SendErrorLog($">>> Turn Off Output Failed, Port [{HubPort}] | Ch [{Channel}]: [OFF ({dutyCycle})] | Ref: {ProsimDataRef}");
                SendErrorLog(ex.Message.ToString());
            }
        }

        public void Close()
        {
            try
            {
            //digitalOutput.Close();
            _variableSubscription?.Dispose();
            digitalOutput.Close();
            }
            catch (Exception ex)
            {
            SendInfoLog($"-> Detached/Closed {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
            }   
         }
         
			

        public async Task Open()
        {
            Debug.WriteLine("<-- OPENING " + ProsimDataRef + " to channel:" + Channel);
            try
            {
                await Task.Run(() => digitalOutput.Open());
             //   Debug.WriteLine("<-- OPENED " + ProsimDataRef + " to channel:" + Channel);
            }
            catch (Exception ex)
            {
                SendErrorLog("ERROR: Open failed for " + ProsimDataRef + " Serial:" + digitalOutput.DeviceSerialNumber);
                SendErrorLog(ex.ToString());
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
            _ = HandleDataChangeAsync(dataRef.name, dataRef.value);
        }

  
        public async Task HandleDataChangeAsync(string name, object refValue)
        {

            try
            {
                if (IsGate)
                {
                    var value = Convert.ToBoolean(refValue);
                    if (value == true && name == ProsimDataRef)
                    {
                        if (Inverse)
                        {
                            await TurnOff(ValueOff);
                        }
                        else
                        {
                            await TurnOn(ValueOn);
                        }
                    }

                    if (value == true && name == ProsimDataRefOff)
                    {
                        SendInfoLog("Turn Off from ProsimDataRefOff" + refValue + " " + name);
                        if (Inverse)
                        {
                            await TurnOn(ValueOn);
                        }
                        else
                        {
                            await TurnOff(ValueOff);
                        }
                    }

                    if (ProsimDataRefOff == null && value == false)
                    {
                        if (Inverse)
                        {
                            await TurnOn(ValueOn);
                        }
                        else
                        {
                            await TurnOff(ValueOff);
                        }
                    }
                }
                else
                {
                    var value = Convert.ToInt32(refValue);
                    if (value == 4) //blink fast
                    {
                        if (blinkCancellation == null)
                        {
                            blinkCancellation = new CancellationTokenSource();
                            _ = BlinkAsyncFast(blinkCancellation.Token);
                        }
                    }
                    else if (value == 3) //blink slow
                    {
                        if (blinkCancellation == null)
                        {
                            blinkCancellation = new CancellationTokenSource();
                            _ = BlinkAsyncSlow(blinkCancellation.Token);
                        }
                    }
                    else
                    {
                        if (Inverse)
                        {   // Dim
                            if (value == 1)
                            {
                                await TurnOn(ValueDim);
                            }
                            // On
                            if (value == 2)
                            {
                                await TurnOff(ValueOff);
                                await StopBlinking();
                            }
                            // Off
                            else if (value == 0)
                            {
                                 await TurnOn(ValueOn);
                            }
                        }
                        else
                        {
                            // Dim
                            if (value == 1)
                            {
                                await TurnOn(ValueDim);
                            }
                            // On
                            if (value == 2)
                            {
                                await TurnOn(ValueOn);
                            }
                            // Off
                            else if (value == 0)
                            {
                                await TurnOff(ValueOff);
                                await StopBlinking();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("DataRef_onDataChange failed for " + ProsimDataRef + " ch:" + Channel);
                SendErrorLog("value " + refValue);
                SendErrorLog(ex.ToString());
            }
        }

        private async Task BlinkAsyncSlow(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    digitalOutput.DutyCycle = digitalOutput.DutyCycle == 1 ? 0 : 1;
                    await Task.Delay(BlinkSlowIntervalMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                digitalOutput.DutyCycle = 0; // Ensure output is off when stopping
            }
        }

        private async Task BlinkAsyncFast(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    digitalOutput.DutyCycle = digitalOutput.DutyCycle == 1 ? 0 : 1;
                    await Task.Delay(BlinkFastIntervalMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                digitalOutput.DutyCycle = 0; // Ensure output is off when stopping
            }
        }
        private async Task StopBlinking()
        {
            blinkCancellation?.Cancel();
            blinkCancellation = null;
        }
    }
}
