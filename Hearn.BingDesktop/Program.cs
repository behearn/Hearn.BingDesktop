using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Hearn.BingDesktop
{
    class Program
    {

        const string BingBaseUrl = "https://www.bing.com";

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        static void Main(string[] args)
        {

            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hearn", "BingDesktop");
            var logPath = Path.Combine(appDataPath, "log.txt");

            StreamWriter logStream;
            try
            {
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                logStream = new StreamWriter(logPath, false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to create {logPath}");
                Console.Error.WriteLine(ex.ToString());
                return;
            }

            try
            {

                var feedUrl = $"{BingBaseUrl}/HPImageArchive.aspx?format=xml&idx=0&n=1&mkt=en-GB";

                var feedXml = XDocument.Load(feedUrl);

                var imageUrl = feedXml.Descendants().Where(n => n.Name == "url").FirstOrDefault()?.Value;
                var fullUrl = String.Concat(BingBaseUrl, imageUrl);

                var jpgPath = Path.Combine(appDataPath, "Desktop.jpg");

                using (var webClient = new WebClient())
                {
                    var imageStream = webClient.OpenRead(fullUrl);
                    
                    using (var jpgStream = new FileStream(jpgPath, FileMode.Create))
                    {
                        imageStream.CopyTo(jpgStream);
                        jpgStream.Flush();
                        jpgStream.Close();
                    }

                    imageStream.Flush();
                    imageStream.Close();
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, jpgPath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                var headline = feedXml.Descendants().Where(n => n.Name == "headline").FirstOrDefault()?.Value;
                logStream.WriteLine($"{headline}");

                var copyrightLink = feedXml.Descendants().Where(n => n.Name == "copyrightlink").FirstOrDefault()?.Value;
                logStream.WriteLine($"{copyrightLink}");

            }
            catch(Exception ex)
            {
                logStream.WriteLine("ERROR : " + ex.ToString());
                Console.Error.WriteLine($"Failed to update desktop image.  See {logPath} for full details.");
            }
            finally
            {
                if (logStream != null)
                {
                    logStream.Flush();
                    logStream.Close();
                }
            }

        }
    }
}
