﻿using Amib.Threading;
using Newtonsoft.Json;
using Quartz;
using RuiJi.Net.Core.Expression;
using RuiJi.Net.Core.Extractor;
using RuiJi.Net.Core.Extractor.Selector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuiJi.Net.Core.RTS
{
    /// <summary>
    /// feed extract job
    /// </summary>
    public class FeedExtractJob : FeedExtractJobBase<string>
    {
        /// <summary>
        /// snapshot path
        /// </summary>
        private static readonly string snapshotPath;

        /// <summary>
        /// base path
        /// </summary>
        private static readonly string basePath;

        static FeedExtractJob()
        {
            snapshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshot");

            basePath = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(basePath + @"/history"))
            {
                Directory.CreateDirectory(basePath + @"/history");
            }
            if (!Directory.Exists(basePath + @"/pre"))
            {
                Directory.CreateDirectory(basePath + @"/pre");
            }
        }

        /// <summary>
        /// execute on job start
        /// </summary>
        /// <param name="context">job context</param>
        protected override void OnJobStart(IJobExecutionContext context)
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"delay");
            foreach (var file in files)
            {
                var filename = file.Substring(file.LastIndexOf(@"\") + 1);
                var sp = filename.Split('_');
                var ticks = sp[1].Substring(0, sp[1].LastIndexOf("."));

                if (long.Parse(ticks) < DateTime.Now.Ticks)
                {
                    var desFile = file.Replace("delay", "snapshot");
                    File.Move(file, desFile);
                }
            }
        }

        /// <summary>
        /// get snapshot files
        /// </summary>
        /// <returns></returns>
        protected override List<string> GetSnapshot()
        {
            return Directory.GetFiles(snapshotPath).ToList();
        }

        /// <summary>
        /// read file and check history
        /// </summary>
        /// <param name="path">snapshot file path</param>
        public override void DoTask(string path)
        {
            try
            {
                var filename = path.Substring(path.LastIndexOf(@"\") + 1);
                var sp = filename.Split('_');
                var id = Convert.ToInt32(sp[0]);
                var index = Convert.ToInt32(sp[1]);

                var content = File.ReadAllText(path);

                var snapshot = JsonConvert.DeserializeObject<Snapshot>(content);
                var urls = ExtractFeedAddress(snapshot);

                var hisFile = AppDomain.CurrentDomain.BaseDirectory + @"history\" + id + ".txt";
                var urlsHistory = new string[0];
                if (File.Exists(hisFile))
                {
                    urlsHistory = File.ReadAllLines(hisFile, Encoding.UTF8);
                }

                File.WriteAllLines(hisFile, urls, Encoding.UTF8);

                urls.RemoveAll(m => urlsHistory.Contains(m));
                urls.RemoveAll(m => string.IsNullOrEmpty(m));
                urls.RemoveAll(m => !Uri.IsWellFormedUriString(m, UriKind.Absolute));

                foreach (var u in urls)
                {
                    Save(u);
                }

                var destFile = path.Replace("snapshot", "pre").Replace(filename, id + ".txt");
                if (File.Exists(destFile))
                    File.Delete(destFile);

                File.Move(path, destFile);
            }
            catch { }
        }

        /// <summary>
        /// extract snapshot url
        /// </summary>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        protected List<string> ExtractFeedAddress(Snapshot snapshot)
        {
            var block = new ExtractBlock();
            block.TileSelector.Selectors.Add(new CssSelector("a", "href"));

            if (!string.IsNullOrEmpty(snapshot.Expression))
            {
                block.TileSelector.Selectors.Clear();

                var parser = new RuiJiParser();

                var s = RuiJiBlockParser.ParserBase(snapshot.Expression).Selectors;
                block.TileSelector.Selectors.AddRange(s);
            }

            var result = RuiJiExtractor.Extract(snapshot.Content, block);
            var results = new List<string>();

            if (result.Tiles != null)
            {
                foreach (var item in result.Tiles)
                {
                    var href = item.Content.ToString();
                    if (href.Contains("#"))
                    {
                        href = href.Substring(0, href.IndexOf('#'));
                    }
                    if (Uri.IsWellFormedUriString(href, UriKind.Relative))
                        href = new Uri(new Uri(snapshot.RequestUrl), href).AbsoluteUri.ToString();
                    results.Add(href);
                }
            }

            return results.Distinct().ToList();
        }

        /// <summary>
        /// save new url as you want
        /// </summary>
        /// <param name="url"></param>
        protected void Save(string url)
        {
            
        }
    }
}