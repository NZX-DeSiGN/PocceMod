﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public static class ExtraMenu
    {
        private static bool _crazyOceanWaves = false;

        public static void ToggleCrazyOceanWaves()
        {
            _crazyOceanWaves = !_crazyOceanWaves;
            if (_crazyOceanWaves)
                API.SetWavesIntensity(8f);
            else
                API.ResetWavesIntensity();
        }

        public static async Task RappelFromHeli()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                if (!Vehicles.GetPedSeat(heli, player, out int seat))
                    return;

                switch (seat)
                {
                    case -1:
                        if (API.AreAnyVehicleSeatsFree(heli))
                        {
                            await Autopilot.Activate();
                            API.TaskRappelFromHeli(player, 0);
                        }
                        break;

                    case 0:
                        if (Vehicles.GetFreeSeat(heli, out int goodSeat, true))
                        {
                            API.SetPedIntoVehicle(player, heli, goodSeat);
                            API.TaskRappelFromHeli(player, 0);
                        }
                        break;

                    default:
                        API.TaskRappelFromHeli(player, 0);
                        break;
                }
            }
            else
            {
                Common.Notification("Player is not in a heli");
            }
        }

        public static void UltrabrightHeadlight()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            Vehicles.EnableUltrabrightHeadlight(vehicle);
            Common.Notification("Use arrow up/down keys to change brightness");
        }

        public static void CargobobMagnet()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                if (API.GetPedInVehicleSeat(heli, -1) != player)
                {
                    Common.Notification("Player is not the pilot of this heli");
                    return;
                }

                if (API.IsCargobobMagnetActive(heli))
                {
                    API.SetCargobobPickupMagnetActive(heli, false);
                    API.RetractCargobobHook(heli);
                }
                else
                {
                    API.EnableCargobobHook(heli, 1);
                    API.SetCargobobPickupMagnetActive(heli, true);
                }
            }
            else
            {
                Common.Notification("Player is not in a heli");
            }
        }

        public static void ToggleZeroGravity()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            if (AntiGravity.Contains(vehicle))
                AntiGravity.Remove(vehicle);
            else
                AntiGravity.Add(vehicle, 1.5f);
        }

        public static async Task Balloons()
        {
            var player = API.GetPlayerPed(-1);
            var coords = API.GetEntityCoords(player, true);
            coords.Z += 1f;

            var balloon = await Props.SpawnBalloons(coords);
            Ropes.PlayerAttach(balloon, Vector3.Zero);
            API.SetEntityAsNoLongerNeeded(ref balloon);
        }
    }
}
