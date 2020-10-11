using System;
using System.Collections.Generic;
using System.Text;

namespace AccLiverySyncer.Model
{
    public class Livery
    {
        
        public long Id { get; set; }
        public string Checksum { get; set; }
        public string Name { get; set; }
        public DateTime InsertTime { get; set; }
        public string OwnerId { get; set; }


        public bool NeedsUpdate { get; set; }
        public bool IsInstalled { get; set; }



        public string IsInstalledString
        {
            get
            {
                if (IsInstalled)
                {
                    return "Installed";
                }
                else
                {
                    return "";
                }
            }
        }



        public string NeedsUpdateString
        {
            get
            {
                if (NeedsUpdate)
                {
                    return "Update available";
                }
                else
                {
                    return "";
                }
            }
            
        }
    }
}
