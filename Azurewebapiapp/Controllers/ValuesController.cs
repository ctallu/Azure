using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Azurewebapiapp.Models;
using System.Web.Http.Cors;

namespace Azurewebapiapp.Controllers
{
    [EnableCors(origins: "http://localhost:4200", headers: "*", methods: "*")]
    public class ValuesController : ApiController
    {
        // GET api/values
        [SwaggerOperation("GetAll")]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [SwaggerOperation("GetById")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public string Get(int id)
        {
            return "value";
        }

        //Create VM using Azure  Rest API, Post data to Azure rest api
        // POST api/values
        [SwaggerOperation("Create")]
        [SwaggerResponse(HttpStatusCode.Created)]
         
        public HttpResponseMessage Post([FromBody]Vmdata input)
        {

            try {

                string ClientId = input.ClientID.ToString().Trim();
                string ClientSecret = input.ClientSecret.ToString().Trim();
                string TenentID = input.TenentID.ToString().Trim();

                //provide ClientID ,ClientSecret and TenentID for getting credentails .Pls refer https://docs.microsoft.com/en-us/dotnet/azure/dotnet-sdk-azure-authenticate?view=azure-dotnet fro more details

                // var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal("0f1cc5b1-1c10-444b-8997-0c513d57771a", "4dfe6adf-2182-44c0-9e0a-b8541f7c5ab7",
                // "334e368b-6621-4c01-bc02-1074416eb923", AzureEnvironment.AzureGlobalCloud);
                 var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(ClientId, ClientSecret,TenentID, AzureEnvironment.AzureGlobalCloud);







                var azure = Azure
                 .Configure()
                 .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                 .Authenticate(credentials)
                 .WithDefaultSubscription();
            
               
                //  .Configure()
                //  .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                //  .Authenticate(credentials)
                //  .WithDefaultSubscription().LoadBalancers.Define("App-LB");

             

            var region = Region.USEast;
            var VMname = input.VMname.ToString().Trim();// "MyVM3";
            //var NIC_name = "WebVM";
            var rgName = input.Resource_Grp_Name.ToString().Trim();
            var userName = input.UserName.ToString().Trim();
            var password = input.Password.ToString().Trim();
            var size1 = input.Disk1_Size;
            var size2 = input.Disk2_Size;
            string availabilitysetname = input.Availabilityset.ToString().Trim();
            string vnetname = input.Vnet.ToString().Trim();
            var SubscriptionId = azure.SubscriptionId.ToString();
            var NatRuleName =input.NatRuleName.ToString();
            var FrontEndPort = input.FrontEndPort;
            var BackEndPort = input.BackEndPort;

            var LBName = azure.LoadBalancers.GetById("/subscriptions/"+ SubscriptionId + "/resourceGroups/Mynewresourcegrp1/providers/Microsoft.Network/loadBalancers/MyLoadBalancer1");
            var NSG = azure.NetworkSecurityGroups.GetById("/subscriptions/" + SubscriptionId + "/resourceGroups/Mynewresourcegrp1/providers/Microsoft.Network/networkSecurityGroups/Nsg-webtier");
            var vnet = azure.Networks.GetById("/subscriptions/" + SubscriptionId + "/resourceGroups/" + rgName + "/providers/Microsoft.Network/virtualNetworks/" + vnetname);
            var availabilityset = azure.AvailabilitySets.GetById("/subscriptions/" + SubscriptionId + "/resourceGroups/" + rgName + "/providers/Microsoft.Compute/availabilitySets/"+ availabilitysetname);
            var publicIP = azure.PublicIPAddresses.GetById("/subscriptions/" + SubscriptionId + "/resourceGroups/Mynewresourcegrp1/providers/Microsoft.Network/publicIPAddresses/myPublicIP");

                var dataDiskCreatable = azure.Disks.Define("dsk-1")
                         .WithRegion(region)
                         .WithExistingResourceGroup(rgName)
                         .WithData()
                         .WithSizeInGB(size1);

                //create a disk 
                var dataDisk = azure.Disks.Define("dsk-1")
                       .WithRegion(region)
                       .WithNewResourceGroup(rgName)
                       .WithData()
                       .WithSizeInGB(size2)
                       .CreateAsync();

                //create new nat rule and add it to existing loadbalancer
                var natrule = LBName.Update().DefineInboundNatRule(NatRuleName)
                    .WithProtocol(TransportProtocol.Tcp)
                    .FromFrontend("myFrontendIp")
                    .FromFrontendPort(FrontEndPort)  //eg 4223
                    .ToBackendPort(BackEndPort) // eg 3389
                    .Attach().Apply();

           //create a Netwrok interface 
                var nic = azure.NetworkInterfaces.Define(VMname)
                     .WithRegion(region)
                        .WithNewResourceGroup(rgName)
                        .WithExistingPrimaryNetwork(vnet)
                        .WithSubnet("webtiersubnet")
                        .WithPrimaryPrivateIPAddressDynamic()
                        .WithExistingLoadBalancerBackend(LBName, "MyBackEndPool1")
                        .WithExistingNetworkSecurityGroup(NSG)
                        .WithExistingLoadBalancerInboundNatRule(LBName, NatRuleName)
                        .Create();




                
                //create a VM with details provided
                var windowsVM = azure.VirtualMachines.Define(VMname)
                        .WithRegion(region)
                        .WithNewResourceGroup(rgName)                        
                        .WithExistingPrimaryNetworkInterface(nic)                   
                        .WithPopularWindowsImage(KnownWindowsVirtualMachineImage.WindowsServer2012R2Datacenter)
                        .WithAdminUsername(userName)
                        .WithAdminPassword(password)
                        .WithNewDataDisk(size2)
                        .WithExistingAvailabilitySet(availabilityset)
                        .WithSize(VirtualMachineSizeTypes.StandardD3V2)
                         .Create();

               // var addVM = LBName.Update().UpdateBackend("MyBackEndPool1").


           //send response 
                var message = Request.CreateResponse(HttpStatusCode.Created, "VM created");
            return message;
            }

            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed, ex.ToString()+"Exception in VM creation");

            }
        }

        // PUT api/values/5
        [SwaggerOperation("Update")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [SwaggerOperation("Delete")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public void Delete(int id)
        {
        }
    }
}
