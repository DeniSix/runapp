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
using Microsoft.Win32;
#endregion

namespace RunApp
{
    class Program
    {
        // The URL handler for this app
        const string APP_PREFIX = "runapp://";
        const string REGISTRY_KEY = "runapp";

        // The name of this app for user messages
        const string APP_TITLE = "RunApp URL Protocol Handler";

        // Path to the configuration file
        static string APP_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static string APP_EXE = Assembly.GetExecutingAssembly().Location;
        static string APP_CONFIG = Path.Combine(APP_PATH, "runapp.xml");

        static void Main(string[] args)
        {
            // Verify the command line arguments
            if (args.Length == 0 || !args[0].StartsWith(APP_PREFIX))
            {
                if (args.Length > 0 && args[0].ToLower() == "/uninstall")
                {
                    Uninstall();
                    ShowInfo("RunApp handler uninstalled.");
                    return;
                }

                if (IsInstalled())
                {
                    ShowInfo("URL Syntax:\n  runapp://<key>[?parameters]");
                    return;
                }

                if (AskQuestion("Do you want to install handler?"))
                {
                    Install();
                    ShowInfo("RunApp handler installed.");
                }
                return;
            }

            // Verify the config file exists
            if (!File.Exists(APP_CONFIG))
            {
                ShowError("Could not find configuration file.\n" + APP_CONFIG);
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
                ShowError(string.Format("Error loading the XML config file.\n{0}\n{1}", APP_CONFIG, e.Message));
                return;
            }

            // Parse app URI
            var myUri = new Uri(args[0]);
            var query = HttpUtility.ParseQueryString(myUri.Query);
            var app = myUri.Host;

            // Locate the app to run
            XmlNode node = xml.SelectSingleNode(string.Format("/RunApp/App[@key='{0}']", app));

            // If the app is not found, let the user know
            if (node == null)
            {
                ShowError("Key not found: " + app);
                return;
            }

            // Resolve the target app name
            string target = Environment.ExpandEnvironmentVariables(node.SelectSingleNode("@target").Value);

#if DEBUG
            string placeholders = "";
#endif
            string appArgs = "";
            if (node.SelectSingleNode("@args") != null)
            {
                appArgs = node.SelectSingleNode("@args").Value;
                var reg = new Regex(@"\{(\S+?)\}", RegexOptions.IgnoreCase);
                var mc = reg.Matches(appArgs);
                foreach (Match mat in mc)
                {
                    var name = mat.Groups[1].Value;
                    appArgs = appArgs.Replace(string.Format("{{{0}}}", name), query.Get(name));
#if DEBUG
                    placeholders += name + ", ";
#endif
                }
            }

            // Pull the command line args for the target app if they exist
            string procargs = Environment.ExpandEnvironmentVariables(appArgs);
#if DEBUG
            ShowInfo(string.Format("Full URI: {0}\nApp name: {1}\nQuery: {2}\nTarget: {3}\nArgs: {4}\nPlaceholders: {5}",
                myUri, app, query, target, appArgs, placeholders));
#endif
            // Start the application
            try
            {
                Process.Start(target, procargs);
            }
            catch (Exception e)
            {
                ShowError(string.Format("Error starting process:\n{0}\n{1} {2}", e.Message, target, procargs));
            }
        }

        #region Message boxes
        static void ShowInfo(string text)
        {
            MessageBox.Show(text, APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        static void ShowError(string text)
        {
            MessageBox.Show(text, APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static bool AskQuestion(string text)
        {
            var res = MessageBox.Show(text, APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return res == DialogResult.Yes;
        }
        #endregion

        #region Installer/Uninstaller
        static bool IsInstalled()
        {
            var key = Registry.ClassesRoot.OpenSubKey(REGISTRY_KEY);
            return key != null;
        }

        static void Install()
        {
            var rootKey = Registry.ClassesRoot.CreateSubKey(REGISTRY_KEY);
            rootKey.SetValue("", "RunApp Protocol");
            rootKey.SetValue("URL Protocol", "");

            var shellKey = rootKey.CreateSubKey("shell");
            shellKey.SetValue("", "open");

            var openKey = shellKey.CreateSubKey("open");

            var commandKey = openKey.CreateSubKey("command");
            commandKey.SetValue("", string.Format("\"{0}\" \"%1\"", APP_EXE));
        }

        static void Uninstall()
        {
            Registry.ClassesRoot.DeleteSubKeyTree(REGISTRY_KEY);
        }
        #endregion
    }
}
