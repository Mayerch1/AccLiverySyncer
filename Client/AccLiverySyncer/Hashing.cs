using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AccLiverySyncer
{
    public class Hash
    {
        /// <summary>
        /// Create the md5 hash over a directory and its content
        /// </summary>
        /// <param name="path">Path to directory</param>
        /// <param name="whitelist">if not empty: only these files are considered for hashing</param>
        /// <returns>null if directory is empty, hash otherwise</returns>
        public static string CreateMd5ForFolder(string path, string[] whitelist = null)
        {
            // assuming you want to include nested folders
            var files = GetFileinDirWhitelist(path, whitelist);

            MD5 md5 = MD5.Create();

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];


                // hash path
                string relativePath = file.Substring(path.Length + 1);
                byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                byte[] contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }


            if(md5.Hash == null)
            {
                return null;
            }
            else
            {
                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
            
        }


        public static List<string> GetFileinDirWhitelist(string path, string[] whitelist = null)
        {
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();


            List<string> result = new List<string>();


            foreach(var file in files)
            {
                // ignore files which are not on the whitelist
                if (whitelist != null && !whitelist.Contains(Path.GetFileName(file.ToLower())))
                {
                    continue;
                }
                else
                {
                    result.Add(file);
                }
            }

            return result;
        }
    }
}
