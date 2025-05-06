using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    /// <summary>
    /// Base class for traffic ambient events used in Traffic Policer.
    /// Subclasses implement specific logic in MainLogic().
    /// </summary>
    abstract internal class AmbientEvent
    {
        public Ped driver;                              // Suspect ped assigned to this ambient event
        protected bool eventRunning = true;             // Controls whether the ambient event is still active
        public Vehicle car;                             // The vehicle driven by the suspect
        protected float speed;                          // Driving speed used by event logic
        protected Blip driverBlip;                      // Optional blip to mark the driver
        protected bool performingPullover = false;      // True if a pullover is in progress

        public GameFiber DrivingStyleFiber;             // Fiber handling driver behavior
        public GameFiber AmbientEventMainFiber;         // Fiber executing the main event logic
        public bool ReadyForGameFiberCleanup = false;   // Flag to mark this event for fiber cleanup

        /// <summary>
        /// Empty base constructor.
        /// </summary>
        public AmbientEvent() { }

        /// <summary>
        /// Constructor with optional on-screen message.
        /// </summary>
        public AmbientEvent(bool ShowMessage, string Message)
        {
            if (ShowMessage)
            {
                Game.DisplayNotification(Message);
            }
        }

        /// <summary>
        /// Primary constructor that sets up the driver, attaches blip, and shows message if requested.
        /// </summary>
        public AmbientEvent(Ped Driver, bool CreateBlip, bool ShowMessage, string Message)
        {
            driver = Driver;
            driver.BlockPermanentEvents = true;
            driver.IsPersistent = true;

            car = driver.CurrentVehicle;
            car.IsPersistent = true;

            if (CreateBlip)
            {
                driverBlip = driver.AttachBlip();
                driverBlip.Color = System.Drawing.Color.Beige;
                driverBlip.Scale = 0.7f;
            }

            if (ShowMessage)
            {
                Game.DisplayNotification(Message);
            }
        }

        /// <summary>
        /// Abstract method that child classes must implement to define event behavior.
        /// </summary>
        protected abstract void MainLogic();

        /// <summary>
        /// Performs cleanup of the ambient event, dismisses entities, and logs final status.
        /// </summary>
        protected virtual void End()
        {
            eventRunning = false;

            // Safely delete the blip if it exists
            if (driverBlip.Exists())
            {
                driverBlip.Delete();
            }

            // If the player is NOT pulling over this driver and no pullover was triggered
            if (!Functions.IsPlayerPerformingPullover() && !performingPullover)
            {
                // Dismiss driver if not involved in a pursuit
                if (driver.Exists() && (Functions.GetActivePursuit() == null || !Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(driver)))
                {
                    driver.Dismiss();
                }

                // Dismiss vehicle only if it's not involved in an ongoing pursuit
                if (car.Exists() && (!driver.Exists() || Functions.GetActivePursuit() == null || !Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(driver)))
                {
                    car.Dismiss();
                }
            }
            else
            {
                // If a pullover occurred and LSPDFR+ is installed, record stat
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Traffic ambient event vehicles pulled over");
                }
            }

            // Add fibers to the cleanup list for safe disposal
            TrafficPolicerHandler.AmbientEventGameFibersToAbort.Add(DrivingStyleFiber);
            TrafficPolicerHandler.AmbientEventGameFibersToAbort.Add(AmbientEventMainFiber);

            Game.LogTrivial("Added ambient event fibers to cleanup");
        }
    }
}
