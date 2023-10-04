using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;
using static SimplifiedMoveset.MainMod;
using static SimplifiedMoveset.ProcessManagerMod;

namespace SimplifiedMoveset;

public class MainModOptions : OptionInterface {
    public static MainModOptions main_mod_options = new();

    //
    // options
    //

    public static Configurable<bool> beam_climb = main_mod_options.config.Bind("beamClimb", defaultValue: true, new ConfigurableInfo("Climb straight up horizontal beams. Grab beams when holding down in various cases. Lean instead of dropping from beams.", null, "", "Beam Climb"));
    public static Configurable<bool> belly_slide = main_mod_options.config.Bind("bellySlide", defaultValue: true, new ConfigurableInfo("Removes timing for rocket jumps and throwing spears during belly slides. Adds sliding down slopes.", null, "", "Belly Slide"));
    public static Configurable<bool> crawl = main_mod_options.config.Bind("crawl", defaultValue: true, new ConfigurableInfo("Crawl turns can be used to initiate rolls and on slopes. Increases turn rate in corridors. Removes slowdown when holding down.", null, "", "Crawl"));

    public static Configurable<bool> crouch_jump = main_mod_options.config.Bind("crouchJump", defaultValue: true, new ConfigurableInfo("Decreases charge time for super launch jumps.", null, "", "Crouch Jump"));
    public static Configurable<bool> grab = main_mod_options.config.Bind("grab", defaultValue: false, new ConfigurableInfo("Grab dead large creatures only when crouching.", null, "", "Grab"));
    public static Configurable<bool> gourmand = main_mod_options.config.Bind("gourmand", defaultValue: true, new ConfigurableInfo("Exhaust only when throwing spears. Stun creatures with rocket jumps. Slides, slow rocket jumps and rolls only stun and deal no damage.", null, "", "Gourmand"));

    public static Configurable<bool> roll_1 = main_mod_options.config.Bind("roll_1", defaultValue: true, new ConfigurableInfo("Rocket jumps from rolls have consistent height.", null, "", "Roll 1"));
    public static Configurable<bool> roll_2 = main_mod_options.config.Bind("roll_2_", defaultValue: false, new ConfigurableInfo("When enabled, removes the ability to chain rolls from rocket jumps.", null, "", "Roll 2"));
    public static Configurable<bool> slide_turn = main_mod_options.config.Bind("slideTurn", defaultValue: false, new ConfigurableInfo("Reduces the requirements for slide turns. Backflips are possible earlier.", null, "", "Slide Turn"));

    public static Configurable<bool> spear_throw = main_mod_options.config.Bind("spearThrow", defaultValue: true, new ConfigurableInfo("The throw momentum does not affect slugcats on the ground. The momentum while climbing beams is reduced. Throw boosting in the air is still possible. Weapons cannot change direction after being thrown.", null, "", "Spear Throw"));
    public static Configurable<bool> stand_up = main_mod_options.config.Bind("standUp", defaultValue: true, new ConfigurableInfo("Stand up after various animations.", null, "", "Stand Up"));
    public static Configurable<bool> swim = main_mod_options.config.Bind("swim", defaultValue: false, new ConfigurableInfo("Removes breath limit underwater. You can eat underwater. Increases swim speed. Adjusts buoyancy.", null, "", "Swim"));

    public static Configurable<bool> tube_worm = main_mod_options.config.Bind("tubeWorm", defaultValue: true, new ConfigurableInfo("Adds auto - aim grappling to beams. Changes affect Saint.", null, "", "Tube Worm"));
    public static Configurable<bool> wall_climb = main_mod_options.config.Bind("wallClimb", defaultValue: false, new ConfigurableInfo("Adds crawling on walls. Removes wall sliding. Normal jumps are prioritized for small obstacles instead of wall climbing / jumping.", null, "", "Wall Climb"));
    public static Configurable<bool> wall_jump = main_mod_options.config.Bind("wallJump", defaultValue: true, new ConfigurableInfo("Only wall jump when facing away from the wall. Wall jumps are prioritized over using tube worms. Removes wall sliding when not holding down. Normal jumps are prioritized for small obstacles instead of wall climbing / jumping.", null, "", "Wall Jump"));

    //
    // parameters
    //

    private readonly float _font_height = 20f;
    private readonly float _spacing = 20f;
    private readonly int _number_of_checkboxes = 3;
    private readonly float _check_box_size = 24f;
    private float CheckBoxWithSpacing => _check_box_size + 0.25f * _spacing;

    //
    // variables
    //

    private Vector2 _margin_x = new();
    private Vector2 _position = new();
    private readonly List<OpLabel> _text_labels = new();
    private readonly List<float> _box_end_positions = new();

    private readonly List<Configurable<bool>> _check_box_configurables = new();
    private readonly List<OpLabel> _check_boxes_text_labels = new();

    //
    // main
    //

    private MainModOptions() {
        On.OptionInterface._SaveConfigFile -= Save_Config_File;
        On.OptionInterface._SaveConfigFile += Save_Config_File;
    }

    private void Save_Config_File(On.OptionInterface.orig__SaveConfigFile orig, OptionInterface option_interface) {
        // the event OnConfigChange is triggered too often;
        // it is triggered when you click on the mod name in the
        // remix menu;
        // initializing the hooks takes like half a second;
        // I don't want to do that too often;

        orig(option_interface);
        if (option_interface != main_mod_options) return;
        Debug.Log(mod_id + ": Save_Config_File.");
        Initialize_Option_Specific_Hooks();
    }

    //
    // public
    //

    public override void Initialize() {
        base.Initialize();

        Tabs = new OpTab[1];
        Tabs[0] = new OpTab(this, "Options");
        InitializeMarginAndPos();

        // Title
        AddNewLine();
        AddTextLabel(mod_id + " Mod", big_text: true);
        DrawTextLabels(ref Tabs[0]);

        // Subtitle
        AddNewLine(0.5f);
        AddTextLabel("Version " + version, FLabelAlignment.Left);
        AddTextLabel("by " + author, FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[0]);

        // Content //
        AddNewLine();
        AddBox();

        AddCheckBox(beam_climb, (string)beam_climb.info.Tags[0]);
        AddCheckBox(belly_slide, (string)belly_slide.info.Tags[0]);
        AddCheckBox(crawl, (string)crawl.info.Tags[0]);

        AddCheckBox(crouch_jump, (string)crouch_jump.info.Tags[0]);
        AddCheckBox(grab, (string)grab.info.Tags[0]);
        AddCheckBox(gourmand, (string)gourmand.info.Tags[0]);

        AddCheckBox(roll_1, (string)roll_1.info.Tags[0]);
        AddCheckBox(roll_2, (string)roll_2.info.Tags[0]);
        AddCheckBox(slide_turn, (string)slide_turn.info.Tags[0]);

        AddCheckBox(spear_throw, (string)spear_throw.info.Tags[0]);
        AddCheckBox(stand_up, (string)stand_up.info.Tags[0]);
        AddCheckBox(swim, (string)swim.info.Tags[0]);

        AddCheckBox(tube_worm, (string)tube_worm.info.Tags[0]);
        AddCheckBox(wall_climb, (string)wall_climb.info.Tags[0]);
        AddCheckBox(wall_jump, (string)wall_jump.info.Tags[0]);

        DrawCheckBoxes(ref Tabs[0]);
        DrawBox(ref Tabs[0]);
    }

    public void Log_All_Options() {
        Debug.Log(mod_id + ": Option_BeamClimb " + Option_BeamClimb);
        Debug.Log(mod_id + ": Option_BellySlide " + Option_BellySlide);
        Debug.Log(mod_id + ": Option_Crawl " + Option_Crawl);

        Debug.Log(mod_id + ": Option_CrouchJump " + Option_CrouchJump);
        Debug.Log(mod_id + ": Option_Grab " + Option_Grab);
        Debug.Log(mod_id + ": Option_Gourmand " + Option_Gourmand);

        Debug.Log(mod_id + ": Option_Roll_1 " + Option_Roll_1);
        Debug.Log(mod_id + ": Option_Roll_2 " + Option_Roll_2);
        Debug.Log(mod_id + ": Option_SlideTurn " + Option_SlideTurn);

        Debug.Log(mod_id + ": Option_SpearThrow " + Option_SpearThrow);
        Debug.Log(mod_id + ": Option_StandUp " + Option_StandUp);
        Debug.Log(mod_id + ": Option_Swim " + Option_Swim);

        Debug.Log(mod_id + ": Option_TubeWorm " + Option_TubeWorm);
        Debug.Log(mod_id + ": Option_WallClimb " + Option_WallClimb);
        Debug.Log(mod_id + ": Option_WallJump " + Option_WallJump);
    }

    //
    // private
    //

    private void InitializeMarginAndPos() {
        _margin_x = new Vector2(50f, 550f);
        _position = new Vector2(50f, 600f);
    }

    private void AddNewLine(float spacing_modifier = 1f) {
        _position.x = _margin_x.x; // left margin
        _position.y -= spacing_modifier * _spacing;
    }

    private void AddBox() {
        _margin_x += new Vector2(_spacing, -_spacing);
        _box_end_positions.Add(_position.y);
        AddNewLine();
    }

    private void DrawBox(ref OpTab tab) {
        _margin_x += new Vector2(-_spacing, _spacing);
        AddNewLine();

        float box_width = _margin_x.y - _margin_x.x;
        int last_index = _box_end_positions.Count - 1;
        tab.AddItems(new OpRect(_position, new Vector2(box_width, _box_end_positions[last_index] - _position.y)));
        _box_end_positions.RemoveAt(last_index);
    }

    private void AddCheckBox(Configurable<bool> configurable, string text) {
        _check_box_configurables.Add(configurable);
        _check_boxes_text_labels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
    }

    private void DrawCheckBoxes(ref OpTab tab) { // changes pos.y but not pos.x
        if (_check_box_configurables.Count != _check_boxes_text_labels.Count) return;

        float width = _margin_x.y - _margin_x.x;
        float element_width = (width - ((_number_of_checkboxes - 1) * 0.5f * _spacing)) / _number_of_checkboxes;
        _position.y -= _check_box_size;
        float position_x = _position.x;

        for (int check_box_index = 0; check_box_index < _check_box_configurables.Count; ++check_box_index) {
            Configurable<bool> configurable = _check_box_configurables[check_box_index];
            OpCheckBox check_box = new(configurable, new Vector2(position_x, _position.y)) {
                description = configurable.info?.description ?? ""
            };
            tab.AddItems(check_box);
            position_x += CheckBoxWithSpacing;

            OpLabel check_box_label = _check_boxes_text_labels[check_box_index];
            check_box_label.pos = new Vector2(position_x, _position.y + 2f);
            check_box_label.size = new Vector2(element_width - CheckBoxWithSpacing, _font_height);
            tab.AddItems(check_box_label);

            if (check_box_index < _check_box_configurables.Count - 1) {
                if ((check_box_index + 1) % _number_of_checkboxes == 0) {
                    AddNewLine();
                    _position.y -= _check_box_size;
                    position_x = _position.x;
                } else {
                    position_x += element_width - CheckBoxWithSpacing + (0.5f * _spacing);
                }
            }
        }

        _check_box_configurables.Clear();
        _check_boxes_text_labels.Clear();
    }

    private void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool big_text = false) {
        float text_height = (big_text ? 2f : 1f) * _font_height;
        if (_text_labels.Count == 0) {
            _position.y -= text_height;
        }

        // minimal size.x = 20f
        OpLabel text_label = new(new Vector2(), new Vector2(20f, text_height), text, alignment, big_text) {
            autoWrap = true
        };
        _text_labels.Add(text_label);
    }

    private void DrawTextLabels(ref OpTab tab) {
        if (_text_labels.Count == 0) return;
        float width = (_margin_x.y - _margin_x.x) / _text_labels.Count;

        foreach (OpLabel text_label in _text_labels) {
            text_label.pos = _position;
            text_label.size += new Vector2(width - 20f, 0.0f);
            tab.AddItems(text_label);
            _position.x += width;
        }

        _position.x = _margin_x.x;
        _text_labels.Clear();
    }
}
