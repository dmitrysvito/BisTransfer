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

            //Console.WriteLine(JsonConvert.SerializeObject(response.ValueRanges));

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
                    character.PhaseList.Add(new Phase(phase.Id, phase.Range) {
                        CharacterRaw = phase.Raw[character.Id]
                    });
                }
            }


            Console.Read();
        }

        //=HYPERLINK("https://classic.wowhead.com/item=10504/green-lens","Green Lens of Arcane Wrath")
        private Item ParseItemRow(string row)
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                return null;
            }

            Regex regex = new Regex(@"=(\d+).+\\""([a-zA-Z0-9' ]*)\\""");
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

            Item item = new Item(id, name, SlotEnum.None, null);
            return item;
        }
    }
}