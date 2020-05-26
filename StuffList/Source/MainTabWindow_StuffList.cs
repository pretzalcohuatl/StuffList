using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StuffList
{
    public class MainTabWindow_StuffList : MainTabWindow
    {
        public override Vector2 InitialSize => new Vector2(
                    ICON_WIDTH + LABEL_WIDTH + 14*STAT_WIDTH + 30,
                    800);

        // Display variables
        private const int HEADER_HEIGHT = 50;
        private const int ROW_HEIGHT = 30;
        private const int STAT_WIDTH = 80;
        private const int ICON_WIDTH = 29;
        private const int LABEL_WIDTH = 200;

        private enum Source : byte
        {
            Bases,
            Factors,
            Offset,
            Name
        }

        private struct colDef
        {
            public string label;
            public string property;
            public StatDef statDef;
            public Source source;

            public colDef(string label, string property, Source source)
            {
                this.label = label;
                this.property = property;
                this.source = source;
                this.statDef = StatDefOf.MarketValue;
            }

            public colDef(string label, StatDef statDef, Source source)
            {
                this.label = label;
                this.source = source;
                this.statDef = statDef;
                this.property = "";
            }

        }

        private string sortProperty = "label";
        private string sortOrder;
        private StatDef sortDef;
        private Source sortSource = Source.Name;

        private bool isDirty = true;

        public Vector2 scrollPosition = Vector2.zero;

        private float tableHeight;

        private bool showMetallic = true;
        private bool showWoody = true;
        private bool showStony = true;
        private bool showFabric = true;
        private bool showLeathery = true;

        private TemperatureDisplayMode tdm;
        private string tempUnit;
        private float tempCoeff;

        private int stuffCount = 0;

        // Data storage
        
        internal static IEnumerable<ThingDef> metallicStuff = (
            from thing in DefDatabase<ThingDef>.AllDefsListForReading
            where thing.category == ThingCategory.Item
            && thing.stuffProps != null
            && !thing.stuffProps.categories.NullOrEmpty()
            && thing.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic)
            && thing.stuffProps.statFactors != null
            select thing
            );
        internal static IEnumerable<ThingDef> woodyStuff = (
            from thing in DefDatabase<ThingDef>.AllDefsListForReading
            where thing.category == ThingCategory.Item
            && thing.stuffProps != null
            && !thing.stuffProps.categories.NullOrEmpty()
            && thing.stuffProps.categories.Contains(StuffCategoryDefOf.Woody)
            && thing.stuffProps.statFactors != null
            select thing
            );
        internal static IEnumerable<ThingDef> stonyStuff = (
            from thing in DefDatabase<ThingDef>.AllDefsListForReading
            where thing.category == ThingCategory.Item
            && thing.stuffProps != null
            && !thing.stuffProps.categories.NullOrEmpty()
            && thing.stuffProps.categories.Contains(StuffCategoryDefOf.Stony)
            && thing.stuffProps.statFactors != null
            select thing
            );
        internal static IEnumerable<ThingDef> fabricStuff = (
            from thing in DefDatabase<ThingDef>.AllDefsListForReading
            where thing.category == ThingCategory.Item
            && thing.stuffProps != null
            && !thing.stuffProps.categories.NullOrEmpty()
            && thing.stuffProps.categories.Contains(StuffCategoryDefOf.Fabric)
            && thing.stuffProps.statFactors != null
            select thing
            );
        internal static IEnumerable<ThingDef> leatheryStuff = (
            from thing in DefDatabase<ThingDef>.AllDefsListForReading
            where thing.category == ThingCategory.Item
            && thing.stuffProps != null
            && !thing.stuffProps.categories.NullOrEmpty()
            && thing.stuffProps.categories.Contains(StuffCategoryDefOf.Leathery)
            && thing.stuffProps.statFactors != null
            select thing
            );

        internal IEnumerable<ThingDef> stuff = Enumerable.Empty<ThingDef>();

        public MainTabWindow_StuffList()
        {

        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.isDirty = true;
            this.tdm = Prefs.TemperatureMode;
            if (this.tdm == TemperatureDisplayMode.Celsius)
            {
                this.tempUnit = "°C";
                this.tempCoeff = 1.0f;
            }
            else if (this.tdm == TemperatureDisplayMode.Fahrenheit)
            {
                this.tempUnit = "°F";
                this.tempCoeff = 1.8f;
            }
            else if (this.tdm == TemperatureDisplayMode.Kelvin)
            {
                this.tempUnit = "K";
                this.tempCoeff = 1.0f;
            }
            else
            {
                this.tempUnit = "";
                this.tempCoeff = 1.0f;
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);

            if (this.isDirty)
            {
                this.UpdateList();
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float currentX = rect.x;

            bool showMetallicOld = this.showMetallic;
            bool showWoodyOld = this.showWoody;
            bool showStonyOld = this.showStony;
            bool showFabricOld = this.showFabric;
            bool showLeatheryOld = this.showLeathery;

            PrintAutoCheckbox("StuffList.Metallic".Translate(), ref this.showMetallic, ref currentX, ref rect);
            PrintAutoCheckbox("StuffList.Woody".Translate(), ref this.showWoody, ref currentX, ref rect);
            PrintAutoCheckbox("StuffList.Stony".Translate(), ref this.showStony, ref currentX, ref rect);
            PrintAutoCheckbox("StuffList.Fabric".Translate(), ref this.showFabric, ref currentX, ref rect);
            PrintAutoCheckbox("StuffList.Leathery".Translate(), ref this.showLeathery, ref currentX, ref rect);

            if( showMetallicOld != this.showMetallic || showWoodyOld != this.showWoody || showStonyOld != this.showStony 
                || showFabricOld != this.showFabric || showLeatheryOld != this.showLeathery )
            {
                this.isDirty = true;
            }

            // HEADERS

            IEnumerable<colDef> colHeaders = Enumerable.Empty<colDef>();
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.MarketValue".Translate(), StatDefOf.MarketValue, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.ArmorSharp".Translate(), StatDefOf.StuffPower_Armor_Sharp, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.ArmorBlunt".Translate(), StatDefOf.StuffPower_Armor_Blunt, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.ArmorHeat".Translate(), StatDefOf.StuffPower_Armor_Heat, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.InsulCold".Translate(), StatDefOf.StuffPower_Insulation_Cold, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.InsulHot".Translate(), StatDefOf.StuffPower_Insulation_Heat, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.DmgSharp".Translate(), StatDefOf.SharpDamageMultiplier, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Base.DmgBlunt".Translate(), StatDefOf.BluntDamageMultiplier, Source.Bases));
            colHeaders = colHeaders.Append(new colDef("StuffList.Offset.Beauty".Translate(), StatDefOf.Beauty, Source.Offset));
            colHeaders = colHeaders.Append(new colDef("StuffList.Factor.MaxHP".Translate(), StatDefOf.MaxHitPoints, Source.Factors));
            colHeaders = colHeaders.Append(new colDef("StuffList.Factor.Beauty".Translate(), StatDefOf.Beauty, Source.Factors));
            colHeaders = colHeaders.Append(new colDef("StuffList.Factor.WorkMake".Translate(), StatDefOf.WorkToMake, Source.Factors));
            colHeaders = colHeaders.Append(new colDef("StuffList.Factor.WorkBuild".Translate(), StatDefOf.WorkToBuild, Source.Factors));
            colHeaders = colHeaders.Append(new colDef("StuffList.Factor.Flammability".Translate(), StatDefOf.Flammability, Source.Factors));
            colHeaders = colHeaders.Append(new colDef("StuffList.Factor.MeleeCooldown".Translate(), StatDefOf.MeleeWeapon_CooldownMultiplier, Source.Factors));
            
            /*foreach (StatDef stat in this.bases)
            {
                colHeaders = colHeaders.Append(new colDef(stat.label, stat.defName));
            }*/

            rect.y += HEADER_HEIGHT;
            GUI.BeginGroup(rect);
            tableHeight = stuffCount * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 4, tableHeight + 100);
            int num = 0;
            int ww = ICON_WIDTH;
            //DrawCommon(-1, inRect.width);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0, HEADER_HEIGHT, inRect.width);
            GUI.color = Color.white;
            printCellSort("label", "Name", ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            foreach (colDef h in colHeaders)
            {
                printCellSort(h.statDef.defName, h.statDef, h.source, h.label, ww);
                ww += STAT_WIDTH;
            }

            Rect scrollRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (ThingDef thing in this.stuff)
            {
                DrawRow(thing, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawRow(ThingDef t, int num, float w)
        {
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawIcon(ww, num, w, t);
            printCell(t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.statBases.GetStatValueFromList(StatDefOf.MarketValue, 1) + "", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH;
            printCell(t.statBases.GetStatValueFromList(StatDefOf.StuffPower_Armor_Sharp, 1) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH;
            printCell(t.statBases.GetStatValueFromList(StatDefOf.StuffPower_Armor_Blunt, 1) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH;
            printCell(t.statBases.GetStatValueFromList(StatDefOf.StuffPower_Armor_Heat, 1) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH;
            printCell(t.statBases.GetStatValueFromList(StatDefOf.StuffPower_Insulation_Cold, 0) * this.tempCoeff + this.tempUnit, num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.statBases.GetStatValueFromList(StatDefOf.StuffPower_Insulation_Heat, 0) * this.tempCoeff + this.tempUnit, num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.statBases.GetStatValueFromList(StatDefOf.SharpDamageMultiplier, 1) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.statBases.GetStatValueFromList(StatDefOf.BluntDamageMultiplier, 1) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.stuffProps.statOffsets.GetStatOffsetFromList(StatDefOf.Beauty) + "", num, ww, STAT_WIDTH, "Beauty = ((Base * Factor) + Offset) * Quality");
            ww += STAT_WIDTH; 
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MaxHitPoints) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.Beauty) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.WorkToMake) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.WorkToBuild) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH;
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.Flammability) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH; 
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MeleeWeapon_CooldownMultiplier) * 100 + "%", num, ww, STAT_WIDTH);
            /*


            */
        }

        private int DrawIcon(int x, int rowNum, float rowWidth, ThingDef t)
        {
            Rect icoRect = new Rect(0, ROW_HEIGHT * rowNum, ICON_WIDTH, ICON_WIDTH);
            //Thing tmpThing = ThingMaker.MakeThing(t);
            Widgets.ThingIcon(icoRect, t);
            return ICON_WIDTH + 2;
        }

        private void printCell(string content, int rowNum, int x, int width = STAT_WIDTH, string tooltip = "")
        {
            Rect tmpRec = new Rect(x, ROW_HEIGHT * rowNum + 3, width, ROW_HEIGHT - 3);
            Widgets.Label(tmpRec, content);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(tmpRec, tooltip);
            }
        }

        private void printCellSort(string sortProperty, StatDef sortDef, Source sortSource, string content, int x, int width = STAT_WIDTH)
        {
            Rect tmpRec = new Rect(x + 2, 2, width - 2, HEADER_HEIGHT - 2);
            Widgets.Label(tmpRec, content);
            if (Mouse.IsOver(tmpRec))
            {
                GUI.DrawTexture(tmpRec, TexUI.HighlightTex);
            }

            if (Widgets.ButtonInvisible(tmpRec))
            {
                if (this.sortProperty == sortProperty && this.sortSource == sortSource)
                {
                    this.sortOrder = this.sortOrder == "ASC" ? "DESC" : "ASC";
                }
                else
                {
                    this.sortDef = sortDef;
                    this.sortProperty = sortProperty;
                    this.sortSource = sortSource;
                }

                this.isDirty = true;
            }

            if (this.sortProperty == sortProperty)
            {
                Texture2D texture2D = (this.sortOrder == "ASC")
                    ? ContentFinder<Texture2D>.Get("UI/Icons/Sorting", true)
                    : ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending", true);
                Rect p = new Rect(tmpRec.xMax - (float)texture2D.width - 30, tmpRec.yMax - (float)texture2D.height - 1, (float)texture2D.width,
                    (float)texture2D.height);
                GUI.DrawTexture(p, texture2D);
            }
        }

        private void printCellSort(string sortProperty, string content, int x, int width = STAT_WIDTH)
        {
            Rect tmpRec = new Rect(x+2, 2, width-2, HEADER_HEIGHT - 2);
            Widgets.Label(tmpRec, content);
            if (Mouse.IsOver(tmpRec))
            {
                GUI.DrawTexture(tmpRec, TexUI.HighlightTex);
            }

            if (Widgets.ButtonInvisible(tmpRec))
            {
                if (this.sortProperty == sortProperty)
                {
                    this.sortOrder = this.sortOrder == "ASC" ? "DESC" : "ASC";
                }
                else
                {
                    this.sortProperty = sortProperty;
                    this.sortSource = Source.Name;
                }

                this.isDirty = true;
            }

            if (this.sortProperty == sortProperty)
            {
                Texture2D texture2D = (this.sortOrder == "ASC")
                    ? ContentFinder<Texture2D>.Get("UI/Icons/Sorting", true)
                    : ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending", true);
                Rect p = new Rect(tmpRec.xMax - (float)texture2D.width - 30, tmpRec.yMax - (float)texture2D.height - 1, (float)texture2D.width,
                    (float)texture2D.height);
                GUI.DrawTexture(p, texture2D);
            }
        }

        private void DrawCommon(int num, float w)
        {
            int fnum = num;
            if (num == -1)
            {
                fnum = 0;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0, ROW_HEIGHT * (fnum + 1), w);
            GUI.color = Color.white;
            Rect rowRect = new Rect(0, ROW_HEIGHT * num, w, ROW_HEIGHT);
            if (num > -1)
            {
                if (Mouse.IsOver(rowRect))
                {
                    GUI.DrawTexture(rowRect, TexUI.HighlightTex);
                }
            }
        }

        private void UpdateList()
        {
            this.stuff = Enumerable.Empty<ThingDef>();
            if (this.showMetallic)
            {
                stuff = stuff.Union(metallicStuff);
            }
            if (this.showWoody)
            {
                stuff = stuff.Union(woodyStuff);
            }
            if (this.showStony)
            {
                stuff = stuff.Union(stonyStuff);
            }
            if (this.showFabric)
            {
                stuff = stuff.Union(fabricStuff);
            }
            if (this.showLeathery)
            {
                stuff = stuff.Union(leatheryStuff);
            }

            stuffCount = stuff.Count<ThingDef>();

            UpdateListSorting();

            this.isDirty = false;
        }

        private void UpdateListSorting()
        {
            switch (this.sortSource)
            {
                case Source.Name:
                    stuff = this.sortOrder == "DESC"
                        ? stuff.OrderByDescending(o => o.label)
                        : stuff.OrderBy(o => o.label);
                    break;
                case Source.Bases:
                    stuff = this.sortOrder == "DESC"
                        ? stuff.OrderByDescending(o => o.statBases.GetStatValueFromList(sortDef, 1))
                        : stuff.OrderBy(o => o.statBases.GetStatValueFromList(sortDef, 1));
                    break;
                case Source.Factors:
                    stuff = this.sortOrder == "DESC"
                        ? stuff.OrderByDescending(o => o.stuffProps.statFactors.GetStatFactorFromList(sortDef))
                        : stuff.OrderBy(o => o.stuffProps.statFactors.GetStatFactorFromList(sortDef));
                    break;
                case Source.Offset:
                    stuff = this.sortOrder == "DESC"
                        ? stuff.OrderByDescending(o => o.stuffProps.statOffsets.GetStatOffsetFromList(sortDef))
                        : stuff.OrderBy(o => o.stuffProps.statOffsets.GetStatOffsetFromList(sortDef));
                    break;
            }



            /*    
             * 
             * printCell(t.statBases.GetStatValueFromList(StatDefOf.BluntDamageMultiplier, 1) * 100 + "%", num, ww, STAT_WIDTH);
            ww += STAT_WIDTH;
            printCell(t.stuffProps.statOffsets.GetStatOffsetFromList(StatDefOf.Beauty) + "", num, ww, STAT_WIDTH, "Beauty = ((Base * Factor) + Offset) * Quality");
            ww += STAT_WIDTH;
            printCell(t.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MaxHitPoints) * 100
            System.Reflection.PropertyInfo pir;
            pir = typeof(RangedWeapon).GetProperty(this.sortProperty);
            if (pir != null)
            {
                rangedList = this.sortOrder == "DESC"
                    ? rangedList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : rangedList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }
            */
        }

        private void PrintAutoCheckbox(string text, ref bool value, ref float currentX, ref Rect rect, bool defaultValue = false)
        {
            var textWidth = Text.CalcSize(text).x + 25f;
            Widgets.CheckboxLabeled(new Rect(currentX, rect.y, textWidth, 30), text, ref value, defaultValue);
            currentX += textWidth + 25f;
        }
    }

}
