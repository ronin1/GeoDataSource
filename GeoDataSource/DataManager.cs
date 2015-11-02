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
        //NOTE: framework level thread-safe lazy singleton pattern
        private DataManager() { }
        class Inner { static readonly internal DataManager SINGLETON = new DataManager(); }
        public static DataManager Instance { get { return Inner.SINGLETON; } }

        public string DataFile
        {
            get { return System.IO.Path.Combine(Root, dataFile + ".dat"); }
        }

        string Root
        {
            get
            {
                string dll = typeof (DataManager).Assembly.CodeBase;
                Uri u;
                Uri.TryCreate(dll, UriKind.RelativeOrAbsolute, out u);
                FileInfo fi = new FileInfo(u.LocalPath);
                return fi.Directory.FullName;
            }
        }

        const string LastModified = "GeoDataSource-LastModified.txt";
        const string allCountriesUrl = "http://download.geonames.org/export/dump/allCountries.zip";
        //private static string alternateNamesUrl = "http://download.geonames.org/export/dump/alternateNames.zip";
        //private static string admin1CodesUrl = "http://download.geonames.org/export/dump/admin1CodesASCII.txt";
        //private static string admin2CodesUrl = "http://download.geonames.org/export/dump/admin2Codes.txt";
        const string countryInfoUrl = "http://download.geonames.org/export/dump/countryInfo.txt";
        const string featureCodes_enUrl = "http://download.geonames.org/export/dump/featureCodes_en.txt";
        //private static string languagecodesUrl = "http://download.geonames.org/export/dump/iso-languagecodes.txt";
        const string timeZonesUrl = "http://download.geonames.org/export/dump/timeZones.txt";

        const string dataFile = "GeoDataSource";
        private string countriesRawFile = "allCountries.txt";
        private static readonly object _lock = new object();

        bool CleanupExistingFiles(string tmpFile)
        {
            //check to see if we have read/write permission to disk
            bool canReadWrite = false;
            try
            {
                System.IO.File.WriteAllText(tmpFile, "testing write access");
                string contents = System.IO.File.ReadAllText(tmpFile);
                canReadWrite = !string.IsNullOrEmpty(contents);
                System.IO.File.Delete(tmpFile);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
            return canReadWrite;
        }

        bool ShouldDownloadCheck(bool canReadWrite, string lastModifiedFile)
        {
            bool shouldDownload = !System.IO.File.Exists(lastModifiedFile);
            if (canReadWrite && System.IO.File.Exists(lastModifiedFile))
            {
                var lastModified = System.IO.File.ReadAllText(lastModifiedFile);
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

        public Task Update()
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_lock)
                {
                    string lastModifiedFile = System.IO.Path.Combine(Root, LastModified);
                    string tmpFile = System.IO.Path.Combine(Root, Guid.NewGuid().ToString());

                    countriesRawFile = System.IO.Path.Combine(Root, countriesRawFile);
                    string allCountriesFile = System.IO.Path.Combine(Root, dataFile + ".zip");
                    string countryInfoFile = System.IO.Path.Combine(Root, "countryInfo.txt");
                    string featureCodes_enFile = System.IO.Path.Combine(Root, "featureCodes_en.txt");
                    string timeZonesFile = System.IO.Path.Combine(Root, "timeZones.txt");

                    bool canReadWrite = CleanupExistingFiles(tmpFile);

                    //load file: GeoData-LastModified.txt
                    //if available then 
                    //try to http head the data source url: http://download.geonames.org/export/dump/allCountries.zip
                    //pull off last-modified header
                    //if newer than the file last-modified, do a GET to download
                    //else
                    // do a get to download

                    bool shouldDownload = ShouldDownloadCheck(canReadWrite, lastModifiedFile);
                    if (canReadWrite && shouldDownload)
                    {
                        var downloadTasks = new List<Task>();
                        var request = new WebClientRequest();
                        request.Headers = new WebHeaderCollection();
                        var client = new ParallelWebClient(request);

                        downloadTasks.Add(client.DownloadData(allCountriesUrl).ContinueWith(t =>
                        {
                            if (File.Exists(allCountriesFile))
                                File.Delete(allCountriesFile);

                            File.WriteAllBytes(allCountriesFile, t.Result);
                            var headers = client.ResponseHeaders;
                            foreach (string h in headers.Keys)
                            {
                                if (h.ToLowerInvariant() == "last-modified")
                                {
                                    var header = headers[h];
                                    DateTime headerDate = DateTime.Now;
                                    DateTime.TryParse(header.ToString(), out headerDate);
                                    System.IO.File.WriteAllText(lastModifiedFile, headerDate.ToString());
                                    break;
                                }
                            }
                        }));

                        downloadTasks.Add(DownloadFile(client, countryInfoUrl, countryInfoFile));
                        downloadTasks.Add(DownloadFile(client, featureCodes_enUrl, featureCodes_enFile));
                        downloadTasks.Add(DownloadFile(client, timeZonesUrl, timeZonesFile));

                        Task.WaitAll(downloadTasks.ToArray());
                        ConvertZipToDat(allCountriesFile, timeZonesFile, featureCodes_enFile, countryInfoFile);
                    }
                }
            });
        }

        Task DownloadFile(ParallelWebClient client, string url, string file)
        {
            return client.DownloadData(url).ContinueWith(t =>
            {
                if (System.IO.File.Exists(file))
                    System.IO.File.Delete(file);

                System.IO.File.WriteAllBytes(file, t.Result);
            });
        }

        void ConvertZipToDat(string allCountriesFile, string timeZonesFile, string featureCodes_enFile, string countryInfoFile)
        {
            //downloaded, now convert zip into serialized dat file
            if (System.IO.File.Exists(allCountriesFile))
            {
                if (System.IO.File.Exists(countriesRawFile)) System.IO.File.Delete(countriesRawFile);
                ICSharpCode.SharpZipLib.Zip.FastZip fz = new FastZip();
                fz.ExtractZip(allCountriesFile, Root, FastZip.Overwrite.Always, null, null, null, true);
                if (System.IO.File.Exists(countriesRawFile))
                {
                    GeoData gd = GeoNameParser.ParseFile(countriesRawFile, timeZonesFile,
                                                            featureCodes_enFile, countryInfoFile);

                    Serialize.SerializeBinaryToDisk(gd, DataFile);
                }
                if (System.IO.File.Exists(allCountriesFile)) System.IO.File.Delete(allCountriesFile);
                if (System.IO.File.Exists(countriesRawFile)) System.IO.File.Delete(countriesRawFile);
                if (System.IO.File.Exists(countryInfoFile)) System.IO.File.Delete(countryInfoFile);
                if (System.IO.File.Exists(featureCodes_enFile)) System.IO.File.Delete(featureCodes_enFile);
                if (System.IO.File.Exists(timeZonesFile)) System.IO.File.Delete(timeZonesFile);
            }
        }

    }
}