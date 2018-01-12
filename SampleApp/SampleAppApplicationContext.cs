using System;
using System.Windows.Forms;
using Tenduke.EntitlementClient;

namespace SampleApp
{
    /// <summary>
    /// Application context in which this sample application is executed. Application-wide static
    /// initialization and clean-up is done by this class.
    /// </summary>
    class SampleAppApplicationContext : ApplicationContext
    {
        /// <summary>
        /// Main form of the application.
        /// </summary>
        private SampleAppForm sampleAppForm;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="SampleAppApplicationContext"/> class.</para>
        /// <para><see cref="EntClient.Initialize"/> is called here for initializing <see cref="EntClient"/>.
        /// This method must be called once before using <see cref="EntClient"/> in the application.</para>
        /// </summary>
        internal SampleAppApplicationContext()
        {
            Application.ApplicationExit += Application_ApplicationExit;

            // Application-wide static initialization of EntClient
            EntClient.Initialize();

            sampleAppForm = new SampleAppForm();
            sampleAppForm.FormClosed += SampleAppForm_FormClosed;
            sampleAppForm.Show();
        }

        /// <summary>
        /// Called when the main form of the application is closed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SampleAppForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// <para>Called when the application is about to shut down.</para>
        /// <para><see cref="EntClient.Shutdown"/> is called here for cleaning up resources used by <see cref="EntClient"/>.
        /// This method must be called once when <see cref="EntClient"/> is not anymore used by the application.</para>
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            // Application-wide static clean-up of EntClient
            EntClient.Shutdown();
        }
    }
}
