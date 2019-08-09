﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class WheelFireEffect : IEffect
    {
        private static readonly TimeSpan _timeout;
        private static readonly float _topSpeed;
        private static readonly float _density;
        private static readonly string[] _wheelBoneNames;

        private readonly int _vehicle;
        private readonly int[] _wheelBones;
        private readonly Dictionary<int, DateTime> _fires;
        private Vector3[] _lastCoords;

        static WheelFireEffect()
        {
            _timeout = TimeSpan.FromSeconds(Config.GetConfigFloat("WheelFireTimeoutSec"));
            _topSpeed = Config.GetConfigFloat("WheelFireTopSpeed");
            _density = Config.GetConfigFloat("WheelFireDensity");
            _wheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr" };

            if (_topSpeed < 10f)
                _topSpeed = 10f;
        }

        public WheelFireEffect(int vehicle)
        {
            _vehicle = vehicle;
            _wheelBones = _wheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();
            _fires = new Dictionary<int, DateTime>();
            _lastCoords = WheelCoords;
        }

        public string Key
        {
            get { return "wheel_fire_" + _vehicle; }
        }

        public bool Expired
        {
            get { return !API.DoesEntityExist(_vehicle) || !Vehicles.GetLastState(_vehicle, Vehicles.StateFlag.BackToTheFuture); }
        }

        private bool ShouldSpawnFire
        {
            get
            {
                if (API.IsVehicleInBurnout(_vehicle))
                    return true;

                var speed = API.GetEntitySpeed(_vehicle);
                var rpm = API.GetVehicleCurrentRpm(_vehicle);
                var gear = API.GetVehicleCurrentGear(_vehicle);

                if (speed > 0.5f && speed < _topSpeed && rpm > 0.25f && gear > 0)
                    return true;

                return false;
            }
        }

        private Vector3[] WheelCoords
        {
            get
            {
                return _wheelBones.Select(wheel => API.GetWorldPositionOfEntityBone(_vehicle, wheel)).ToArray();
            }
        }

        public Task Init()
        {
            return Task.FromResult(0);
        }

        public void Update()
        {
            if (_wheelBones.Length == 0)
                return;

            var now = DateTime.Now;
            foreach (var fire in _fires.ToArray())
            {
                if (fire.Value < now)
                {
                    API.RemoveScriptFire(fire.Key);
                    _fires.Remove(fire.Key);
                }
            }
            
            if (ShouldSpawnFire)
            {
                var coords = WheelCoords;
                var steps = (int)((_lastCoords[0] - coords[0]).Length() * _density);
                var timeout = now + _timeout;

                for (int step = 0; step < steps; ++step)
                {
                    for (int wheel = 0; wheel < coords.Length; ++wheel)
                    {
                        var pos = Vector3.Lerp(_lastCoords[wheel], coords[wheel], (float)step / steps);
                        var fire = API.StartScriptFire(pos.X, pos.Y, pos.Z, 1, true);
                        _fires.Add(fire, timeout);
                    }
                }

                if (steps > 0)
                    _lastCoords = coords;
            }
            else
            {
                _lastCoords = WheelCoords;
            }


            if (API.IsEntityOnFire(_vehicle))
                API.StopEntityFire(_vehicle);
        }

        public void Clear()
        {
            foreach (var fire in _fires)
                API.RemoveScriptFire(fire.Key);

            _fires.Clear();
        }
    }
}
