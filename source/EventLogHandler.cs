using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace RDACcs
{
    class EventLogHandler
    {
        private ManualResetEvent stop_success_queue = new ManualResetEvent(false);

        private AutoResetEvent new_success_instance = new AutoResetEvent(false);
        private ConcurrentQueue<EventLogEntry> success_queue = new ConcurrentQueue<EventLogEntry>();
        private Thread consumer_success_queue = null;

        private AutoResetEvent new_error_instance = new AutoResetEvent(false);
        private ConcurrentQueue<EventLogEntry> error_queue = new ConcurrentQueue<EventLogEntry>();
        private Thread consumer_error_queue = null;

        private const Int32 threads_join_timeout = 5000; //miliseconds

        private EventLog log = null;

        public static String module_name = "RDAC";

        public void init(String source_)
        {
            if (!EventLog.SourceExists(source_))
            {
                throw new Exception(String.Format("Source: {0} failed", source_));
            }

            log = new EventLog(source_);
            {
                //producer
                log.EntryWritten += new EntryWrittenEventHandler((Object source, EntryWrittenEventArgs e) =>
                                                    {
                                                        success_queue.Enqueue(e.Entry);
                                                        new_success_instance.Set();
                                                    });
                log.EnableRaisingEvents = true;
            }

            consumer_success_queue = new Thread(delegate()
                                                    {
                                                        try
                                                        {
                                                            Options options = Singleton<Options>.Instance;
                                                            String url = "http://" +
                                                                         options.getValue("server_host") +
                                                                         ":" +
                                                                         options.getValue("server_port") +
                                                                         options.getValue("customer_path");

                                                            JournalsFilter filter = Singleton<JournalsFilter>.Instance;

                                                            EventLogEntry entry;
                                                            while (true)
                                                            {
                                                                while (success_queue.TryDequeue(out entry))
                                                                {
                                                                    if (!filter.isExist(log.Log, entry.EntryType.ToString(), entry.EventID.ToString()/*entry.InstanceId.ToString()*/) ||
                                                                        entry.Source == module_name)
                                                                    {
                                                                        continue;
                                                                    }

                                                                    if (!SendAsync(url, getSendData(entry)))
                                                                    {
                                                                        error_queue.Enqueue(entry);
                                                                        new_error_instance.Set();             
                                                                    }
                                                                }

                                                                new_success_instance.WaitOne();
                                                                stop_success_queue.WaitOne();
                                                            }
                                                        }
                                                        catch (ThreadInterruptedException)
                                                        {

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

                                                            EventLog.WriteEntry(module_name, sb.ToString(), EventLogEntryType.Error);
                                                        }
                                                    });
            consumer_success_queue.Start();

            consumer_error_queue = new Thread(delegate()
                                                    {
                                                        try
                                                        {
                                                            Options options = Singleton<Options>.Instance;
                                                            String url = "http://" + 
                                                                         options.getValue("server_host") + 
                                                                         ":" +
                                                                         options.getValue("server_port") +
                                                                         options.getValue("customer_path");
                                                            Int32 attempts_number = Int32.Parse(options.getValue("attempts_number"));
                                                            Int32 sleep_time = Int32.Parse(options.getValue("sleep_time")) * 1000; // in milliseconds

                                                            EventLogEntry entry;
                                                            while (true)
                                                            {
                                                                while (error_queue.TryDequeue(out entry))
                                                                {
                                                                    for (Int32 counter = 0; counter < attempts_number && !SendAsync(url, getSendData(entry)); ++counter)
                                                                    {
                                                                        Thread.Sleep(sleep_time);
                                                                    }
                                                                }

                                                                new_error_instance.WaitOne();
                                                            }
                                                        }
                                                        catch (ThreadInterruptedException)
                                                        {

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

                                                            EventLog.WriteEntry(module_name, sb.ToString(), EventLogEntryType.Error);
                                                        }
                                                    });
            consumer_error_queue.Start();
        }
        
        public void Close()
        {
            log.Close();

            consumer_success_queue.Interrupt();
            consumer_error_queue.Interrupt();

            if (!consumer_success_queue.Join(threads_join_timeout))
            {
                consumer_success_queue.Abort();
            }

            if (!consumer_error_queue.Join(threads_join_timeout))
            {
                consumer_error_queue.Abort();
            }

            stop_success_queue.Close();
            new_success_instance.Close();
            new_error_instance.Close();
        }

        public void start()
        {
            stop_success_queue.Set();
        }

        public void stop()
        {
            stop_success_queue.Reset();
        }

        private Boolean SendAsync(String url, String data)
        {
            try
            {
                HttpWebRequest request = null;
                {//Request
                    request = (HttpWebRequest)WebRequest.Create(url);

                    //CookieCollection cookies = new CookieCollection();
                    //request.CookieContainer = new CookieContainer();
                    //request.CookieContainer.Add(cookies); //recover cookies First request
                    request.Method = WebRequestMethods.Http.Post;
                
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    request.AllowWriteStreamBuffering = true;
                    request.ProtocolVersion = HttpVersion.Version11;
                    request.AllowAutoRedirect = false;
                    request.ContentType = "application/x-www-form-urlencoded";
                    
                    byte[] bytes = Encoding.GetEncoding("windows-1251").GetBytes(data);
                    String data_ = Convert.ToBase64String(bytes);
                    byte[] bytes_ = Encoding.GetEncoding("windows-1251").GetBytes(data_);

                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.ContentLength = bytes_.Length;

                    var newStream = request.GetRequestStream();
                    {
                        newStream.Write(bytes_, 0, bytes_.Length);
                        newStream.Close();
                    }
                }

                if (request != null)
                {//Responce
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response != null)
                    {
                        StreamReader strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        String responseToString = strreader.ReadToEnd();
                        if (responseToString.Equals("success") != true)
                        {
                            throw new Exception(String.Format("Responce: {0}", responseToString));   
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                {
                    StringBuilder sb = new StringBuilder();
                    do
                    {
                        sb.Append(ex.Message);
                        sb.Append('\n');

                        ex = ex.InnerException;
                    } while (ex != null);

                    EventLog.WriteEntry(module_name, sb.ToString(), EventLogEntryType.Error);
                }
                return false;
            }
        }

        private String getSendData(EventLogEntry entry)
        {
            SendData data = new SendData();
            {
                data.EventLogRecordNumber = entry.Index.ToString();
                data.EventLogTimeGenerated = DateTime.SpecifyKind(entry.TimeGenerated, DateTimeKind.Local).ToString();
                data.EventLogTimeWritten = DateTime.SpecifyKind(entry.TimeWritten, DateTimeKind.Local).ToString(); 
                data.EventLogId = entry.EventID.ToString(); /*entry.InstanceId.ToString();*/
                data.EventLogType = entry.EntryType.ToString();
                data.EventLogCategory = entry.CategoryNumber.ToString();
                data.EventLogRecordMessage = entry.Message;
                data.EventLogSource = entry.Source;
                data.EventLogMachine = entry.MachineName;
                data.EventLogJournal = log.Log;
                data.GUID = "3170AA1C-2A18-4396-B2EF-671773929AF3";
            }

            JavaScriptSerializer jss = new JavaScriptSerializer();
            String result = jss.Serialize(data);
            return result;
        }

        [Serializable]
        private class SendData {
            public String EventLogRecordNumber { get; set; }
            public String EventLogTimeGenerated { get; set; }
            public String EventLogTimeWritten { get; set; }
            public String EventLogId { get; set; }
            public String EventLogType { get; set; }
            public String EventLogCategory { get; set; }
            public String EventLogRecordMessage { get; set; }
            public String EventLogSource { get; set; }
            public String EventLogMachine { get; set; }
            public String EventLogJournal { get; set; }
            public String GUID { get; set; }
        }
    }
}
