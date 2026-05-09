using System.Globalization;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Botanism.Items
{
    public class ItemPropagule : Item
    {
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string plantName = itemStack?.Attributes?.GetString("plantDisplayName");

            if (string.IsNullOrWhiteSpace(plantName))
            {
                string targetPlantCode = itemStack?.Attributes?.GetString("targetPlantCode");

                if (!string.IsNullOrWhiteSpace(targetPlantCode))
                {
                    plantName = GetPlantDisplayName(targetPlantCode);
                }
            }

            if (string.IsNullOrWhiteSpace(plantName))
            {
                return base.GetHeldItemName(itemStack);
            }

            string propagationType = itemStack.Attributes.GetString("propagationType", "generic");
            string materialName = GetPropagationMaterialDisplayName(propagationType);

            return Lang.Get("botanism:item-propagule-named-material", plantName, materialName);
        }

        public override void GetHeldItemInfo(
            ItemSlot inSlot,
            StringBuilder dsc,
            IWorldAccessor world,
            bool withDebugInfo
        )
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            ItemStack itemStack = inSlot?.Itemstack;

            if (itemStack == null)
            {
                return;
            }

            string plantName = itemStack.Attributes.GetString("plantDisplayName");
            string targetPlantCode = itemStack.Attributes.GetString("targetPlantCode");
            string propagationType = itemStack.Attributes.GetString("propagationType", "generic");

            if (string.IsNullOrWhiteSpace(plantName) && !string.IsNullOrWhiteSpace(targetPlantCode))
            {
                plantName = GetPlantDisplayName(targetPlantCode);
            }

            if (!string.IsNullOrWhiteSpace(plantName))
            {
                dsc.AppendLine(Lang.Get("botanism:item-propagule-source", plantName));
            }

            dsc.AppendLine(Lang.Get(
                "botanism:item-propagule-type",
                GetPropagationTypeDisplayName(propagationType)
            ));
        }

        private static string GetPlantDisplayName(string plantCode)
        {
            string path = plantCode;

            int domainSeparatorIndex = path.IndexOf(':');

            if (domainSeparatorIndex >= 0 && domainSeparatorIndex < path.Length - 1)
            {
                path = path.Substring(domainSeparatorIndex + 1);
            }

            if (path.StartsWith("flower-"))
            {
                path = path.Substring("flower-".Length);
            }

            if (path.EndsWith("-free"))
            {
                path = path.Substring(0, path.Length - "-free".Length);
            }

            if (path.EndsWith("-snow"))
            {
                path = path.Substring(0, path.Length - "-snow".Length);
            }

            path = path.Replace("-", " ");

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(path);
        }

        private static string GetPropagationTypeDisplayName(string propagationType)
        {
            return propagationType switch
            {
                "seed" => Lang.Get("botanism:propagation-type-seed"),
                "bulb" => Lang.Get("botanism:propagation-type-bulb"),
                "spore" => Lang.Get("botanism:propagation-type-spore"),
                "rhizome" => Lang.Get("botanism:propagation-type-rhizome"),
                "pip" => Lang.Get("botanism:propagation-type-pip"),
                "cutting" => Lang.Get("botanism:propagation-type-cutting"),
                "division" => Lang.Get("botanism:propagation-type-division"),
                "fragment" => Lang.Get("botanism:propagation-type-fragment"),
                _ => Lang.Get("botanism:propagation-type-generic")
            };
        }

        private static string GetPropagationMaterialDisplayName(string propagationType)
        {
            return propagationType switch
            {
                "seed" => Lang.Get("botanism:propagule-material-seed"),
                "bulb" => Lang.Get("botanism:propagule-material-bulb"),
                "spore" => Lang.Get("botanism:propagule-material-spore"),
                "rhizome" => Lang.Get("botanism:propagule-material-rhizome"),
                "pip" => Lang.Get("botanism:propagule-material-pip"),
                "cutting" => Lang.Get("botanism:propagule-material-cutting"),
                "division" => Lang.Get("botanism:propagule-material-division"),
                "fragment" => Lang.Get("botanism:propagule-material-fragment"),
                _ => Lang.Get("botanism:propagule-material-generic")
            };
        }
    }
}