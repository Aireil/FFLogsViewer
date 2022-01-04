using System.Collections.Generic;

namespace FFLogsViewer
{
    internal class CharacterData
    {
        internal enum BossesId
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
            Erichthonios = 78,
            Hippokampos = 79,
            Phoinix = 80,
            Hesperos = 81,
            HesperosII = 82,
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
            Zodiark = 1058,
            Hydaelyn = 1059,
        }

        internal enum DataType
        {
            Best,
            Median,
            Kills,
            Job,
        }

        internal Dictionary<int, int> Bests = new();
        internal Dictionary<int, string> Jobs = new();
        internal Dictionary<int, int> Kills = new();
        internal Dictionary<int, int> Medians = new();

        internal CharacterData()
        {
        }

        internal CharacterData(string firstName, string lastName, string worldName)
        {
            this.FirstName = firstName;
            this.LastName = lastName;
            this.WorldName = worldName;
        }

        internal string FirstName { get; set; } = "";
        internal string LastName { get; set; } = "";
        internal string WorldName { get; set; } = "";

        internal string LoadedFirstName { get; set; } = "";
        internal string LoadedLastName { get; set; } = "";
        internal string LoadedWorldName { get; set; } = "";

        internal string RegionName { get; set; } = "";

        internal bool IsEveryLogsReady { get; set; }
        internal bool IsDataLoading { get; set; }

        internal bool IsCharacterReady()
        {
            return this.FirstName != ""
                   && this.LastName != ""
                   && this.WorldName != "";
        }

        internal void ResetLogs()
        {
            this.IsEveryLogsReady = false;
            this.Bests = new Dictionary<int, int>();
            this.Medians = new Dictionary<int, int>();
            this.Kills = new Dictionary<int, int>();
            this.Jobs = new Dictionary<int, string>();
        }
    }
}
