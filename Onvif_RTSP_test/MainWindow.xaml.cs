using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

//추가로 추가한 것들.
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.ServiceModel.Channels;
using System.Net;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Security.Policy;
using onvif10_media;
using onvif.services;


namespace Onvif_RTSP_test
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var endPoint = new UdpDiscoveryEndpoint(DiscoveryVersion.WSDiscoveryApril2005);

            //not sure what this does - if anything.
            //endPoint.Binding.CreateBindingElements().Insert(0, new MulticastCapabilitiesBindingElement(true));

            var discoveryClient = new System.ServiceModel.Discovery.DiscoveryClient(endPoint);

            discoveryClient.FindProgressChanged += DiscoveryClient_FindProgressChanged;

            var findCriteria = new FindCriteria
            {
                Duration = TimeSpan.FromSeconds(1), // Onvif device manager finds cameras almost instantly - so 10s should be plenty
                MaxResults = int.MaxValue
            };

            //Taking cue from sniffing Onvif DM UDP packets - only add one contract type filter
            findCriteria.ContractTypeNames.Add(new XmlQualifiedName("NetworkVideoTransmitter", @"http://www.onvif.org/ver10/network/wsdl"));
            //findCriteria.ContractTypeNames.Add(new XmlQualifiedName("Device", @"http://www.onvif.org/ver10/device/wsdl"));

            Console.WriteLine("Initiating find operation.");

            //discoveryClient.FindAsync(findCriteria);
            //Console.WriteLine("Returned from Async find operation");

            var response = discoveryClient.Find(findCriteria);

            Console.WriteLine($"Operation returned - Found {response.Endpoints.Count} endpoints.");

            foreach (var ep in response.Endpoints)
            {
                Console.WriteLine($" {ep.ListenUris[0]}");
                //Get(ep.ListenUris[0].ToString()) ;
            }
            Get("http://192.168.21.150/onvif/device_service");
            Console.ReadKey();
        }
        private static void DiscoveryClient_FindProgressChanged(object sender, FindProgressChangedEventArgs e)
        {
            Console.WriteLine(e.EndpointDiscoveryMetadata.ContractTypeNames[0].ToString());
        }

        public static void Get(string __url)
        {
            HttpTransportBindingElement httpBinding = new HttpTransportBindingElement();

            EndpointAddress serviceAddress = new EndpointAddress(__url);
            TextMessageEncodingBindingElement messegeElement = new TextMessageEncodingBindingElement();

            httpBinding.AuthenticationScheme = AuthenticationSchemes.Digest;
            messegeElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.WSAddressing10);

            CustomBinding bind = new CustomBinding(messegeElement, httpBinding);

            onvif10_media.MediaClient mclient = new onvif10_media.MediaClient(bind, serviceAddress);

            //MediaClient mclient=onvif10_  _onvifClientFactory.CreateClient<Media>(ep, _connectionParameters, MessageVersion.Soap12, _timeout);

            mclient.ClientCredentials.HttpDigest.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            mclient.ClientCredentials.HttpDigest.ClientCredential.UserName = "admin";
            mclient.ClientCredentials.HttpDigest.ClientCredential.Password = "admin";

            var streamsetup = new StreamSetup();
            streamsetup.stream = StreamType.rtpUnicast;
            streamsetup.transport = new Transport();
            streamsetup.transport.protocol = TransportProtocol.udp;

            var proflist = mclient.GetProfiles(new GetProfilesRequest());
            foreach (var prof in proflist.Profiles)
            {
                var uri = mclient.GetStreamUri(new GetStreamUriRequest(streamsetup, prof.token));

                Console.WriteLine(uri.MediaUri.uri);//rtsp address
            }

        }

    }
}
