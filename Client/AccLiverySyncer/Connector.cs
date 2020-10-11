using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using AccLiverySyncer.Model;
using System.Net;
using System.IO;
using RestSharp.Extensions;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace AccLiverySyncer
{
    public class Connector
    {
        private const string baseUri = "https://accliveries.cj-mayer.de/api/v1/";

        private static  string jwt;


        static public async Task<Tuple<HttpStatusCode, string>> Register(long discordId)
        {
            string token = "";

            var client = new RestClient(baseUri + "users/create");
            var user = new User { DiscordId = discordId};

            var request = new RestRequest();
            request.AddJsonBody(user);

            var response = await client.ExecutePostAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                // report the token to the user
                // and trigger a login
                user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(response.Content);
                token = user.Token;

                // this cannot fail on successfull login
                await Login(discordId, token);
            }

            
            return new Tuple<HttpStatusCode, string>(response.StatusCode, token);
        }


        public static async Task<HttpStatusCode> Login(long discordId, string token)
        {
            var client = new RestClient(baseUri + "users/login");
            var user = new User { DiscordId = discordId, Token = token };

            var request = new RestRequest();
            request.AddJsonBody(user);


            var response = await client.ExecutePostAsync(request);


            if(response.StatusCode == HttpStatusCode.OK)
            {
                // save the jwt token for future transactions
                jwt = response.Content;
            }

            return response.StatusCode;
        }



        public static async Task<List<Livery>> GetLiveries()
        {
            var client = new RestClient(baseUri + "liveries");
            var request = new RestRequest();
            request.AddHeader("Authorization", jwt);

            var response = await client.ExecuteGetAsync(request);


            if(response.StatusCode == HttpStatusCode.OK)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<Livery>>(response.Content);
            }
            else
            {
                return new List<Livery>();
            }

            
        }

        public static async Task<bool> DownloadLivery(string accPath, Livery liv)
        {
            var client = new RestClient(baseUri + "liveries/" + liv.Id);


            var request = new RestRequest();
            request.AddHeader("Authorization", jwt);

            var response = await client.ExecuteGetAsync(request);



            if (response.StatusCode == HttpStatusCode.OK)
            {
                
                var path = Path.GetTempPath() + "/AccLiveryManager/";
                var zipPath = path + "/" + liv.Name + ".zip";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (File.Exists(zipPath))
                {
                    // in case the previous download was aborted
                    File.Delete(zipPath);
                }


                var fileBytes = response.RawBytes;


                File.WriteAllBytes(zipPath, fileBytes);

                ZipFile.ExtractToDirectory(zipPath, accPath + "/" + liv.Name);


                // keep the tmp folder clean
                File.Delete(zipPath);

                

                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<HttpStatusCode> UploadLivery(string path, string Name, string Checksum, bool isUpdate = false)
        {
            var tmpPath = Path.GetTempPath() + "/AccLiveryManager/";
            var zipPath = tmpPath + "/" + Name + ".zip";

            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }


            if (File.Exists(zipPath))
            {
                // in case the program died on last upload
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(path, zipPath);


            var client = new RestClient(baseUri + "liveries");

            var request = new RestRequest();
            request.AddHeader("Authorization", jwt);
            request.AddParameter("Name", Name);
            request.AddParameter("Checksum", Checksum);
            request.AddFile("file", zipPath);

            var taskSource = new TaskCompletionSource<IRestResponse>();
            
            IRestResponse response;
            if (isUpdate)
            {
                request.Method = Method.PUT;
                response = await client.ExecuteAsync(request);
            }
            else
            {
                response = await client.ExecutePostAsync(request);
            }


            File.Delete(zipPath);

            return response.StatusCode;
        }


        public static async Task<HttpStatusCode> DeleteLivery(Livery liv)
        {
            var client = new RestClient(baseUri + "liveries/" + liv.Id);

            var request = new RestRequest(Method.DELETE);
            request.AddHeader("Authorization", jwt);


            var response = await client.ExecuteAsync(request);

            return response.StatusCode;
        }
    }
}
