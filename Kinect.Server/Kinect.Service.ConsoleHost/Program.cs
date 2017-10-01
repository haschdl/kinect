using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Kinect.Service.ConsoleHost
{
    class Program
    {
        //The URL to be reserved for the service
        //Reserving a URL in Windows requires Administrative permissions,
        //Therefore the app.manifest is marked with <requestedExecutionLevel  level="requireAdministrator" uiAccess="false" />        
        private const string BaseAddress = @"http://127.0.0.1:8000/kinectservice";
        private static Uri _serviceAddressHands = new Uri(BaseAddress + "/hands");
        internal static ServiceHost serviceHostHandsService = null;

        private static Uri _serviceAddressJoints = new Uri(BaseAddress + "/joints");
        internal static ServiceHost serviceHostJointsService = null;

        private static Uri _serviceAddressDepth = new Uri(BaseAddress + "/depth");
        internal static ServiceHost serviceHostDepthService = null;
 

        private static void Main(string[] args)
        {
            //Uncomment the following lines if you're interested in consuming the hands position
            //Console.WriteLine("Initiating host for Kinect Hands...", EventLogEntryType.Information);
            //InitKinectService(serviceHostHandsService, typeof(KinectServiceHands), _serviceAddressHands);
            //Console.WriteLine($"The service {typeof(KinectServiceHands).Name} is ready at {_serviceAddressHands}");


            //Console.WriteLine("Initiating host for Kinect Joints...", EventLogEntryType.Information);
            //InitKinectService(serviceHostJointsService, typeof(KinectServiceJoints), _serviceAddressJoints);
            //Console.WriteLine($"The service {typeof(KinectServiceJoints).Name} is ready at {_serviceAddressJoints}");

            

            var version = typeof(KinectServiceDepth).Assembly.GetName().Version;
            Console.WriteLine("Initiating host for Kinect Depth Sensor...", EventLogEntryType.Information);
            Console.WriteLine($"Assembly version: {version.ToString()}", EventLogEntryType.Information);
            InitKinectService(serviceHostJointsService, typeof(KinectServiceDepth), _serviceAddressDepth);
            Console.WriteLine($"The service {typeof(KinectServiceDepth).Name} is ready at {_serviceAddressDepth}");

            Console.WriteLine("Press <Enter> to stop the service.");
            Console.ReadLine();

        }
        private static void ServiceHost_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("Service host faulted!" + Environment.NewLine + e);
        }

        private static void ServiceHostOnClosed(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("Host was closed.");
        }
        private static void CurrentDomain_UnhandledException(
                                                        Object sender,
                                                        UnhandledExceptionEventArgs e)
        {

            if (e?.ExceptionObject != null)
            {
                Console.WriteLine("Unhandled exception." + Environment.NewLine + e, EventLogEntryType.Error);
            }
        }

        private static void InitKinectService(ServiceHost serviceHost, Type serviceImplementationType, Uri serviceAddress)
        {
            if (serviceHost != null)
            {
                //this.EventLog.WriteEntry("Host wast not null at service start-up. Closing host.", EventLogEntryType.Information);
                serviceHost.Close();
            }

            // Create the ServiceHost
            serviceHost = new ServiceHost(serviceImplementationType, serviceAddress);

          
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior
            {
                HttpGetEnabled = false
            };
            serviceHost.Description.Behaviors.Add(smb);

            CustomBinding binding = new CustomBinding();       
            binding.Elements.Add(new ByteStreamMessageEncodingBindingElement());
            
            binding.ReceiveTimeout = TimeSpan.FromHours(5);
            binding.SendTimeout = TimeSpan.FromHours(5); 

            HttpTransportBindingElement transport = new HttpTransportBindingElement
            {
                
                WebSocketSettings =
                {
                    TransportUsage = WebSocketTransportUsage.Always,
                    CreateNotificationOnConnection = true,
                    

                }
            };            

            binding.Elements.Add(transport);

            serviceHost.AddServiceEndpoint(typeof(IKinectService), binding, "");
            serviceHost.Closed += ServiceHostOnClosed;
            serviceHost.Faulted += ServiceHost_Faulted;           

            serviceHost.Open();
        }

    }
}
