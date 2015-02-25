//  Helios is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  Helios is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace GadrocsWorkshop.Helios.Interfaces.SSOHListener
{
    using GadrocsWorkshop.Helios.TCPInterface;

    public class SSOHAlert : TCPFunction
    {
        private string _id;
        private string _format;

        private string _pushActionData;
        private string _releaseActionData;
        private string _pushValue;
        private string _releaseValue;

        private HeliosAction _pushAction;
        private HeliosAction _releaseAction;
        private HeliosTrigger _pushedTrigger;
        private HeliosTrigger _releasedTrigger;
        private HeliosValue _value;

        // advise Helios (input tab) that an alert has been received
        // advise the interface (output tab) that an acknowledgement has been performed

        //private List<IBindingTrigger> _triggers = new List<IBindingTrigger>();
        //_alertTrigger = new HeliosTrigger(this, "", "SSOH FAIL alert", "received", "Fires when an SSOH FAIL alert is received.", "Always returns true.", BindingValueUnits.Boolean);
         //   _acknowledgeAction = new HeliosAction(this, "", "", "ACK", "Acknowledge the SSOH FAIL alert.", "TODO", BindingValueUnits.Boolean);
         //   _acknowledgeAction.Execute += new HeliosActionHandler(Acknowledge_ExecuteAction);

         //   _triggers.Add(_alertTrigger);
         //   Actions.Add(_acknowledgeAction);

        public SSOHAlert(BaseTCPInterface sourceInterface, string deviceId, string buttonId, string argId, string device, string name)
            : this(sourceInterface, deviceId, buttonId, argId, device, name, "1.0", "0.0", "%.1f")
        {
        }

        public SSOHAlert(BaseTCPInterface sourceInterface, string deviceId, string buttonId, string argId, string device, string name, string pushValue, string releaseValue, string exportFormat)
            : base(sourceInterface)
        {
            _id = argId;
            _format = exportFormat;
            _pushValue = pushValue;
            _releaseValue = releaseValue;

            _value = new HeliosValue(sourceInterface, new BindingValue(false), device, name, "Current state of the alert.", "True if the alert is currently active, otherwise false.", BindingValueUnits.Boolean);
            Values.Add(_value);
            Triggers.Add(_value);

            _pushedTrigger = new HeliosTrigger(sourceInterface, device, name, "ALERT", "Fired when an alert is received from the remote client.");
            // Passes straight through to the gauge - can it not do something first like setting state?
            Triggers.Add(_pushedTrigger);
            // The incoming message latches, so a release action is inappropriate (I think!)
            // _releasedTrigger = new HeliosTrigger(sourceInterface, device, name, "released", "Fired when this button is released in the simulator.");
            // Triggers.Add(_releasedTrigger);

            _pushAction = new HeliosAction(sourceInterface, device, name, "ACK", "Fire to acknowledge the Alert.");
            _pushAction.Execute += new HeliosActionHandler(PushAction_Execute);
            Actions.Add(_pushAction);
            // Superfluous?
            // _releaseAction = new HeliosAction(sourceInterface, device, name, "ACK Release", "Releases the button in the simulator.");
            // _releaseAction.Execute += new HeliosActionHandler(ReleaseAction_Execute);
            // Actions.Add(_releaseAction);
        }

        void ReleaseAction_Execute(object action, HeliosActionEventArgs e)
        {
            // not used
        }

        void PushAction_Execute(object action, HeliosActionEventArgs e)
        {
            // do something now that the Ack button is pressed
        }

        public override void ProcessNetworkData(string id, string value)
        {
            if (value.Equals(_pushValue))
            {
                _value.SetValue(new BindingValue(true), false);
                _pushedTrigger.FireTrigger(BindingValue.Empty);
            }

            else if (value.Equals(_releaseValue))
            {
                _value.SetValue(new BindingValue(false), false);
                _releasedTrigger.FireTrigger(BindingValue.Empty);
            }
        }

        public override void Reset()
        {
            _value.SetValue(BindingValue.Empty, true);
        }

    }
}
