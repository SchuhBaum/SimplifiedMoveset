using System.Collections.Generic;
using OptionalUI;
using UnityEngine;

namespace SimplifiedMoveset
{
    public class MainModOptions : OptionalUI.OptionInterface
    {
        private Vector2 marginX = new Vector2();
        private Vector2 pos = new Vector2();
        private readonly float spacing = 20f;

        private readonly List<float> boxEndPositions = new List<float>();

        private readonly int numberOfCheckboxes = 3;
        private readonly float checkBoxSize = 24f;
        private readonly List<OpCheckBox> checkBoxes = new List<OpCheckBox>();
        private readonly List<OpLabel> checkBoxesTextLabels = new List<OpLabel>();

        private readonly float fontHeight = 20f;
        private readonly List<OpLabel> textLabels = new List<OpLabel>();

        private float CheckBoxWithSpacing => checkBoxSize + 0.25f * spacing;

        public MainModOptions() : base(MainMod.instance)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            Tabs = new OpTab[1];
            Tabs[0] = new OpTab("Options");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("Simplified Moveset Mod", bigText: true);
            DrawTextLabels(ref Tabs[0]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.instance?.Info.Metadata.Version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.instance?.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[0]);

            // Content //
            AddNewLine();
            AddBox();

            AddCheckBox("beamClimb", "Beam Climb", "Climb straight up horizontal beams. Grab beams when holding down in various cases. Lean instead of dropping from beams.", defaultBool: true);
            AddCheckBox("bellySlide", "Belly Slide", "Removes timing for rocket jumps and throwing spears during belly slides. Slope collision is adepted to faster movement. Stand up belly slides, and rocket jumps (same effect as with the option Roll).", defaultBool: true);
            AddCheckBox("crawl", "Crawl", "Crawl turns can be used to initiate rolls and on slopes. Increases turn rate in corridors. Removes slowdown when holding down.", defaultBool: true);

            AddCheckBox("crouchJump", "Crouch Jump", "Stand up during crouch jumps. Decreases charge time for super launch jumps.", defaultBool: true);
            AddCheckBox("ledgeGrab", "Ledge Grab", "Slugcat stays in the ledge grab animation until jump is pressed.", defaultBool: false);
            AddCheckBox("rocketJump", "Rocket Jump", "When disabled, you can only perform normal jumps during rolls.", defaultBool: true);

            AddCheckBox("roll", "Roll", "Stand up after rolls, and rocket jumps (same effect as with the option Belly Slide). Removes timing / variable height for rocket jumps from rolls.\nThe changes for rocket jumps are only in effect if the option Rocket Jump is enabled as well.", defaultBool: true);
            AddCheckBox("slideTurn", "Slide Turn", "Reduces the requirements for slide turns. Backflips are possible earlier.", defaultBool: false);
            AddCheckBox("spearThrow", "Spear Throw", "The throw momentum does not affect slugcat on the ground. The momentum while climbing beams is reduced. Throw boosting in the air is still possible.", defaultBool: true);

            AddCheckBox("swim", "Swim", "Removes breath limit underwater. You can eat underwater. Increases swim speed. Adjusts buoyancy.", defaultBool: false);
            AddCheckBox("tubeWorm", "Tube Worm", "Adds auto - aim grappling to beams.", defaultBool: true);
            AddCheckBox("wallClimb", "Wall Climb", "Adds crawling on walls. Removes wall sliding. Normal jumps are prioritized for small obstacles instead of wall climbing / jumping.", defaultBool: false);

            AddCheckBox("wallJump", "Wall Jump", "Only wall jump when facing away from the wall.  Wall jumps are prioritized over using tube worms. Removes wall sliding when not holding down. Normal jumps are prioritized for small obstacles instead of wall climbing / jumping.", defaultBool: true);
            // AddCheckBox("slopes", "Slopes", "Makes moving down slopes at higher speed possible. Animations might still slow you down. Changes collision with slopes to be identical with solid tiles. These changes only affect player bodyChunks.", defaultBool: true);

            DrawCheckBoxes(ref Tabs[0]);
            DrawBox(ref Tabs[0]);
        }
        public override void Update(float dt)
        {
            base.Update(dt);
        }
        public override void ConfigOnChange()
        {
            base.ConfigOnChange();

            MainMod.Option_BeamClimb = bool.Parse(config["beamClimb"]);
            MainMod.Option_BellySlide = bool.Parse(config["bellySlide"]);
            MainMod.Option_Crawl = bool.Parse(config["crawl"]);

            MainMod.Option_CrouchJump = bool.Parse(config["crouchJump"]);
            MainMod.Option_LedgeGrab = bool.Parse(config["ledgeGrab"]);
            MainMod.Option_RocketJump = bool.Parse(config["rocketJump"]);

            MainMod.Option_Roll = bool.Parse(config["roll"]);
            MainMod.Option_SlideTurn = bool.Parse(config["slideTurn"]);
            MainMod.Option_SpearThrow = bool.Parse(config["spearThrow"]);

            MainMod.Option_Swim = bool.Parse(config["swim"]);
            MainMod.Option_TubeWorm = bool.Parse(config["tubeWorm"]);
            MainMod.Option_WallClimb = bool.Parse(config["wallClimb"]);

            MainMod.Option_WallJump = bool.Parse(config["wallJump"]);

            Debug.Log("SimplifiedMoveset: Option_BeamClimb " + MainMod.Option_BeamClimb);
            Debug.Log("SimplifiedMoveset: Option_BellySlide " + MainMod.Option_BellySlide);
            Debug.Log("SimplifiedMoveset: Option_Crawl " + MainMod.Option_Crawl);

            Debug.Log("SimplifiedMoveset: Option_CrouchJump " + MainMod.Option_CrouchJump);
            Debug.Log("SimplifiedMoveset: Option_LedgeGrab " + MainMod.Option_LedgeGrab);
            Debug.Log("SimplifiedMoveset: Option_RocketJump " + MainMod.Option_RocketJump);

            Debug.Log("SimplifiedMoveset: Option_Roll " + MainMod.Option_Roll);
            Debug.Log("SimplifiedMoveset: Option_SlideTurn " + MainMod.Option_SlideTurn);
            Debug.Log("SimplifiedMoveset: Option_SpearThrow " + MainMod.Option_SpearThrow);

            Debug.Log("SimplifiedMoveset: Option_Swim " + MainMod.Option_Swim);
            Debug.Log("SimplifiedMoveset: Option_TubeWorm " + MainMod.Option_TubeWorm);
            Debug.Log("SimplifiedMoveset: Option_WallClimb " + MainMod.Option_WallClimb);

            Debug.Log("SimplifiedMoveset: Option_WallJump " + MainMod.Option_WallJump);
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private void InitializeMarginAndPos()
        {
            marginX = new Vector2(50f, 550f);
            pos = new Vector2(50f, 600f);
        }

        private void AddNewLine(float spacingModifier = 1f)
        {
            pos.x = marginX.x; // left margin
            pos.y -= spacingModifier * spacing;
        }

        private void AddBox()
        {
            marginX += new Vector2(spacing, -spacing);
            boxEndPositions.Add(pos.y);
            AddNewLine();
        }

        private void DrawBox(ref OpTab tab)
        {
            marginX += new Vector2(-spacing, spacing);
            AddNewLine();

            float boxWidth = marginX.y - marginX.x;
            int lastIndex = boxEndPositions.Count - 1;
            tab.AddItems(new OpRect(pos, new Vector2(boxWidth, boxEndPositions[lastIndex] - pos.y)));
            boxEndPositions.RemoveAt(lastIndex);
        }

        private void AddCheckBox(string key, string text, string description, bool? defaultBool = null)
        {
            OpCheckBox opCheckBox = new OpCheckBox(new Vector2(), key, defaultBool: defaultBool ?? false)
            {
                description = description
            };

            checkBoxes.Add(opCheckBox);
            checkBoxesTextLabels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
        }

        private void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
        {
            if (checkBoxes.Count != checkBoxesTextLabels.Count)
            {
                return;
            }

            float width = marginX.y - marginX.x;
            float elementWidth = (width - (numberOfCheckboxes - 1) * 0.5f * spacing) / numberOfCheckboxes;
            pos.y -= checkBoxSize;
            float _posX = pos.x;

            for (int index = 0; index < checkBoxes.Count; ++index)
            {
                OpCheckBox checkBox = checkBoxes[index];
                checkBox.pos = new Vector2(_posX, pos.y);
                tab.AddItems(checkBox);
                _posX += CheckBoxWithSpacing;

                OpLabel checkBoxLabel = checkBoxesTextLabels[index];
                checkBoxLabel.pos = new Vector2(_posX, pos.y + 2f);
                checkBoxLabel.size = new Vector2(elementWidth - CheckBoxWithSpacing, fontHeight);
                tab.AddItems(checkBoxLabel);

                if (index < checkBoxes.Count - 1)
                {
                    if ((index + 1) % numberOfCheckboxes == 0)
                    {
                        AddNewLine();
                        pos.y -= checkBoxSize;
                        _posX = pos.x;
                    }
                    else
                    {
                        _posX += elementWidth - CheckBoxWithSpacing + 0.5f * spacing;
                    }
                }
            }

            checkBoxes.Clear();
            checkBoxesTextLabels.Clear();
        }

        private void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool bigText = false)
        {
            float textHeight = (bigText ? 2f : 1f) * fontHeight;
            if (textLabels.Count == 0)
            {
                pos.y -= textHeight;
            }

            OpLabel textLabel = new OpLabel(new Vector2(), new Vector2(20f, textHeight), text, alignment, bigText) // minimal size.x = 20f
            {
                autoWrap = true
            };
            textLabels.Add(textLabel);
        }

        private void DrawTextLabels(ref OpTab tab)
        {
            if (textLabels.Count == 0)
            {
                return;
            }

            float width = (marginX.y - marginX.x) / textLabels.Count;
            foreach (OpLabel textLabel in textLabels)
            {
                textLabel.pos = pos;
                textLabel.size += new Vector2(width - 20f, 0.0f);
                tab.AddItems(textLabel);
                pos.x += width;
            }

            pos.x = marginX.x;
            textLabels.Clear();
        }
    }
}