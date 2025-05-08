using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace Phidgets2Prosim
{

    internal class PhidgetsMultiInput : PhidgetDevice
    {
        string prosimDataRef;
        ProSimConnect connection;
        PhidgetsMultiInputItem[] multiInputs;
        string allValues = "";
        Dictionary<string, int> inputsRefs;
        int[] channels;

        public PhidgetsMultiInput(
            int serial,
            int hubPort,
            int[] channels,
            ProSimConnect connection,
            string prosimDataRef, 
            Dictionary<string, int> inputsRefs)

        {

            this.prosimDataRef = prosimDataRef;
            this.connection = connection;
            this.inputsRefs = inputsRefs;
            this.channels = channels;
            allValues = new string('0', channels.Length);
            multiInputs = new PhidgetsMultiInputItem[10];

            var index = 0;

            foreach (var channel in channels)
            {
                multiInputs[index] = new PhidgetsMultiInputItem(serial, hubPort, channel, index);
                multiInputs[index].InputChanged += HandleInputChanged;
                multiInputs[index].ErrorLog += SendErrorLog;
                multiInputs[index].InfoLog += SendInfoLog;
                multiInputs[index].Open();
                index++;
            }
        }

        public void Close() 
        {
            var index = 0;
            foreach (var channel in channels)
            {
                multiInputs[index].Close();
                index++;
            }
        }

        private void HandleInputChanged(object sender, InputChangedEventArgs e)
        {
            // Event handler to receive input change
            int index = e.Index;
            bool state = e.State;

            // Update value based on index
            allValues = UpdateStringAtIndex(allValues, index, state);
            SendInfoLog($"State received: {e.State} new values {allValues}");


            //Update ref
            int dataRefVal;
            if (!inputsRefs.TryGetValue(allValues, out dataRefVal))
            {
                // the key isn't in the dictionary.
                return; 
            }
            SendInfoLog($"Update values: {allValues} REF: {dataRefVal} VAL: {dataRefVal}");
            UpdateRef(dataRefVal);
        }

        static string UpdateStringAtIndex(string inputString, int index, bool value)
        {
            // Check if the index is within the valid range
            if (index < 0 || index >= inputString.Length)
            {
                Debug.WriteLine("ERROR: UpdateStringAtIndex. Invalid index");
                return inputString;
            }

            // Update the string at the specified index based on the boolean value
            char newValue = value ? '1' : '0';
            char[] charArray = inputString.ToCharArray();
            charArray[index] = newValue;

            // Convert the character array back to a string
            return new string(charArray);
        }

        private void UpdateRef(int inputValue)
        {

            DataRef dataRef = new DataRef(prosimDataRef, 100, connection);

            try
            {
                dataRef.value = inputValue;
                SendInfoLog($"--> Multi - {prosimDataRef} | val: {inputValue}");

            }
            catch (System.Exception ex)
            {
                //SendErrorLog("Error: Multi Input " + prosimDataRef + " - Value:" + inputValue);
                //SendErrorLog(ex.ToString());
            }
        }

    }


    internal class PhidgetsMultiInputItem : PhidgetDevice
    {
        DigitalInput digitalInput = new DigitalInput();
        int Index { get; set; } = -1;

        private int hubPort;
        private int serial;

        public event EventHandler<InputChangedEventArgs> InputChanged;

        public bool State { get; set; } = false;
        public PhidgetsMultiInputItem(int serial, int hubPort, int channel, int index)
        {
            Channel = channel;
            Index = index;
            this.hubPort = hubPort;
            this.serial = serial;
                    }

        public async void Open()
        {
            try
            {
                if (hubPort >= 0)
                {
                    digitalInput.HubPort = hubPort;
                    digitalInput.IsRemote = true;
                }
                digitalInput.Channel = Channel;
                digitalInput.StateChange += StateChange;
                digitalInput.DeviceSerialNumber = serial;
                await Task.Run(() => digitalInput.Open(500));
            }
            catch (Exception ex)
            {
                SendErrorLog($"Multi input Open failed - {ProsimDataRef} to {serial} [{hubPort}] Ch:{Channel}");
                SendErrorLog(ex.ToString());
            }
        }


        public void Close()
        {
            try
            {
                digitalInput.Close();
                SendInfoLog($"--> Multi - {ProsimDataRef} | ch: {Channel} CLOSED" );

            }
            catch (Exception ex)
            {
                SendErrorLog("Multi input Open failed to Close" + ProsimDataRef + " ch:" + Channel);
                SendErrorLog(ex.ToString());
            }
        }

        private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
        {
            SendInfoLog($"--> Multi [{HubPort}] Ch {Channel}: {e.State} | Ref: {ProsimDataRef}");
            State = e.State;
            InputChanged.Invoke(this, new InputChangedEventArgs(Index, Channel, State));
        }

    }

    public class InputChangedEventArgs : EventArgs
    {
        public int Index { get; }
        public int Channel { get; }
        public bool State { get; }

        public InputChangedEventArgs(int index, int channel, bool state)
        {
            Index = index;
            Channel = channel;
            State = state;
        }
    }
}
