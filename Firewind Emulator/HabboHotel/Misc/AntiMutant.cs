using System;
using Firewind.Core;
using System.Net;
using System.IO;
using System.Text;

namespace Firewind.HabboHotel.Misc
{
    static class AntiMutant
    {
        // TODO: Recode this
        // This is now a hidden license check
        internal static bool ValidateLook(string Look, string Gender)
        {
            return true;
            //WebRequest req = WebRequest.Create("http://getfirewind.com/auth");

            //req.ContentType = "application/x-www-form-urlencoded";
            //req.Method = "POST";

            //byte[] bytes = Encoding.UTF8.GetBytes(string.Format("key={0}", FirewindEnvironment.Key));
            //req.ContentLength = bytes.Length;

            //Stream os = req.GetRequestStream();
            //os.Write(bytes, 0, bytes.Length); //Push it out there
            //os.Close();

            //WebResponse resp = req.GetResponse();
            //if (resp == null)
            //    return true;

            //StreamReader sr = new StreamReader(resp.GetResponseStream());
            //string result = sr.ReadToEnd().Trim();
            //if (result == "not_authed" || result == "expired")
            //    return false;

            //return true;
        //    bool HasHead = false;

        //    if (Look.Length < 1)
        //    {
        //        return false;
        //    }

        //    try
        //    {
        //        string[] Sets = Look.Split('.');

        //        if (Sets.Length < 4)
        //        {
        //            return false;
        //        }

        //        foreach (string Set in Sets)
        //        {
        //            string[] Parts = Set.Split('-');

        //            if (Parts.Length < 3)
        //            {
        //                return false;
        //            }

        //            string Name = Parts[0];
        //            int Type = int.Parse(Parts[1]);
        //            int Color = int.Parse(Parts[1]);

        //            if (Type <= 0 || Color < 0)
        //            {
        //                return false;
        //            }

        //            if (Name.Length != 2)
        //            {
        //                return false;
        //            }

        //            if (Name == "hd")
        //            {
        //                HasHead = true;
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logging.HandleException(e, "AntiMutant.ValidateLook");
        //        return false;
        //    }

        //    if (!HasHead || (Gender != "M" && Gender != "F"))
        //    {
        //        return false;
        //    }
        }
    }
}
