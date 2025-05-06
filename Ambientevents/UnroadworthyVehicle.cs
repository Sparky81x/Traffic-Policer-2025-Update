using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    internal class UnroadworthyVehicle : AmbientEvent
    {
        public UnroadworthyVehicle(Ped Driver, bool createBlip, bool showMessage)
            : base(Driver, createBlip, showMessage, "Creating unroadworthy vehicle event.")
        {
            MainLogic();
        }

        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    if (!car.Exists() || !driver.Exists())
                    {
                        End();
                        return;
                    }

                    // Randomly remove 1 or 2 windows
                    for (int i = 0; i < 2; i++)
                    {
                        int index = TrafficPolicerHandler.rnd.Next(0, 6); // GTA V has 6 windows typically
                        var window = car.Windows[index];
                        if (window != null && window.IsIntact)
                            window.Remove();
                    }

                    // Burst 1 or 2 tires
                    for (int i = 0; i < 2; i++)
                    {
                        int index = TrafficPolicerHandler.rnd.Next(0, 4); // Most vehicles have 4 wheels
                        if (car.Wheels[index] != null)
                        {
                            car.Wheels[index].BurstTire();
                        }
                    }


                    // Break off 1 door if any exist
                    int doorIndex = TrafficPolicerHandler.rnd.Next(0, 6); // 0–5 valid door indices
                    var door = car.Doors[doorIndex];
                    // Fixed line
                    if (door != null && door.IsValid())

                        Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOOR_BROKEN(car, doorIndex, true);

                    // Reduce health
                    car.EngineHealth = 60f;
                    car.FuelTankHealth = 70f;
                    car.Health = (int)300f;
                    car.IsDriveable = true;

                    // Add exhaust smoke for realism
                    Rage.Native.NativeFunction.Natives._START_PARTICLE_FX_NON_LOOPED_ON_ENTITY("exp_grd_bzgas_smoke", car, 0f, 0f, -0.5f, 0f, 0f, 0f, 1.5f, false, false, false);

                    // LSPDFR+ support
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "What happened to your vehicle?", new List<string>
                        {
                            "It needs a trip to the garage, officer.",
                            "It's getting a bit old.",
                            "Someone slashed my tires!",
                            "What's wrong with it?",
                            "It still runs, doesn’t it?"
                        });
                    }

                    // Make driver cruise slowly
                    driver.Tasks.CruiseWithVehicle(car, 14f, VehicleDrivingFlags.Normal);

                    while (eventRunning)
                    {
                        GameFiber.Yield();

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
                catch (Exception ex)
                {
                    Game.LogTrivial($"[Traffic Policer] Exception in UnroadworthyVehicle: {ex}");
                    if (driver.Exists()) driver.Dismiss();
                    if (driverBlip.Exists()) driverBlip.Delete();
                }
                finally
                {
                    End();
                }
            }, "UnroadworthyVehicleFiber");
        }
    }
}
