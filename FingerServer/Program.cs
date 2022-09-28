using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FingerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try{
                string puerto = ConfigurationManager.AppSettings["puerto"].ToString();
                string huellero = ConfigurationManager.AppSettings["huellero"].ToString();
                string equipo = ConfigurationManager.AppSettings["equipo"].ToString();
                bool enabled_finger = false;

                if (puerto == string.Empty){
                    puerto = "9999";
                }

                if(huellero == "1"){
                    enabled_finger = true;
                }

                //WebServer server = new WebServer(Convert.ToInt32(puerto), enabled_finger, Convert.ToInt32(equipo));
                WebServerV2 server = new WebServerV2(Convert.ToInt32(puerto), enabled_finger, Convert.ToInt32(equipo));

                server.Start();
                server.Listen();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
           
        }
    }
}
