using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using EasyConfig;   // http://easyconfig.codeplex.com/

namespace PlanktonPopulations
{
    /// <summary>
    /// A static class to load and store all settings from a config file.
    /// </summary>
    public static class Settings
    {
        public static bool FULLSCREEN;
        public static bool SHOW_MOUSE;
        public static int RESOLUTION_X;
        public static int RESOLUTION_Y;
        public static bool ANTIALIASING;

        public static bool INPUT_SWIPES_ONLY;
        public static bool INPUT_USE_MOUSE;
        public static int INPUT_TOUCH_TIME;
        public static bool INPUT_USE_ORIENTATION;
        public static bool INPUT_USE_FIDUCIALS;
        public static bool INPUT_USE_GLOBAL_CALIBRATION;
        public static bool USE_INDIVIDUAL_CALIBRATION;
        public static int[,] CALIBRATIONS = new int[4096, 3]; // an array of arrays to hold the fiducial calibration values [symbolID][0 = x, 1 = y, 2 = angle in degrees]
        public static int INPUT_GLOBAL_X_SHIFT;
        public static int INPUT_GLOBAL_Y_SHIFT;

        public static float CIRCLE_RADIUS;
        public static float CIRCLE_RADIUS_OVERSCAN;
        public static float CIRCLE_VELOCITY;
        public static Color CIRCLE_BORDER_COLOR;
        public static float CIRCLE_BORDER_WIDTH;
        public static Color CIRCLE_BACKGROUND_COLOR;
        public static Color CIRCLE_ON_LAND_BACKGROUND_COLOR; 
        public static int CIRCLE_OPEN_TIME;
        public static int MAX_CIRCLES;
        public static int CIRCLE_DETECTION_RADIUS;
        public static int CIRCLE_FADEIN_TIME;
        public static int CIRCLE_FADEOUT_TIME;
        public static float CIRCLE_POSITION_CHANGE_THRESHOLD;

        public static bool SHOW_CALLOUT;
        public static int CALLOUT_HORIZONTAL_ADJUST;
        public static int CALLOUT_HORIZONTAL_HIDE;
        public static int CALLOUT_VERTICAL_ADJUST;
        public static int CALLOUT_DETECTION_RADIUS;
        public static int CALLOUT_OPENING_TIME;
        public static int CALLOUT_CLOSING_TIME;
        public static Vector2 CALLOUT_CLOSE_BUTTON;
        public static Vector2 CALLOUT_OPEN_BUTTON;
        public static Vector2 CALLOUT_TAB1_BUTTON;
        public static Vector2 CALLOUT_TAB2_BUTTON;
        public static Vector2 CALLOUT_TAB3_BUTTON;
        public static Vector2 CALLOUT_TAB4_BUTTON;
        public static Vector2 CALLOUT_CLOSE_BUTTON_LEFT;
        public static Vector2 CALLOUT_OPEN_BUTTON_LEFT;
        public static Vector2 CALLOUT_TAB1_BUTTON_LEFT;
        public static Vector2 CALLOUT_TAB2_BUTTON_LEFT;
        public static Vector2 CALLOUT_TAB3_BUTTON_LEFT;
        public static Vector2 CALLOUT_TAB4_BUTTON_LEFT;
        public static int CALLOUT_TAB_HEIGHT;
        public static int CALLOUT_TAB_WIDTH;

        public static float OFFSET_DISTANCE;
        public static float OFFSET_RADIUS;
        public static Color OFFSET_BORDER_COLOR;
        public static float OFFSET_BORDER_WIDTH;
        public static int OFFSET_DETECTION_RADIUS;
        public static int TANGENT_WIDTH;

        public static float TIMELINE_X;
        public static float TIMELINE_Y;
        public static int TIMELINE_MONTH_HASH_HEIGHT;
        public static int TIMELINE_MONTH_NAME_OFFSET;
        public static int TIMELINE_SCRUBBER_HEIGHT;
        public static int TIMELINE_SCRUBBER_WIDTH;
        public static bool TIMELINE_ONE_YEAR;
        public static bool TIMELINE_CIRCULAR;
        public static bool TIMELINE_LINEAR;
        public static bool TIMELINE_MONTHNAME;
        public static int TIMELINE_MONTHNAME_Y;
        public static bool TIMELINE_MIRROR;
        public static float TIMELINE_CIRCULAR_X;
        public static float TIMELINE_CIRCULAR_Y;
        public static int TIMELINE_CIRCULAR_RADIUS;
        public static int TIMELINE_MONTHNAME_CENTER_WIDTH;
        public static int TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH;
        public static int TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH;
        public static int TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH;
        public static int TIMELINE_MONTHNAME_SPACING;
        public static bool TIMELINE_MONTHNAME_EXPAND_FROM_BASELINE;
        public static Color TIMELINE_MONTHNAME_CURRENT_COLOR;
        public static Color TIMELINE_MONTHNAME_OTHER_COLOR;
        public static bool TIMELINE_MONTHNAME_STATIC;
        public static int TIMELINE_MONTHNAME_STATIC_SPACING;
        public static int TIMELINE_MONTHNAME_STATIC_MARKER_OFFSET;

        public static float[] PHOSPHORUS_CONVERSIONS;
        public static float[] PLANKTON_COUNT_CONVERSIONS;
        public static byte PLANKTON_OPACITY;
        public static float[] PLANKTON_SIZES;
        public static int PLANKTON_FADEIN_TIME;
        public static int PLANKTON_FADEOUT_TIME;
        public static int PLANKTON_MAX_TOTAL;
        public static int PLANKTON_MAX_PER_CIRCLE;

        public static bool SHOW_LIGHT;
        public static bool SHOW_TEMP;
        public static bool SHOW_SILICA;
        public static bool SHOW_NITRATE;
        public static float DASHBOARD_ORIENTATION;
        public static float DASHBOARD_SPACING;
        public static int DASHBOARD_READOUT_SIZE;
        public static int READOUT_DISTANCE;
        public static int READOUT_LABEL_DISTANCE;
        public static Color READOUT_ICON_COLOR;
        public static Color READOUT_LABEL_COLOR;

        public static bool MOVIE_PAUSE_WHEN_CIRCLES_SHOWN;
        public static bool MOVIE_SLOWER;
        public static bool MOVIE_BLUE_WATER;
        public static bool MOVIE_BLUE_WATER_SATURATED;

        public static int NUM_TEMPTOOLS;
        public static int NUM_NUTRIENTTOOLS;

        public static bool SHOW_RUNNING_SLOWLY;
        public static bool SHOW_TOUCHES;
        public static bool SHOW_HITBOXES;

        public static bool CROSSHAIRS_MODE;
        public static int CROSSHAIRS_WIDTH;
        public static int CROSSHAIRS_LENGTH;
        public static Color CROSSHAIRS_COLOR;
        public static int CROSSHAIRS_MEDIUM_THRESHOLD_VELOCITY;
        public static int CROSSHAIRS_MEDIUM_OPACITY;
        public static int CROSSHAIRS_ON_MEDIUM_FADE_TIME;
        public static int CROSSHAIRS_SLOW_DELAY_TIME;
        public static int CROSSHAIRS_ON_SLOW_FADE_TIME;
        public static int CROSSHAIRS_RING_UP_DELAY_TIME;
        public static int CROSSHAIRS_ON_RING_UP_ZOOM_TIME;
        public static int CROSSHAIRS_ON_RING_DOWN_ZOOM_TIME;
        public static int CROSSHAIRS_ANTI_JITTER_DELAY;

        public static Color CONTINENT_COLOR;

        public static bool TOUCHONLY;
        public static int TOUCHONLY_NUM_RINGS;
        public static float TOUCHONLY_LENS_POWER;
        public static int TOUCHONLY_MEDIUM_THRESHOLD_VELOCITY;

        /// <summary>
        /// Loads settings from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to load settings from.</param>
        public static void LoadSettings(string filename)
        {
            ConfigFile file = new ConfigFile(filename);

            SettingsGroup screen = file.SettingGroups["Screen"];

            FULLSCREEN = screen.Settings["fullscreen"].GetValueAsBool();
            SHOW_MOUSE = screen.Settings["show_mouse"].GetValueAsBool();
            RESOLUTION_X = screen.Settings["resolution_x"].GetValueAsInt();
            RESOLUTION_Y = screen.Settings["resolution_y"].GetValueAsInt();
            ANTIALIASING = screen.Settings["antialiasing"].GetValueAsBool();

            SettingsGroup input = file.SettingGroups["Input"];

            INPUT_USE_MOUSE = input.Settings["mouse_input"].GetValueAsBool();
            INPUT_USE_ORIENTATION = input.Settings["use_orientation"].GetValueAsBool();
            INPUT_USE_FIDUCIALS = input.Settings["use_fiducials"].GetValueAsBool();
            INPUT_TOUCH_TIME = input.Settings["touch_time"].GetValueAsInt();
            INPUT_USE_GLOBAL_CALIBRATION = input.Settings["use_global_calibration"].GetValueAsBool();
            INPUT_GLOBAL_X_SHIFT = input.Settings["global_x_shift"].GetValueAsInt();
            INPUT_GLOBAL_Y_SHIFT = input.Settings["global_y_shift"].GetValueAsInt();
            INPUT_SWIPES_ONLY = input.Settings["swipes_only"].GetValueAsBool();

            USE_INDIVIDUAL_CALIBRATION = input.Settings["use_individual_calibration"].GetValueAsBool();
            if (!INPUT_USE_FIDUCIALS)
            {
                USE_INDIVIDUAL_CALIBRATION = false;
                INPUT_USE_GLOBAL_CALIBRATION = false;
            }
            if (USE_INDIVIDUAL_CALIBRATION)
            {
                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (input.Settings.ContainsKey("marker_" + i))
                            CALIBRATIONS[i, j] = input.Settings["marker_" + i].GetValueAsIntArray()[j];
                    }
                }
            }

            SettingsGroup circles = file.SettingGroups["Circles"];

            CIRCLE_RADIUS = circles.Settings["radius"].GetValueAsFloat();
            CIRCLE_RADIUS_OVERSCAN = circles.Settings["radius_overscan"].GetValueAsFloat();
            CIRCLE_BORDER_WIDTH = circles.Settings["border_width"].GetValueAsFloat();
            int[] colorValues = circles.Settings["border_color"].GetValueAsIntArray();
            CIRCLE_BORDER_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);
            colorValues = circles.Settings["background_color"].GetValueAsIntArray();
            CIRCLE_BACKGROUND_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);
            colorValues = circles.Settings["on_land_background_color"].GetValueAsIntArray();
            CIRCLE_ON_LAND_BACKGROUND_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);
            CIRCLE_OPEN_TIME = circles.Settings["open_time"].GetValueAsInt();
            MAX_CIRCLES = circles.Settings["max_number"].GetValueAsInt();
            CIRCLE_VELOCITY = circles.Settings["velocity"].GetValueAsFloat();
            CIRCLE_DETECTION_RADIUS = circles.Settings["detection_radius"].GetValueAsInt();
            CIRCLE_FADEIN_TIME = circles.Settings["fadein_time"].GetValueAsInt();
            CIRCLE_FADEOUT_TIME = circles.Settings["fadeout_time"].GetValueAsInt();
            CIRCLE_POSITION_CHANGE_THRESHOLD = circles.Settings["position_change_threshold"].GetValueAsFloat();

            SettingsGroup callout = file.SettingGroups["Guide"];

            SHOW_CALLOUT = callout.Settings["show_callout"].GetValueAsBool();
            CALLOUT_VERTICAL_ADJUST = callout.Settings["vertical_adjust"].GetValueAsInt();
            CALLOUT_HORIZONTAL_ADJUST = callout.Settings["horizontal_adjust"].GetValueAsInt();
            CALLOUT_HORIZONTAL_HIDE = callout.Settings["horizontal_hide"].GetValueAsInt();
            CALLOUT_DETECTION_RADIUS = callout.Settings["detection_radius"].GetValueAsInt();
            CALLOUT_OPENING_TIME = callout.Settings["opening_time"].GetValueAsInt();
            CALLOUT_CLOSING_TIME = callout.Settings["closing_time"].GetValueAsInt();
            int[] closeCoords = callout.Settings["close_coords"].GetValueAsIntArray();
            CALLOUT_CLOSE_BUTTON = new Vector2(closeCoords[0], closeCoords[1]);
            int[] openCoords = callout.Settings["open_coords"].GetValueAsIntArray();
            CALLOUT_OPEN_BUTTON = new Vector2(openCoords[0], openCoords[1]);
            int[] tab1Coords = callout.Settings["tab1_coords"].GetValueAsIntArray();
            CALLOUT_TAB1_BUTTON = new Vector2(tab1Coords[0], tab1Coords[1]);
            int[] tab2Coords = callout.Settings["tab2_coords"].GetValueAsIntArray();
            CALLOUT_TAB2_BUTTON = new Vector2(tab2Coords[0], tab2Coords[1]);
            int[] tab3Coords = callout.Settings["tab3_coords"].GetValueAsIntArray();
            CALLOUT_TAB3_BUTTON = new Vector2(tab3Coords[0], tab3Coords[1]);
            int[] tab4Coords = callout.Settings["tab4_coords"].GetValueAsIntArray();
            CALLOUT_TAB4_BUTTON = new Vector2(tab4Coords[0], tab4Coords[1]);
            int[] closeCoords_reflected = callout.Settings["close_coords_reflected"].GetValueAsIntArray();
            CALLOUT_CLOSE_BUTTON_LEFT = new Vector2(closeCoords_reflected[0], closeCoords_reflected[1]);
            int[] openCoords_reflected = callout.Settings["open_coords_reflected"].GetValueAsIntArray();
            CALLOUT_OPEN_BUTTON_LEFT = new Vector2(openCoords_reflected[0], openCoords_reflected[1]);
            int[] tab1Coords_reflected = callout.Settings["tab1_coords_reflected"].GetValueAsIntArray();
            CALLOUT_TAB1_BUTTON_LEFT = new Vector2(tab1Coords_reflected[0], tab1Coords_reflected[1]);
            int[] tab2Coords_reflected = callout.Settings["tab2_coords_reflected"].GetValueAsIntArray();
            CALLOUT_TAB2_BUTTON_LEFT = new Vector2(tab2Coords_reflected[0], tab2Coords_reflected[1]);
            int[] tab3Coords_reflected = callout.Settings["tab3_coords_reflected"].GetValueAsIntArray();
            CALLOUT_TAB3_BUTTON_LEFT = new Vector2(tab3Coords_reflected[0], tab3Coords_reflected[1]);
            int[] tab4Coords_reflected = callout.Settings["tab4_coords_reflected"].GetValueAsIntArray();
            CALLOUT_TAB4_BUTTON_LEFT = new Vector2(tab4Coords_reflected[0], tab4Coords_reflected[1]);
            CALLOUT_TAB_HEIGHT = callout.Settings["tab_height"].GetValueAsInt();
            CALLOUT_TAB_WIDTH = callout.Settings["tab_width"].GetValueAsInt();

            SettingsGroup offset = file.SettingGroups["Offset"];

            OFFSET_DISTANCE = offset.Settings["offset_distance"].GetValueAsFloat();
            OFFSET_RADIUS = offset.Settings["radius"].GetValueAsFloat();
            OFFSET_BORDER_WIDTH = offset.Settings["border_width"].GetValueAsFloat();
            colorValues = offset.Settings["border_color"].GetValueAsIntArray();
            OFFSET_BORDER_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);
            OFFSET_DETECTION_RADIUS = offset.Settings["detection_radius"].GetValueAsInt();
            TANGENT_WIDTH = offset.Settings["tangent_lines_width"].GetValueAsInt();

            SettingsGroup timeline = file.SettingGroups["Timeline"];

            int[] timelinePosition = timeline.Settings["position"].GetValueAsIntArray();
            TIMELINE_X = timelinePosition[0];
            TIMELINE_Y = timelinePosition[1];
            TIMELINE_MONTH_HASH_HEIGHT = timeline.Settings["month_hash_height"].GetValueAsInt();
            TIMELINE_MONTH_NAME_OFFSET = timeline.Settings["month_name_offset"].GetValueAsInt();
            TIMELINE_SCRUBBER_HEIGHT = timeline.Settings["scrubber_height"].GetValueAsInt();
            TIMELINE_SCRUBBER_WIDTH = timeline.Settings["scrubber_width"].GetValueAsInt();
            TIMELINE_ONE_YEAR = timeline.Settings["one_year_only"].GetValueAsBool();
            TIMELINE_CIRCULAR = timeline.Settings["circular"].GetValueAsBool();
            TIMELINE_MIRROR = timeline.Settings["mirror"].GetValueAsBool();
            timelinePosition = timeline.Settings["circular_position"].GetValueAsIntArray();
            TIMELINE_CIRCULAR_X = timelinePosition[0];
            TIMELINE_CIRCULAR_Y = timelinePosition[1];
            TIMELINE_CIRCULAR_RADIUS = timeline.Settings["circular_radius"].GetValueAsInt();
            TIMELINE_LINEAR = timeline.Settings["linear"].GetValueAsBool();
            TIMELINE_MONTHNAME = timeline.Settings["month_name"].GetValueAsBool();
            TIMELINE_MONTHNAME_Y = timeline.Settings["month_name_y"].GetValueAsInt();
            TIMELINE_MONTHNAME_CENTER_WIDTH = timeline.Settings["month_name_center_width"].GetValueAsInt();
            TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH = timeline.Settings["month_name_center_transition_width"].GetValueAsInt();
            TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH = timeline.Settings["month_name_blank_edge_width"].GetValueAsInt();
            TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH = timeline.Settings["month_name_edge_transition_width"].GetValueAsInt();
            TIMELINE_MONTHNAME_SPACING = timeline.Settings["month_name_spacing"].GetValueAsInt();
            TIMELINE_MONTHNAME_EXPAND_FROM_BASELINE = timeline.Settings["month_name_expand_from_baseline"].GetValueAsBool();
            colorValues = timeline.Settings["month_name_current_color"].GetValueAsIntArray();
            TIMELINE_MONTHNAME_CURRENT_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2]);
            colorValues = timeline.Settings["month_name_other_color"].GetValueAsIntArray();
            TIMELINE_MONTHNAME_OTHER_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2]);
            TIMELINE_MONTHNAME_STATIC = timeline.Settings["month_name_static"].GetValueAsBool();
            TIMELINE_MONTHNAME_STATIC_SPACING = timeline.Settings["month_name_static_spacing"].GetValueAsInt();
            TIMELINE_MONTHNAME_STATIC_MARKER_OFFSET = timeline.Settings["month_name_static_marker_offset"].GetValueAsInt();

            SettingsGroup plankton = file.SettingGroups["Plankton"];

            PHOSPHORUS_CONVERSIONS = plankton.Settings["phosphorus_conversions"].GetValueAsFloatArray();
            PLANKTON_COUNT_CONVERSIONS = plankton.Settings["count_conversions"].GetValueAsFloatArray();
            PLANKTON_OPACITY = (byte)plankton.Settings["opacity"].GetValueAsInt();
            PLANKTON_SIZES = plankton.Settings["sizes"].GetValueAsFloatArray();
            PLANKTON_FADEIN_TIME = plankton.Settings["fadein_time"].GetValueAsInt();
            PLANKTON_FADEOUT_TIME = plankton.Settings["fadeout_time"].GetValueAsInt();
            PLANKTON_MAX_TOTAL = plankton.Settings["max_total"].GetValueAsInt();
            PLANKTON_MAX_PER_CIRCLE = plankton.Settings["max_per_circle"].GetValueAsInt();
            
            SettingsGroup dashboard = file.SettingGroups["Dashboard"];

            SHOW_LIGHT = dashboard.Settings["show_light"].GetValueAsBool();
            SHOW_TEMP = dashboard.Settings["show_temperature"].GetValueAsBool();
            SHOW_SILICA = dashboard.Settings["show_silica"].GetValueAsBool();
            SHOW_NITRATE = dashboard.Settings["show_nitrate"].GetValueAsBool();
            DASHBOARD_ORIENTATION = dashboard.Settings["orientation"].GetValueAsFloat();
            DASHBOARD_SPACING = dashboard.Settings["spacing"].GetValueAsFloat();
            DASHBOARD_READOUT_SIZE = dashboard.Settings["size"].GetValueAsInt();
            READOUT_DISTANCE = dashboard.Settings["distance"].GetValueAsInt();
            READOUT_LABEL_DISTANCE = dashboard.Settings["label_distance"].GetValueAsInt();
            colorValues = dashboard.Settings["icon_color"].GetValueAsIntArray();
            READOUT_ICON_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);
            colorValues = dashboard.Settings["label_color"].GetValueAsIntArray();
            READOUT_LABEL_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);

            SettingsGroup tools = file.SettingGroups["Tools"];

            NUM_TEMPTOOLS = tools.Settings["num_temptools"].GetValueAsInt();
            NUM_NUTRIENTTOOLS = tools.Settings["num_nutrienttools"].GetValueAsInt();

            SettingsGroup movie = file.SettingGroups["Movie"];

            MOVIE_PAUSE_WHEN_CIRCLES_SHOWN = movie.Settings["pause_when_circles_shown"].GetValueAsBool();
            MOVIE_SLOWER = movie.Settings["slower_movie"].GetValueAsBool();
            MOVIE_BLUE_WATER = movie.Settings["blue_water"].GetValueAsBool();
            MOVIE_BLUE_WATER_SATURATED = movie.Settings["blue_water_saturated"].GetValueAsBool();

            SettingsGroup debug = file.SettingGroups["Debug"];

            SHOW_RUNNING_SLOWLY = debug.Settings["show_running_slowly_indicator"].GetValueAsBool();
            SHOW_TOUCHES = debug.Settings["show_touches"].GetValueAsBool();
            SHOW_HITBOXES = debug.Settings["show_hitboxes"].GetValueAsBool();

            SettingsGroup crosshairs = file.SettingGroups["Crosshairs"];

            CROSSHAIRS_MODE = crosshairs.Settings["crosshairs_mode"].GetValueAsBool();
            CROSSHAIRS_WIDTH = crosshairs.Settings["width"].GetValueAsInt();
            CROSSHAIRS_LENGTH = crosshairs.Settings["length"].GetValueAsInt();
            colorValues = crosshairs.Settings["color"].GetValueAsIntArray();
            CROSSHAIRS_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);
            CROSSHAIRS_MEDIUM_THRESHOLD_VELOCITY = crosshairs.Settings["medium_threshold_velocity"].GetValueAsInt();
            CROSSHAIRS_MEDIUM_OPACITY = crosshairs.Settings["medium_opacity"].GetValueAsInt();
            CROSSHAIRS_ON_MEDIUM_FADE_TIME = crosshairs.Settings["on_medium_fade_time"].GetValueAsInt();
            CROSSHAIRS_SLOW_DELAY_TIME = crosshairs.Settings["slow_delay_time"].GetValueAsInt();
            CROSSHAIRS_ON_SLOW_FADE_TIME = crosshairs.Settings["on_slow_fade_time"].GetValueAsInt();
            CROSSHAIRS_RING_UP_DELAY_TIME = crosshairs.Settings["ring_up_delay_time"].GetValueAsInt();
            CROSSHAIRS_ON_RING_UP_ZOOM_TIME = crosshairs.Settings["on_ring_up_zoom_time"].GetValueAsInt();
            CROSSHAIRS_ON_RING_DOWN_ZOOM_TIME = crosshairs.Settings["on_ring_down_zoom_time"].GetValueAsInt();
            CROSSHAIRS_ANTI_JITTER_DELAY = crosshairs.Settings["anti_jitter_delay"].GetValueAsInt();

            SettingsGroup continent = file.SettingGroups["Continents"];

            colorValues = continent.Settings["color"].GetValueAsIntArray();
            CONTINENT_COLOR = new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]);

            SettingsGroup touchonly = file.SettingGroups["TouchOnly"];

            TOUCHONLY = touchonly.Settings["touch_only"].GetValueAsBool();
            TOUCHONLY_NUM_RINGS = touchonly.Settings["num_rings"].GetValueAsInt();
            TOUCHONLY_LENS_POWER = touchonly.Settings["lens_power"].GetValueAsFloat();
            TOUCHONLY_MEDIUM_THRESHOLD_VELOCITY = touchonly.Settings["medium_threshold_velocity"].GetValueAsInt();
        }

        /// <summary>
        /// Loads additional settings from another file, overwriting current settings.
        /// </summary>
        /// <param name="filename">The name of the file to load additional settings from.</param>
        public static void LoadAdditionalSettings(string filename)
        {
            ConfigFile file = new ConfigFile(filename);
            foreach (SettingsGroup group in file.SettingGroups.Values)
            {
                // TODO if multiple settings files becomes necessary
            }
        }
    }
}
