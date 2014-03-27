using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
//using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using C3.XNA;

namespace PlanktonPopulations
{
    public class ZoomCircle : MovableObject
    {
        public static Texture2D ZoomCircleImage;
        List<Plankton> planktonList = new List<Plankton>();
        public List<Readout> readoutList = new List<Readout>();
        int[] planktonCounts = { 0, 0, 0, 0 };
        int[] planktonTaggedForDeletion = { 0, 0, 0, 0 };
        int[] targetPlanktonCounts = { 0, 0, 0, 0 };
        private Vector2 _position;
        public Vector2 position
        {
            get
            {
                if (this.antiJitter)
                    return _antiJitterPosition;
                else
                    return _position;
            }
            set
            {
                _position = value;
            }
        }
        private float velocity;
        public bool isExpiring = false;
        public double expirationStartTime;
        private enum VelocityState { SLOW, MEDIUM, PRESLOW };
        private VelocityState myVelocityState;
        private double creationTime, slowStartTime, mediumStartTime, preslowStartTime, currentTime;
        public Vector2 offsetPosition;
        //public Vector2 destination;
        public Texture2D planktonTexture, calloutTexture, lensmapTexture;
        RenderTarget2D planktonTarget, lensmapTarget;
        public Color fadeColor;        // used for fadein / fadeout
        double fadeStart;
        public string fadeType;
        public float fadeRatio;
        private float currentOpacity, slowStartOpacity, mediumStartOpacity;
        private double circleCreationTime;
        public long id;
        public int dominantType; // the dominant type of plankton at this location, 0-3
        public bool overLand;
        public float angle; // when drawing an offset circle, the angle that the larger circle is relative to the smaller circle
        public Rectangle[] infoTabs = new Rectangle[4];
        private Dictionary<string, float> dataValues = new Dictionary<string, float>();
        private Dictionary<string, float> nextDataValues = new Dictionary<string, float>();
        private float MEDIUM_OPACITY_CHANGE_PER_MS, SLOW_OPACITY_CHANGE_PER_MS;
        private Texture2D zoomTexture;
        private float myStartSize = Settings.OFFSET_RADIUS * 2;
        private float myEndSize = (Settings.CIRCLE_RADIUS) * 2;
        private float zoomCurrentSize;
        private float zoomLerpProgress;
        GraphicsDevice graphicsDevice;
        private bool antiJitter = false;
        private Vector2 _antiJitterPosition;
        public Guide AttachedGuide;
        public static RenderTarget2D MaskTarget;
        public static int ZoomCircleWidth = 25;
        Vector2 ZoomCircleImageOffset = new Vector2(8, 5);

        public ZoomCircle(GameTime gameTime, long id)
        {
            this.id = id;
            this.circleCreationTime = gameTime.TotalGameTime.TotalMilliseconds;
            this.graphicsDevice = PlanktonPopulations.graphicsDeviceManager.GraphicsDevice;
            CreateRenderTargets();
            if (Settings.SHOW_LIGHT)
                readoutList.Add(new Readout("PAR", graphicsDevice));
            if (Settings.SHOW_NITRATE)
                readoutList.Add(new Readout("NO3", graphicsDevice));
            if (Settings.SHOW_TEMP)
                readoutList.Add(new Readout("T", graphicsDevice));
            if (Settings.SHOW_SILICA)
                readoutList.Add(new Readout("SiO2", graphicsDevice));

            this.FadeIn(gameTime);
            this.myVelocityState = VelocityState.SLOW;
            this.slowStartTime = gameTime.TotalGameTime.TotalMilliseconds;
            this.creationTime = gameTime.TotalGameTime.TotalMilliseconds;
            this.currentOpacity = 255;
            this.MEDIUM_OPACITY_CHANGE_PER_MS = (255 - Settings.CROSSHAIRS_MEDIUM_OPACITY) / Settings.CROSSHAIRS_ON_MEDIUM_FADE_TIME;
            this.SLOW_OPACITY_CHANGE_PER_MS = (255 - Settings.CROSSHAIRS_MEDIUM_OPACITY) / Settings.CROSSHAIRS_ON_SLOW_FADE_TIME;
            this.AttachedGuide = new Guide(this);
        }

        public void CreateRenderTargets()
        {
            if (Settings.TOUCHONLY)
            {
                //planktonTarget = new RenderTarget2D(graphicsDevice, (int)(Settings.CIRCLE_RADIUS - ZoomCircleWidth + PlanktonPopulations.ArrowsOffset.Y) * 2, (int)(Settings.CIRCLE_RADIUS - ZoomCircleWidth + PlanktonPopulations.ArrowsOffset.Y) * 2, false, graphicsDevice.DisplayMode.Format, DepthFormat.Depth24, 16, RenderTargetUsage.PlatformContents);
                planktonTarget = new RenderTarget2D(graphicsDevice, (int)(Settings.CIRCLE_RADIUS - ZoomCircleWidth) * 2, (int)(Settings.CIRCLE_RADIUS - ZoomCircleWidth) * 2, false, graphicsDevice.DisplayMode.Format, DepthFormat.Depth24, 16, RenderTargetUsage.PlatformContents);
                lensmapTarget = new RenderTarget2D(graphicsDevice, planktonTarget.Width, planktonTarget.Height, false, graphicsDevice.DisplayMode.Format, DepthFormat.Depth24, 16, RenderTargetUsage.PlatformContents);
            }
            else
            {
                planktonTarget = new RenderTarget2D(graphicsDevice, (int)(Settings.CIRCLE_RADIUS + Settings.CIRCLE_RADIUS_OVERSCAN) * 2, (int)(Settings.CIRCLE_RADIUS + Settings.CIRCLE_RADIUS_OVERSCAN) * 2, false, graphicsDevice.DisplayMode.Format, DepthFormat.Depth24, 16, RenderTargetUsage.PlatformContents);
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime)
        {
            this.currentTime = gameTime.TotalGameTime.TotalMilliseconds;

            if ((currentTime - circleCreationTime) < Settings.CROSSHAIRS_ON_RING_DOWN_ZOOM_TIME)
            {
                // Circle is zooming in

                //// Move circle if necessary
                //if ((position - destination).Length() > LivingLiquid.CIRCLE_VELOCITY)
                //{
                //    Vector2 movement = (destination - position);
                //    movement.Normalize();
                //    movement *= LivingLiquid.CIRCLE_VELOCITY;
                //    position += movement;
                //}
                //else
                //{
                //    position = destination;
                //}

                // Linear interpolation factor
                zoomLerpProgress = ((float)gameTime.TotalGameTime.TotalMilliseconds - (float)circleCreationTime) / (float)Settings.CROSSHAIRS_ON_RING_DOWN_ZOOM_TIME;

                // Calculate current size of zooming circle
                //zoomCurrentSize = (myEndSize - myStartSize) * zoomLerpProgress;
                zoomCurrentSize = MathHelper.SmoothStep(myStartSize, myEndSize, zoomLerpProgress);
                if (zoomCurrentSize >= myEndSize)
                    zoomCurrentSize = myEndSize;

                CreateZoomingTextures();
            }
            else if (this.isExpiring && (currentTime - expirationStartTime > Settings.CROSSHAIRS_RING_UP_DELAY_TIME))
            {
                // Circle is zooming out
                //position = destination;

                // Linear interpolation factor
                zoomLerpProgress = ((float)gameTime.TotalGameTime.TotalMilliseconds - (float)expirationStartTime - Settings.CROSSHAIRS_RING_UP_DELAY_TIME) / (float)Settings.CROSSHAIRS_ON_RING_UP_ZOOM_TIME;

                // Calculate current size of zooming circle
                //zoomCurrentSize = (myEndSize - myStartSize) * (1.0f-zoomLerpProgress);
                zoomCurrentSize = MathHelper.SmoothStep(myEndSize, myStartSize, zoomLerpProgress);
                //Debug.WriteLine(zoomCurrentSize);
                if ((int)zoomCurrentSize <= myStartSize)
                    zoomCurrentSize = myStartSize;

                CreateZoomingTextures();
            }
            else
            {
                // Circle is not zooming, calculate opacity based on velocity
                int mediumVelocityThreshold;
                if (Settings.TOUCHONLY)
                {
                    mediumVelocityThreshold = Settings.TOUCHONLY_MEDIUM_THRESHOLD_VELOCITY;
                }
                else
                {
                    mediumVelocityThreshold = Settings.CROSSHAIRS_MEDIUM_THRESHOLD_VELOCITY;
                }
                // Update velocity state
                if (this.velocity >= mediumVelocityThreshold)
                {
                    if (!myVelocityState.Equals(VelocityState.MEDIUM))
                    {
                        mediumStartTime = gameTime.TotalGameTime.TotalMilliseconds;
                        mediumStartOpacity = currentOpacity;
                        myVelocityState = VelocityState.MEDIUM;
                        Debug.WriteLine("Transitioning to MEDIUM");
                        this.antiJitter = false;
                        Debug.WriteLine("Stopping anti-jitter");
                    }
                    //Debug.WriteLine(currentTime - mediumStartTime);
                }
                else
                {
                    if (myVelocityState.Equals(VelocityState.MEDIUM))
                    {
                        preslowStartTime = gameTime.TotalGameTime.TotalMilliseconds;
                        myVelocityState = VelocityState.PRESLOW;
                        Debug.WriteLine("Transitioning to PRESLOW");
                    }
                    else if (myVelocityState.Equals(VelocityState.PRESLOW) && ((currentTime - preslowStartTime) > Settings.CROSSHAIRS_SLOW_DELAY_TIME))
                    {
                        slowStartTime = gameTime.TotalGameTime.TotalMilliseconds;
                        slowStartOpacity = currentOpacity;
                        myVelocityState = VelocityState.SLOW;
                        Debug.WriteLine("Transitioning to SLOW");
                    }
                    //Debug.WriteLine(currentTime - slowStartTime);
                }

                // Update opacity of zoomed circle contents
                if (myVelocityState.Equals(VelocityState.MEDIUM))
                {

                    // Fade towards CROSSHAIRS_MEDIUM_OPACITY
                    float opacityProgress = (255f - currentOpacity) / (float)(255f - (float)Settings.CROSSHAIRS_MEDIUM_OPACITY);
                    //Debug.WriteLine(currentOpacity);
                    if (opacityProgress < 1)
                    {
                        float lerpAmount = (float)(currentTime - mediumStartTime) / (float)(Settings.CROSSHAIRS_ON_MEDIUM_FADE_TIME - (Settings.CROSSHAIRS_ON_MEDIUM_FADE_TIME * opacityProgress));
                        currentOpacity = MathHelper.Lerp(mediumStartOpacity, Settings.CROSSHAIRS_MEDIUM_OPACITY, lerpAmount);
                    }
                }
                else if (myVelocityState.Equals(VelocityState.SLOW))
                {

                    // Fade towards 255 (opaque)
                    float opacityProgress = (currentOpacity - (float)Settings.CROSSHAIRS_MEDIUM_OPACITY) / (float)(255f - (float)Settings.CROSSHAIRS_MEDIUM_OPACITY);
                    //Debug.WriteLine(currentOpacity);
                    if (opacityProgress < 1)
                    {
                        float lerpAmount = (float)(currentTime - slowStartTime) / (float)(Settings.CROSSHAIRS_ON_SLOW_FADE_TIME - (Settings.CROSSHAIRS_ON_SLOW_FADE_TIME * opacityProgress));
                        currentOpacity = MathHelper.Lerp(slowStartOpacity, 255, lerpAmount);
                    }
                    // If we've been in this state longer than a certain amount of time, stop updating apparent position until circle is moved fast enough to change state
                    if (!this.antiJitter && currentTime - slowStartTime > Settings.CROSSHAIRS_ANTI_JITTER_DELAY && !Settings.TOUCHONLY)
                    {
                        Debug.WriteLine("Starting anti-jitter");
                        this._antiJitterPosition = this.position;
                        this.antiJitter = true;
                    }
                }
                if (currentOpacity > 255)
                    currentOpacity = 255;
                else if (currentOpacity < Settings.CROSSHAIRS_MEDIUM_OPACITY)
                    currentOpacity = Settings.CROSSHAIRS_MEDIUM_OPACITY;

                //// NOTE: commented out ring fading because it's been replaced by zooming animation
                //// Linearly interpolate transparency color if fading in or out
                //if (fadeType == "none")
                //{
                //    fadeRatio = 1;
                //    this.fadeColor = Color.White;
                //}
                //else if (fadeType == "in")
                //{
                //    if (LivingLiquid.CIRCLE_ZOOM_TRANSITION)
                //    {
                //        fadeRatio = 1;
                //        this.fadeColor = Color.White;
                //    }
                //    else
                //    {
                //        fadeRatio = (float)(gameTime.TotalGameTime.TotalMilliseconds - fadeStart) / LivingLiquid.CIRCLE_FADEIN_TIME;
                //        if (fadeRatio > 1)
                //            fadeRatio = 1;
                //        this.fadeColor = Color.Lerp(Color.Transparent, Color.White, (float)fadeRatio);
                //    }
                //}
                //else if (fadeType == "out")
                //{
                //    fadeRatio = 1.0F - (float)(gameTime.TotalGameTime.TotalMilliseconds - fadeStart) / LivingLiquid.CIRCLE_FADEOUT_TIME;
                //    if (fadeRatio < 0)
                //        fadeRatio = 0;
                //    this.fadeColor = Color.Lerp(Color.Transparent, Color.White, (float)fadeRatio);
                //}

                //// NOTE: commented out position interpolation because we get enough position updates
                //// Move circle if necessary
                //if ((position - destination).Length() > LivingLiquid.CIRCLE_VELOCITY)
                //{
                //    Vector2 movement = (destination - position);
                //    movement.Normalize();
                //    movement *= LivingLiquid.CIRCLE_VELOCITY;
                //    position += movement;
                //}
                //else
                //{
                //    position = destination;
                //}
                
                // If circles are being drawn offset from touch point, update offset position
                if (!Settings.CROSSHAIRS_MODE)
                {
                    this.offsetPosition = this.getOffsetPosition();
                }

            }

            // If callouts being drawn, do various calculations and prepare texture
            if (Settings.SHOW_CALLOUT)
            {
                this.AttachedGuide.Update();
            }

            UpdatePlankton(gameTime);

        }

        private void CreateZoomingTextures()
        {
            // Create two render targets corresponding to the current size of the circle
            RenderTarget2D currentTarget = new RenderTarget2D(graphicsDevice, (int)(zoomCurrentSize), (int)(zoomCurrentSize));
            RenderTarget2D maskTarget = new RenderTarget2D(graphicsDevice, (int)(zoomCurrentSize), (int)(zoomCurrentSize));

            Vector2 center = new Vector2(maskTarget.Width / 2, maskTarget.Height / 2);

            // Draw circular mask texture into maskTarget
            graphicsDevice.SetRenderTarget(maskTarget);
            graphicsDevice.Clear(Color.White);
            PlanktonPopulations.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            PlanktonPopulations.spriteBatch.DrawCircle(center, zoomCurrentSize / 2 + Settings.CIRCLE_BORDER_WIDTH, 64, Color.Transparent, zoomCurrentSize / 2 + Settings.CIRCLE_BORDER_WIDTH);
            PlanktonPopulations.spriteBatch.End();
            //maskTexture = new Texture2D(GraphicsDevice, renderTargetTemp.Width, renderTargetTemp.Height);
            graphicsDevice.SetRenderTarget(null);
            Texture2D maskTexture = (Texture2D)maskTarget;

            // Calculate source rectangle
            Rectangle sourceRect;
            int sourceX = (int)((float)(position.X - (myStartSize / 2)) / PlanktonPopulations.movieScale);
            int sourceY = (int)((float)(position.Y - (myStartSize / 2) - PlanktonPopulations.movieVerticalOffset) / PlanktonPopulations.movieScale);
            sourceRect = new Rectangle(sourceX, sourceY, (int)myStartSize, (int)myStartSize);

            // Draw a small, selected part of the current frame into currentTarget, probably scaling up
            graphicsDevice.SetRenderTarget(currentTarget);
            PlanktonPopulations.spriteBatch.Begin();
            PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.currentFrame, currentTarget.Bounds, sourceRect, Color.White);
            PlanktonPopulations.spriteBatch.End();

            // Subtract mask texture from the render target
            PlanktonPopulations.spriteBatch.Begin(SpriteSortMode.Deferred, PlanktonPopulations.subtractAlpha);
            PlanktonPopulations.spriteBatch.Draw(maskTexture, Vector2.Zero, Color.White);
            PlanktonPopulations.spriteBatch.End();

            // Save the texture for the draw step
            graphicsDevice.SetRenderTarget(null);
            zoomTexture = (Texture2D)currentTarget;
        }

        private void UpdatePlankton(GameTime gameTime)
        {
            // For each plankton type, create new ones or delete old ones to match desired numbers
            for (int i = 0; i < 4; i++)
            {
                while (targetPlanktonCounts[i] - planktonCounts[i] > 0)
                {
                    if (PlanktonPool.AvailableCount() > 0)
                    {
                        Plankton planktonToAdd = PlanktonPool.GetPlankton();
                        planktonToAdd.Initialize(i, this.circleCreationTime, gameTime);
                        planktonList.Add(planktonToAdd);
                        planktonCounts[i]++;
                    }
                }

                while (targetPlanktonCounts[i] - planktonCounts[i] + planktonTaggedForDeletion[i] < 0)
                {
                    // tag some plankton of the correct type for deletion
                    Plankton result = planktonList.Find(
                        delegate(Plankton p)
                        {
                            return p.type == i && !p.taggedForDeletion;
                        }
                    );
                    if (result == null)
                    {
                        Console.WriteLine("Error: no matching plankton found for deletion");
                    }
                    else
                    {
                        result.taggedForDeletion = true;
                        result.FadeOut(gameTime);
                        planktonTaggedForDeletion[i]++;
                    }
                }
            }

            List<Plankton> deletionList = new List<Plankton>();
            // Update all phytoplankton
            foreach (Plankton p in planktonList)
            {
                p.Update(gameTime);
                // If plankton has become completely transparent, delete it
                if (p.color.A == 0 && p.taggedForDeletion)
                {
                    deletionList.Add(p);
                    planktonCounts[p.type]--;
                    planktonTaggedForDeletion[p.type]--;
                }
            }
            foreach (Plankton p in deletionList)
            {
                planktonList.Remove(p);
                PlanktonPool.ReturnPlankton(p);
            }
        }

        public void DrawPlankton(SpriteBatch spriteBatch, Vector2 center)
        {
            // Draw all phytoplankton in the array
            foreach (Plankton p in planktonList)
            {
                float scale;
                if (p.image.Width > p.image.Height)
                    scale = p.size / p.image.Width;
                else
                    scale = p.size / p.image.Height;
                //Vector2 drawPosition = p.position + this.position - new Vector2(p.image.Width * scale / 2, p.image.Height * scale / 2);
                Vector2 textureOrigin = new Vector2(p.image.Width / 2, p.image.Height / 2);
                Vector2 drawPosition = center + p.position - new Vector2(p.image.Width * scale / 2, p.image.Height * scale / 2);
                spriteBatch.Draw(p.image, drawPosition, null, p.color, p.orientation, textureOrigin, scale, SpriteEffects.None, 0);
                //spriteBatch.Draw(p.image,drawPosition,p.color);
            }
        }

        /// <summary>
        /// Opens this circle at the specified coordinates.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void OpenAt(int mouseX, int mouseY, float angle, int timestamp, double timestampRaw)
        {
            this.position = new Vector2(mouseX, mouseY);
            //this.destination = new Vector2(mouseX, mouseY);
            this.LoadDataAt(mouseX, mouseY, timestamp, timestampRaw);
            this.angle = angle;
        }

        public void MoveTo(int mouseX, int mouseY, float angle, int timestamp, double timestampRaw, float velocity)
        {
            this.position = new Vector2(mouseX, mouseY);
            //this.destination = new Vector2(mouseX, mouseY);
            this.LoadDataAt(mouseX, mouseY, timestamp, timestampRaw);
            this.angle = angle;
            this.velocity = velocity;

            // If we moved more than a threshold amount, set selected infoPanel to dominant plankton type
            //if ((this.destination - this.position).Length() > LivingLiquid.INFO_TAB_UPDATE_DISTANCE)
            //    this.selectedInfoPanel = dominantType;

        }

        public void LoadDataAt(int mouseX, int mouseY, int timestamp, double timestampRaw)
        {
            Rectangle movieDestination = PlanktonPopulations.movieDestination;

            // Check if input coordinates are within the movie
            if (movieDestination.Contains(mouseX, mouseY))
            {

                // Retrieve data from the selected point
                byte[] bytes = new byte[4];
                byte[] nextBytes = new byte[4];

                // Calculate file data position based on mouse x,y coordinates. Data stored is stored in a 540x270 grid.
                double movieX = mouseX - movieDestination.X;
                double movieY = movieDestination.Height - (mouseY - movieDestination.Y) - 1; // Data is vertically flipped

                // However, since we've cropped the bottom 60 lines from the 2160x1080 video, this translates to a 540x255 grid.
                long offset = (int)(movieY / (double)movieDestination.Height * 255.0) * 540;
                offset += (int)(movieX / (double)movieDestination.Width * 540);

                // Add an offset to the beginning of the file read reflecting the inaccessible cropped area (15 lines)
                offset += 15 * 540;

                //values[i] = Game1.theData[Game1.dataNames[i]][timestamp][offset];

                // Check if over land
                byte[] landBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    landBytes[i] = PlanktonPopulations.landmaskArray[offset * 4 + i];
                }
                // Convert from big-endian
                int landValue = (int)BitConverter.ToSingle(landBytes.Reverse().ToArray(), 0);
                //Debug.WriteLine(landValue);

                if (landValue == 1)
                {
                    this.overLand = true;
                }
                else
                {
                    this.overLand = false;

                    // Retrieve data
                    dataValues.Clear();
                    nextDataValues.Clear();
                    for (int i = 0; i < PlanktonPopulations.dataNames.Count; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            //bytes[j] = Game1.phygrpData[timestamp][i][offset * 4 + j];
                            bytes[j] = PlanktonPopulations.theData[PlanktonPopulations.dataNames[i]][timestamp][offset * 4 + j];
                            if (timestamp + PlanktonPopulations.TEMPORAL_RESOLUTION <= 211463)
                                nextBytes[j] = PlanktonPopulations.theData[PlanktonPopulations.dataNames[i]][timestamp + PlanktonPopulations.TEMPORAL_RESOLUTION][offset * 4 + j];
                            else
                                nextBytes = bytes;
                        }
                        // Convert from big-endian
                        dataValues.Add(PlanktonPopulations.dataNames[i], BitConverter.ToSingle(bytes.Reverse().ToArray(), 0));
                        nextDataValues.Add(PlanktonPopulations.dataNames[i], BitConverter.ToSingle(nextBytes.Reverse().ToArray(), 0));

                        // Check for NaN's
                        if (float.IsNaN(dataValues[PlanktonPopulations.dataNames[i]]))
                            dataValues[PlanktonPopulations.dataNames[i]] = 0;
                        if (float.IsNaN(nextDataValues[PlanktonPopulations.dataNames[i]]))
                            nextDataValues[PlanktonPopulations.dataNames[i]] = 0;

                        // Linear temporal interpolation
                        double timeDifference = timestampRaw - (double)timestamp;
                        double proportion = timeDifference / (double)PlanktonPopulations.TEMPORAL_RESOLUTION;
                        float valueDifference = nextDataValues[PlanktonPopulations.dataNames[i]] - dataValues[PlanktonPopulations.dataNames[i]];
                        dataValues[PlanktonPopulations.dataNames[i]] += (float)proportion * valueDifference;

                        if (i < 4)
                        {
                            // Plankton-specific operations here (assumes first 4 in dataNames are plankton)

                            // Calculate desired number of phytoplankton from input
                            targetPlanktonCounts[i] = (int)Math.Sqrt((int)(dataValues[PlanktonPopulations.dataNames[i]] * Settings.PHOSPHORUS_CONVERSIONS[i]));
                            targetPlanktonCounts[i] = (int)(targetPlanktonCounts[i] * Settings.PLANKTON_COUNT_CONVERSIONS[i]);
                            if (targetPlanktonCounts[i] < 0)
                                targetPlanktonCounts[i] = 0;
                            if (targetPlanktonCounts[i] > Settings.PLANKTON_MAX_PER_CIRCLE)
                                targetPlanktonCounts[i] = Settings.PLANKTON_MAX_PER_CIRCLE;
                            //Debug.WriteLine(targetPlanktonCounts[i]);
                            //targetPlanktonCounts[i] = 10;
                        }
                    }
                    // Assign data to readouts
                    foreach (Readout readout in readoutList)
                    {
                        readout.value = dataValues[readout.dataName];
                    }
                    // Find dominant plankton type at this location
                    int maxIndex = 0;
                    double maxValue = 0f;
                    for (int i = 0; i < 4; i++)
                    {
                        double normalizedValue = Math.Sqrt(dataValues[PlanktonPopulations.dataNames[i]]);
                        if (normalizedValue > maxValue)
                        {
                            maxIndex = i;
                            maxValue = normalizedValue;
                        }
                        // Debug.WriteLine("Type: " + i + ", Value: " + normalizedValue);
                    }
                    this.dominantType = maxIndex;

                    // Debug.WriteLine("Selected info panel: " + selectedInfoPanel);
                    // Debug.WriteLine("Dominant type: " + dominantType);
                }
            }
        }

        public void UpdateTextures()
        {
            //RenderTarget2D renderTarget = new RenderTarget2D(graphics, (int)(Game1.CIRCLE_RADIUS + Game1.CIRCLE_RADIUS_OVERSCAN) * 2, (int)(Game1.CIRCLE_RADIUS + Game1.CIRCLE_RADIUS_OVERSCAN) * 2);
            Vector2 center = new Vector2(planktonTarget.Width / 2, planktonTarget.Height / 2);
            graphicsDevice.SetRenderTarget(planktonTarget);
            SpriteBatch spriteBatch = PlanktonPopulations.spriteBatch;

            if (this.overLand)
            {
                // Clear to more transparent background color
                graphicsDevice.Clear(Settings.CIRCLE_ON_LAND_BACKGROUND_COLOR);

                // Draw an X in the circle
                // Debug.WriteLine("over land");
                //Vector2 topLeft = new Vector2(planktonTarget.Bounds.Left, planktonTarget.Bounds.Top);
                //Vector2 bottomRight = new Vector2(planktonTarget.Bounds.Right, planktonTarget.Bounds.Bottom);
                //Vector2 topRight = new Vector2(planktonTarget.Bounds.Right, planktonTarget.Bounds.Top);
                //Vector2 bottomLeft = new Vector2(planktonTarget.Bounds.Left, planktonTarget.Bounds.Bottom);
                //spriteBatch.Begin(SpriteSortMode.Immediate, null);
                //spriteBatch.DrawLine(topLeft, bottomRight, Settings.CIRCLE_BORDER_COLOR, Settings.CIRCLE_BORDER_WIDTH);
                //spriteBatch.DrawLine(topRight, bottomLeft, Settings.CIRCLE_BORDER_COLOR, Settings.CIRCLE_BORDER_WIDTH);
                //spriteBatch.End();

                // Write "No plankton on land."
                string noPlanktonString1 = "No plankton";
                string noPlanktonString2 = "on land.";
                Vector2 stringSize1 = PlanktonPopulations.smallFont.MeasureString(noPlanktonString1);
                Vector2 stringSize2 = PlanktonPopulations.smallFont.MeasureString(noPlanktonString2);
                float padding = 10f; // Move text away from center to avoid overlapping crosshairs
                spriteBatch.Begin();
                spriteBatch.DrawString(PlanktonPopulations.smallFont, noPlanktonString1, center - new Vector2(stringSize1.X / 2, stringSize1.Y + padding), Settings.CIRCLE_BORDER_COLOR);
                spriteBatch.DrawString(PlanktonPopulations.smallFont, noPlanktonString2, center - new Vector2(stringSize2.X / 2, -padding), Settings.CIRCLE_BORDER_COLOR);
                spriteBatch.End();
            }
            else
            {
                // Clear to specified background color
                graphicsDevice.Clear(Settings.CIRCLE_BACKGROUND_COLOR);
                // Draw all the plankton in this circle
                if (PlanktonPopulations.showZoomedCircleContents)
                {
                    spriteBatch.Begin();
                    this.DrawPlankton(spriteBatch, center);
                    spriteBatch.End();
                }
            }

            // Subtract mask texture from the plankton texture and store it
            //Game1.maskTexture.SetData<Color>(Game1.maskTextureArray);
            spriteBatch.Begin(SpriteSortMode.Deferred, PlanktonPopulations.subtractAlpha);
            spriteBatch.Draw(PlanktonPopulations.maskTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
            planktonTexture = (Texture2D)planktonTarget;

            // Create a background texture
            if (Settings.TOUCHONLY)
            {
                graphicsDevice.SetRenderTarget(lensmapTarget);

                // Draw the portion of the back buffer that this circle will be on
                Rectangle sourceRect = new Rectangle((int)this.position.X - planktonTarget.Width / 2, (int)this.position.Y - planktonTarget.Height / 2, planktonTarget.Width, planktonTarget.Height);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                spriteBatch.Draw(PlanktonPopulations.fullScreenTarget, Vector2.Zero, sourceRect, Color.White);
                
                // Draw the planktonTarget onto this background at the current opacity
                int drawOpacity;

                if (this.overLand)
                    drawOpacity = 255;
                else
                    drawOpacity = (int)currentOpacity;
                spriteBatch.Draw(planktonTexture, lensmapTarget.Bounds, new Color(255,255,255,drawOpacity));
                spriteBatch.End();

                // Subtract mask texture from the texture and store it
                spriteBatch.Begin(SpriteSortMode.Deferred, PlanktonPopulations.subtractAlpha);
                spriteBatch.Draw(PlanktonPopulations.maskTexture, Vector2.Zero, Color.White);
                spriteBatch.End();
                graphicsDevice.SetRenderTarget(null);
                lensmapTexture = (Texture2D)lensmapTarget;
            }
        }

        public void FadeIn(GameTime gameTime)
        {
            fadeStart = gameTime.TotalGameTime.TotalMilliseconds;
            fadeType = "in";
        }

        public void FadeOut(GameTime gameTime)
        {
            if (fadeType != "out")
            {
                fadeStart = gameTime.TotalGameTime.TotalMilliseconds;
                fadeType = "out";
            }
        }

        /// <summary>
        /// Utility method that calculates an offset position vector for drawing ZoomedCircles.
        /// </summary>
        /// <param name="position">Position vector of actual data location.</param>
        public Vector2 getOffsetPosition()
        {
            if (Settings.CROSSHAIRS_MODE)
            {
                return position;
            }
            else
            {
                if (Settings.INPUT_USE_ORIENTATION)
                {
                    //Debug.WriteLine(this.angle);
                    Vector2 offsetPosition = Vector2.Multiply(Vector2.One, Settings.OFFSET_DISTANCE + Settings.CIRCLE_RADIUS + Settings.OFFSET_RADIUS);
                    //Debug.WriteLine(offsetPosition);
                    offsetPosition = Vector2.Transform(offsetPosition, Matrix.CreateRotationZ(MathHelper.ToRadians(this.angle)));
                    //Debug.WriteLine(offsetPosition);
                    offsetPosition = offsetPosition + position;
                    return offsetPosition;
                }
                else
                {
                    Vector2 offsetPosition = position - Vector2.Multiply(Vector2.One, (float)((Settings.OFFSET_DISTANCE + Settings.CIRCLE_RADIUS + Settings.OFFSET_RADIUS) / Math.Sqrt(2)));
                    Vector2 positiveOffsetPosition = position + Vector2.Multiply(Vector2.One, (float)((Settings.OFFSET_DISTANCE + Settings.CIRCLE_RADIUS + Settings.OFFSET_RADIUS) / Math.Sqrt(2)));

                    // If part of circle would be off screen, use a different offset position
                    if (offsetPosition.X - Settings.CIRCLE_RADIUS < 0)
                        offsetPosition.X = positiveOffsetPosition.X;
                    if (offsetPosition.Y - Settings.CIRCLE_RADIUS < 0)
                        offsetPosition.Y = positiveOffsetPosition.Y;
                    return offsetPosition;
                }
            }
        }

        /// <summary>
        /// Draw the circle outlines and contents.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch to draw with.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            //if ((((currentTime - circleCreationTime) < LivingLiquid.CROSSHAIRS_ON_RING_DOWN_ZOOM_TIME) && (zoomCurrentSize < myEndSize)) || // we're zooming in, and we haven't reached the right size yet, or
            if (((currentTime - circleCreationTime) < Settings.CROSSHAIRS_ON_RING_DOWN_ZOOM_TIME) || // we're zooming in, or
                (this.isExpiring && (currentTime - expirationStartTime > Settings.CROSSHAIRS_RING_UP_DELAY_TIME))) // we're zooming out
            {
                // Circle is zooming in or out

                // Calculate current location of destination rectangle, if offset
                Vector2 offsetPosition = this.getOffsetPosition();
                Vector2 interpolatedPosition = (offsetPosition - this.position) * zoomLerpProgress + this.position;
                Rectangle destRect = new Rectangle((int)(interpolatedPosition.X - (zoomCurrentSize / 2)), (int)(interpolatedPosition.Y - (zoomCurrentSize / 2)), (int)zoomCurrentSize, (int)zoomCurrentSize);
                // Draw zooming color texture
                spriteBatch.Draw(zoomTexture, destRect, Color.White);

                // Don't want to draw entire planktonTexture because it is larger than the circle due to overscan area, so calculate the right sourceRect
                Rectangle sourceRect = new Rectangle(planktonTexture.Width / 2 - (int)Settings.CIRCLE_RADIUS, planktonTexture.Height / 2 - (int)Settings.CIRCLE_RADIUS, (int)Settings.CIRCLE_RADIUS * 2, (int)Settings.CIRCLE_RADIUS * 2);

                // Crossfade transparency
                int interpolatedAlpha = (int)MathHelper.SmoothStep(0, 255, zoomLerpProgress);
                // If zooming out, invert the alpha
                if (this.isExpiring)
                    interpolatedAlpha = 255 - interpolatedAlpha;

                // Draw plankton texture using sourceRect and zoomAlpha
                spriteBatch.Draw(planktonTexture, destRect, sourceRect, new Color(255, 255, 255, interpolatedAlpha));

                // Draw a small circle around data location
                if (!Settings.CROSSHAIRS_MODE)
                {
                    spriteBatch.DrawCircle(this.position, Settings.OFFSET_RADIUS + Settings.OFFSET_BORDER_WIDTH, 32, Settings.OFFSET_BORDER_COLOR, Settings.OFFSET_BORDER_WIDTH);
                    // Draw tangent lines between small circle and large circle
                    this.DrawTangents(this.position, this.zoomCurrentSize / 2, interpolatedPosition, Settings.OFFSET_RADIUS, Settings.OFFSET_BORDER_COLOR, spriteBatch);
                }


                // Draw a border around zoomed circle

                // Adjust border width based on zoom size
                int interpolatedBorderWidth = (int)MathHelper.SmoothStep(1, Settings.CIRCLE_BORDER_WIDTH, zoomLerpProgress);
                // If zooming out, invert the width
                if (this.isExpiring)
                    interpolatedBorderWidth = (int)Settings.CIRCLE_BORDER_WIDTH - interpolatedBorderWidth;

                spriteBatch.DrawCircle(interpolatedPosition, this.zoomCurrentSize / 2 + Settings.CIRCLE_BORDER_WIDTH, 64, Settings.CIRCLE_BORDER_COLOR, interpolatedBorderWidth);
            }
            else
            {
                // Circle not zooming in or out

                if (Settings.CROSSHAIRS_MODE) // Draw circles centered at touch or object point
                {

                    // Draw callouts if enabled
                    if (Settings.SHOW_CALLOUT)
                    {
                        // Draw the callout!
                        this.AttachedGuide.Draw(spriteBatch);
                    }

                    // Draw zoomed circle contents
                    int drawOpacity;

                    if (this.overLand)
                        drawOpacity = 255;
                    else
                        drawOpacity = (int)currentOpacity;

                    //Debug.WriteLine(drawOpacity);
                    if (Settings.TOUCHONLY)
                    {
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                        PlanktonPopulations.LensEffect.Parameters["BarrelPower"].SetValue(Settings.TOUCHONLY_LENS_POWER + (float)PlanktonPopulations.ArrowsOffset.X / 10f);
                        PlanktonPopulations.LensEffect.Parameters["Alpha"].SetValue(1f);
                        PlanktonPopulations.LensEffect.CurrentTechnique.Passes[0].Apply();

                        // Draw portion lens texture at full opacity
                        spriteBatch.Draw(this.lensmapTexture, this.position - new Vector2(this.planktonTexture.Width / 2, this.planktonTexture.Height / 2), Color.White);

                        // Draw plankton texture at current opacity
                        //PlanktonPopulations.LensEffect.Parameters["BarrelPower"].SetValue(Settings.TOUCHONLY_LENS_POWER);
                        //PlanktonPopulations.LensEffect.Parameters["Alpha"].SetValue((float)drawOpacity / 255f);
                        //PlanktonPopulations.LensEffect.Parameters["Alpha"].SetValue((float)100f / 255f);
                        //PlanktonPopulations.LensEffect.CurrentTechnique.Passes[0].Apply();

                        //spriteBatch.Draw(this.planktonTexture, this.position - new Vector2(this.planktonTexture.Width / 2, this.planktonTexture.Height / 2), new Color(255, 255, 255, drawOpacity));
                        spriteBatch.End();
                        //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, null, null);            
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
                        spriteBatch.Draw(ZoomCircleImage, this.position - new Vector2(Settings.CIRCLE_RADIUS, Settings.CIRCLE_RADIUS) - ZoomCircleImageOffset, Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(this.planktonTexture, this.position - new Vector2(this.planktonTexture.Width / 2, this.planktonTexture.Height / 2), new Color(255, 255, 255, drawOpacity));

                        // Draw a border around zoomed circle
                        Color fadeColor = Settings.CIRCLE_BORDER_COLOR;
                        fadeColor.A = (byte)((float)fadeColor.A * this.fadeRatio);
                        spriteBatch.DrawCircle(this.position, Settings.CIRCLE_RADIUS + Settings.CIRCLE_BORDER_WIDTH, 64, Settings.CIRCLE_BORDER_COLOR, Settings.CIRCLE_BORDER_WIDTH);
                    }

                    // Draw crosshairs at circle center if on land, or moving fast enough
                    if (this.overLand)
                    {
                        // Make crosshairs half as big as normal so they don't overlap with text
                        spriteBatch.DrawLine(this.position.X - Settings.CROSSHAIRS_LENGTH / 4f, this.position.Y, this.position.X + Settings.CROSSHAIRS_LENGTH / 4f, this.position.Y, Settings.CROSSHAIRS_COLOR, Settings.CROSSHAIRS_WIDTH);
                        spriteBatch.DrawLine(this.position.X, this.position.Y - Settings.CROSSHAIRS_LENGTH / 4f, this.position.X, this.position.Y + Settings.CROSSHAIRS_LENGTH / 4f, Settings.CROSSHAIRS_COLOR, Settings.CROSSHAIRS_WIDTH);
                    }
                    else
                    {
                        Color crosshairsColor = Settings.CROSSHAIRS_COLOR;
                        crosshairsColor.A = (byte)(255f * (float)(255 - currentOpacity) / (float)(255 - Settings.CROSSHAIRS_MEDIUM_OPACITY));
                        spriteBatch.DrawLine(this.position.X - Settings.CROSSHAIRS_LENGTH / 2f, this.position.Y, this.position.X + Settings.CROSSHAIRS_LENGTH / 2f, this.position.Y, crosshairsColor, Settings.CROSSHAIRS_WIDTH);
                        spriteBatch.DrawLine(this.position.X, this.position.Y - Settings.CROSSHAIRS_LENGTH / 2f, this.position.X, this.position.Y + Settings.CROSSHAIRS_LENGTH / 2f, crosshairsColor, Settings.CROSSHAIRS_WIDTH);
                        // Debug.WriteLine(LivingLiquid.CROSSHAIRS_COLOR.A);
                    }
                }
                else // Draw circles offset from touch or object point
                {
                    // Draw a small circle around data location
                    Color fadeColor = Settings.OFFSET_BORDER_COLOR;
                    fadeColor.A = (byte)((float)fadeColor.A * this.fadeRatio);
                    spriteBatch.DrawCircle(this.position, Settings.OFFSET_RADIUS + Settings.OFFSET_BORDER_WIDTH, 32, fadeColor, Settings.OFFSET_BORDER_WIDTH);

                    // Draw zoomed offset circle
                    spriteBatch.Draw(this.planktonTexture, this.offsetPosition - new Vector2(this.planktonTexture.Width / 2, this.planktonTexture.Height / 2), null, this.fadeColor, 0.0F, Vector2.Zero, 1.0F, SpriteEffects.None, 0.0F);

                    // Draw a border around zoomed circle
                    fadeColor = Settings.CIRCLE_BORDER_COLOR;
                    fadeColor.A = (byte)((float)fadeColor.A * this.fadeRatio);
                    spriteBatch.DrawCircle(this.offsetPosition, Settings.CIRCLE_RADIUS + Settings.CIRCLE_BORDER_WIDTH, 64, fadeColor, Settings.CIRCLE_BORDER_WIDTH);

                    this.DrawTangents(this.position, Settings.CIRCLE_RADIUS, this.offsetPosition, Settings.OFFSET_RADIUS, fadeColor, spriteBatch);
                }

                // Draw readouts next to zoomed circle
                float currentAngle = Settings.DASHBOARD_ORIENTATION * (float)Math.PI / 180.0F;
                foreach (Readout readout in this.readoutList)
                {
                    // Calculate readout position
                    Vector2 readoutPosition = new Vector2((float)Math.Cos(currentAngle), (float)Math.Sin(currentAngle));
                    readoutPosition = readoutPosition * (Settings.CIRCLE_RADIUS + Settings.READOUT_DISTANCE);
                    if (Settings.CROSSHAIRS_MODE)
                        readoutPosition = this.position - readoutPosition;
                    else
                        readoutPosition = this.offsetPosition - readoutPosition;
                    currentAngle += Settings.DASHBOARD_SPACING * (float)Math.PI / 180.0F;
                    //spriteBatch.DrawString(font, readout.value.ToString(), readoutPosition, Color.White);                    

                    // How much to scale readout texture
                    float scaleFactor = (float)Settings.DASHBOARD_READOUT_SIZE / (float)PlanktonPopulations.readoutImages[readout.dataName][0].Height;

                    // Center readout position
                    Vector2 drawPosition = readoutPosition - new Vector2(Settings.DASHBOARD_READOUT_SIZE / 2, Settings.DASHBOARD_READOUT_SIZE / 2);

                    // Draw the texture
                    Color fadeColor = Settings.READOUT_ICON_COLOR;
                    fadeColor.A = (byte)((float)fadeColor.A * this.fadeRatio);
                    spriteBatch.Draw(readout.texture, drawPosition, null, fadeColor, 0.0F, Vector2.Zero, scaleFactor, SpriteEffects.None, 0.0F);

                    // Draw text
                    string displayName = readout.displayNames[readout.dataName];
                    Vector2 displayStringBounds = PlanktonPopulations.mediumFont.MeasureString(displayName);
                    Vector2 textPosition = readoutPosition + new Vector2(0, Settings.READOUT_LABEL_DISTANCE) - new Vector2(displayStringBounds.X / 2, 0);

                    fadeColor = Settings.READOUT_LABEL_COLOR;
                    fadeColor.A = (byte)((float)fadeColor.A * this.fadeRatio);
                    spriteBatch.DrawString(PlanktonPopulations.mediumFont, readout.displayNames[readout.dataName], textPosition, fadeColor);
                }
            }
        }

        protected void DrawTangents(Vector2 position, double radius, Vector2 offsetPosition, double offsetRadius, Color fadeColor, SpriteBatch spriteBatch)
        {
            // Draw tangent lines from small circle to zoomed circle

            // Some crazy math using right triangles to determine tangent points
            double a = offsetRadius;
            double c = radius;
            double abc = (position - offsetPosition).Length();
            double d = Math.Sqrt((abc * abc - (c - a) * (c - a)));
            double e = Math.Sqrt(d * d + a * a);
            double theta1 = Math.Asin(Math.Abs(c - a) / abc);
            double theta2 = Math.Asin(a / e);
            double theta = theta1 + theta2;
            Matrix rotationToBeDone = Matrix.CreateRotationZ((float)theta);
            Vector2 t1 = Vector2.Transform(offsetPosition - position, rotationToBeDone);
            t1.Normalize();
            t1 = Vector2.Multiply(t1, (float)e);
            float t1length = t1.Length();

            rotationToBeDone = Matrix.CreateRotationZ(-(float)theta);
            Vector2 t3 = Vector2.Transform(offsetPosition - position, rotationToBeDone);
            t3.Normalize();
            t3 = Vector2.Multiply(t3, (float)e);

            t1 += position;
            t3 += position;

            // Now the other side
            a = radius;
            c = offsetRadius;
            d = Math.Sqrt((abc * abc - (c - a) * (c - a)));
            e = Math.Sqrt(d * d + a * a);

            double f = Math.Sqrt(d * d + c * c);
            theta = Math.Asin(Math.Abs(c - a) / abc) - Math.Asin(a / e);
            rotationToBeDone = Matrix.CreateRotationZ((float)theta);
            Vector2 t4 = Vector2.Transform(position - offsetPosition, rotationToBeDone);
            t4.Normalize();
            t4 = Vector2.Multiply(t4, (float)e);
            rotationToBeDone = Matrix.CreateRotationZ(-(float)theta);
            Vector2 t2 = Vector2.Transform(position - offsetPosition, rotationToBeDone);
            t2.Normalize();
            t2 = Vector2.Multiply(t2, (float)e);
            t2 += offsetPosition;
            t4 += offsetPosition;

            // t1: big circle clockwise tangent point
            // t4: small circle clockwise tangent point

            // t3: big circle counterclockwise tangent point
            // t2: small circle counterclockwise tangent point

            // Now offset tangent points by the border widths of the circles so they look right
            float big_adjust_ratio = ((float)radius + Settings.CIRCLE_BORDER_WIDTH) / (float)radius;
            float small_adjust_ratio = ((float)offsetRadius + Settings.OFFSET_BORDER_WIDTH) / (float)offsetRadius;

            t1 = ((t1 - offsetPosition) * big_adjust_ratio) + offsetPosition;
            t4 = ((t4 - position) * small_adjust_ratio) + position;

            // The other pair of tangent points also needs to be adjusted inward by the width of the tangent lines                    
            big_adjust_ratio = ((float)radius + Settings.CIRCLE_BORDER_WIDTH - Settings.TANGENT_WIDTH) / (float)radius;
            small_adjust_ratio = ((float)offsetRadius + Settings.OFFSET_BORDER_WIDTH - Settings.TANGENT_WIDTH) / (float)offsetRadius;

            t3 = ((t3 - offsetPosition) * big_adjust_ratio) + offsetPosition;
            t2 = ((t2 - position) * small_adjust_ratio) + position;

            // Draw the tangent lines already!
            //spriteBatch.DrawLine(t1, t4, OFFSET_BORDER_COLOR*zoomedCircle.fadeRatio, TANGENT_WIDTH);
            //spriteBatch.DrawLine(t3, t2, OFFSET_BORDER_COLOR * zoomedCircle.fadeRatio, TANGENT_WIDTH);
            spriteBatch.DrawLine(t1, t4, fadeColor, Settings.TANGENT_WIDTH);
            spriteBatch.DrawLine(t3, t2, fadeColor, Settings.TANGENT_WIDTH);
        }

        public void ReturnPlankton()
        {
            foreach (Plankton p in planktonList)
            {
                PlanktonPool.ReturnPlankton(p);
            }
        }
    }
}
