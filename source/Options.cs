﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RDACcs
{
    class Options
    {
        private SortedDictionary<String, String> options = new SortedDictionary<String, String>();

        //--server_host=localhost --server_port=17092 --customer_path=/EventLog/CreateByEventLog --journals=Application(Error[All])
        public void init(string[] args)
        {
            foreach (String iter in args)
            {
                char[] options_separator = { '=' };
                String[] options_tokens = iter.Split(options_separator);
                {
                    String key = options_tokens[0].Replace("--", "").Trim();
                    String value = options_tokens[1].Trim();
                    if (options.ContainsKey(key))
                    {
                        options[key] = value;
                    }
                    else
                    {
                        options.Add(key, value);
                    }
                }
            }
        }

        public void clear()
        {
            options.Clear();
        }

        public Options()
        {
            options.Add("server_host", "test-devsrv");
            options.Add("server_port", "80");
            options.Add("customer_path", "/EventLog/CreateByEventLog");
            options.Add("journals", "Application(Error[All],Warning[All])|System(Error[All],Warning[All])|Security(AuditFailure[All])");
            options.Add("attempts_number", "100");
            options.Add("sleep_time", "60"); // in seconds
        }

        public String getValue(String key)
        {
            return options[key];
        }
    }
}
