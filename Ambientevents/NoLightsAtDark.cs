using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    internal class NoLightsAtDark : AmbientEvent
    {
        private int driftChance = 45;
        private bool tailLightGlitch = false;

        public NoLightsAtDark(Ped driver, bool createBlip, bool showMessage)
            : base(driver, createBlip, showMessage, "Creating no lights at dark event.")
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

            speed = Math.Max(car.Speed - 3f, 12.1f); // safer default

            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    Game.LogTrivial("[Traffic Policer] NoLightsAtDark event started.");

                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver,
                            "Why are your lights off?",
                            new List<string>
                            {
                                "Sorry, officer. I forgot.",
                                "It's not that dark, is it?",
                                "What the hell do you care?",
                                "I thought I had them on!",
                                "Battery saver mode? No?"
                            });
                    }

                    int tick = 0;

                    while (eventRunning && car.Exists() && driver.Exists())
                    {
                        GameFiber.Yield();

                        // Keep lights OFF
                        Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 1); // 1 = Force off
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);

                        // Randomly drift to simulate bad driving due to low visibility
                        if (tick % driftChance == 0)
                        {
                            float drift = TrafficPolicerHandler.rnd.Next(-10, 10);
                            car.Heading += drift;
                        }

                        // Simulate tail light glitching
                        if (car.Speed < 5f && tick % 25 == 0)
                        {
                            tailLightGlitch = !tailLightGlitch;
                            Rage.Native.NativeFunction.Natives.SET_VEHICLE_BRAKE_LIGHTS(car, tailLightGlitch);
                        }

                        // Player initiates pullover
                        if (!performingPullover &&
                            Functions.IsPlayerPerformingPullover() &&
                            Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 20f)
                        {
                            performingPullover = true;

                            GameFiber.Wait(4000);
                            if (car.Exists())
                            {
                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 0); // restore auto mode
                            }
                            break;
                        }

                        // Too far, end event
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;
                            break;
                        }

                        tick++;
                    }

                    if (car.Exists())
                    {
                        Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 0); // restore auto
                    }
                }
                catch (Exception ex)
                {
                    Game.LogTrivial($"[Traffic Policer] Exception in NoLightsAtDark: {ex}");
                    if (car.Exists()) Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 0);
                }
                finally
                {
                    if (driver.Exists()) driver.Dismiss();
                    if (driverBlip.Exists()) driverBlip.Delete();
                    End();
                }
            }, "NoLightsAtDarkLogicFiber");
        }
    }
}

