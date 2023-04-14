using System.Collections.Generic;
using UnityEngine;

namespace VAMLaunchPlugin.MotionSources
{

    class Command
    {
        public string Method;
        public int Device;
        public int Motor;
        public float Percent;
    }

    public class ManualSource : IMotionSource
    {

        List<JSONStorableFloat> _storableCommands = new List<JSONStorableFloat>();
        List<UIDynamicSlider> _uiCommands = new List<UIDynamicSlider>();
        VAMLaunch _plugin;

        Dictionary<string, Command> _flattenedCommandQueue = new Dictionary<string, Command>();
        private void EnqueueVibration(int device, int motor, float percent)
        {
            var key = $"vibration-{device}-{motor}";
            //SuperController.LogMessage($"enqueue {key}");
            lock(_flattenedCommandQueue)
            {
                _flattenedCommandQueue[key] = new Command
                {
                    Method = "vibration",
                    Device = device,
                    Motor = motor,
                    Percent = percent
                };
            }
        }

        public void OnInitStorables(VAMLaunch plugin)
        {
            _plugin = plugin;
            _storableCommands = new List<JSONStorableFloat>();

            _storableCommands.Add(new JSONStorableFloat("Vibrate All", 0, (float x) => { EnqueueVibration(0, 0, x); }, 0, 1, constrain: true, interactable: true));
            for (var i = 0; i < 3; i++)
            {
                var device = i + 1;
                _storableCommands.Add(new JSONStorableFloat($"Vibrate Dev {i + 1} : All", 0, (float x) => { EnqueueVibration(device, 0, x); }, 0, 1, constrain: true, interactable: true));
                _storableCommands.Add(new JSONStorableFloat($"Vibrate Dev {i + 1} : Motor 1", 0, (float x) => { EnqueueVibration(device, 1, x); }, 0, 1, constrain: true, interactable: true));
                _storableCommands.Add(new JSONStorableFloat($"Vibrate Dev {i + 1} : Motor 2", 0, (float x) => { EnqueueVibration(device, 2, x); }, 0, 1, constrain: true, interactable: true));
            }

            foreach(var s in _storableCommands)
            {
                plugin.RegisterFloat(s);
            }
        }
        
        public void OnInit(VAMLaunch plugin)
        {
            lock(_flattenedCommandQueue)
            {
                _flattenedCommandQueue = new Dictionary<string, Command>();
            }

            _uiCommands = new List<UIDynamicSlider>();
            foreach(var s in _storableCommands)
            {
                _uiCommands.Add(plugin.CreateSlider(s, rightSide: true));
            }

        }

        public void OnDestroy(VAMLaunch plugin)
        {
            foreach(var ui in _uiCommands)
            {
                plugin.RemoveSlider(ui);
            }
            _uiCommands = new List<UIDynamicSlider>();

            foreach(var s in _storableCommands)
            {
                plugin.RemoveSlider(s);
            }
            _storableCommands = new List<JSONStorableFloat>();

            _plugin = null;
        }



        public void OnSimulatorUpdate(float prevPos, float newPos, float deltaTime)
        {
        }

        public bool OnUpdate(ref byte outPos, ref byte outSpeed)
        {
            if(_plugin == null)
            {
                return false; // always return false since this class handles sending the commands
            }

            var commands = new List<Command>();
            lock(_flattenedCommandQueue)
            {
                commands = _flattenedCommandQueue.Values.ToList();
                _flattenedCommandQueue = new Dictionary<string, Command>();
            }

            foreach(var cmd in commands)
            {
                if(cmd.Method.Equals("vibration"))
                {
                    //SuperController.LogMessage($"Vibrate {cmd.Device} {cmd.Motor} {cmd.Percent}");
                    _plugin.SetVibration(cmd.Device, cmd.Motor, cmd.Percent);
                }
            }

            return false; // always return false since this class handles sending the commands
        }
    }
}
