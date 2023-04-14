using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace VAMLaunchPlugin
{
    public class VAMLaunchNetwork
    {
        private UdpClient _udpClient;
        private Thread _recvThread;
        private bool _listening;

        private IPEndPoint _sendEndPoint;
        
        private readonly Queue<byte[]> _recvQueue = new Queue<byte[]>();
        public int QueuedMsgCount => _recvQueue.Count;

        ~VAMLaunchNetwork()
        {
            Stop();
        }
        
        public bool Init(string serverIp, int recvPort, int sendPort)
        {
            try
            {
                var address = IPAddress.Parse(serverIp);
                _sendEndPoint = new IPEndPoint(address, sendPort);
            }
            catch (Exception e)
            {
                SuperController.LogMessage(e.Message);
                return false;
            }

            try
            {
                _udpClient = new UdpClient(recvPort);
            }
            catch (Exception e)
            {
                SuperController.LogMessage(string.Format("Failed to init recv on port {0} ({1})", recvPort,
                    e.Message));
                return false;
            }
            
            _recvThread = new Thread(Receive);
            _recvThread.IsBackground = true;
            _listening = true;
            _recvThread.Start();

            return true;
        }

        private void Receive()
        {
            IPEndPoint recvEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            while (_listening)
            {
                try
                {
                    byte[] recvBytes = _udpClient.Receive(ref recvEndPoint);
                    lock (_recvQueue)
                    {
                        _recvQueue.Enqueue(recvBytes);
                    }
                }
                catch (SocketException e)
                {
                    // 10004 thrown when socket is closed
                    //if (e.ErrorCode != 10004)
                    //{
                        //SuperController.LogMessage("Socket exception while receiving data from udp client: " + e.Message);                        
                    //}
                }
                catch (Exception e)
                {
                    //SuperController.LogMessage("Error receiving data from udp client: " + e.Message);
                }
            }
        }

        public void Send(byte[] data, int length)
        {
            try
            {
                _udpClient.Send(data, length, _sendEndPoint);
            }
            catch (Exception e)
            {
                
            }
        }

        public void Stop()
        {
            _listening = false;
            if (_recvThread != null && _recvThread.IsAlive)
            {
                _recvThread.Abort();
            }

            if (_udpClient != null)
            {
                _udpClient.Close();
            }
        }

        public byte[] GetNextMessage()
        {
            return _recvQueue.Count > 0 ? _recvQueue.Dequeue() : null;
        }

        private const byte LINEAR_CMD = 0;
        private const byte VIBRATE_CMD = 1;
        private const byte ROTATE_CMD = 2;
        private const byte DEVICE_ALL = 0;
        private const byte MOTOR_ALL = 0;

        public void SendLinearCmd(int device, int motor, float duration, float position)
        {
            byte[] data = new byte[4 + 4 + 4];
            data[0] = (byte)data.Length;
            data[1] = LINEAR_CMD;
            data[2] = (byte)device;
            data[3] = (byte)motor;

            var durationData = BitConverter.GetBytes(duration);
            data[4] = durationData[0];
            data[5] = durationData[1];
            data[6] = durationData[2];
            data[7] = durationData[3];

            var positionData = BitConverter.GetBytes(position);
            data[8] = positionData[0];
            data[9] = positionData[1];
            data[10] = positionData[2];
            data[11] = positionData[3];

            Send(data, data.Length);
        }

        public void SendLinearCmd(float duration, float position)
        {
            SendLinearCmd(DEVICE_ALL, MOTOR_ALL, duration, position);
        }

        public void SendVibrateCmd(int device, int motor, float speed)
        {
            byte[] data = new byte[4 + 4];
            data[0] = (byte)data.Length;
            data[1] = VIBRATE_CMD;
            data[2] = (byte)device;
            data[3] = (byte)motor;

            var speedData = BitConverter.GetBytes(speed);
            data[4] = speedData[0];
            data[5] = speedData[1];
            data[6] = speedData[2];
            data[7] = speedData[3];

            Send(data, data.Length);
        }

        public void SendVibrateCmd(float speed)
        {
            SendVibrateCmd(DEVICE_ALL, MOTOR_ALL, speed);
        }

        public void SendRotateCmd(int device, int motor, float speed, bool clockwise)
        {
            byte[] data = new byte[4 + 4 + 1];
            data[0] = (byte)data.Length;
            data[1] = ROTATE_CMD;
            data[2] = (byte)device;
            data[3] = (byte)motor;

            var speedData = BitConverter.GetBytes(speed);
            data[4] = speedData[0];
            data[5] = speedData[1];
            data[6] = speedData[2];
            data[7] = speedData[3];

            data[8] = clockwise ? (byte)1 : (byte)0;

            Send(data, data.Length);
        }

        public void SendRotateCmd(float speed, bool clockwise)
        {
            SendRotateCmd(DEVICE_ALL, MOTOR_ALL, speed, clockwise);
        }
    }
}