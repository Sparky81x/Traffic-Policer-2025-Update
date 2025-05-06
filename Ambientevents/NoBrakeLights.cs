using System;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    internal class NoBrakeLights : AmbientEvent
    {
        private int driftChance = 50; // 1 in 50 ticks (~3–5 seconds)
        private int honkChance = 80;  // 1 in 80 ticks (~5–7 seconds)
        private bool tailLightFlicker = false;

        public NoBrakeLights(Ped driver, bool createBlip, bool showMessage)
            : base(driver, createBlip, showMessage, "Creating no brake lights event.")
        {
            MainLogic();
        }

        protected override void MainLogic()
        {
            if (!car.Exists() || !driver.Exists())
            {
                End();
                return;
            }

            speed = Math.Max(car.Speed - 1f, 12.1f);

            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    Game.LogTrivial("[Traffic Policer] NoBrakeLights event started.");

                    int tickCounter = 0;

                    while (eventRunning && car.Exists() && driver.Exists())
                    {
                        GameFiber.Yield();

                        // Always force brake lights off
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                        Rage.Native.NativeFunction.Natives.SET_VEHICLE_BRAKE_LIGHTS(car, false);

                        // Simulate flickering brake light bug at low speeds
                        if (car.Speed < 5f && tickCounter % 20 == 0)
                        {
                            tailLightFlicker = !tailLightFlicker;
                            Rage.Native.NativeFunction.Natives.SET_VEHICLE_BRAKE_LIGHTS(car, tailLightFlicker);
                        }

                        // Simulate slight drift or honk if randomly triggered
                        if (tickCounter % driftChance == 0)
                        {
                            float newHeading = car.Heading + (TrafficPolicerHandler.rnd.Next(-8, 8));
                            car.Heading = newHeading;
                        }

                        if (tickCounter % honkChance == 0)
                        {
                            Rage.Native.NativeFunction.Natives.START_VEHICLE_HORN(car, 200, "NORMAL", false);
                        }

                        // Check if player initiated pullover
                        if (!performingPullover &&
                            Functions.IsPlayerPerformingPullover() &&
                            Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 20f)
                        {
                            performingPullover = true;

                            while (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                GameFiber.Yield();
                                if (car.Exists())
                                {
                                    Rage.Native.NativeFunction.Natives.SET_VEHICLE_BRAKE_LIGHTS(car, false);
                                }
                            }
                            break;
                        }

                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;
                            break;
                        }

                        tickCounter++;
                    }
                }
                catch (Exception ex)
                {
                    Game.LogTrivial($"[Traffic Policer] Exception in NoBrakeLights: {ex}");
                }
                finally
                {
                    if (driver.Exists())
                    {
                        driver.Tasks.Clear();
                        driver.Dismiss();
                    }

                    if (driverBlip.Exists())
                        driverBlip.Delete();

                    End();
                }
            }, "NoBrakeLightsLogicFiber");
        }
    }
}
