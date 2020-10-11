using AccLiverySyncer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AccLiverySyncer
{
    public class LiveryController
    {
        public static List<Livery> liveries = new List<Livery>();


        public static void RefreshInstalled(string accPath)
        {
            foreach(var liv in liveries)
            {
                if(Directory.Exists(accPath + "/" + liv.Name))
                {
                    liv.IsInstalled = true;
                }
                else
                {
                    liv.IsInstalled = false;
                }

            }

        }


        public static void RefreshUpdateNeeded(string accPath)
        {
            foreach (var liv in liveries)
            {
                if (!liv.IsInstalled)
                {
                    continue;
                }

                // calculate hash over directroy
                // compare it to the hash of the database
                // mark for update when hashs are different
                var currentHash = Hash.CreateMd5ForFolder(accPath + "/" + liv.Name);
                if(currentHash != liv.Checksum)
                {
                    liv.NeedsUpdate = true;
                }
                else
                {
                    liv.NeedsUpdate = false;
                }

            }
        }



        public static async Task<string> UpdateAllRemoteLiveries(string accPath)
        {
            string err = "";

            for(int i=0; i<liveries.Count; i++)
            {
                string errIntern = await UpdateRemoteLivery(accPath, i);

                if (!String.IsNullOrWhiteSpace(errIntern))
                {
                    err += errIntern + "\n";
                }
            }

            return err;
        }


        public static async Task<string> UpdateRemoteLivery(string accPath, int index)
        {
            string err = "";

            if (liveries[index].IsInstalled && !liveries[index].NeedsUpdate)
            {
                err = index + ". no update available";
                return err;
            }


            string liveryPath = accPath + "/" + liveries[index].Name;



            // clean outdated livery
            if (Directory.Exists(liveryPath))
            {
                // make sure the server version is newer
                var lastWrite = Directory.GetLastWriteTime(liveryPath);

                if(lastWrite > liveries[index].InsertTime)
                {
                    err = index + ". Local livery is more recent";
                    return err;
                }


                Directory.Delete(liveryPath, true);
            }



            if(await Connector.DownloadLivery(accPath, liveries[index])){

                var newHash = Hash.CreateMd5ForFolder(liveryPath);

                if(newHash != liveries[index].Checksum)
                {
                    err = index + ". Hash missmatch, please contact the developer";
                }
                else
                {
                    err = index + ". Updated Livery";
                }


                liveries[index].NeedsUpdate = false;
            }

            return err;
        }




        public static async Task<string> DeleteRemoteLivery(int index)
        {
            string err;

            var code = await Connector.DeleteLivery(liveries[index]);

            if(code == System.Net.HttpStatusCode.Unauthorized)
            {
                err = "You do not have permission to delete this livery";
            }
            else if(code == System.Net.HttpStatusCode.NoContent)
            {
                err = "Livery deleted from server but not local";
            }
            else if(code == System.Net.HttpStatusCode.NotFound)
            {
                err = "Livery is not existing on the server anymore. Refresh your livery list";
            }
            else
            {
                err = "Unexpected error. Please contact the developer";
            }

            return err;
        }




        public static async Task<string> UploadLivery(string liveryPath)
        {
            string err = "";

            if (!Directory.Exists(liveryPath))
            {
                err = "Livey not found on disk";
                return err;
            }

            var hash = Hash.CreateMd5ForFolder(liveryPath);
            var name = new DirectoryInfo(liveryPath).Name;
            
            // first check if there is any duplicates on the server
            // when a duplicate is found, immediately try to PATCH
            var match = liveries.Find(l => l.Name == name);
            bool isUpdate = false;

            if(match != null)
            {
                isUpdate = true;
            }


            var result = await Connector.UploadLivery(liveryPath, name, hash, isUpdate);

            // ok
            if (result == System.Net.HttpStatusCode.OK)
            {
                err = "Livery uploaded";
            }
            // the livery is existing, try to update it (only possbile when owner
            else if (result == System.Net.HttpStatusCode.Conflict)
            {
                err = "Please refresh the livery list manually and try again";
            }

            // failing errors
            else if (result == 0)
            {
                err = "Cannot reach server";
            }
            else if(result == System.Net.HttpStatusCode.Unauthorized)
            {
                if (isUpdate)
                {
                    err = "The livery is owned by another user";
                }
                else
                {
                    err = "Invalid Credentials. Keep the token empty if you need to register";
                }
            }
            else
            {
                err = "Unexpected error. Please contact the developer";
            }

            return err;
        }



    }
}
