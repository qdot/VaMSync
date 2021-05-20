using System;
using System.Collections.Generic;
using System.Threading;

namespace VAMLaunch
{
    public class CommandEventArgs : EventArgs
    {
        public Command Command;
    }


    public class VAMLaunchServer
    {
        public EventHandler<CommandEventArgs> CommandUpdate;
        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_LISTEN_PORT = 15601;
        private const int SERVER_SEND_PORT = 15600;
        private const int NETWORK_POLL_RATE = 60;
        private const int LAUNCH_UPDATE_RATE = 60;
        private const float LAUNCH_UPDATE_INTERVAL = 1.0f / LAUNCH_UPDATE_RATE;
        
        private VAMLaunchNetwork _network;
        private Thread _updateThread;

        private object _inputLock = new object();
        private string _userCmd;

        private bool _running;

        private bool _hasNewCommands;
        private DateTime _timeOfLastLaunchUpdate;

        public void Run()
        {
            _updateThread = new Thread(UpdateThread);
            _updateThread.Start();

            _running = true;
            
            while (_updateThread.IsAlive)
            {
                if (_running)
                {
                    string input = Console.ReadLine();

                    lock (_inputLock)
                    {
                        _userCmd = input;
                    }
                }
            }
        }

        public void UpdateThread()
        {
            _network = new VAMLaunchNetwork();
            if (!_network.Init(SERVER_IP, SERVER_LISTEN_PORT, SERVER_SEND_PORT))
            {
                return;
            }

            _running = true;

            Console.WriteLine("SERVER IS ON");

            _timeOfLastLaunchUpdate = DateTime.Now;
            
            while (_running)
            {
                lock (_inputLock)
                {
                    _userCmd = null;
                }

                ProcessNetworkMessages();
                UpdateMovement();

                if (_running)
                {
                    Thread.Sleep(1000 / NETWORK_POLL_RATE);
                }
            }

            _network.Stop();
        }

        private void UpdateMovement()
        {
            var now = DateTime.Now;
            TimeSpan timeSinceLastUpdate = now - _timeOfLastLaunchUpdate;
            if (timeSinceLastUpdate.TotalSeconds > LAUNCH_UPDATE_INTERVAL)
            {
                if (_hasNewCommands)
                {
                    foreach(var cmd in _latestCommands.Values)
                    {
                        CommandUpdate?.Invoke(this, new CommandEventArgs { Command = cmd });
                    }
                    _latestCommands.Clear();
                    _hasNewCommands = false;
                }
                
                _timeOfLastLaunchUpdate = now;
            }
        }

        private Dictionary<(int, int, int), Command> _latestCommands = new Dictionary<(int, int, int), Command>();
        private void ProcessNetworkMessages()
        {
            var cmd = Command.Parse(_network.GetNextMessage());
            if(cmd != null)
            {
                _latestCommands[(cmd.Type, cmd.Device, cmd.Motor)] = cmd;
                _hasNewCommands = true;
            }
        }
    }

    public class Command
    {
        public const byte LINEAR_CMD = 0;
        public const byte VIBRATE_CMD = 1;
        public const byte ROTATE_CMD = 2;

        public const byte DEVICE_ALL = 0;
        public const byte MOTOR_ALL = 0;

        public int Type;
        public int Device;
        public int Motor;
        public List<float> Params;

        public Command()
        {
            Type = -1;
            Device = DEVICE_ALL;
            Motor = MOTOR_ALL;
            Params = new List<float>();
        }

        public static Command Parse(byte[] data)
        {
            if(data == null || data.Length < 4)
            {
                return null;
            }

            var cmd = new Command()
            {
                Device = data[2],
                Motor = data[3]
            };

            switch(data[1])
            {
                case LINEAR_CMD:
                    if(data.Length < 12)
                    {
                        return null;
                    }
                    cmd.Type = LINEAR_CMD;
                    cmd.Params.Add(BitConverter.ToSingle(data, 4)); // duration
                    cmd.Params.Add(BitConverter.ToSingle(data, 8)); // position
                    break;
                case VIBRATE_CMD:
                    if(data.Length < 8)
                    {
                        return null;
                    }
                    cmd.Type = VIBRATE_CMD;
                    cmd.Params.Add(BitConverter.ToSingle(data, 4)); // speed
                    break;
                case ROTATE_CMD:
                    if(data.Length < 9)
                    {
                        return null;
                    }
                    cmd.Type = ROTATE_CMD;
                    cmd.Params.Add(BitConverter.ToSingle(data, 4)); // speed
                    cmd.Params.Add((float)data[8]); // clockwise
                    break;
                default:
                    return null;
            }

            return cmd;
        }

    }
}
