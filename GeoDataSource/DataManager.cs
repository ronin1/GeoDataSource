using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using GeoDataSource.Extensions;
using ICSharpCode.SharpZipLib.Zip;
using log4net;

namespace GeoDataSource
{
    public sealed class DataManager
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(DataManager));

        #region singleton boiler plate

        //NOTE: framework level thread-safe lazy singleton pattern
        private DataManager() { }
        class Inner { static readonly internal DataManager SINGLETON = new DataManager(); }
        public static DataManager Instance { get { return Inner.SINGLETON; } }

        #endregion

        public string DataFile
        {
            get { return Path.Combine(Root, dataFile + ".dat"); }
        }

        string Root
        {
            get
            {
                string dll = typeof(DataManager).Assembly.CodeBase;
                Uri u;
                Uri.TryCreate(dll, UriKind.RelativeOrAbsolute, out u);
                FileInfo fi = new FileInfo(u.LocalPath);
                return fi.Directory.FullName;
            }
        }

        const string LastModified = "GeoDataSource-LastModified.txt";
        const string allCountriesUrl = "http://download.geonames.org/export/dump/allCountries.zip";
        //const string alternateNamesUrl = "http://download.geonames.org/export/dump/alternateNames.zip";
        //const string admin1CodesUrl = "http://download.geonames.org/export/dump/admin1CodesASCII.txt";
        //const string admin2CodesUrl = "http://download.geonames.org/export/dump/admin2Codes.txt";
        const string countryInfoUrl = "http://download.geonames.org/export/dump/countryInfo.txt";
        const string featureCodes_enUrl = "http://download.geonames.org/export/dump/featureCodes_en.txt";
        //const string languagecodesUrl = "http://download.geonames.org/export/dump/iso-languagecodes.txt";
        const string timeZonesUrl = "http://download.geonames.org/export/dump/timeZones.txt";

        const string dataFile = "GeoDataSource";
        const string countriesRawFile = "allCountries.txt";
        static readonly object _lock = new object();

        #region update pre-checks

        bool CanWriteTest(string tmpFile)
        {
            //check to see if we have read/write permission to disk
            bool canReadWrite = false;
            var f = new FileInfo(tmpFile);
            try
            {
                File.WriteAllText(tmpFile, "testing write access");
                string contents = File.ReadAllText(tmpFile);
                canReadWrite = !string.IsNullOrEmpty(contents);
                File.Delete(tmpFile); //cleanup existing file
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            _logger.DebugFormat("CanWriteTest({0}) => {1}", f.Name, canReadWrite);
            return canReadWrite;
        }

        bool ShouldDownloadCheck(bool canReadWrite, string lastModifiedFile)
        {
            var f = new FileInfo(lastModifiedFile);
            bool shouldDownload = !File.Exists(lastModifiedFile);
            if (canReadWrite && File.Exists(lastModifiedFile))
            {
                var lastModified = File.ReadAllText(lastModifiedFile);
                DateTime lastModifiedDate;
                if (!DateTime.TryParse(lastModified, out lastModifiedDate))
                {
                    shouldDownload = true;
                }
                else
                {
                    var request = new WebClientRequest();
                    request.Headers = new WebHeaderCollection();
                    var client = new ParallelWebClient(request);
                    client.OpenReadTask(allCountriesUrl, "HEAD").ContinueWith(t =>
                    {
                        var headers = client.ResponseHeaders;
                        foreach (string h in headers.Keys)
                        {
                            if (h.ToLowerInvariant() == "last-modified")
                            {
                                var header = headers[h];
                                DateTime headerDate = DateTime.Now;
                                DateTime.TryParse(header.ToString(), out headerDate);

                                if (lastModifiedDate < headerDate)
                                    shouldDownload = true;
                                break;
                            }

                        }
                    }).Wait();
                }
            }
            _logger.DebugFormat("ShouldDownloadCheck({0} , {1}) => {2}", canReadWrite, f.Name, shouldDownload);
            return shouldDownload;
        }

        #endregion

        [Flags]
        internal enum UpdateStep : int
        {
            None = 0,
            WriteCheck = 1,
            UpdateCheck = 2,
            Download = 4,
            ChangeModifiedDate = 8,
            Extraction = 16,
            Cleanup = 32,

            All = WriteCheck | UpdateCheck | Download | Extraction | Cleanup,
        }

        public Task Update()
        {
            return Update(UpdateStep.All);
        }

        /// <summary>
        /// load file: GeoData - LastModified.txt
        /// if available then
        /// try to HTTP head the data source <see cref="http://download.geonames.org/export/dump/allCountries.zip"/>
        /// pull off last - modified header 
        /// if newer than the file last-modified, do a GET to download
        /// else do a get to download
        /// </summary>
        internal Task Update(UpdateStep steps)
        {
            if (steps == UpdateStep.None)
                throw new InvalidOperationException("steps == UpdateStep.None");

            return Task.Factory.StartNew(() =>
            {
                lock (_lock)
                {
                    var fs = new GeoFileSet(Root)
                    {
                        allCountriesFile = Path.Combine(Root, dataFile + ".zip"),
                        countryInfoFile = Path.Combine(Root, "countryInfo.txt"),
                        featureCodes_enFile = Path.Combine(Root, "featureCodes_en.txt"),
                        timeZonesFile = Path.Combine(Root, "timeZones.txt"),
                    };

                    string lastModifiedFile = Path.Combine(Root, LastModified);
                    string tmpFile = Path.Combine(Root, Guid.NewGuid().ToString());
                    bool canReadWrite = steps.HasFlag(UpdateStep.WriteCheck) ? CanWriteTest(tmpFile) : true;
                    bool shouldDownload = steps.HasFlag(UpdateStep.UpdateCheck) ? 
                        ShouldDownloadCheck(canReadWrite, lastModifiedFile) : true;

                    if (canReadWrite && shouldDownload)
                    {
                        var downloadTasks = new List<Task>();
                        
                        if (steps.HasFlag(UpdateStep.Download))
                        {
                            downloadTasks.Add(DownloadFile(countryInfoUrl, fs.countryInfoFile));
                            downloadTasks.Add(DownloadFile(featureCodes_enUrl, fs.featureCodes_enFile));
                            downloadTasks.Add(DownloadFile(timeZonesUrl, fs.timeZonesFile));
                            downloadTasks.Add(DownloadFile(allCountriesUrl, fs.allCountriesFile, c =>
                            {
                                try
                                {
                                    if (steps.HasFlag(UpdateStep.ChangeModifiedDate))
                                    {
                                        var headers = c.ResponseHeaders;
                                        foreach (string h in headers.Keys)
                                        {
                                            if (h.ToLowerInvariant() == "last-modified")
                                            {
                                                var header = headers[h];
                                                DateTime headerDate = DateTime.Now;
                                                DateTime.TryParse(header.ToString(), out headerDate);
                                                File.WriteAllText(lastModifiedFile, headerDate.ToString());
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception dex)
                                {
                                    string s = string.Format("DownloadFile: CallBack => {0}", new FileInfo(fs.allCountriesFile).Name);
                                    _logger.Error(s, dex);
                                }
                            }));

                            Task.WaitAll(downloadTasks.ToArray());
                        }
                        if (steps.HasFlag(UpdateStep.Extraction))
                        {
                            if (ConvertZipToDat(fs))
                            {
                                if (steps.HasFlag(UpdateStep.Cleanup))
                                    FileCleanup(fs);
                                else
                                    _logger.Warn("Update: No Cleanup flags");
                            }
                            else
                                _logger.Warn("Update: Dat conversion failed");
                        }
                        else
                            _logger.Warn("Update: No Extraction flags");
                    }
                    else
                        _logger.WarnFormat("Update: Not running: canReadWrite=={0} && shouldDownload=={1}", canReadWrite, shouldDownload);
                }
            });
        }

        #region core update helpers

        Task DownloadFile(string url, string file, Action<WebClient> callback = null)
        {
            var f = new FileInfo(file);
            var client = new WebClient();
            if (f.Exists)
            {
                _logger.DebugFormat("DownloadFile: remove local => {0}", f.Name);
                File.Delete(file);
            }

            var u = new Uri(url);
            double lastPct = 0;
            client.DownloadProgressChanged += (o,e) =>
            {
                try {
                    double pct = Math.Round((double)e.BytesReceived / e.TotalBytesToReceive, 3) * 100;
                    if (lastPct < pct)
                    {
                        lastPct = pct;
                        _logger.DebugFormat("DownloadFile: {0:F1}% {1}", pct, f.Name);
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(string.Format("DownloadFile: DownloadProgressChanged {0}", f.Name), ex);
                }
            };
            client.DownloadFileCompleted += (o,e) =>
            {
                try {
                    _logger.InfoFormat("DownloadFile: completed => {0}", f.Name);
                    if (client != null)
                    {
                        if (callback != null)
                            callback(client);

                        client.Dispose();
                    }
                    else
                        _logger.WarnFormat("DownloadFile: null client handle => {0}", f.Name);
                }
                catch(Exception ex)
                {
                    _logger.Error(string.Format("DownloadFile: DownloadFileCompleted {0}", f.Name), ex);
                }
            };
            _logger.InfoFormat("DownloadFile: Begin => {0}", url);
            return client.DownloadFileTaskAsync(u, f.Name);
        }

        class GeoFileSet
        {
            public readonly string Root;
            public GeoFileSet(string root)
            {
                if (string.IsNullOrWhiteSpace(root))
                    throw new ArgumentException("root can not be null or blank!");

                Root = root;
            }

            public string allCountriesFile { get; set; }
            public string timeZonesFile { get; set; }
            public string featureCodes_enFile { get; set; }
            public string countryInfoFile { get; set; }

            public string countriesRawPath
            {
                get { return Path.Combine(Root, countriesRawFile); }
            }

            public IEnumerable<string> AllFiles
            {
                get
                {
                    yield return allCountriesFile;
                    yield return timeZonesFile;
                    yield return featureCodes_enFile;
                    yield return countryInfoFile;
                    yield return countriesRawPath;
                }
            }
        }

        bool ConvertZipToDat(GeoFileSet fs)
        {
            bool success = false;
            var f = new FileInfo(fs.allCountriesFile);
            //downloaded, now convert zip into serialized dat file
            if (File.Exists(fs.allCountriesFile))
            {
                if (File.Exists(fs.countriesRawPath))
                {
                    _logger.DebugFormat("ConvertZipToDat: removing {0}", new FileInfo(fs.countriesRawPath).Name);
                    File.Delete(fs.countriesRawPath);
                }

                var fz = new FastZip();
                fz.ExtractZip(fs.allCountriesFile, Root, FastZip.Overwrite.Always, null, null, null, true);
                if (File.Exists(fs.countriesRawPath))
                {
                    _logger.InfoFormat("ConvertZipToDat: parsing extracted => {0}", f.Name);
                    GeoData gd = GeoNameParser.ParseFile(
                        fs.countriesRawPath, 
                        fs.timeZonesFile, 
                        fs.featureCodes_enFile, 
                        fs.countryInfoFile);

                    _logger.DebugFormat("ConvertZipToDat: storing dat => {0}", DataFile);
                    Serialize.SerializeBinaryToDisk(gd, DataFile);

                    _logger.Info("ConvertZipToDat: completed");
                    success = true;
                }
                else
                    _logger.WarnFormat("ConvertZipToDat: Extraction failed => {0}", f.Name);
            }
            else
                _logger.WarnFormat("ConvertZipToDat: FileNotFound => {0}", f.Name);

            return success;
        }

        void FileCleanup(GeoFileSet fs)
        {
            foreach(string f in fs.AllFiles)
            {
                if (File.Exists(f))
                {
                    _logger.DebugFormat("FileCleanup: {0}", new FileInfo(f).Name);
                    File.Delete(f);
                }
            }
        }

        #endregion
    }
}