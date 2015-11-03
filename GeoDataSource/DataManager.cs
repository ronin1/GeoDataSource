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

namespace GeoDataSource
{
    public sealed class DataManager
    {
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
            try
            {
                File.WriteAllText(tmpFile, "testing write access");
                string contents = File.ReadAllText(tmpFile);
                canReadWrite = !string.IsNullOrEmpty(contents);
                File.Delete(tmpFile); //cleanup existing file
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
            return canReadWrite;
        }

        bool ShouldDownloadCheck(bool canReadWrite, string lastModifiedFile)
        {
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
                        var request = new WebClientRequest();
                        request.Headers = new WebHeaderCollection();
                        var client = new ParallelWebClient(request);

                        if (steps.HasFlag(UpdateStep.Download))
                        {
                            downloadTasks.Add(DownloadFile(client, allCountriesUrl, fs.allCountriesFile, c =>
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
                            }));
                            downloadTasks.Add(DownloadFile(client, countryInfoUrl, fs.countryInfoFile));
                            downloadTasks.Add(DownloadFile(client, featureCodes_enUrl, fs.featureCodes_enFile));
                            downloadTasks.Add(DownloadFile(client, timeZonesUrl, fs.timeZonesFile));
                            Task.WaitAll(downloadTasks.ToArray());
                        }
                        if (steps.HasFlag(UpdateStep.Extraction) && ConvertZipToDat(fs))
                        {
                            if(steps.HasFlag(UpdateStep.Cleanup))
                                FileCleanup(fs);
                        }
                    }
                }
            });
        }

        #region core update helpers

        Task DownloadFile(ParallelWebClient client, string url, string file, Action<ParallelWebClient> callback = null)
        {
            return client.DownloadData(url).ContinueWith(t =>
            {
                if (File.Exists(file))
                    File.Delete(file);

                File.WriteAllBytes(file, t.Result);
                if (callback != null)
                    callback(client);
            });
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
            //downloaded, now convert zip into serialized dat file
            if (File.Exists(fs.allCountriesFile))
            {
                if (File.Exists(fs.countriesRawPath))
                    File.Delete(fs.countriesRawPath);

                var fz = new FastZip();
                fz.ExtractZip(fs.allCountriesFile, Root, FastZip.Overwrite.Always, null, null, null, true);
                if (File.Exists(fs.countriesRawPath))
                {
                    GeoData gd = GeoNameParser.ParseFile(fs.countriesRawPath, fs.timeZonesFile, fs.featureCodes_enFile, fs.countryInfoFile);
                    Serialize.SerializeBinaryToDisk(gd, DataFile);
                    success = true;
                }
            }
            return success;
        }

        void FileCleanup(GeoFileSet fs)
        {
            foreach(string f in fs.AllFiles)
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
        }

        #endregion
    }
}