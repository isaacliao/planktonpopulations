using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PlanktonPopulations
{
    class Plankton
    {
        const float VELOCITY_LIMIT = 1;
        const double OPACITY_STEP = 1;
        const double ROTATION_VELOCITY_LIMIT = 2.0*Math.PI/600.0;    // radians per frame

        public float orientation;
        public float rotationVelocity;
        public Vector2 position;
        public Vector2 velocity;
        public float size;
        public Color color;
        public Texture2D image;
        public bool taggedForDeletion;
        public int type;
        private double fadeStart;
        private string fadeType; // can be in, out, or none 

        public static Color[] colors = { new Color(77, 175, 74, Settings.PLANKTON_OPACITY), new Color(228, 126, 78, Settings.PLANKTON_OPACITY), new Color(55, 126, 184, Settings.PLANKTON_OPACITY), new Color(152, 78, 163, Settings.PLANKTON_OPACITY) };
        Random rand = PlanktonPopulations.rand;

        // Constructor
        public Plankton() {
        }

        // Sets a bunch of parameters on this Plankton and makes it the specified type (0,1,2,3)
	    public void Initialize(int type, double circleCreationTime, GameTime gameTime)
	    {
            this.type = type;

            // Assign a texture based on type. Randomly choose one of the available images for each type.
            image = PlanktonPopulations.planktonImages[type][rand.Next(PlanktonPopulations.planktonImages[type].Length)];

            // Set size based on type.
            size = Settings.PLANKTON_SIZES[type];

            // Don't tag for deletion when created. That would be silly.
            taggedForDeletion = false;

            // Assign a random position within the circle.
            position = new Vector2(rand.Next(-(int)(Settings.CIRCLE_RADIUS), (int)(Settings.CIRCLE_RADIUS)), rand.Next(-(int)(Settings.CIRCLE_RADIUS), (int)(Settings.CIRCLE_RADIUS)));

            // Assign a random velocity within certain limits.
            velocity = new Vector2((float)(rand.NextDouble()-0.5)*VELOCITY_LIMIT, (float)(rand.NextDouble()-0.5) * VELOCITY_LIMIT);

            // Assign a random starting orientation.
            orientation = (float) (2*Math.PI*rand.NextDouble());

            // Assign a random rotation velocity within limits.
            rotationVelocity = (float) (ROTATION_VELOCITY_LIMIT * (2.0*(rand.NextDouble()-0.5)));

            // Set plankton to fade in when created, if after one round of fadein time after circle creation.
            if ((gameTime.TotalGameTime.TotalMilliseconds - circleCreationTime) > Settings.PLANKTON_FADEIN_TIME)
            {
                this.FadeIn(gameTime);
            }    
	    }

        public void Update(GameTime gameTime)
        {        
            // Update opacity based on fadeState
            if (fadeType == "out")
            {
                double fadeRatio = (gameTime.TotalGameTime.TotalMilliseconds - fadeStart) / Settings.PLANKTON_FADEOUT_TIME;
                this.color = Color.Lerp(colors[type], Color.Transparent, (float)fadeRatio);
            }
            else if (fadeType == "in")
            {
                double fadeRatio = (gameTime.TotalGameTime.TotalMilliseconds - fadeStart) / Settings.PLANKTON_FADEIN_TIME;
                this.color = Color.Lerp(Color.Transparent, colors[type], (float)fadeRatio);
            }
            else
            {
                this.color = colors[type];
            }

            // Update this plankton's color to reflect new opacity
            /*
             * if (opacity >= 0 && opacity <= 255)
                color.A = (byte) opacity;
            if (opacity < 0)
                color.A = 0;
            */

            // Move based on position and velocity
            position = position + velocity;

            // Rotate based on rotational velocity
            orientation = orientation + rotationVelocity;

            // Check if this plankton is out of the circle and moving away, and if so, reset to the other side of the circle
            if (position.Length() > Settings.CIRCLE_RADIUS+Settings.CIRCLE_RADIUS_OVERSCAN && (position+velocity).Length() > position.Length())
            {
                position.X = -position.X;
                position.Y = -position.Y;
            }
        }

        public void FadeOut(GameTime gameTime)
        {
            fadeStart = gameTime.TotalGameTime.TotalMilliseconds;
            fadeType = "out";
        }

        public void FadeIn(GameTime gameTime)
        {
            fadeStart = gameTime.TotalGameTime.TotalMilliseconds;
            fadeType = "in";
        }
    }
}