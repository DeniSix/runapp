/*
 * RunApp URL Protocol
 * by Noah Coad, http://noahcoad.com, http://coadblog.com
 *
 * Created: Oct 12, 2006
 * An example of creating a URL Protocol Handler in Windows
 *
 * For information, references, resources, etc, see:
 *
 * Register a Custom URL Protocol Handler
 * http://blogs.msdn.com/noahc/archive/2006/10/19/register-a-custom-url-protocol-handler.aspx
 *
 */
/*
 * Modified by Denis Shemanaev <denis@shemanaev.com>
 */

#region Namespace Inclusions
using System;
using System.IO;
using System.Xml;
using System.Web;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
#endregion

namespace RunApp
{
    class Program
    {
        // The URL handler for this app
        const string APP_PREFIX = "runapp://";

        // The name of this app for user messages
        const string APP_TITLE = "RunApp URL Protocol Handler";

        // Path to the configuration file
        static string APP_CONFIG = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RegisteredApps.xml");

        static void Main(string[] args)
        {
            // Verify the command line arguments
            if (args.Length == 0 || !args[0].StartsWith(APP_PREFIX))
            {
                MessageBox.Show("Syntax:\nrunapp://<key>", APP_TITLE);
                return;
            }

            // Verify the config file exists
            if (!File.Exists(APP_CONFIG))
            {
                MessageBox.Show("Could not find configuration file.\n" + APP_CONFIG, APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load the config file
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(APP_CONFIG);
            }
            catch (XmlException e)
            {
                MessageBox.Show(String.Format("Error loading the XML config file.\n{0}\n{1}", APP_CONFIG, e.Message), APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Parse app URI
            var myUri = new Uri(args[0]);
            var query = HttpUtility.ParseQueryString(myUri.Query);
            var app = myUri.Host;

            // Locate the app to run
            XmlNode node = xml.SelectSingleNode(String.Format("/RunApp/App[@key='{0}']", app));

            // If the app is not found, let the user know
            if (node == null)
            {
                MessageBox.Show("Key not found: " + app, APP_TITLE);
                return;
            }

            // Resolve the target app name
            string target = Environment.ExpandEnvironmentVariables(node.SelectSingleNode("@target").Value);

            string appArgs = "";
            if (node.SelectSingleNode("@args") != null)
            {
                appArgs = node.SelectSingleNode("@args").Value;
                var reg = new Regex(@"\{(\S+)\}", RegexOptions.IgnoreCase);
                var mc = reg.Matches(appArgs);
                foreach (Match mat in mc)
                {
                    var name = mat.Groups[1].Value;
                    appArgs = appArgs.Replace(String.Format("{{{0}}}", name), query.Get(name));
                }
            }

            // Pull the command line args for the target app if they exist
            string procargs = Environment.ExpandEnvironmentVariables(appArgs);

            // Start the application
            Process.Start(target, procargs);
        }
    }
}
