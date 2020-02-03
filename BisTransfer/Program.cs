﻿using Google.Apis.Auth.OAuth2;
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

namespace BisTransfer
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "BisTransfer";

        static void Main(string[] args)
        {
            const string spreadsheetId = "10IV_3NEmZdhh8iQXrs2Ek77YyUJbjBMWO5UxIWLlZl0";
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
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var phaseList = new List<Phase>()
            {
                new Phase(2, "B124:BC144"),
                new Phase(3, "B203:BC223"),
                new Phase(4, "B244:BC264"),
                new Phase(5, "B361:BC381"),
                new Phase(6, "B402:BC419"),
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

                new Character(17, CharacterEnum.Mage, SpecEnum.Frost),

                new Character(20, CharacterEnum.Paladin, SpecEnum.Holy),
                new Character(23, CharacterEnum.Paladin, SpecEnum.Retribution),
                new Character(26, CharacterEnum.Paladin, SpecEnum.Protection),

                new Character(29, CharacterEnum.Priest, SpecEnum.Holy),
                new Character(32, CharacterEnum.Priest, SpecEnum.Shadow),

                new Character(35, CharacterEnum.Rogue, SpecEnum.Sword),

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
                foreach (var phase in phaseList)
                {
                    var charRaw = phase.Raw[character.Id];
                    character.PhaseList.Add(new Phase(phase.Id, phase.Range) {
                        CharacterRaw = charRaw
                    });

                    for (int i = 0; i < charRaw.Count; i++)
                    {
                        var itemRaw = charRaw[i];
                        var item = ParseItemRow((string)itemRaw, slotList[i], phase);
                        if (item != null)
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


            Console.Read();
        }

        //=HYPERLINK("https://classic.wowhead.com/item=10504/green-lens","Green Lens of Arcane Wrath")
        private static Item ParseItemRow(string row, SlotEnum slot, Phase phase)
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                return null;
            }
            Regex regex = new Regex(@"=(\d+).*"".+""(.+)""");
            MatchCollection matches = regex.Matches(row);

            int id = 0;
            string name = string.Empty;

            foreach (Match match in matches)
            {
                int result;
                if (int.TryParse(match.Groups[1].Value, out result))
                {
                    id = result;
                }
                name = match.Groups[2].Value;
            }

            Item item = new Item(id, name, slot);
            item.PhaseList.Add(phase.Id);
            return item;
        }
    }
}