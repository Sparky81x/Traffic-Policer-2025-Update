using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;

namespace Traffic_Policer.Ambientevents
{
    internal class RevEngineWhenStationary : AmbientEvent
    {
        public RevEngineWhenStationary(Ped Driver, bool createBlip, bool showMessage)
            : base(Driver, createBlip, showMessage, "Creating RevEngineWhenStationary event")
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
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.Normal | VehicleDrivingFlags.AllowWrongWay);

                    Game.LogTrivial("[Traffic Policer] BurnoutWhenStationary event started.");

                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "Why were you burning out back there?", new List<string>
                        {
                            "Just messing around, officer.",
                            "Didn’t think anyone was watching.",
                            "It’s a powerful car, can’t help it!",
                            "Just testing the tires."
                        });
                    }

                    int revCount = 0;

                    while (eventRunning && car.Exists() && driver.Exists())
                    {
                        GameFiber.Yield();

                        if (car.Speed < 1f)
                        {
                            // Tire smoke effect
                            Rage.Native.NativeFunction.Natives._START_PARTICLE_FX_NON_LOOPED_ON_ENTITY("exp_grd_bzgas_smoke", car, 0f, 0f, -0.2f, 0f, 0f, 0f, 1f, false, false, false);

                            // Tailpipe pop (visual/sound simulation using backfire)
                            Rage.Native.NativeFunction.Natives._PLAY_VEHICLE_ENGINE_SOUND_FROM_POSITION("SPEED_BOOST", car.Position.X, car.Position.Y, car.Position.Z);

                            // Perform burnout maneuver
                            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.BurnOut);
                            GameFiber.Sleep(3000);
                            revCount++;
                        }

                        // Re-engage cruise to simulate erratic driving after burnout
                        if (revCount >= 2)
                        {
                            driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.Normal | VehicleDrivingFlags.AllowWrongWay);
                            revCount = 0;
                        }

                        if (Functions.IsPlayerPerformingPullover())
                        {
                            performingPullover = true;
                            break;
                        }

                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Game.LogTrivial($"[Traffic Policer] Exception in BurnoutWhenStationary: {e.Message}");
                }
                finally
                {
                    if (driver.Exists()) driver.Dismiss();
                    if (driverBlip.Exists()) driverBlip.Delete();
                    End();
                }
            }, "BurnoutStationaryFiber");
        }
    }
}
