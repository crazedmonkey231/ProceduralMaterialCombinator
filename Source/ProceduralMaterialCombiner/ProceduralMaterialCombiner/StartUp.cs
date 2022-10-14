using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace ProceduralMaterialCombiner
{

    [DefOf]
    public static class ProceduralMaterialCombinerDefOf
    {

        public static ThingDef FueledSmithy;
        public static ThingDef ElectricSmithy;

        public static StuffAppearanceDef Metal;

        public static SoundDef BulletImpact_Metal;
        public static SoundDef MeleeHit_Metal_Sharp;
        public static SoundDef MeleeHit_Metal_Blunt;
    }


    public class ProceduralMaterialCombinerSettings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
        }

    }

    public class ProceduralMaterialCombiner : Mod
    {

        public static ProceduralMaterialCombinerSettings settings;
        public static ModContentPack cachedModContentPack;

        public ProceduralMaterialCombiner(ModContentPack content) : base(content)
        {
            ProceduralMaterialCombiner.settings = this.GetSettings<ProceduralMaterialCombinerSettings>();
            ProceduralMaterialCombiner.cachedModContentPack = content;

            Log.Message("content pack " + content);
        }

    }

    [StaticConstructorOnStartup]
    public class StartUp
    {

        /*
         * Helpers
        */

        private static List<StatDef> statsToCombine = new List<StatDef>() {

            StatDefOf.Mass,
            StatDefOf.MaxHitPoints,
            StatDefOf.Beauty,
            StatDefOf.Flammability,
            StatDefOf.WorkToMake,
            StatDefOf.WorkToBuild,
            StatDefOf.SellPriceFactor,
            StatDefOf.SharpDamageMultiplier,
            StatDefOf.BluntDamageMultiplier,
            StatDefOf.StuffEffectMultiplierInsulation_Cold,
            StatDefOf.StuffEffectMultiplierInsulation_Heat,
            StatDefOf.StuffPower_Armor_Blunt,
            StatDefOf.StuffPower_Armor_Heat,
            StatDefOf.StuffPower_Armor_Sharp,
            StatDefOf.StuffPower_Insulation_Cold,
            StatDefOf.StuffPower_Insulation_Heat

        };

        private static List<ThingDef> GetAllMetallicMaterialsFromDatabase()
        {

            return DefDatabase<ThingDef>.AllDefs.Where(x => x.IsMetal).ToList();

        }

        #region new def default configuration
        private static ThingDef GetNewDef()
        {

            ThingDef x = new ThingDef();

            SetDefaultValues(x);
            SetDefaultStuffCategories(x);
            SetDefaultThingCategories(x);
            SetDefaultStuffProps(x);
            SetDefaultStatFactors(x);
            SetDefaultComps(x);

            return x;

        }

        private static void SetDefaultValues(ThingDef x)
        {

            x.thingClass = typeof(ThingWithComps);
            x.category = ThingCategory.Item;
            x.drawerType = DrawerType.MapMeshOnly;
            x.resourceReadoutPriority = ResourceCountPriority.Middle;
            x.altitudeLayer = AltitudeLayer.Item;
            x.useHitPoints = true;
            x.selectable = true;
            x.alwaysHaulable = true;
            x.drawGUIOverlay = true;
            x.rotatable = false;
            x.allowedArchonexusCount = 75;
            x.stackLimit = 75;
            x.healthAffectsPrice = false;
            x.resourceReadoutAlwaysShow = true;
            x.deepCommonality = 0.5f;
            x.deepCountPerPortion = 70;
            x.deepLumpSizeRange = new IntRange(1, 4);
            x.burnableByRecipe = false;
            x.smeltable = true;
            x.terrainAffordanceNeeded = TerrainAffordanceDefOf.Medium;
            x.soundDrop = SoundDefOf.Standard_Drop;
            x.soundInteract = SoundDefOf.Standard_Drop;
            x.generated = true;
            x.modContentPack = ProceduralMaterialCombiner.cachedModContentPack;

        }

        private static void SetDefaultStuffCategories(ThingDef x)
        {

            if (x.stuffCategories == null)
                x.stuffCategories = new List<StuffCategoryDef>();

            x.stuffCategories.Add(StuffCategoryDefOf.Metallic);

        }

        private static void SetDefaultThingCategories(ThingDef x)
        {

            if (x.thingCategories == null)
                x.thingCategories = new List<ThingCategoryDef>();

            x.thingCategories.Add(ThingCategoryDefOf.ResourcesRaw);

        }

        private static void SetDefaultStuffProps(ThingDef x)
        {

            if (x.stuffProps == null)
                x.stuffProps = new StuffProperties();

            if (x.stuffProps == null)
                x.stuffProps.categories = new List<StuffCategoryDef>();

            x.stuffProps.categories.Add(StuffCategoryDefOf.Metallic);
            x.stuffProps.commonality = 0.02f;
            x.stuffProps.allowColorGenerators = true;
            x.stuffProps.appearance = ProceduralMaterialCombinerDefOf.Metal;
            x.stuffProps.soundImpactStuff = ProceduralMaterialCombinerDefOf.BulletImpact_Metal;
            x.stuffProps.soundMeleeHitSharp = ProceduralMaterialCombinerDefOf.MeleeHit_Metal_Sharp;
            x.stuffProps.soundMeleeHitBlunt = ProceduralMaterialCombinerDefOf.MeleeHit_Metal_Blunt;

        }

        private static void SetDefaultStatFactors(ThingDef x)
        {

            if (x.stuffProps.statFactors == null)
                x.stuffProps.statFactors = new List<StatModifier>();

            x.stuffProps.statFactors.Add(new StatModifier()
            {
                stat = StatDefOf.Flammability,
                value = 0
            });

        }

        private static void SetDefaultComps(ThingDef x)
        {

            if (x.comps == null)
                x.comps = new List<CompProperties>();

            x.comps.Add(new CompProperties_Forbiddable());

        }

        private static void BuildRecipe(ThingDef material)
        {

            RecipeMakerProperties recipeMaker = material.recipeMaker;
            if (recipeMaker == null)
                recipeMaker = new RecipeMakerProperties();
            recipeMaker.useIngredientsForColor = true;
            
            RecipeDef recipeDef = new RecipeDef();

            if (recipeDef.recipeUsers == null)
                recipeDef.recipeUsers = new List<ThingDef>();

            recipeDef.defName = "Make_" + material.defName;
            recipeDef.label = "Make " + material.label;
            recipeDef.description = "Alloy made from two metals.";
            recipeDef.recipeUsers.Add(ProceduralMaterialCombinerDefOf.FueledSmithy);
            recipeDef.recipeUsers.Add(ProceduralMaterialCombinerDefOf.ElectricSmithy);
            recipeDef.defaultIngredientFilter = recipeMaker.defaultIngredientFilter;
            recipeDef.useIngredientsForColor = recipeMaker.useIngredientsForColor;
            recipeDef.products.Clear();
            recipeDef.products.Add(new ThingDefCountClass(material, 25));
            SetIngredients(recipeDef, material);
            recipeDef.generated = true;
            recipeDef.ResolveReferences();

            DefGenerator.AddImpliedDef(recipeDef);

        }

        private static void SetIngredients(RecipeDef r, ThingDef def)
        {

            r.ingredients.Clear();
            
            if (def.CostList == null)
            {
                return;
            }

            foreach (ThingDefCountClass cost in def.CostList)
            {
                IngredientCount ingredientCount2 = new IngredientCount();
                ingredientCount2.SetBaseCount(cost.count);
                ingredientCount2.filter.SetAllow(cost.thingDef, allow: true);
                r.ingredients.Add(ingredientCount2);
            }

        }

        #endregion

        private static void SetDefName(ThingDef material, List<ThingDef> metalsToExtractNames)
        {

            string name = "AutoMaterial_";

            metalsToExtractNames.ForEach(i =>
            {
                name += i.defName + "_";
            });

            name += "GeneratedDef";

            material.defName = name;

        }

        private static void SetLabel(ThingDef material, List<ThingDef> metalsToExtractNames)
        {

            string name = "AutoMaterial ";

            metalsToExtractNames.ForEach(i =>
            {
                name += i.label + " ";
            });

            name += "GeneratedDef";

            material.label = name;

        }

        private static void SetDescription(ThingDef material, string newDescription = "")
        {

            material.description = "Brand new material created by combining 2 other materials. {0}".Formatted(newDescription);

        }

        private static StatModifier GetStatModifierValue(ThingDef material, StatDef stat)
        {

            return material.statBases.Where(x => x.stat.Equals(stat)).First();

        }

        private static float GetStatValue(ThingDef material, StatDef stat)
        {

            return material.statBases.Where(x => x.stat.Equals(stat)).First().value;

        }

        private static void SetStatBasesStatModifier(ThingDef material, StatModifier modifier)
        {

            if (material.statBases == null)
                material.statBases = new List<StatModifier>();

            material.statBases.Add(modifier);

        }

        private static void SetStatOffsetsStatModifier(ThingDef material, StatModifier modifier)
        {

            if (material.stuffProps.statOffsets == null)
                material.stuffProps.statOffsets = new List<StatModifier>();

            material.stuffProps.statOffsets.Add(modifier);

        }

        private static void ClearStatBases(ThingDef material)
        {

            material.statBases.Clear();
        }

        private static void SetGenerateCommonality(ThingDef material, List<ThingDef> metalsToAlloy)
        {
            float output = 0;

            metalsToAlloy.ForEach(i =>
            {

                output += i.generateCommonality;

            });

            output = output / metalsToAlloy.Count;

            if (output < 0)
                output = 0;

            material.generateCommonality = output;

        }

        private static void SetMaterialGraphicData(ThingDef material)
        {

            string texPath = "Things/Items/Resources/Ingot";

            material.graphicData = new GraphicData()
            {

                graphicClass = typeof(Graphic_StackCount),
                texPath = texPath,
                shaderType = ShaderTypeDefOf.CutoutComplex

            };

            material.uiIcon = (Texture2D)material.graphicData.Graphic.MatAt(Rot4.East).mainTexture;

        }

        private static void SetStackLimit(ThingDef material, int stackLimit)
        {

            if (stackLimit < 0)
                stackLimit = 0;

            if (material.stackLimit > 75)
                stackLimit = 75;

            material.stackLimit = stackLimit;

        }

        private static bool HasStuffPropsGoldenAdjective(ThingDef material)
        {

            return material.stuffProps.stuffAdjective != null && material.stuffProps.stuffAdjective.Equals("golden");

        }

        private static void SetStuffAdjectiveGolden(ThingDef material, List<ThingDef> metalsToAlloy)
        {

            bool flag = false;

            metalsToAlloy.ForEach(i =>
            {
                if (HasStuffPropsGoldenAdjective(i))
                    flag = true;
            });

            if (flag)
            {
                material.stuffProps.stuffAdjective = "golden";
            }

        }

        private static Color GetColorForThingDef(ThingDef i)
        {
            return i.stuffProps.color;
        }

        private static void SetThingDefStuffColorWithLerp(ThingDef material, List<ThingDef> metalsToAlloy)
        {

            material.stuffProps.color = Color.Lerp(metalsToAlloy[0].stuffProps.color, metalsToAlloy[1].stuffProps.color, 0.5f);

        }

        private static void SetStuffPropsSoundsFromThingDef(ThingDef material, ThingDef mat1)
        {

            material.stuffProps.soundImpactStuff = mat1.stuffProps.soundImpactStuff;
            material.stuffProps.soundMeleeHitSharp = mat1.stuffProps.soundMeleeHitSharp;
            material.stuffProps.soundMeleeHitBlunt = mat1.stuffProps.soundMeleeHitBlunt;

        }


        private static void SetCostList(ThingDef newMaterial, List<ThingDef> metalsToAlloy)
        {

            if(newMaterial.costList == null)
            {

                

            }

            metalsToAlloy.ForEach(i =>{ newMaterial.costList.Add(new ThingDefCountClass(i, 25)); });

        }

        /*
         * Main
        */

        static StartUp()
        {

            ClearCache();

            List<ThingDef> v = GetAllMetallicMaterialsFromDatabase();
            List<ThingDef> x = v;
            List<ThingDef> y = v;
            List<ThingDef> newMaterials = new List<ThingDef>();

            foreach (IEnumerable<ThingDef> i in Combinations<ThingDef>(v, 2))
            {

                var combinations = i.ToList();
                newMaterials.Add(Combinator(combinations));

            }

            if (!newMaterials.NullOrEmpty())
            {

                newMaterials.ForEach(nm =>
                {

                    DefGenerator.AddImpliedDef(nm);

                });

                Log.Message("Created {0} new material combinations.".Formatted(newMaterials.Count));

                CleanUp();

            }
            else
            {

                Log.Message("No new materials created. This may be a script error.");

            }

        }

        private static void ClearCache()
        {

            DefDatabase<ThingDef>.ClearCachedData();
            DefDatabase<RecipeDef>.ClearCachedData();

        }

        private static void CleanUp()
        {

            ThingSetMakerUtility.Reset();
            DefOfHelper.RebindAllDefOfs(false);
            DefDatabase<RecipeDef>.ResolveAllReferences();
            DefDatabase<ThingDef>.ResolveAllReferences();

        }

        private static ThingDef Combinator(List<ThingDef> metalsToAlloy)
        {

            string message = "";
            metalsToAlloy.ForEach(i => message += "_" + i.defName);
            Log.Message("\t`New material created! Material is : " + message);

            ThingDef newMaterial = GetNewDef();

            SetDefName(newMaterial, metalsToAlloy);

            SetLabel(newMaterial, metalsToAlloy);

            SetDescription(newMaterial);

            SetMaterialGraphicData(newMaterial);

            SetCostList(newMaterial, metalsToAlloy);

            SetGenerateCommonality(newMaterial, metalsToAlloy);

            SetStuffAdjectiveGolden(newMaterial, metalsToAlloy);

            SetThingDefStuffColorWithLerp(newMaterial, metalsToAlloy);

            SetStuffPropsSoundsFromThingDef(newMaterial, metalsToAlloy[0]);

            BuildRecipe(newMaterial);

            return newMaterial;

        }

        private static bool NextCombination(IList<int> num, int n, int k)
        {
            bool finished;

            var changed = finished = false;

            if (k <= 0) return false;

            for (var i = k - 1; !finished && !changed; i--)
            {
                if (num[i] < n - 1 - (k - 1) + i)
                {
                    num[i]++;

                    if (i < k - 1)
                        for (var j = i + 1; j < k; j++)
                            num[j] = num[j - 1] + 1;
                    changed = true;
                }
                finished = i == 0;
            }

            return changed;
        }

        private static IEnumerable Combinations<T>(IEnumerable<T> elements, int k)
        {
            var elem = elements.ToArray();
            var size = elem.Length;

            if (k > size) yield break;

            var numbers = new int[k];

            for (var i = 0; i < k; i++)
                numbers[i] = i;

            do
            {
                yield return numbers.Select(n => elem[n]);
            } while (NextCombination(numbers, size, k));
        }

    }
}
