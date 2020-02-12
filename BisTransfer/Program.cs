using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Data = Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using RestSharp;
using System.Threading.Tasks;
using System.Net;
using IronOcr;
using NLog;

namespace BisTransfer
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "BisTransfer";
        const string spreadsheetId = "10IV_3NEmZdhh8iQXrs2Ek77YyUJbjBMWO5UxIWLlZl0";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            //var url = "https://i.imgur.com/EGOLHFU.png";
            //var Op = GetTextFromImage(url);
            //var url1 = "https://i.imgur.com/pPNpJRO.png";
            //var Op1 = GetTextFromImage(url1);
            //var url2 = "https://i.imgur.com/5aYGkhU.png";
            //var Op2 = GetTextFromImage(url2);

            //var res = GetWowHeadItemInfo(new List<string>() { "Naglering", "Thmll's Resolve", "Cape of the Black Baron" });
            //return;
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Logger.Info("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var phaseList = new List<Phase>()
            {
                new Phase(1, "B83:BC103"),
                new Phase(2, "B124:BC144"),
                new Phase(3, "B203:BC223"),
                new Phase(4, "B244:BC264"),
                new Phase(5, "B361:BC381"),
                new Phase(6, "B402:BC419")
            };

            var rangeList = phaseList.Select(x => x.GetPhaseString());
            var ranges = new Google.Apis.Util.Repeatable<string>(rangeList);

            SpreadsheetsResource.ValuesResource.BatchGetRequest request = new SpreadsheetsResource.ValuesResource.BatchGetRequest(service, spreadsheetId)
            {
                Ranges = ranges,
                ValueRenderOption = SpreadsheetsResource.ValuesResource.BatchGetRequest.ValueRenderOptionEnum.FORMULA,
                MajorDimension = SpreadsheetsResource.ValuesResource.BatchGetRequest.MajorDimensionEnum.COLUMNS,
                DateTimeRenderOption = SpreadsheetsResource.ValuesResource.BatchGetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING
            };

            Data.BatchGetValuesResponse response = request.Execute();

            var characterList = new List<Character>()
            {
                new Character(2, CharacterEnum.Druid, SpecEnum.Balance),
                new Character(5, CharacterEnum.Druid, SpecEnum.Bear),
                new Character(8, CharacterEnum.Druid, SpecEnum.Cat),
                new Character(11, CharacterEnum.Druid, SpecEnum.Restoration),

                new Character(14, CharacterEnum.Hunter),

                new Character(17, CharacterEnum.Mage),

                new Character(20, CharacterEnum.Paladin, SpecEnum.Holy),
                new Character(23, CharacterEnum.Paladin, SpecEnum.Retribution),
                new Character(26, CharacterEnum.Paladin, SpecEnum.Protection),

                new Character(29, CharacterEnum.Priest, SpecEnum.Holy),
                new Character(32, CharacterEnum.Priest, SpecEnum.Shadow),

                new Character(35, CharacterEnum.Rogue),

                new Character(38, CharacterEnum.Shaman, SpecEnum.Elemental),
                new Character(41, CharacterEnum.Shaman, SpecEnum.Enchancement),
                new Character(44, CharacterEnum.Shaman, SpecEnum.Restoration),

                new Character(47, CharacterEnum.Warlock),

                new Character(50, CharacterEnum.Warrior, SpecEnum.Fury),
                new Character(53, CharacterEnum.Warrior, SpecEnum.Protection)
            };

            var slotList = new SlotEnum[21];
            slotList[0] = SlotEnum.Head;
            slotList[1] = SlotEnum.Neck;
            slotList[2] = SlotEnum.Shoulders;
            slotList[3] = SlotEnum.Back;
            slotList[4] = SlotEnum.Chest;
            slotList[5] = SlotEnum.Bracers;
            slotList[6] = SlotEnum.Gloves;
            slotList[7] = SlotEnum.Belt;
            slotList[8] = SlotEnum.Legs;
            slotList[9] = SlotEnum.Feet;
            slotList[10] = SlotEnum.Ring;
            slotList[11] = SlotEnum.Ring;
            slotList[12] = SlotEnum.Trinket;
            slotList[13] = SlotEnum.Trinket;
            slotList[14] = SlotEnum.MainHand;
            slotList[15] = SlotEnum.OffHand;
            slotList[16] = SlotEnum.None;
            slotList[17] = SlotEnum.None;
            slotList[18] = SlotEnum.None;
            slotList[19] = SlotEnum.TwoHand;
            slotList[20] = SlotEnum.Ranged;

            foreach (var range in response.ValueRanges)
            {
                foreach (var phase in phaseList)
                {
                    if (range.Range.Contains(phase.Range))
                    {
                        phase.Raw = range.Values;
                    }
                }
            }

            foreach (var character in characterList)
            {
                Logger.Info($"Started processing {character.Title} {character.Spec}");
                foreach (var phase in phaseList)
                {
                    Logger.Info($"Phase {phase.Id}");
                    var charRaw = phase.Raw[character.Id];
                    character.PhaseList.Add(new Phase(phase.Id, phase.Range)
                    {
                        CharacterRaw = charRaw
                    });

                    for (int i = 0; i < charRaw.Count; i++)
                    {
                        var itemRaw = charRaw[i];
                        IList<Item> items = ParseItemRow((string)itemRaw, slotList[i], phase);
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                var existingItem = character.Items.FirstOrDefault(x => x.Id == item.Id);
                                if (existingItem == null)
                                {
                                    character.Items.Add(item);
                                }
                                else
                                {
                                    if (!existingItem.PhaseList.Contains(phase.Id))
                                    {
                                        existingItem.PhaseList.Add(phase.Id);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Logger.Info("Document parsed. Creating files.");

            string writePath = @"{0}{1}.lua";
            string writePathShort = @"{0}.lua";
            string bisPattern = @"<Script file=""{0}{1}.lua"" />";
            string bisPatternShort = @"<Script file=""{0}.lua"" />";
            string bis = @"BIS.xml";
            //char, spec
            var initialStringPattern = @"local bis = ExoLink:RegisterBIS(""{0}"", ""{1}"")";
            var initialStringPatternShort = @"local bis = ExoLink:RegisterBIS(""{0}"")";
            //id, slot, name, phases
            var regularStringPattern = @"ExoLink: BISitem(bis, ""{0}"", ""{1}"", ""{2}"", ""{3}"")";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<Ui xmlns=""http://www.blizzard.com/wow/ui/"">");
            foreach (var character in characterList)
            {
                Logger.Info($"Creating file for {character.Title} {character.Spec}");
                string filePath;
                if (character.Spec != SpecEnum.None)
                {
                    filePath = string.Format(writePath, character.Title, character.Spec);
                    sb.AppendLine(string.Format(bisPattern, character.Title, character.Spec));
                }
                else
                {
                    filePath = string.Format(writePathShort, character.Title);
                    sb.AppendLine(string.Format(bisPatternShort, character.Title));
                }

                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Default))
                {
                    if (character.Spec != SpecEnum.None)
                    {
                        sw.WriteLine(initialStringPattern, character.Title, character.Spec);
                    }
                    else
                    {
                        sw.WriteLine(initialStringPatternShort, character.Title);
                    }

                    foreach (var item in character.Items)
                    {
                        sw.WriteLine(regularStringPattern, item.Id, item.Slot, item.Name, item.GetPhaseString());
                    }
                }
            }
            sb.AppendLine(@"</Ui>");

            using (StreamWriter sw = new StreamWriter(bis, false, Encoding.Default))
            {
                sw.Write(sb.ToString());
            }

            Logger.Info("Work Complete!");
            Console.Read();
        }

        /// <summary>
        /// Get Access token to query Blizzard API
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <returns>access token object</returns>
        internal static string GetAccessToken(string clientId, string clientSecret)
        {
            var client = new RestClient("https://eu.battle.net/oauth/token");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type=client_credentials&client_id={clientId}&client_secret={clientSecret}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            var tokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(response.Content);

            return tokenResponse.access_token;
        }

        internal static string GetTextFromImage(string url)
        {
            const string imageDir = @"images";
            string filename = string.Empty;
            string filePath = string.Empty;

            Directory.CreateDirectory(imageDir);

            var imgUri = new Uri(url);

            filename = Path.GetFileName(imgUri.LocalPath);
            filePath = $@"{imageDir}\{filename}";

            var Ocr = new AdvancedOcr()
            {
                CleanBackgroundNoise = false,
                EnhanceContrast = false,
                EnhanceResolution = true,
                Language = IronOcr.Languages.English.OcrLanguagePack,
                Strategy = AdvancedOcr.OcrStrategy.Advanced,
                ColorSpace = AdvancedOcr.OcrColorSpace.Color,
                DetectWhiteTextOnDarkBackgrounds = false,
                InputImageType = AdvancedOcr.InputTypes.Snippet,
                RotateAndStraighten = false,
                ReadBarCodes = false,
                ColorDepth = 0
            };

            if (!File.Exists(filePath))
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(imgUri, filePath);
                }
            }

            var result = string.Empty;

            using (BisTransferDbEntities db = new BisTransferDbEntities())
            {
                try
                {
                    var scan = db.Scans.SingleOrDefault(s => s.Path == filename);
                    if (scan == null)
                    {
                        result = Ocr.Read(filePath).Text;
                        var newScan = new Scan()
                        {
                            Path = filename,
                            Text = result
                        };
                        db.Scans.Add(newScan);
                        db.SaveChanges();
                        Logger.Info($"File dowloaded and scanned {filename}. Result text: {result}");
                    }
                    else
                    {
                        result = scan.Text;
                        Logger.Info($"File scan info retrived from DB {filename}. Result text: {result}");
                    }                    
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error during file scan");
                }                
            }

            return result;
        }

        internal static IList<Item> GetWowHeadItemInfo(IList<string> names, SlotEnum slot = SlotEnum.None, Phase phase = null)
        {
            var result = new List<Item>();

            if (!names.Any())
            {
                return result;
            }

            var searchUrl = @"https://classic.wowhead.com/search/suggestions-template?q=";

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
            Random rnd = new Random();
            using (BisTransferDbEntities db = new BisTransferDbEntities())
            {
                using (WebClient client = new WebClient())
                {
                    foreach (string name in names)
                    {
                        var item = db.Items.SingleOrDefault(i => i.Name.Contains(name));
                        if (item != null)
                        {
                            item.PhaseList.Add(phase.Id);
                            result.Add(item);
                            Logger.Info($"Retrived info from database for {name}. Id is {item.Id}");
                        }
                        else
                        {
                            Task<string> task = Task.Factory.StartNew(() => client.DownloadString($"{searchUrl}{name}"));
                            var seed = rnd.Next(10, 600) * 1000;
                            task.Wait(seed);
                            if (task.IsCompleted)
                            {
                                var data = task.Result;
                                var response = JsonConvert.DeserializeObject<WowHeadSearchResponse>(data);
                                if (response != null && response.results.Any())
                                {
                                    //Type == 3 => item
                                    //Quality 2 - green 3 - blue 4 - epic 5 - legendary
                                    var first = response.results.FirstOrDefault(r => r.Type == 3 && (r.Quality == 2 || r.Quality == 3 || r.Quality == 4 || r.Quality == 5));
                                    if (first != null)
                                    {
                                        var itemToAdd = new Item(first.Id, first.Name, slot)
                                        {
                                            Quality = first.Quality,
                                            Icon = first.Icon,
                                            Type = first.Type,
                                            TypeName = first.TypeName
                                        };

                                        var itemExists = db.Items.SingleOrDefault(i => i.Id == itemToAdd.Id);

                                        if (itemExists == null)
                                        {
                                            db.Items.Add(itemToAdd);
                                            itemToAdd.PhaseList.Add(phase.Id);
                                            
                                        }
                                        result.Add(itemToAdd);
                                        Logger.Info($"Retrived info from WowHead for {name}. Id is {itemToAdd.Id}");
                                    }
                                }
                            }
                        }                        
                    }
                }
                db.SaveChanges();
            }

            return result;
        }

        internal static IList<Item> ParseItemRow(string row, SlotEnum slot, Phase phase)
        {
            IList<Item> result = new List<Item>();

            if (string.IsNullOrWhiteSpace(row))
            {
                return result;
            }

            Regex idAndName = new Regex(@"=(\d+).*"".+""(.+)""");
            MatchCollection idAndNameMatches = idAndName.Matches(row);

            foreach (Match idAndNameMatch in idAndNameMatches)
            {
                int id = 0;
                string name = string.Empty;

                int idValue;
                if (int.TryParse(idAndNameMatch.Groups[1].Value, out idValue))
                {
                    id = idValue;
                }
                if (!string.IsNullOrWhiteSpace(idAndNameMatch.Groups[2].Value) && idAndNameMatch.Groups[2].Value.Contains("http"))
                {
                    var itemNameFromImage = GetTextFromImage(idAndNameMatch.Groups[2].Value);
                    
                    Regex alts = new Regex(@"(\b[A-Za-z ',-]{2,}\b)");
                    Regex correction = new Regex(@"( t )|( I )");

                    var corrected = correction.Replace(itemNameFromImage, " / ");

                    MatchCollection altsMatches = alts.Matches(corrected);
                    if (altsMatches.Count > 0)
                    {
                        var nameList = new List<string>();
                        foreach (Match altsMatch in altsMatches)
                        {
                            var val = altsMatch.Groups[1].Value;
                            nameList.Add(val);
                        }
                        nameList = nameList.Distinct().ToList();

                        var newItem = new Item(id, nameList.First(), slot);
                        newItem.PhaseList.Add(phase.Id);
                        result.Add(newItem);
                        Logger.Info($"Item {newItem.Name} added for phase {phase.Id}");

                        nameList.Remove(nameList.First());                       

                        var wowHeadItems = GetWowHeadItemInfo(nameList, slot, phase);
                        foreach (var wowHeadItem in wowHeadItems)
                        {
                            result.Add(wowHeadItem);
                            Logger.Info($"Item {wowHeadItem.Name} added for phase {phase.Id}");
                        }
                    }
                }
                else
                {
                    name = idAndNameMatch.Groups[2].Value;
                }

                if (id > 0 && !string.IsNullOrWhiteSpace(name))
                {
                    Item item = new Item(id, name, slot);
                    item.PhaseList.Add(phase.Id);
                    result.Add(item);
                    Logger.Info($"Item {item.Name} added for phase {phase.Id}");
                }
            }

            return result;
        }
    }
}