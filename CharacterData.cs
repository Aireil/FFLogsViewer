using System.Collections.Generic;

namespace FFLogsViewer
{
    public class CharacterData
    {
        public enum BossesId
        {
            Ramuh = 69,
            IfritAndGaruda = 70,
            TheIdolOfDarkness = 71,
            Shiva = 72,
            CloudOfDarkness = 73,
            Shadowkeeper = 74,
            Fatebreaker = 75,
            EdensPromise = 76,
            OracleOfDarkness = 77,
            TheRubyWeaponI = 1051,
            TheRubyWeaponIi = 1052,
            VarisYaeGalvus = 1053,
            WarriorOfLight = 1054,
            TheEmeraldWeaponI = 1055,
            TheEmeraldWeaponIi = 1056,
            TheDiamondWeapon = 1057,
            ShivaUnreal = 3001,
            TitanUnreal = 3002,
            LeviathanUnreal = 3003,
            Tea = 1050,
            UCoB = 1047,
            UwU = 1048,
        }

        public enum DataType
        {
            Best,
            Median,
            Kills,
            Job,
        }

        public Dictionary<int, int> Bests = new();
        public Dictionary<int, string> Jobs = new();
        public Dictionary<int, int> Kills = new();
        public Dictionary<int, int> Medians = new();

        public CharacterData()
        {
        }

        public CharacterData(string firstName, string lastName, string worldName)
        {
            FirstName = firstName;
            LastName = lastName;
            WorldName = worldName;
        }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string WorldName { get; set; } = "";

        public string LoadedFirstName { get; set; } = "";
        public string LoadedLastName { get; set; } = "";
        public string LoadedWorldName { get; set; } = "";

        public string RegionName { get; set; } = "";

        public bool IsEveryLogsReady { get; set; }
        public bool IsDataLoading { get; set; }

        public bool IsCharacterReady()
        {
            return FirstName != ""
                   && LastName != ""
                   && WorldName != "";
        }

        internal void ResetLogs()
        {
            IsEveryLogsReady = false;
            Bests = new Dictionary<int, int>();
            Medians = new Dictionary<int, int>();
            Kills = new Dictionary<int, int>();
            Jobs = new Dictionary<int, string>();
        }
    }
}