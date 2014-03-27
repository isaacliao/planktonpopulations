using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Graphics;
using C3.XNA;


namespace PlanktonPopulations
{
    /// <summary>
    /// Holds all state for time indicators, and draws them.
    /// </summary>
    class Timeline
    {
        private RenderTarget2D clockTarget;
        private Texture2D clockTexture;
        private GraphicsDevice graphicsDevice;
        private Video video;
        private VideoPlayer player;
        private int[] staticXPositions = new int[12];
        private string[] monthNames;
        public static string[] FourMonthNames = { "Jan", "Apr", "Jul", "Oct" };
        public static string[] MonthNamesShort = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Jan" };
        public static string[] MonthNamesFull = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        public Timeline(GraphicsDevice graphicsDevice, Video video, VideoPlayer player)
        {
            this.graphicsDevice = graphicsDevice;
            this.video = video;
            this.player = player;

            // Initialize clock texture
            clockTexture = new Texture2D(graphicsDevice, 4 * Settings.TIMELINE_CIRCULAR_RADIUS, 4 * Settings.TIMELINE_CIRCULAR_RADIUS);
            clockTarget = new RenderTarget2D(graphicsDevice, 4 * Settings.TIMELINE_CIRCULAR_RADIUS, 4 * Settings.TIMELINE_CIRCULAR_RADIUS, false, graphicsDevice.DisplayMode.Format, DepthFormat.Depth24, 16, RenderTargetUsage.PlatformContents);

            // Initialize x-position array
            if (Settings.TIMELINE_MONTHNAME_STATIC)
            {
                Settings.TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH = 0;
                Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH = 0;
                Settings.TIMELINE_MONTHNAME_CENTER_WIDTH = Settings.TIMELINE_MONTHNAME_STATIC_SPACING;
                Settings.TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH = Settings.TIMELINE_MONTHNAME_CENTER_WIDTH/2;
                int startX = (Settings.RESOLUTION_X - 11 * Settings.TIMELINE_MONTHNAME_STATIC_SPACING) / 2;
                for (int i = 0; i < 12; i++)
                {
                    staticXPositions[i] = startX + i * Settings.TIMELINE_MONTHNAME_STATIC_SPACING;
                }
            }

            // Choose which set of month names to use
            this.monthNames = MonthNamesShort;
        }

        public void Update()
        {
            graphicsDevice.SetRenderTarget(clockTarget);
            graphicsDevice.Clear(Color.Black);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState, SamplerState.LinearClamp, null, null);
            float timelineX = Settings.TIMELINE_CIRCULAR_RADIUS * 2;
            float timelineY = Settings.TIMELINE_CIRCULAR_RADIUS * 2;
            Vector2 timelineCenterPosition = new Vector2(timelineX, timelineY);
            spriteBatch.DrawCircle(timelineX, timelineY, (float)Settings.TIMELINE_CIRCULAR_RADIUS, 64, Color.Gray, 1.0F);
            float playPositionAngle = ((float)player.PlayPosition.Ticks % ((float)video.Duration.Ticks / 6.0F)) / ((float)video.Duration.Ticks / 6.0F) * 2.0F * (float)Math.PI + (float)Math.PI * 3.0F / 2.0F;
            playPositionAngle = playPositionAngle % ((float)Math.PI * 2);
            spriteBatch.DrawLine(new Vector2(timelineX, timelineY), Settings.TIMELINE_CIRCULAR_RADIUS, playPositionAngle, Color.White);

            for (int i = 0; i < 13; i += 1) // iterate over a year in increments of 1 month
            {
                double monthAngle = i / 12.0 * 2.0 * Math.PI + (float)Math.PI * 3.0F / 2.0F;
                monthAngle = monthAngle % (Math.PI * 2);
                Vector2 monthPosition = new Vector2((float)Math.Cos(monthAngle) * (Settings.TIMELINE_CIRCULAR_RADIUS - 10), (float)Math.Sin(monthAngle) * (Settings.TIMELINE_CIRCULAR_RADIUS - 10)) + timelineCenterPosition;
                Color monthNameColor = Color.Gray;
                if (Math.Abs(monthAngle - playPositionAngle) <= (Math.PI * 2.0 / 12.0) || Math.Abs(monthAngle + (Math.PI * 2) - playPositionAngle) <= (Math.PI * 2.0 / 12.0))
                    monthNameColor = Color.White;
                if (i % 3 == 0)
                {
                    spriteBatch.DrawLine(monthPosition, 10.0F, (float)monthAngle, monthNameColor);
                    float fontOffsetX = PlanktonPopulations.mediumFont.MeasureString(MonthNamesShort[i]).X / 2;
                    float fontOffsetY = PlanktonPopulations.mediumFont.MeasureString(MonthNamesShort[i]).Y / 2;
                    Vector2 monthNamePosition = new Vector2((float)Math.Cos(monthAngle) * (Settings.TIMELINE_CIRCULAR_RADIUS + 20) - fontOffsetX, (float)Math.Sin(monthAngle) * (Settings.TIMELINE_CIRCULAR_RADIUS + 12 + 20 * (float)Math.Abs(Math.Cos(monthAngle))) - fontOffsetY) + timelineCenterPosition;
                    spriteBatch.DrawString(PlanktonPopulations.mediumFont, MonthNamesShort[i], monthNamePosition, monthNameColor);
                }
            }
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
            clockTexture = (Texture2D)clockTarget;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw a playback circle if specified
            if (Settings.TIMELINE_CIRCULAR)
            {
                float timelineX = graphicsDevice.Viewport.X + Settings.TIMELINE_CIRCULAR_X - Settings.TIMELINE_CIRCULAR_RADIUS * 2;
                float timelineY = graphicsDevice.Viewport.Y + graphicsDevice.Viewport.Height - Settings.TIMELINE_CIRCULAR_Y - Settings.TIMELINE_CIRCULAR_RADIUS * 2;
                Vector2 clockPosition = new Vector2(timelineX, timelineY);
                spriteBatch.Draw(clockTexture, clockPosition, Color.White);
                if (Settings.TIMELINE_MIRROR)
                {
                    timelineX = graphicsDevice.Viewport.Width - Settings.TIMELINE_CIRCULAR_X;
                    timelineY = graphicsDevice.Viewport.Y + Settings.TIMELINE_CIRCULAR_Y;
                    clockPosition = new Vector2(timelineX, timelineY);
                    spriteBatch.Draw(clockTexture, clockPosition, null, Color.White, (float)Math.PI, new Vector2(Settings.TIMELINE_CIRCULAR_RADIUS * 2, Settings.TIMELINE_CIRCULAR_RADIUS * 2), 1f, SpriteEffects.None, 0f);
                }
            }
            if (Settings.TIMELINE_LINEAR)
            {
                // Draw a playback timeline
                float timelineX1 = graphicsDevice.Viewport.X + Settings.TIMELINE_X;
                float timelineX2 = graphicsDevice.Viewport.X + graphicsDevice.Viewport.Width - Settings.TIMELINE_X;
                float timelineY1 = graphicsDevice.Viewport.Y + graphicsDevice.Viewport.Height - Settings.TIMELINE_Y;
                float timelineY2 = timelineY1;
                spriteBatch.DrawLine(timelineX1, timelineY1, timelineX2, timelineY2, Color.Gray);

                // Draw a line indicating current playback time
                float playPositionNormalized = (float)player.PlayPosition.Ticks / (float)video.Duration.Ticks;
                if (Settings.TIMELINE_ONE_YEAR)
                    playPositionNormalized = ((float)player.PlayPosition.Ticks % ((float)video.Duration.Ticks / 6)) / ((float)video.Duration.Ticks / 6);
                float scrubberX = playPositionNormalized * (timelineX2 - timelineX1) + timelineX1;
                spriteBatch.DrawLine(scrubberX, timelineY1 + Settings.TIMELINE_SCRUBBER_HEIGHT / 2, scrubberX, timelineY1 - Settings.TIMELINE_SCRUBBER_HEIGHT / 2, Color.White, Settings.TIMELINE_SCRUBBER_WIDTH);

                // Draw hash marks for months and write month names
                if (Settings.TIMELINE_ONE_YEAR)
                {
                    int monthCounter = 0;
                    for (int i = 0; i < 13; i++) // iterate over entire timespan in increments of 1 month
                    {
                        float monthPositionNormalized = i / 12.0F;
                        float monthX = monthPositionNormalized * (timelineX2 - timelineX1) + timelineX1;
                        spriteBatch.DrawLine(monthX, timelineY1 + Settings.TIMELINE_MONTH_HASH_HEIGHT / 2, monthX, timelineY1 - Settings.TIMELINE_MONTH_HASH_HEIGHT / 2, Color.Gray);
                        float fontOffsetX = PlanktonPopulations.mediumFont.MeasureString(MonthNamesShort[monthCounter]).X / 2;
                        float fontOffsetY = PlanktonPopulations.mediumFont.MeasureString(MonthNamesShort[monthCounter]).Y;
                        Color monthNameColor = Color.Gray;
                        if (Math.Abs(monthPositionNormalized - playPositionNormalized) < (1.0 / 12.0))
                            monthNameColor = Color.White;
                        spriteBatch.DrawString(PlanktonPopulations.mediumFont, MonthNamesShort[monthCounter], new Vector2(monthX - fontOffsetX, timelineY1 - Settings.TIMELINE_MONTH_NAME_OFFSET - fontOffsetY), monthNameColor);
                        if (monthCounter >= (MonthNamesShort.Length - 1))
                            monthCounter = 0;
                        else
                            monthCounter++;
                    }
                }
                else
                {
                    int monthCounter = 0;
                    for (int i = 52704; i < 210385; i += 6570) // iterate over entire timespan in increments of 3 months
                    {
                        float monthPositionNormalized = (i - 52704.0F) / (210384.0F - 52704.0F);
                        float monthX = monthPositionNormalized * (timelineX2 - timelineX1) + timelineX1;
                        spriteBatch.DrawLine(monthX, timelineY1 + Settings.TIMELINE_MONTH_HASH_HEIGHT / 2, monthX, timelineY1 - Settings.TIMELINE_MONTH_HASH_HEIGHT / 2, Color.Gray);
                        float fontOffsetX = PlanktonPopulations.mediumFont.MeasureString(FourMonthNames[monthCounter]).X / 2;
                        float fontOffsetY = PlanktonPopulations.mediumFont.MeasureString(FourMonthNames[monthCounter]).Y;
                        Color monthNameColor = Color.Gray;
                        if (Math.Abs(monthPositionNormalized - playPositionNormalized) < (1.0 / 24.0))
                            monthNameColor = Color.White;
                        spriteBatch.DrawString(PlanktonPopulations.mediumFont, FourMonthNames[monthCounter], new Vector2(monthX - fontOffsetX, timelineY1 - Settings.TIMELINE_MONTH_NAME_OFFSET - fontOffsetY), monthNameColor);
                        if (monthCounter >= (FourMonthNames.Length - 1))
                            monthCounter = 0;
                        else
                            monthCounter++;
                    }
                }
            }
            if (Settings.TIMELINE_MONTHNAME)
            {
                Vector2 screenMax = new Vector2(Settings.RESOLUTION_X, Settings.RESOLUTION_Y);
                // entire dataset spans 6 years, figure out which month we're in
                float yearProportion = ((float)player.PlayPosition.Ticks % ((float)video.Duration.Ticks / 6.0F)) / ((float)video.Duration.Ticks / 6.0F);
                int monthIndex = (int)Math.Floor(yearProportion * 12);

                // If static, draw a triangle indicating what part of the year we're in
                float markerXPos = yearProportion * Settings.TIMELINE_MONTHNAME_STATIC_SPACING * 12 + (Settings.RESOLUTION_X - Settings.TIMELINE_MONTHNAME_STATIC_SPACING * 12) / 2;
                float markerYPos = Settings.RESOLUTION_Y - Settings.TIMELINE_MONTHNAME_Y + Settings.TIMELINE_MONTHNAME_STATIC_MARKER_OFFSET;
                int markerHeight = 8;
                int markerWidth = 4;
                for (int i = 0; i < markerWidth; i++)
                {
                    spriteBatch.DrawLine(new Vector2(markerXPos, markerYPos), new Vector2(markerXPos - markerWidth / 2 + i, markerYPos + markerHeight), Color.White);
                }
                if (Settings.TIMELINE_MIRROR)
                {
                    markerXPos = Settings.RESOLUTION_X - markerXPos;
                    markerYPos = Settings.RESOLUTION_Y - markerYPos;
                    for (int i = 0; i < markerWidth; i++)
                    {
                        spriteBatch.DrawLine(new Vector2(markerXPos, markerYPos), new Vector2(markerXPos - markerWidth / 2 + i, markerYPos - markerHeight), Color.White);
                    }
                }

                // figure out where in that month we are
                float monthProportion = yearProportion % (1f / 12f) / (1f / 12f);
                //int colorByte = (int) MathHelper.Lerp(255F, 0F, monthProportion);
                //Color monthNameColor = new Color(255, 255, 255, colorByte);

                Color currentMonthColor = Settings.TIMELINE_MONTHNAME_CURRENT_COLOR;
                Color backgroundMonthColor = Settings.TIMELINE_MONTHNAME_OTHER_COLOR;
                Vector3 bgColorVector = backgroundMonthColor.ToVector3();
                Vector3 currentColorVector = currentMonthColor.ToVector3();
                Vector2 mediumFontSize = PlanktonPopulations.mediumFont.MeasureString(monthNames[monthIndex]);
                Vector2 largeFontSize = PlanktonPopulations.largeFont.MeasureString(monthNames[monthIndex]);
                float minScale = mediumFontSize.X / largeFontSize.X;
                float scale = 1;

                // Calculate x-position of center of current month name
                float xPos = (float)Settings.RESOLUTION_X / 2 + (float)Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2 - monthProportion * (float)Settings.TIMELINE_MONTHNAME_CENTER_WIDTH;
                Vector2 position = new Vector2(xPos, Settings.RESOLUTION_Y - Settings.TIMELINE_MONTHNAME_Y);

                // Check if growing or shrinking
                if (xPos > (Settings.RESOLUTION_X / 2 + Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2 - Settings.TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH))
                {
                    float proportion = (Settings.RESOLUTION_X / 2 + Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2 - xPos) / Settings.TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH;
                    scale = MathHelper.SmoothStep(minScale, 1, proportion);
                    //currentMonthColor = Color.Lerp(backgroundMonthColor, currentMonthColor, proportion);
                    currentMonthColor = new Color(Vector3.SmoothStep(bgColorVector, currentColorVector, proportion));
                }
                else if (xPos < (Settings.RESOLUTION_X / 2 - Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2 + Settings.TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH))
                {
                    float proportion = (xPos - (Settings.RESOLUTION_X / 2 - Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2)) / Settings.TIMELINE_MONTHNAME_CENTER_TRANSITION_WIDTH;
                    scale = MathHelper.Lerp(minScale, 1, proportion);
                    currentMonthColor = Color.Lerp(backgroundMonthColor, currentMonthColor, proportion);
                }
                Vector2 offset;
                if (Settings.TIMELINE_MONTHNAME_EXPAND_FROM_BASELINE)
                {
                    offset = largeFontSize * scale / 2;
                    offset.Y = largeFontSize.Y * scale - mediumFontSize.Y / 2;

                    // Try to keep enlarging fonts at the same baseline as the smaller background fonts
                    float proportion = (scale - minScale) / (1 - minScale);
                    offset.Y -= proportion * largeFontSize.Y * 0.12f; // Last term is percent of large font height that baseline is at; 12% seems to work for Explo font
                }
                else
                {
                    offset = largeFontSize * scale / 2;
                }
                if (Settings.TIMELINE_MONTHNAME_STATIC)
                {
                    position.X = staticXPositions[monthIndex];
                }
                spriteBatch.DrawString(PlanktonPopulations.largeFont, monthNames[monthIndex], position - offset, currentMonthColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                if (Settings.TIMELINE_MIRROR)
                    spriteBatch.DrawString(PlanktonPopulations.largeFont, monthNames[monthIndex], screenMax - (position - offset), currentMonthColor, (float)Math.PI, Vector2.Zero, scale, SpriteEffects.None, 0);

                // Calculate positions of month names to the right of center
                float otherXPos = (float)Settings.RESOLUTION_X / 2 + (float)Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2 + (1 - monthProportion) * (float)Settings.TIMELINE_MONTHNAME_SPACING;
                int otherMonthIndex = (monthIndex + 1) % 12;
                Color otherMonthColor;
                while ((otherXPos < Settings.RESOLUTION_X - Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH) || (Settings.TIMELINE_MONTHNAME_STATIC && otherMonthIndex > monthIndex))
                {
                    mediumFontSize = PlanktonPopulations.mediumFont.MeasureString(monthNames[otherMonthIndex]);
                    largeFontSize = PlanktonPopulations.largeFont.MeasureString(monthNames[otherMonthIndex]);
                    minScale = mediumFontSize.X / largeFontSize.X;
                    scale = minScale;
                    position.X = otherXPos;
                    // Check if fading in 
                    if (otherXPos > (Settings.RESOLUTION_X - Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH - Settings.TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH))
                    {
                        otherMonthColor = Color.Lerp(Color.Black, backgroundMonthColor, (float)(Settings.RESOLUTION_X - otherXPos - Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH) / (float)Settings.TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH);
                    }
                    // Check if enlarging
                    //else if ((otherXPos - RESOLUTION_X/2) < (centerWidth/2)) {
                    //    float proportion = (centerWidth/2 - (otherXPos - RESOLUTION_X/2)) / centerTransitionWidth;
                    //    scale = MathHelper.Lerp(minScale, 1, proportion);
                    //    otherMonthColor = Color.Lerp(currentMonthColor, backgroundMonthColor, proportion);
                    //}
                    else
                    {
                        otherMonthColor = backgroundMonthColor;
                    }
                    if (Settings.TIMELINE_MONTHNAME_STATIC)
                    {
                        position.X = staticXPositions[otherMonthIndex];
                        otherMonthColor = backgroundMonthColor;
                    }
                    spriteBatch.DrawString(PlanktonPopulations.largeFont, monthNames[otherMonthIndex], position - largeFontSize * scale / 2, otherMonthColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                    if (Settings.TIMELINE_MIRROR)
                        spriteBatch.DrawString(PlanktonPopulations.largeFont, monthNames[otherMonthIndex], screenMax - (position - largeFontSize * scale / 2), otherMonthColor, (float)Math.PI, Vector2.Zero, scale, SpriteEffects.None, 0);

                    otherMonthIndex = (otherMonthIndex + 1) % 12;
                    otherXPos += Settings.TIMELINE_MONTHNAME_SPACING;
                }

                // Calculate positions of month names to the left of center
                otherXPos = (float)Settings.RESOLUTION_X / 2 - (float)Settings.TIMELINE_MONTHNAME_CENTER_WIDTH / 2 - monthProportion * (float)Settings.TIMELINE_MONTHNAME_SPACING;
                otherMonthIndex = (monthIndex - 1);
                if (otherMonthIndex < 0)
                    otherMonthIndex += 12;
                while ((otherXPos > Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH) || (Settings.TIMELINE_MONTHNAME_STATIC && otherMonthIndex < monthIndex))
                {
                    mediumFontSize = PlanktonPopulations.mediumFont.MeasureString(monthNames[otherMonthIndex]);
                    largeFontSize = PlanktonPopulations.largeFont.MeasureString(monthNames[otherMonthIndex]);
                    minScale = mediumFontSize.X / largeFontSize.X;
                    scale = minScale;
                    position.X = otherXPos;
                    // Check if fading out
                    if (otherXPos < (Settings.TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH + Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH))
                    {
                        otherMonthColor = Color.Lerp(Color.Black, backgroundMonthColor, (float)(otherXPos - Settings.TIMELINE_MONTHNAME_BLANK_EDGE_WIDTH) / (float)Settings.TIMELINE_MONTHNAME_EDGE_TRANSITION_WIDTH);
                    }
                    // Check if shrinking
                    //else if (otherXPos > RESOLUTION_X / 2 - centerWidth / 2 - centerTransitionWidth)
                    //{
                    //    float proportion = (RESOLUTION_X / 2 - centerWidth / 2 - otherXPos) / centerTransitionWidth;
                    //    scale = MathHelper.Lerp(1, minScale, proportion);
                    //    otherMonthColor = Color.Lerp(currentMonthColor, backgroundMonthColor, proportion);
                    //}
                    else
                    {
                        otherMonthColor = backgroundMonthColor;
                    }
                    if (Settings.TIMELINE_MONTHNAME_STATIC)
                    {
                        position.X = staticXPositions[otherMonthIndex];
                        otherMonthColor = backgroundMonthColor;
                    }
                    spriteBatch.DrawString(PlanktonPopulations.largeFont, monthNames[otherMonthIndex], position - largeFontSize * scale / 2, otherMonthColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                    if (Settings.TIMELINE_MIRROR)
                        spriteBatch.DrawString(PlanktonPopulations.largeFont, monthNames[otherMonthIndex], screenMax - (position - largeFontSize * scale / 2), otherMonthColor, (float)Math.PI, Vector2.Zero, scale, SpriteEffects.None, 0);

                    otherMonthIndex = (otherMonthIndex - 1);
                    if (otherMonthIndex < 0)
                        otherMonthIndex += 12;
                    otherXPos -= Settings.TIMELINE_MONTHNAME_SPACING;
                }
            }
        }
    }
}
