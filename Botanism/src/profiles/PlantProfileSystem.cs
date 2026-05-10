using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Botanism.Profiles
{
    public class PlantProfileSystem : ModSystem
    {
        private const float DefaultExtractionSeconds = 3f;

        private readonly Dictionary<string, PlantProfile> profilesByCode =
            new Dictionary<string, PlantProfile>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PlantProfile> profilesByBlockCode =
            new Dictionary<string, PlantProfile>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PlantProfile> generatedProfilesByBlockCode =
            new Dictionary<string, PlantProfile>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> disabledBlockCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<PlantGroupProfile> plantGroups =
            new List<PlantGroupProfile>();

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

            string normalizedBlockCode = NormalizeCode(blockCode.ToString());

            if (disabledBlockCodes.Contains(normalizedBlockCode))
            {
                return null;
            }

            if (profilesByBlockCode.TryGetValue(normalizedBlockCode, out PlantProfile exactProfile))
            {
                return exactProfile;
            }

            if (generatedProfilesByBlockCode.TryGetValue(normalizedBlockCode, out PlantProfile generatedProfile))
            {
                return generatedProfile;
            }

            PlantProfile groupProfile = CreateProfileFromMatchingGroup(blockCode);

            if (groupProfile != null)
            {
                generatedProfilesByBlockCode[normalizedBlockCode] = groupProfile;
                profilesByCode[groupProfile.Code] = groupProfile;

                return groupProfile;
            }

            return null;
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
            generatedProfilesByBlockCode.Clear();
            disabledBlockCodes.Clear();
            plantGroups.Clear();

            Dictionary<AssetLocation, PlantProfileFile> profileFiles =
                api.Assets.GetMany<PlantProfileFile>(
                    Mod.Logger,
                    "config/plantprofiles"
                );

            foreach (KeyValuePair<AssetLocation, PlantProfileFile> entry in profileFiles)
            {
                PlantProfileFile profileFile = entry.Value;

                if (profileFile == null)
                {
                    continue;
                }

                if (profileFile.Profiles != null)
                {
                    foreach (PlantProfile profile in profileFile.Profiles)
                    {
                        RegisterProfile(entry.Key, profile);
                    }
                }

                if (profileFile.PlantGroups != null)
                {
                    foreach (PlantGroupProfile plantGroup in profileFile.PlantGroups)
                    {
                        RegisterPlantGroup(entry.Key, plantGroup);
                    }
                }
            }

            // TODO: Add a command or config reload path for plant profiles later.
            Mod.Logger.Notification(
                "Loaded {0} Botanism plant profiles, {1} plant groups, and {2} exact block matches",
                profilesByCode.Count,
                plantGroups.Count,
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
            profile.PlantCategory = NormalizeSimpleCode(profile.PlantCategory, "wildFlower");
            profile.PlacementType = NormalizeSimpleCode(profile.PlacementType, "surface");

            if (string.IsNullOrWhiteSpace(profile.Code))
            {
                Mod.Logger.Warning(
                    "Skipped Botanism plant profile in {0} because it has no code",
                    sourceFile
                );

                return;
            }

            if (!profile.Enabled)
            {
                RegisterDisabledProfile(profile);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                profile.DisplayName = CreateDisplayNameFromCode(profile.Code, "", "");
            }

            if (profile.Yield < 1)
            {
                profile.Yield = 1;
            }

            if (profile.ExtractionSeconds <= 0)
            {
                profile.ExtractionSeconds = DefaultExtractionSeconds;
            }

            profilesByCode[profile.Code] = profile;

            foreach (string rawBlockCode in profile.MatchBlockCodes)
            {
                RegisterExactBlockMatch(sourceFile, profile, rawBlockCode);
            }

            if (!string.IsNullOrWhiteSpace(profile.TargetBlockCode)
                && !profilesByBlockCode.ContainsKey(profile.TargetBlockCode))
            {
                profilesByBlockCode[profile.TargetBlockCode] = profile;
            }
        }

        private void RegisterDisabledProfile(PlantProfile profile)
        {
            disabledBlockCodes.Add(profile.Code);

            if (!string.IsNullOrWhiteSpace(profile.TargetBlockCode))
            {
                disabledBlockCodes.Add(profile.TargetBlockCode);
            }

            foreach (string rawBlockCode in profile.MatchBlockCodes)
            {
                string blockCode = NormalizeCode(rawBlockCode);

                if (!string.IsNullOrWhiteSpace(blockCode))
                {
                    disabledBlockCodes.Add(blockCode);
                }
            }
        }

        private void RegisterExactBlockMatch(
            AssetLocation sourceFile,
            PlantProfile profile,
            string rawBlockCode
        )
        {
            string blockCode = NormalizeCode(rawBlockCode);

            if (string.IsNullOrWhiteSpace(blockCode))
            {
                return;
            }

            if (disabledBlockCodes.Contains(blockCode))
            {
                return;
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

        private void RegisterPlantGroup(AssetLocation sourceFile, PlantGroupProfile plantGroup)
        {
            if (plantGroup == null || !plantGroup.Enabled)
            {
                return;
            }

            plantGroup.Code = NormalizeCode(plantGroup.Code);
            plantGroup.TargetBlockCode = NormalizeCode(plantGroup.TargetBlockCode);
            plantGroup.PlantCategory = NormalizeSimpleCode(plantGroup.PlantCategory, "wildFlower");
            plantGroup.PropagationType = NormalizeSimpleCode(plantGroup.PropagationType, "seed");
            plantGroup.PlacementType = NormalizeSimpleCode(plantGroup.PlacementType, "surface");

            if (plantGroup.Match == null)
            {
                plantGroup.Match = new PlantGroupMatch();
            }

            if (plantGroup.ValidTools == null)
            {
                plantGroup.ValidTools = Array.Empty<string>();
            }

            if (plantGroup.ExcludeBlockCodes == null)
            {
                plantGroup.ExcludeBlockCodes = Array.Empty<string>();
            }

            if (string.IsNullOrWhiteSpace(plantGroup.Code))
            {
                Mod.Logger.Warning(
                    "Skipped Botanism plant group in {0} because it has no code",
                    sourceFile
                );

                return;
            }

            if (plantGroup.Yield < 1)
            {
                plantGroup.Yield = 1;
            }

            if (plantGroup.ExtractionSeconds <= 0)
            {
                plantGroup.ExtractionSeconds = DefaultExtractionSeconds;
            }

            plantGroups.Add(plantGroup);
        }

        private PlantProfile CreateProfileFromMatchingGroup(AssetLocation blockCode)
        {
            foreach (PlantGroupProfile plantGroup in plantGroups)
            {
                if (!MatchesPlantGroup(blockCode, plantGroup))
                {
                    continue;
                }

                if (IsExcludedByGroup(blockCode, plantGroup))
                {
                    continue;
                }

                string normalizedBlockCode = NormalizeCode(blockCode.ToString());

                string displayName = string.Equals(
                    plantGroup.DisplayNameMode,
                    "fromBlockCode",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? CreateDisplayNameFromCode(
                        normalizedBlockCode,
                        plantGroup.RemovePathPrefix,
                        plantGroup.RemovePathSuffix
                    )
                    : normalizedBlockCode;

                return new PlantProfile
                {
                    Code = normalizedBlockCode,
                    DisplayName = displayName,
                    PlantCategory = plantGroup.PlantCategory,
                    PropagationType = plantGroup.PropagationType,
                    PlacementType = plantGroup.PlacementType,
                    TargetBlockCode = string.IsNullOrWhiteSpace(plantGroup.TargetBlockCode)
                        ? normalizedBlockCode
                        : plantGroup.TargetBlockCode,
                    MatchBlockCodes = new[] { normalizedBlockCode },
                    Yield = plantGroup.Yield,
                    ExtractionSeconds = plantGroup.ExtractionSeconds,
                    ValidTools = plantGroup.ValidTools,
                    Enabled = true
                };
            }

            return null;
        }

        private static bool MatchesPlantGroup(AssetLocation blockCode, PlantGroupProfile plantGroup)
        {
            PlantGroupMatch match = plantGroup.Match;

            if (!string.IsNullOrWhiteSpace(match.Domain)
                && !blockCode.Domain.Equals(match.Domain, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string path = blockCode.Path ?? "";

            if (!string.IsNullOrWhiteSpace(match.PathStartsWith)
                && !path.StartsWith(match.PathStartsWith, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(match.PathEndsWith)
                && !path.EndsWith(match.PathEndsWith, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(match.PathContains)
                && path.IndexOf(match.PathContains, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return true;
        }

        private static bool IsExcludedByGroup(AssetLocation blockCode, PlantGroupProfile plantGroup)
        {
            string normalizedBlockCode = NormalizeCode(blockCode.ToString());

            foreach (string rawExcludedBlockCode in plantGroup.ExcludeBlockCodes)
            {
                string excludedBlockCode = NormalizeCode(rawExcludedBlockCode);

                if (normalizedBlockCode.Equals(excludedBlockCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string CreateDisplayNameFromCode(
            string code,
            string removePathPrefix,
            string removePathSuffix
        )
        {
            string translatedName = GetTranslatedBlockName(code);

            if (!string.IsNullOrWhiteSpace(translatedName))
            {
                return translatedName;
            }

            string path = code;

            int domainSeparatorIndex = path.IndexOf(':');

            if (domainSeparatorIndex >= 0 && domainSeparatorIndex < path.Length - 1)
            {
                path = path.Substring(domainSeparatorIndex + 1);
            }

            if (!string.IsNullOrWhiteSpace(removePathPrefix)
                && path.StartsWith(removePathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(removePathPrefix.Length);
            }

            if (!string.IsNullOrWhiteSpace(removePathSuffix)
                && path.EndsWith(removePathSuffix, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - removePathSuffix.Length);
            }

            path = path.Replace("-", " ").Trim();

            if (string.IsNullOrWhiteSpace(path))
            {
                return code;
            }

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(path);
        }

        private static string GetTranslatedBlockName(string code)
        {
            AssetLocation blockCode = new AssetLocation(code);

            string[] langKeys =
            {
                "block-" + blockCode.Domain + "-" + blockCode.Path,
                "block-" + blockCode.Path
            };

            foreach (string langKey in langKeys)
            {
                string translatedName = Lang.GetMatchingIfExists(langKey);

                if (!string.IsNullOrWhiteSpace(translatedName)
                    && !translatedName.Equals(langKey, StringComparison.OrdinalIgnoreCase))
                {
                    return translatedName;
                }
            }

            return "";
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