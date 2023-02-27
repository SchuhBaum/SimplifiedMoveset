using System.Collections.Generic;
using Menu.Remix.MixedUI;
using UnityEngine;

using static SimplifiedMoveset.MainMod;

namespace SimplifiedMoveset;

public class MainModOptions : OptionInterface
{
    public static MainModOptions instance = new();

    //
    // options
    //

    public static Configurable<bool> beamClimb = instance.config.Bind("beamClimb", defaultValue: true, new ConfigurableInfo("Climb straight up horizontal beams. Grab beams when holding down in various cases. Lean instead of dropping from beams.", null, "", "Beam Climb"));
    public static Configurable<bool> bellySlide = instance.config.Bind("bellySlide", defaultValue: true, new ConfigurableInfo("Removes timing for rocket jumps and throwing spears during belly slides. Adds sliding down slopes.", null, "", "Belly Slide"));
    public static Configurable<bool> crawl = instance.config.Bind("crawl", defaultValue: true, new ConfigurableInfo("Crawl turns can be used to initiate rolls and on slopes. Increases turn rate in corridors. Removes slowdown when holding down.", null, "", "Crawl"));

    public static Configurable<bool> crouchJump = instance.config.Bind("crouchJump", defaultValue: true, new ConfigurableInfo("Decreases charge time for super launch jumps.", null, "", "Crouch Jump"));
    public static Configurable<bool> grab = instance.config.Bind("grab", defaultValue: false, new ConfigurableInfo("Grab dead large creatures only when crouching.", null, "", "Grab"));
    public static Configurable<bool> roll_1 = instance.config.Bind("roll_1", defaultValue: true, new ConfigurableInfo("Rocket jumps from rolls have consistent height.", null, "", "Roll 1"));

    public static Configurable<bool> roll_2 = instance.config.Bind("roll_2_", defaultValue: false, new ConfigurableInfo("When enabled, removes the ability to chain rolls from rocket jumps.", null, "", "Roll 2"));
    public static Configurable<bool> slideTurn = instance.config.Bind("slideTurn", defaultValue: false, new ConfigurableInfo("Reduces the requirements for slide turns. Backflips are possible earlier.", null, "", "Slide Turn"));
    public static Configurable<bool> spearThrow = instance.config.Bind("spearThrow", defaultValue: true, new ConfigurableInfo("The throw momentum does not affect slugcats on the ground. The momentum while climbing beams is reduced. Throw boosting in the air is still possible. Weapons cannot change direction after being thrown.", null, "", "Spear Throw"));

    public static Configurable<bool> standUp = instance.config.Bind("standUp", defaultValue: true, new ConfigurableInfo("Stand up after various animations.", null, "", "Stand Up"));
    public static Configurable<bool> swim = instance.config.Bind("swim", defaultValue: false, new ConfigurableInfo("Removes breath limit underwater. You can eat underwater. Increases swim speed. Adjusts buoyancy.", null, "", "Swim"));
    public static Configurable<bool> tubeWorm = instance.config.Bind("tubeWorm", defaultValue: true, new ConfigurableInfo("Adds auto - aim grappling to beams. Changes affect Saint.", null, "", "Tube Worm"));

    public static Configurable<bool> wallClimb = instance.config.Bind("wallClimb", defaultValue: false, new ConfigurableInfo("Adds crawling on walls. Removes wall sliding. Normal jumps are prioritized for small obstacles instead of wall climbing / jumping.", null, "", "Wall Climb"));
    public static Configurable<bool> wallJump = instance.config.Bind("wallJump", defaultValue: true, new ConfigurableInfo("Only wall jump when facing away from the wall. Wall jumps are prioritized over using tube worms. Removes wall sliding when not holding down. Normal jumps are prioritized for small obstacles instead of wall climbing / jumping.", null, "", "Wall Jump"));

    //
    // parameters
    //

    private readonly float fontHeight = 20f;
    private readonly float spacing = 20f;
    private readonly int numberOfCheckboxes = 3;
    private readonly float checkBoxSize = 24f;
    private float CheckBoxWithSpacing => checkBoxSize + 0.25f * spacing;

    //
    // variables
    //

    private Vector2 marginX = new();
    private Vector2 pos = new();
    private readonly List<OpLabel> textLabels = new();
    private readonly List<float> boxEndPositions = new();

    private readonly List<Configurable<bool>> checkBoxConfigurables = new();
    private readonly List<OpLabel> checkBoxesTextLabels = new();

    //
    // main
    //

    public MainModOptions() => OnConfigChanged += MainModOptions_OnConfigChanged;

    //
    // public
    //

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[1];
        Tabs[0] = new OpTab(this, "Options");
        InitializeMarginAndPos();

        // Title
        AddNewLine();
        AddTextLabel("SimplifiedMoveset Mod", bigText: true);
        DrawTextLabels(ref Tabs[0]);

        // Subtitle
        AddNewLine(0.5f);
        AddTextLabel("Version " + version, FLabelAlignment.Left);
        AddTextLabel("by " + author, FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[0]);

        // Content //
        AddNewLine();
        AddBox();

        AddCheckBox(beamClimb, (string)beamClimb.info.Tags[0]);
        AddCheckBox(bellySlide, (string)bellySlide.info.Tags[0]);
        AddCheckBox(crawl, (string)crawl.info.Tags[0]);

        AddCheckBox(crouchJump, (string)crouchJump.info.Tags[0]);
        AddCheckBox(grab, (string)grab.info.Tags[0]);
        AddCheckBox(roll_1, (string)roll_1.info.Tags[0]);

        AddCheckBox(roll_2, (string)roll_2.info.Tags[0]);
        AddCheckBox(slideTurn, (string)slideTurn.info.Tags[0]);
        AddCheckBox(spearThrow, (string)spearThrow.info.Tags[0]);

        AddCheckBox(standUp, (string)standUp.info.Tags[0]);
        AddCheckBox(swim, (string)swim.info.Tags[0]);
        AddCheckBox(tubeWorm, (string)tubeWorm.info.Tags[0]);

        AddCheckBox(wallClimb, (string)wallClimb.info.Tags[0]);
        AddCheckBox(wallJump, (string)wallJump.info.Tags[0]);

        DrawCheckBoxes(ref Tabs[0]);
        DrawBox(ref Tabs[0]);
    }

    public void MainModOptions_OnConfigChanged()
    {
        Debug.Log("SimplifiedMoveset: Option_BeamClimb " + Option_BeamClimb);
        Debug.Log("SimplifiedMoveset: Option_BellySlide " + Option_BellySlide);
        Debug.Log("SimplifiedMoveset: Option_Crawl " + Option_Crawl);

        Debug.Log("SimplifiedMoveset: Option_CrouchJump " + Option_CrouchJump);
        Debug.Log("SimplifiedMoveset: Option_Grab " + Option_Grab);
        Debug.Log("SimplifiedMoveset: Option_Roll_1 " + Option_Roll_1);

        Debug.Log("SimplifiedMoveset: Option_Roll_2 " + Option_Roll_2);
        Debug.Log("SimplifiedMoveset: Option_SlideTurn " + Option_SlideTurn);
        Debug.Log("SimplifiedMoveset: Option_SpearThrow " + Option_SpearThrow);

        Debug.Log("SimplifiedMoveset: Option_StandUp " + Option_StandUp);
        Debug.Log("SimplifiedMoveset: Option_Swim " + Option_Swim);
        Debug.Log("SimplifiedMoveset: Option_TubeWorm " + Option_TubeWorm);

        Debug.Log("SimplifiedMoveset: Option_WallClimb " + Option_WallClimb);
        Debug.Log("SimplifiedMoveset: Option_WallJump " + Option_WallJump);
    }

    //
    // private
    //

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

    private void AddCheckBox(Configurable<bool> configurable, string text)
    {
        checkBoxConfigurables.Add(configurable);
        checkBoxesTextLabels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
    }

    private void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
    {
        if (checkBoxConfigurables.Count != checkBoxesTextLabels.Count) return;

        float width = marginX.y - marginX.x;
        float elementWidth = (width - (numberOfCheckboxes - 1) * 0.5f * spacing) / numberOfCheckboxes;
        pos.y -= checkBoxSize;
        float _posX = pos.x;

        for (int checkBoxIndex = 0; checkBoxIndex < checkBoxConfigurables.Count; ++checkBoxIndex)
        {
            Configurable<bool> configurable = checkBoxConfigurables[checkBoxIndex];
            OpCheckBox checkBox = new(configurable, new Vector2(_posX, pos.y))
            {
                description = configurable.info?.description ?? ""
            };
            tab.AddItems(checkBox);
            _posX += CheckBoxWithSpacing;

            OpLabel checkBoxLabel = checkBoxesTextLabels[checkBoxIndex];
            checkBoxLabel.pos = new Vector2(_posX, pos.y + 2f);
            checkBoxLabel.size = new Vector2(elementWidth - CheckBoxWithSpacing, fontHeight);
            tab.AddItems(checkBoxLabel);

            if (checkBoxIndex < checkBoxConfigurables.Count - 1)
            {
                if ((checkBoxIndex + 1) % numberOfCheckboxes == 0)
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

        checkBoxConfigurables.Clear();
        checkBoxesTextLabels.Clear();
    }

    private void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool bigText = false)
    {
        float textHeight = (bigText ? 2f : 1f) * fontHeight;
        if (textLabels.Count == 0)
        {
            pos.y -= textHeight;
        }

        OpLabel textLabel = new(new Vector2(), new Vector2(20f, textHeight), text, alignment, bigText) // minimal size.x = 20f
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