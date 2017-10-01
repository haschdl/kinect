using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kinect.Service;

namespace Kinect.Server.Console
{
    class Program
    {
        private static KinectServiceBase kinectServiceJoints;

        /// <summary>
        /// Asyn wait stuff from http://stackoverflow.com/a/39350144/705984
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            kinectServiceJoints = new KinectServiceHands {Callback = new KinectCallbackConsole()};

//            kinectServiceJoints.Initialize();
          


            var source = new CancellationTokenSource();
            System.Console.WriteLine("Press <Enter> to stop the service.");
            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                source.Cancel();
            };

            Task.Run(async () =>
            {
                // Do any async anything you need here without worry
                await kinectServiceJoints.ClientConnectRequest(null);
            }).Wait();
            return 0;

            //try
            //{
            //    MainAsync(args, source.Token).GetAwaiter().GetResult();
            //    return 0;
            //}
            //catch (OperationCanceledException)
            //{
            //    return 1223; // Cancelled.
            //}

        }
        private static async Task<int> MainAsync(string[] args, CancellationToken token)
        {
            // Your code...
            await kinectServiceJoints.ClientConnectRequest(null);
            return await Task.FromResult(0); // Success.
        }


    }
}
