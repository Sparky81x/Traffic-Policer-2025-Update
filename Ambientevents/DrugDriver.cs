

namespace Traffic_Policer.Ambientevents
{
    /// <summary>
    /// Simulates a driver impaired by drugs, driving erratically
    /// </summary>
    internal class DrugDriver : AmbientEvent // updated by pherem 5/5/2025
    {
        private static readonly Random random = new Random(); // Add this line to define a Random instance

        public DrugDriver(Ped Driver, bool createBlip, bool showMessage)
            : base(Driver, createBlip, showMessage, "Creating DrugDriver event.")
        {
            if (createBlip)
            {
                driverBlip = driver.AttachBlip();
                driverBlip.Color = System.Drawing.Color.Yellow;
                driverBlip.Scale = 0.7f;
            }

            if (showMessage)
                Game.DisplayNotification("~o~Traffic Policer~s~: Drug-impaired driver spawned.");

            MainLogic();
        }

        protected override void MainLogic()
        {
            eventRunning = true;

            // Set randomized drug levels
            switch (random.Next(3)) // Replace Game.Random with the new Random instance
            {
                case 0:
                    DrugTestKit.SetPedDrugsLevels(driver, DrugsLevels.POSITIVE, DrugsLevels.POSITIVE); break;
                case 1:
                    DrugTestKit.SetPedDrugsLevels(driver, DrugsLevels.NEGATIVE, DrugsLevels.POSITIVE); break;
                case 2:
                    DrugTestKit.SetPedDrugsLevels(driver, DrugsLevels.POSITIVE, DrugsLevels.NEGATIVE); break;
            }

            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    AnimationSet drunkAnimSet = new AnimationSet("move_m@drunk@verydrunk");
                    drunkAnimSet.LoadAndWait();
                    driver.MovementAnimationSet = drunkAnimSet;
                    NativeFunction.Natives.SET_PED_IS_DRUNK(driver, true);

                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "You seem out of it. Been using anything?", new List<string>
                        {
                            "Nah, just tired officer...",
                            "What are you implying?",
                            "Uh... some herbal tea maybe.",
                            "That's none of your business."
                        });
                    }

                    speed = Math.Max(car.Speed - 1f, 12.1f);
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.Normal);

                    DrivingStyleFiber = GameFiber.StartNew(delegate
                    {
                        while (eventRunning)
                        {
                            NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                            GameFiber.Yield();
                        }
                    });

                    while (eventRunning)
                    {
                        // Erratic driving maneuvers
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight);
                        GameFiber.Sleep(350);
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveLeft);
                        GameFiber.Sleep(500);

                        // Simulate sudden braking or hesitation
                        if (random.Next(0, 100) < 20) // Replace Game.Random with the new Random instance
                        {
                            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.HandBrakeStraight);
                            GameFiber.Sleep(1500);
                        }

                        // Cruise again
                        driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.Normal);
                        GameFiber.Sleep(3500);

                        // Exit conditions
                        if (Functions.IsPlayerPerformingPullover())
                        {
                            eventRunning = false;
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
                    Game.LogTrivial($"DrugDriver Exception: {e.Message}");
                    eventRunning = false;
                    if (driver.Exists()) driver.Dismiss();
                    if (driverBlip.Exists()) driverBlip.Delete();
                }
                finally
                {
                    End();
                }
            });
        }
    }
}
