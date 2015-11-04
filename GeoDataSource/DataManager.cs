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
            get { return Path.Combine(Root, DATA_FILE + ".dat"); }
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

        const string LAST_MODIFIED_FILE = "GeoDataSource-LastModified.txt";
        const string ALL_COUNTRIES_URL = "http://download.geonames.org/export/dump/allCountries.zip";
        //const string alternateNamesUrl = "http://download.geonames.org/export/dump/alternateNames.zip";
        //const string admin1CodesUrl = "http://download.geonames.org/export/dump/admin1CodesASCII.txt";
        //const string admin2CodesUrl = "http://download.geonames.org/export/dump/admin2Codes.txt";
        const string COUNTRY_INFO_URL = "http://download.geonames.org/export/dump/countryInfo.txt";
        const string FEATURE_CODES_EN_URL = "http://download.geonames.org/export/dump/featureCodes_en.txt";
        //const string languagecodesUrl = "http://download.geonames.org/export/dump/iso-languagecodes.txt";
        const string TIME_ZONE_URL = "http://download.geonames.org/export/dump/timeZones.txt";
        const string POSTAL_CODE_URL = "http://download.geonames.org/export/zip/allCountries.zip";
        const string POSTAL_CODE = "allCountries.postal";

        const string DATA_FILE = "GeoDataSource";
        const string COUNTRIES_RAW_FILE = "allCountries.txt";
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
                    client.OpenReadTask(ALL_COUNTRIES_URL, "HEAD").ContinueWith(t =>
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
                        AllCountriesFile = Path.Combine(Root, DATA_FILE + ".zip"),
                        CountryInfoFile = Path.Combine(Root, "countryInfo.txt"),
                        FeatureCodes_EnFile = Path.Combine(Root, "featureCodes_en.txt"),
                        TimeZonesFile = Path.Combine(Root, "timeZones.txt"),
                        AllCountriesPostal = Path.Combine(Root, POSTAL_CODE + ".zip"),
                    };
                    string lastModifiedFile = Path.Combine(Root, LAST_MODIFIED_FILE);
                    string tmpFile = Path.Combine(Root, Guid.NewGuid().ToString());
                    bool canReadWrite = steps.HasFlag(UpdateStep.WriteCheck) ? CanWriteTest(tmpFile) : true;
                    bool shouldDownload = steps.HasFlag(UpdateStep.UpdateCheck) ? 
                        ShouldDownloadCheck(canReadWrite, lastModifiedFile) : true;

                    if (canReadWrite && shouldDownload)
                    {
                        var downloadTasks = new List<Task>();                        
                        if (steps.HasFlag(UpdateStep.Download))
                        {
                            downloadTasks.Add(DownloadFile(COUNTRY_INFO_URL, fs.CountryInfoFile));
                            downloadTasks.Add(DownloadFile(FEATURE_CODES_EN_URL, fs.FeatureCodes_EnFile));
                            downloadTasks.Add(DownloadFile(TIME_ZONE_URL, fs.TimeZonesFile));
                            downloadTasks.Add(DownloadFile(POSTAL_CODE_URL, fs.AllCountriesPostal));
                            downloadTasks.Add(DownloadFile(ALL_COUNTRIES_URL, fs.AllCountriesFile, c =>
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
                                    string s = string.Format("DownloadFile: CallBack => {0}", new FileInfo(fs.AllCountriesFile).Name);
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
            DateTime started = DateTime.UtcNow;
            client.DownloadProgressChanged += (o,e) =>
            {
                try {
                    double pct = Math.Round((double)e.BytesReceived / e.TotalBytesToReceive, 2) * 100;
                    if (lastPct < pct)
                    {
                        lastPct = pct;
                        _logger.DebugFormat("DownloadFile: {0:F0}% {1}", pct, f.Name);
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
                    _logger.InfoFormat("DownloadFile: completed {0} => {1}", DateTime.UtcNow - started, f.Name);
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

            public string AllCountriesFile { get; set; }
            public string TimeZonesFile { get; set; }
            public string FeatureCodes_EnFile { get; set; }
            public string CountryInfoFile { get; set; }
            public string AllCountriesPostal { get; set; }

            public string CountriesRawPath
            {
                get { return Path.Combine(Root, COUNTRIES_RAW_FILE); }
            }

            public string PostalsRawPath
            {
                get { return Path.Combine(Root, POSTAL_CODE, COUNTRIES_RAW_FILE); }
            }

            public IEnumerable<string> AllFiles
            {
                get
                {
                    yield return AllCountriesFile;
                    yield return TimeZonesFile;
                    yield return FeatureCodes_EnFile;
                    yield return CountryInfoFile;
                    yield return CountriesRawPath;
                    yield return PostalsRawPath;
                }
            }
        }

        void Unzip(string source, string destination)
        {
            var fz = new FastZip();
            var af = new FileInfo(source);
            DateTime unzipStart = DateTime.UtcNow;
            _logger.DebugFormat("Unzip: Begin => {0}", af.Name);
            fz.ExtractZip(source, destination, FastZip.Overwrite.Always, null, null, null, true);
            _logger.InfoFormat("Unzip: Completed {0} => {1}", DateTime.UtcNow - unzipStart, af.Name);
        }

        bool ConvertZipToDat(GeoFileSet fs)
        {
            bool success = false;
            var f = new FileInfo(fs.AllCountriesFile);
            //downloaded, now convert zip into serialized dat file
            if (File.Exists(fs.AllCountriesFile))
            {
                if (File.Exists(fs.CountriesRawPath))
                {
                    _logger.DebugFormat("ConvertZipToDat: removing {0}", new FileInfo(fs.CountriesRawPath).Name);
                    File.Delete(fs.CountriesRawPath);
                }

                Unzip(fs.AllCountriesFile, Root);

                var zd = new DirectoryInfo(Path.Combine(Root, POSTAL_CODE));
                if (zd.Exists)
                    zd.Delete(true);

                zd.Create();
                Unzip(fs.AllCountriesPostal, zd.FullName);

                if (File.Exists(fs.CountriesRawPath))
                {
                    GeoData gd = ParseGeoFiles(fs);

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

        GeoData ParseGeoFiles(GeoFileSet fs)
        {
            DateTime extractionStart = DateTime.UtcNow;
            _logger.Debug("ParseGeoFiles: Begin Extraction");

            var gd = new GeoData();            
            gd.TimeZones = new TimeZoneParser(fs.TimeZonesFile).ParseFile();
            gd.FeatureCodes = new FeatureCodeParser(fs.FeatureCodes_EnFile).ParseFile();
            gd.Countries = new CountryParser(fs.CountryInfoFile).ParseFile();
            gd.GeoNames = new GeoNameParser(fs.CountriesRawPath).ParseFile();

            var zf = new FileInfo(fs.PostalsRawPath);
            if (zf.Exists)
            {
                var incCountries = new[] { "US", "CA", "AT", "MX", "GB" };
                gd.PostalCodes = new PostalCodeParser(zf.FullName, incCountries).ParseFile();
                LinkPostalElements(gd);
            }
            _logger.InfoFormat("ParseGeoFiles: Completed Extraction {0}", DateTime.UtcNow - extractionStart);
            return gd;
        }

        void LinkPostalElements(GeoData gd)
        {
            if(gd.PostalCodes != null && gd.PostalCodes.Count > 0 &&
                gd.GeoNames != null && gd.GeoNames.Count > 0)
            {
                Dictionary<string, Country> iso2Map = (from c in gd.Countries
                                                       where c != null && !string.IsNullOrWhiteSpace(c.ISOAlpha2)
                                                       group c by c.ISOAlpha2 into cg
                                                       select cg).ToDictionary(g => g.Key.ToLower().Trim(), g => g.FirstOrDefault());
                foreach(PostalCode p in gd.PostalCodes)
                {
                    string k = p.Country.ISOAlpha2;
                    if (string.IsNullOrWhiteSpace(k))
                        continue;

                    k = k.ToLower().Trim();
                    if (iso2Map.ContainsKey(k))
                        p.Country = iso2Map[k];
                }
            }
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