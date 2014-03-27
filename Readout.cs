using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace PlanktonPopulations
{
    public class Readout
    {
        public string dataName;
        public float value;
        public float min;
        public float max;
        public float top;
        public float bottom;
        public RenderTarget2D target;
        public Texture2D texture;

        // Some constants that are specific to data ranges and icon images
        Dictionary<string,Dictionary<string,float>> constants = new Dictionary<string, Dictionary<string,float>>()
        {
            {"SiO2", new Dictionary<string,float>()
                {
                    {"max",85.0F},
                    {"min",0.0F},
                    {"top",164.0F},
                    {"bottom",19.0F},
                }
            },
            {"NO3", new Dictionary<string,float>()
                {
                    {"max",5.0F}, // could log scale, with max of 30
                    {"min",0.0F},
                    {"top",139.0F},
                    {"bottom",39.0F},
                }
            },
            {"T", new Dictionary<string,float>()
                {
                    {"max",35.0F},
                    {"min",-2.0F},
                    {"top",162.0F},
                    {"bottom",20.0F},
                }
            },
            {"PAR", new Dictionary<string,float>()
                {
                    {"max",80.0F}, // was 160
                    {"min",0.0F},
                    {"top",119.0F},
                    {"bottom",64.0F},
                }
            },
        };

        public Dictionary<string, string> displayNames = new Dictionary<string, string>() 
        {
            {"SiO2", "Silica"},
            {"T","Temp"},
            {"NO3","Nutrient"},
            {"PAR","Light"},
        };

        // Constructor, requires string that associates this readout with data
        public Readout(string dataName, GraphicsDevice graphicsDevice)
        {
            this.dataName = dataName;
            this.min = constants[dataName]["min"];
            this.max = constants[dataName]["max"];
            this.top = constants[dataName]["top"];
            this.bottom = constants[dataName]["bottom"];
            this.target = new RenderTarget2D(graphicsDevice, PlanktonPopulations.readoutImages["T"][0].Width, PlanktonPopulations.readoutImages["T"][0].Height);
        }
    }
}
