using PowerPlanManager.Properties;
using System.Globalization;
using System.Text;

namespace PowerPlanManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MyCustomApplicationContext());
        }
    }

    struct PowerPlan
    {
        public Boolean isActive;
        public string guid;
        public string name;
    }

    public class MyCustomApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public MyCustomApplicationContext()
        {
            ContextMenuStrip strip = new ContextMenuStrip();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            System.Diagnostics.Debug.WriteLine("Found power plans");
            List<PowerPlan> availablePlans = GetAvailablePowerPlans();
            foreach (PowerPlan item in availablePlans)
            {
                System.Diagnostics.Debug.WriteLine(item.name + " - " + item.guid);
                strip.Items.Add(new ToolStripMenuItem(item.name, null, (sender, e) => ActivatePowerPlan(item)));
            }
            strip.Items.Add(new ToolStripMenuItem("Exit", null, Exit));

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = strip,
                Visible = true,
                Text = availablePlans.Find(plan => plan.isActive).name
            };
        }

        void Exit(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
            Dispose();
        }

        List<PowerPlan> GetAvailablePowerPlans()
        {
            string[] lines = RunCommand("powercfg", "/list").Split('\n');

            List<PowerPlan> result = new();

            foreach (string line in lines)
            {
                if (!line.Contains("GUID"))
                {
                    continue;
                }

                result.Add(GetPowerPlanFromString(line));
            }
            return result;
        }

        PowerPlan GetPowerPlanFromString(string inputString)
        {
            PowerPlan temp = new PowerPlan();
            temp.guid = inputString.Substring(inputString.IndexOf(':')+2, 36);

            int startIndex = inputString.IndexOf('(') + 1;
            int endIndex = inputString.IndexOf(')');
            temp.name = inputString.Substring(startIndex, endIndex - startIndex);

            temp.isActive = inputString.Contains('*');

            return temp;
        }

        void ActivatePowerPlan(PowerPlan plan)
        {
            RunCommand("powercfg", "/setactive " + plan.guid);
            UpdateActivePlan(plan);
        }

        string RunCommand(string command, string arguments)
        {
            System.Diagnostics.Process pProcess = new();
            pProcess.StartInfo.FileName = command;
            pProcess.StartInfo.Arguments = arguments;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.Start();
            //Get program output
            string output = pProcess.StandardOutput.ReadToEnd();

            pProcess.WaitForExit();
            return output;
        }

        void UpdateActivePlan(PowerPlan plan)
        {
            trayIcon.Text = plan.name;
        }
    }
}
