using System;

namespace Botanism.Profiles
{
    public class PlantProfileFile
    {
        public PlantProfile[] Profiles { get; set; } = Array.Empty<PlantProfile>();
    }

    public class PlantProfile
    {
        public string Code { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PlantCategory { get; set; } = "wildFlower";
        public string PropagationType { get; set; } = "seed";
        public string PlacementType { get; set; } = "surface";
        public string Priority { get; set; } = "v1";
        public string TargetBlockCode { get; set; } = "";
        public string[] MatchBlockCodes { get; set; } = Array.Empty<string>();
        public int Yield { get; set; } = 2;
        public string[] ValidTools { get; set; } = Array.Empty<string>();

        public bool AllowsTool(string toolCode)
        {
            if (string.IsNullOrWhiteSpace(toolCode))
            {
                return false;
            }

            if (ValidTools == null || ValidTools.Length == 0)
            {
                return toolCode.Equals("knife", StringComparison.OrdinalIgnoreCase);
            }

            foreach (string validTool in ValidTools)
            {
                if (toolCode.Equals(validTool, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}