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
        public static string hostUri = "https://accliveries.cj-mayer.de";

        private static string baseUri
        {
            get
            {
                return hostUri + "/api/v1/";
            }
        }


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

        /// <summary>
        /// Download a livery from the server into the accPath
        /// Function shall only be called if livery is not already existin in the accPath.
        /// </summary>
        /// <param name="accPath">path to acc livery folder</param>
        /// <param name="liv">livery object (holds name/id of livery)</param>
        /// <param name="fileWhitelist">whitelist, security measurement to only download allowed files</param>
        /// <returns></returns>
        public static async Task<bool> DownloadLivery(string accPath, Livery liv, string[] fileWhitelist = null)
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


                if (fileWhitelist == null || fileWhitelist.Length == 0)
                {
                    ZipFile.ExtractToDirectory(zipPath, accPath + "/" + liv.Name);
                }
                else
                {
                    // extract into tmp directory and only copy valid files
                    if (Directory.Exists(path + liv.Name))
                    {
                        Directory.Delete(path + liv.Name, true);
                    }

                    Directory.CreateDirectory(path + liv.Name);
                    ZipFile.ExtractToDirectory(zipPath, path + "/" + liv.Name);

                    // create new livery directory
                    Directory.CreateDirectory(accPath + "/" + liv.Name);

                    var files = Hash.GetFileinDirWhitelist(path + liv.Name, fileWhitelist);
                    foreach(var file in files)
                    {
                        File.Copy(file, accPath + "/" + liv.Name + "/" + Path.GetFileName(file));
                    }

                    // cleanup
                    Directory.Delete(path + liv.Name, true);
                }


                // keep the tmp folder clean
                File.Delete(zipPath);

                

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Upload a livery to the server, might fail and return http error
        /// </summary>
        /// <param name="path">Path to the livery folder</param>
        /// <param name="Name">name of the livery, usually folder name</param>
        /// <param name="Checksum">md5 hash of the folder, fileWhitelist of hash should match fileWhitelist parameter</param>
        /// <param name="isUpdate">specify wether PUT or POST is executed</param>
        /// <param name="fileWhitelist">only upload the specified files (should match whitelist used for hashing)</param>
        /// <returns></returns>
        public static async Task<HttpStatusCode> UploadLivery(string path, string Name, string Checksum, bool isUpdate = false, string[] fileWhitelist = null)
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


            if(fileWhitelist != null && fileWhitelist.Length > 0)
            {
                // create new temp directory for whitelist
                if (Directory.Exists(tmpPath + Name))
                {
                    Directory.Delete(tmpPath + Name, true);
                }

                Directory.CreateDirectory(tmpPath + Name);


                var files = Hash.GetFileinDirWhitelist(path, LiveryController.fileWhitelist);
                foreach(var file in files)
                {
                    File.Copy(file, tmpPath + Name + "/" + Path.GetFileName(file));
                }

                path = tmpPath + Name;

            }
            

            ZipFile.CreateFromDirectory(path, zipPath);



            if (fileWhitelist != null && fileWhitelist.Length > 0)
            {
                // delete the temp directory 
                Directory.Delete(tmpPath + Name, true);
            }




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
