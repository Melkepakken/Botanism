using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Botanism.Profiles
{
    public class PlantProfileSystem : ModSystem
    {
        private readonly Dictionary<string, PlantProfile> profilesByCode =
            new Dictionary<string, PlantProfile>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PlantProfile> profilesByBlockCode =
            new Dictionary<string, PlantProfile>(StringComparer.OrdinalIgnoreCase);

        public override void AssetsLoaded(ICoreAPI api)
        {
            LoadPlantProfiles(api);
        }

        public PlantProfile GetProfileForBlock(AssetLocation blockCode)
        {
            if (blockCode == null)
            {
                return null;
            }

            profilesByBlockCode.TryGetValue(blockCode.ToString(), out PlantProfile profile);

            return profile;
        }

        public PlantProfile GetProfile(string profileCode)
        {
            if (string.IsNullOrWhiteSpace(profileCode))
            {
                return null;
            }

            profilesByCode.TryGetValue(profileCode, out PlantProfile profile);

            return profile;
        }

        private void LoadPlantProfiles(ICoreAPI api)
        {
            profilesByCode.Clear();
            profilesByBlockCode.Clear();

            Dictionary<AssetLocation, PlantProfileFile> profileFiles =
                api.Assets.GetMany<PlantProfileFile>(
                    Mod.Logger,
                    "config/plantprofiles"
                );

            foreach (KeyValuePair<AssetLocation, PlantProfileFile> entry in profileFiles)
            {
                PlantProfileFile profileFile = entry.Value;

                if (profileFile?.Profiles == null)
                {
                    continue;
                }

                foreach (PlantProfile profile in profileFile.Profiles)
                {
                    RegisterProfile(entry.Key, profile);
                }
            }

            Mod.Logger.Notification(
                "Loaded {0} Botanism plant profiles matching {1} block codes",
                profilesByCode.Count,
                profilesByBlockCode.Count
            );
        }

        private void RegisterProfile(AssetLocation sourceFile, PlantProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.Code = NormalizeCode(profile.Code);
            profile.TargetBlockCode = NormalizeCode(profile.TargetBlockCode);

            if (profile.MatchBlockCodes == null)
            {
                profile.MatchBlockCodes = Array.Empty<string>();
            }

            if (profile.ValidTools == null)
            {
                profile.ValidTools = Array.Empty<string>();
            }

            profile.PropagationType = NormalizeSimpleCode(profile.PropagationType, "seed");
            profile.PlantCategory = NormalizeSimpleCode(profile.PlantCategory, "wildflower");
            profile.PlacementType = NormalizeSimpleCode(profile.PlacementType, "surface");
            profile.Priority = NormalizeSimpleCode(profile.Priority, "v1");

            if (string.IsNullOrWhiteSpace(profile.Code))
            {
                Mod.Logger.Warning(
                    "Skipped Botanism plant profile in {0} because it has no code",
                    sourceFile
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                profile.DisplayName = profile.Code;
            }

            if (profile.Yield < 1)
            {
                profile.Yield = 1;
            }

            profilesByCode[profile.Code] = profile;

            foreach (string rawBlockCode in profile.MatchBlockCodes)
            {
                string blockCode = NormalizeCode(rawBlockCode);

                if (string.IsNullOrWhiteSpace(blockCode))
                {
                    continue;
                }

                if (profilesByBlockCode.TryGetValue(blockCode, out PlantProfile existingProfile))
                {
                    Mod.Logger.Warning(
                        "Botanism plant profile block code {0} from {1} is already matched by {2}. It will now be matched by {3}.",
                        blockCode,
                        sourceFile,
                        existingProfile.Code,
                        profile.Code
                    );
                }

                profilesByBlockCode[blockCode] = profile;
            }

            if (!string.IsNullOrWhiteSpace(profile.TargetBlockCode)
                && !profilesByBlockCode.ContainsKey(profile.TargetBlockCode))
            {
                profilesByBlockCode[profile.TargetBlockCode] = profile;
            }
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "";
            }

            string normalized = code.Trim().ToLowerInvariant();

            return normalized.Contains(":")
                ? normalized
                : "game:" + normalized;
        }

        private static string NormalizeSimpleCode(string code, string fallback)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return fallback;
            }

            return code.Trim();
        }
    }
}