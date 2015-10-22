using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RDACcs
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] cmdLine)
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new RDACcs(cmdLine)
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                do
                {
                    sb.Append(ex.Message);
                    sb.Append('\n');

                    ex = ex.InnerException;
                } while (ex != null);

                EventLog.WriteEntry(EventLogHandler.module_name, sb.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
