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

namespace GadrocsWorkshop.Helios.TCPInterface
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;

    public class BaseTCPInterface : HeliosInterface
    {
        private TCPFunctionCollection _functions = new TCPFunctionCollection();
        private Dictionary<string, TCPFunction> _functionsById = new Dictionary<string, TCPFunction>();

        private int _port = 9088;
        private Socket _socket = null;
        private EndPoint _bindEndPoint;
        private EndPoint _client = null;
        private string _clientID = "";

        private bool _started = false;

        public AsyncCallback _socketDataCallback = null;
        private byte[] _dataBuffer = new byte[2048];

        private HeliosTrigger _connectedTrigger;
        private HeliosProfile _profile = null;

        private string[] _tokens = new string[1024];
        private int _tokenCount = 0;

        public BaseTCPInterface(string name)
            : base(name)
        {
            _socketDataCallback = new AsyncCallback(OnDataReceived);

            _connectedTrigger = new HeliosTrigger(this, "", "", "Connected", "Fired when a new client is connected.");
            Triggers.Add(_connectedTrigger);

            _functions.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Functions_CollectionChanged);
        }

        void Functions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                foreach (TCPFunction function in e.OldItems)
                {
                    Triggers.RemoveSlave(function.Triggers);
                    Actions.RemoveSlave(function.Actions);
                    Values.RemoveSlave(function.Values);
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                foreach (TCPFunction function in e.NewItems)
                {
                    Triggers.AddSlave(function.Triggers);
                    Actions.AddSlave(function.Actions);
                    Values.AddSlave(function.Values);

                }
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (!_port.Equals(value))
                {
                    int oldValue = _port;
                    _port = value;
                    OnPropertyChanged("TCPPort", oldValue, value, false);
                }
            }
        }

        public TCPFunctionCollection Functions
        {
            get
            {
                return _functions;
            }
        }

        protected override void OnProfileChanged(HeliosProfile oldProfile)
        {
            base.OnProfileChanged(oldProfile);
            if (oldProfile != null)
            {
                oldProfile.ProfileStarted -= new EventHandler(Profile_ProfileStarted);
                oldProfile.ProfileStopped -= new EventHandler(Profile_ProfileStopped);
            }

            if (Profile != null)
            {
                Profile.ProfileStarted += new EventHandler(Profile_ProfileStarted);
                Profile.ProfileStopped += new EventHandler(Profile_ProfileStopped);
            }
        }

        // TODO work here
        private void WaitForData()
        {
            if (_started)
            {
                ConfigManager.LogManager.LogDebug("UDP interface waiting for socket data. (Interface=\"" + Name + "\")");
                try
                {
                    _socket.BeginReceiveFrom(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, ref _bindEndPoint, _socketDataCallback, null);
                }
                catch (SocketException se)
                {
                    if (HandleSocketException(se))
                    {
                        _socket.BeginReceiveFrom(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, ref _bindEndPoint, _socketDataCallback, null);
                    }
                    else
                    {
                        ConfigManager.LogManager.LogError("UDP interface unable to recover from socket reset, no longer recieving data. (Interface=\"" + Name + "\")");
                    }
                }
            }
        }

        // TODO work here
        private void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                if (_socket != null && _started)
                {
                    int receivedByteCount = _socket.EndReceiveFrom(asyn, ref _client);
                    if (receivedByteCount > 12)
                    {
                        // Don't create the extra strings if we don't need to
                        if (ConfigManager.LogManager.LogLevel == LogLevel.Debug)
                        {
                            ConfigManager.LogManager.LogDebug("UDP Interface received packet. (Interfae=\"" + Name + "\", Packet=\"" + System.Text.Encoding.UTF8.GetString(_dataBuffer, 0, receivedByteCount) + "\")");
                        }

                        String packetClientID = System.Text.Encoding.UTF8.GetString(_dataBuffer, 0, 8);
                        if (!_clientID.Equals(packetClientID))
                        {
                            ConfigManager.LogManager.LogInfo("UDP interface new client connected, sending data reset command. (Interface=\"" + Name + "\", Client=\"" + _client.ToString() + "\", Client ID=\"" + packetClientID + "\")");
                            _connectedTrigger.FireTrigger(BindingValue.Empty);
                            _clientID = packetClientID;
                            SendData("R");
                        }

                        _tokenCount = 0;
                        int parseCount = receivedByteCount - 1;
                        int lastIndex = 8;
                        for (int i = 9; i < parseCount; i++)
                        {
                            if (_dataBuffer[i] == 0x3a || _dataBuffer[i] == 0x3d)
                            {
                                int size = i - lastIndex - 1;
                                _tokens[_tokenCount++] = System.Text.Encoding.UTF8.GetString(_dataBuffer, lastIndex + 1, size);
                                lastIndex = i;
                            }
                        }
                        _tokens[_tokenCount++] = System.Text.Encoding.UTF8.GetString(_dataBuffer, lastIndex + 1, parseCount - lastIndex - 1);

                        if (_tokenCount % 1 > 0)
                        {
                            _tokenCount--;
                        }

                        _profile.Dispatcher.Invoke((Action)ProcessData, System.Windows.Threading.DispatcherPriority.Send);
                    }
                    else
                    {
                        ConfigManager.LogManager.LogWarning("UDP interface short packet received. (Interface=\"" + Name + "\")");
                    }
                }
            }
            catch (SocketException se)
            {
                HandleSocketException(se);
            }
            catch (Exception e)
            {
                ConfigManager.LogManager.LogError("UDP interface threw unhandled exception processing packet. (Interface=\"" + Name + "\")", e);
            }

            WaitForData();
        }

        // TODO work here
        private void ProcessData()
        {
            for (int i = 0; i < _tokenCount; i += 2)
            {
                if (_functionsById.ContainsKey(_tokens[i]))
                {
                    TCPFunction function = _functionsById[_tokens[i]];
                    function.ProcessNetworkData(_tokens[i], _tokens[i + 1]);
                }
                else
                {
                    ConfigManager.LogManager.LogWarning("UDP interface recieved data for missing function. (Key=\"" + _tokens[i] + "\")");
                }
            }

        }
        
        // TODO work here
        private bool HandleSocketException(SocketException se)
        {
            if (se.ErrorCode == 10054)
            {
                _socket.Close();
                _socket = null;
                _socket = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Dgram,
                                          ProtocolType.Udp);
                _socket.Bind(_bindEndPoint);
                _clientID = "";
                _client = new IPEndPoint(IPAddress.Any, 0);
                return true;
            }
            else
            {
                ConfigManager.LogManager.LogError("UDP interface threw unhandled exception handling socket reset. (Interface=\"" + Name + "\")", se);
                return false;
            }
        }

        // TODO work here
        public void SendData(string data)
        {
            try
            {
                if (_client != null && _clientID.Length > 0)
                {
                    ConfigManager.LogManager.LogDebug("UDP interface sending data. (Interface=\"" + Name + "\", Data=\"" + data + "\")");
                    byte[] sendData = System.Text.Encoding.UTF8.GetBytes(data + "\n");
                    _socket.SendTo(sendData, _client);
                }
            }
            catch (SocketException se)
            {
                HandleSocketException(se);
            }
            catch (Exception e)
            {
                ConfigManager.LogManager.LogError("UDP interface threw unhandled exception sending data. (Interface=\"" + Name + "\")", e);
            }
        }

        // TODO work here
        void Profile_ProfileStopped(object sender, EventArgs e)
        {
            /*
            _started = false;
            _socket.Close();
            _socket = null;
            _profile = null;
            */ 
        }

        // TODO work here
        void Profile_ProfileStarted(object sender, EventArgs e)
        {
            ConfigManager.LogManager.LogDebug("UDP interface starting. (Interface=\"" + Name + "\")");
            /*
            _bindEndPoint = new IPEndPoint(IPAddress.Any, Port);
            _socket = new Socket(AddressFamily.InterNetwork,
                                      SocketType.Dgram,
                                      ProtocolType.Udp);
            _socket.ExclusiveAddressUse = false;
            _socket.Bind(_bindEndPoint);
            _client = new IPEndPoint(IPAddress.Any, 0);
            _started = true;
            _clientID = "";

            WaitForData();

            _profile = Profile;
            */
        }

        public override void ReadXml(System.Xml.XmlReader reader)
        {
            // No Op
        }

        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            // No Op
        }

        protected void AddFunction(TCPFunction function)
        {
            Functions.Add(function);
        }

        protected void AddFunction(TCPFunction function, bool debug)
        {
            function.IsDebugMode = debug;
            Functions.Add(function);
        }

        public override void Reset()
        {
            base.Reset();
            foreach(TCPFunction function in Functions)
            {
                function.Reset();
            }
            SendData("R");
        }
    }
}
