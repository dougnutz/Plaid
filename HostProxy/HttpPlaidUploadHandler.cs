using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Globalization;

namespace PlaidUpload
{
    public static class HttpPlaidUploadHandler
    {
        [FunctionName("HttpPlaidUpload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation( "HttpPlaidUpload called.");

            // get query data we will need
            
            string token = req.Query["token"];
            log.LogInformation("token: " + token);

            // convert to uri
            Uri uri = new Uri(token);
            var query = uri.ParseQueryString();

            string containerName = query["containername"];
            string storageAccountName = query["storageaccountname"];

            log.LogInformation( "parsed query");

            //create new http client
            HttpClient client = new HttpClient();
           
            //if http verb is POST, then return the request body    
            if (req.Method == "POST")
            {
                log.LogInformation( "Post method");
                // if this is a POST, then we need the format for azure put block
                // https://myaccount.blob.core.windows.net/mycontainer/myblob?comp=block&blockid=id
                // format azure put block
                
#region validate parameters

                var blockId= req.Query["blockid"];
                // validate the parameters  
                if (string.IsNullOrEmpty(blockId) )
                {
                    return new BadRequestObjectResult("blockid is required");
                }   

                if (string.IsNullOrEmpty(containerName) )
                {
                    return new BadRequestObjectResult("containername is required");
                }       
                var blobName = req.Query["blobname"];
                if (string.IsNullOrEmpty(blobName) )
                {
                    return new BadRequestObjectResult("blobname is required");
                }

#endregion
                // get the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // send the request body to azure blob storage with http put block               
                // create new request message
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"https://{storageAccountName}.blob.core.windows.net/{containerName}/{blobName}?comp=block&blockid={blockId}");
                //add the request body to the content of the request
                request.Content = new StringContent(requestBody);
                //add the authorization header
                request.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
                request.Headers.Add("x-ms-version", "2018-03-28");
                //request.Headers.Add("Authorization", $"SharedKey {storageAccountName}:{storageAccountKey}");
                //send the request
                HttpResponseMessage response = await client.SendAsync(request);
                //return the response
                return new OkObjectResult(response);
            }

            log.LogInformation( "Get method");
            // if this is a GET, then we need to return the blob list
            var sv = query["sv"];
            var se = query["se"];
            var sp = query["sp"];
            var sig = query["sig"];
            var st= query["st"];

            // call azure to get blob file list
            string requesUri = $"https://{storageAccountName}.blob.core.windows.net/{containerName}?restype=container&comp=list&sv={sv}&st={st}&se={se}&sr=c&sp={sp}&sig={sig}";
            log.LogInformation("request url: "+requesUri);
            HttpRequestMessage getRetuest = new HttpRequestMessage(HttpMethod.Get,requesUri);
            
            //send the request
            log.LogInformation( "sending request");
            HttpResponseMessage getResponse = await client.SendAsync(getRetuest);
            if(getResponse.IsSuccessStatusCode)
            {
                log.LogInformation( "success code: " + getResponse.StatusCode);
                //return the response
                return new OkObjectResult(getResponse);
            }

            log.LogInformation( "fail code: " + getResponse.StatusCode);
            if(getResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new BadRequestObjectResult(getResponse.ToString());
            }
         
            return new BadRequestObjectResult(getResponse);
        }
    }
}
