﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoDataSource
{
    public sealed class PhoneManager
    {
        static readonly PhoneManager _current = new PhoneManager();
        const string dataFile = "phones";

        private PhoneManager() { }
        static PhoneManager()
        {
            if (File.Exists(DataFile))
            {
                _current.PhoneInformation = ParseFromFile();
            }
            else
            {
                using (var rdr = new BinaryReader(typeof(PhoneManager).Assembly.GetManifestResourceStream("GeoDataSource.phones.dat")))
                {
                    var bytes = new byte[rdr.BaseStream.Length];
                    rdr.Read(bytes, 0, bytes.Length);
                    _current.PhoneInformation = ParseFromBytes(bytes);
                }
            }
        }

        public static string DataFile
        {
            get { return Path.Combine(Root, dataFile + ".dat"); }
        }

        private static string Root
        {
            get
            {
                string dll = typeof(PhoneManager).Assembly.CodeBase;
                Uri u;
                Uri.TryCreate(dll, UriKind.RelativeOrAbsolute, out u);
                FileInfo fi = new FileInfo(u.LocalPath);
                return fi.Directory.FullName;
            }
        }
        public ICollection<PhoneInformation> PhoneInformation { get; private set; }
        public static PhoneManager Current
        {
            get { return _current; }
        }

        public IEnumerable<PhoneInformation> AllByCountry(string country)
        {
            return from p in this.PhoneInformation
                   where string.Compare(p.Country, country, true)==0
                   select p;
        }

        public PhoneInformation AutoDetect(string Phone)
        {
            var phone = Regex.Replace(Phone, @"[\(\)\- ]", "");
            foreach (var p in this.PhoneInformation)
            {
                if (p.Reliable)
                {
                    if (Regex.IsMatch(phone, p.RegexPattern)) return p;
                }
            }
            return default(PhoneInformation);
        }

        private static List<PhoneInformation> ParseFromBytes(byte[] data)
        {
            var phones = new List<PhoneInformation>();
            var str =  Encoding.UTF8.GetString(data);

            string[] lines = str.Split('\n');
            string currentCountry = "";
            PhoneInformation currentPhone = new PhoneInformation();
            bool nextIsCountry = false;
            bool nextIsCode = false;
            foreach (string line in lines)
            {
                var trimmedLine = line.Trim();
                if (!trimmedLine.StartsWith("#"))
                {
                    phones.Add(currentPhone);
                    currentPhone = new PhoneInformation();
                    currentPhone.Country = currentCountry;

                    if (trimmedLine == "")
                    {
                        nextIsCode = false;
                        nextIsCountry = true;
                    }
                    else
                    {
                        if (nextIsCountry)
                        {
                            currentCountry = trimmedLine;
                            nextIsCountry = false;
                            nextIsCode = true;
                        }
                        else if (nextIsCode)
                        {
                            var commentedBits = trimmedLine.Split('#');
                            var parts = commentedBits[0].Split(',');
                            if (commentedBits.Length > 1) currentPhone.Comment = commentedBits[1];
                            int num = 0;
                            if (int.TryParse(parts[0], out num)) currentPhone.CountryCode = num;
                            if (int.TryParse(parts[1], out num)) currentPhone.MobilePrefix = num;
                            if (int.TryParse(parts[2], out num)) currentPhone.NumberOfDigitsAfterMobilePrevix = num;
                        }
                    }
                }
            }
            var good = new List<PhoneInformation>();
            foreach (var p in phones)
            {
                if ((!string.IsNullOrEmpty(p.Country)) && p.Reliable)
                    good.Add(p);
            }
            return good;
        }

        private static List<PhoneInformation> ParseFromFile()
        {
            return ParseFromBytes(File.ReadAllBytes(DataFile));
        }
    }
}