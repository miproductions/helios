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
    using GadrocsWorkshop.Helios.ComponentModel;
    using System.Globalization;
    using System.Xml;
    using System.Collections.Generic;
    using GadrocsWorkshop.Helios.TCPInterface;

    [HeliosInterface("Helios.SSOHListener", "SSOH Listener", typeof(SSOHListenerInterfaceEditor), typeof(UniqueHeliosInterfaceFactory), AutoAdd=false)]
    public class SSOHListenerInterface : BaseTCPInterface
    {
        #region Devices
        private const string SSOH_LISTENER = "1";
        #endregion
        #region Buttons
        private const string SSOH_FAIL_ACK = "2";
        #endregion
        
        public SSOHListenerInterface()
            : base("SSOH Listener")
        {
            AddFunction(new SSOHAlert(this, SSOH_LISTENER, SSOH_FAIL_ACK, "0", "HeliosSSOH", "SSOH FAIL"));
        }

        public int SSOHPort
        {
            get
            {
                return 9088; // TODO make this configurable
            }
            set
            {
                // TODO make this configurable
            }
        }

        public override void ReadXml(XmlReader reader)
        {
            SSOHPort = int.Parse(reader.ReadElementString("SSOHPort"), CultureInfo.InvariantCulture);
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("SSOHPort", SSOHPort.ToString(CultureInfo.InvariantCulture));
        }
    }
}
