using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RDACcs
{
    public partial class RDACcs : ServiceBase
    {
        public RDACcs(string[] cmdLine)
        {
            InitializeComponent();
            {
                Options options = Singleton<Options>.Instance;
                options.init(cmdLine);

                JournalsFilter filter = Singleton<JournalsFilter>.Instance;
                filter.init(options.getValue("journals"));

                List<EventLogHandler> handlers = Singleton<List<EventLogHandler>>.Instance;
                foreach (String journal in filter.getJournals())
                {
                    EventLogHandler handler = new EventLogHandler();
                    {
                        handler.init(journal);
                    }
                    handlers.Add(handler);
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            foreach (EventLogHandler handler in Singleton<List<EventLogHandler>>.Instance)
            {
                handler.start();      
            }
        }

        protected override void OnStop()
        {
            foreach (EventLogHandler handler in Singleton<List<EventLogHandler>>.Instance)
            {
                handler.stop();
            }
        }
    }
}
