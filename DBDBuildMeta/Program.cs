using System.Net;
using System.IO.Compression;
using Newtonsoft.Json;

namespace DBDBuildMeta
{
    public struct MetaEntry
    {
        public string name;
        public string displayName;
        public string dbdName;
        public string dbdUrl;
        public string[] layoutHashes;   
    }

    class Program
    {
        private const string DBDefsRepoUrl = "https://codeload.github.com/wowdev/WoWDBDefs/zip/refs/heads/master";
        private const string WorkDir = "WorkDir";
        private const string MetaDir = "Meta";

        static void Main(string[] args)
        {
            DownloadDBDefsRepo();
            var dbDefsList = GenerateDBDMeta();

            if (Directory.Exists(WorkDir))
                Directory.Delete(WorkDir, true);

            if (Directory.Exists(MetaDir))
                Directory.Delete(MetaDir, true);

            Directory.CreateDirectory(MetaDir);


            Console.WriteLine("Write DBDBuildMeta files...");
            foreach (var dbDef in dbDefsList)
            {
                var listFilePath = Path.Combine(MetaDir, dbDef.Key + ".json");
                File.WriteAllText(listFilePath, JsonConvert.SerializeObject(dbDef.Value.Values, Formatting.Indented));
            }
        }

        static void DownloadDBDefsRepo()
        {
            if (Directory.Exists(WorkDir))
                Directory.Delete(WorkDir, true);

            Directory.CreateDirectory(WorkDir);

            string zipPath = $"{WorkDir}/WoWDBDefs.zip";

            Console.WriteLine("Download WoWDBDefs...");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(DBDefsRepoUrl, zipPath);
            }

            Console.WriteLine("Extrect WoWDBDefs...");
            ZipFile.ExtractToDirectory(zipPath, WorkDir);
        }

        static Dictionary<string, Dictionary<string, MetaEntry>> GenerateDBDMeta()
        {
            Dictionary<string, Dictionary<string, MetaEntry>> dbdBuildMeta = new();

            Console.WriteLine("Read WoWDBDefs...");
            foreach (string file in Directory.GetFiles($"{WorkDir}/WoWDBDefs-master/definitions"))
            {
                string fileName = Path.GetFileName(file);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                DBDefsLib.DBDReader dbdReader = new();
                var dbDefinition = dbdReader.Read(file);

                foreach (var versions in dbDefinition.versionDefinitions)
                {
                    foreach (var build in versions.builds)
                    {
                        var buildName = build.ToString();
                        Dictionary<string, MetaEntry> metaEntries = dbdBuildMeta.ContainsKey(buildName) ? dbdBuildMeta[buildName] : new();

                        if (metaEntries.ContainsKey(fileName))
                            continue;

                        metaEntries.Add(fileName, new MetaEntry
                        {
                            name = fileNameWithoutExtension.ToLower(),
                            displayName = fileNameWithoutExtension,
                            dbdName = fileName,
                            dbdUrl = $"https://raw.githubusercontent.com/wowdev/WoWDBDefs/master/definitions/{fileName}",
                            layoutHashes = versions.layoutHashes
                        });

                        dbdBuildMeta[buildName] = metaEntries;
                    }
                }
            }

            return dbdBuildMeta;
        }
    }
}