using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using C3.XNA;

namespace PlanktonPopulations
{
    /// <summary>
    /// Holds all state for a guide and its open/close tab, and draws them. Doubly linked to a ZoomCircle (they should have references to each other).
    /// </summary>
    public class Guide
    {
        public ZoomCircle ParentZoomCircle;
        public bool IsLeftward;
        public bool IsUpsideDown;
        public enum GuideState { CLOSED, OPENING, OPEN, CLOSING };
        public GuideState CurrentState;

        private Vector2 guidePosition;
        private Rectangle guideSourceRect;
        private RenderTarget2D renderTarget;
        private Texture2D texture;
        private int selectedInfoPanel; // which info panel to show, 0-4
        private double openingTime, closingTime;
        private Vector2 guideTabRightOffset = new Vector2(17, 13);
        private Vector2 guideTabLeftOffset = new Vector2(4, 13);

        public Guide(ZoomCircle zoomCircle)
        {
            this.ParentZoomCircle = zoomCircle;
            this.IsUpsideDown = false;
            this.CurrentState = GuideState.CLOSED;
            this.selectedInfoPanel = 0;
        }

        public void Update()
        {
            int calloutWidth = PlanktonPopulations.guideImagesRight[0].Width;
            int calloutHeight = PlanktonPopulations.guideImagesRight[0].Height;
            int openButtonWidth = PlanktonPopulations.OpenTabRightImage.Width; // both left and right tab images should be same width
            int calloutVerticalOffset = calloutHeight - Settings.CALLOUT_VERTICAL_ADJUST - (int)Settings.CIRCLE_RADIUS;
            int x_closed, x_open, x_current = 0;

            // Determine if we're too close to the right edge
            if (this.ParentZoomCircle.position.X > Settings.RESOLUTION_X - calloutWidth)
            {
                // We're too close; reflect callouts
                this.IsLeftward = true;
                x_open = -calloutWidth + (int)Settings.CIRCLE_RADIUS;                 // x-coordinate relative to circle center of where to draw callout texture when callout is open
                x_closed = Settings.CALLOUT_HORIZONTAL_ADJUST - (int)Settings.CIRCLE_RADIUS - Settings.CALLOUT_HORIZONTAL_HIDE;   // x-coordinate relative to circle center of where to draw callout texture when callout is closed
            }
            else
            {
                // We're not too close, draw callouts normally
                this.IsLeftward = false;
                x_open = (int)Settings.CIRCLE_RADIUS + 2 * (int)Settings.CIRCLE_BORDER_WIDTH + Settings.CALLOUT_HORIZONTAL_HIDE;                                                           // x-coordinate relative to circle center of where to draw callout texture when callout is open
                x_closed = calloutWidth - (int)Settings.CIRCLE_RADIUS + Settings.CALLOUT_HORIZONTAL_HIDE + Settings.CALLOUT_HORIZONTAL_ADJUST;  // x-coordinate relative to circle center of where to draw callout texture when callout is closed
            }

            // Manage callout state, and calculate x-position of the callout source rectangle based on state and time since state began
            switch (this.CurrentState)
            {
                case GuideState.CLOSED:
                    x_current = x_closed;
                    break;
                case GuideState.OPENING:
                    float progress = (float)(PlanktonPopulations.gameTime.TotalGameTime.TotalMilliseconds - openingTime) / (float)Settings.CALLOUT_OPENING_TIME;
                    if (progress >= 1)
                    {
                        this.CurrentState = GuideState.OPEN;
                        x_current = x_open;
                    }
                    else
                    {
                        x_current = (int)MathHelper.SmoothStep(x_closed, x_open, progress);
                        //x = (int)((float)x - progress * (float)(x - LivingLiquid.CALLOUT_HORIZONTAL_HIDE));
                        //Debug.WriteLine(x);
                    }
                    break;
                case GuideState.OPEN:
                    x_current = x_open;
                    break;
                case GuideState.CLOSING:
                    float close_progress = (float)(PlanktonPopulations.gameTime.TotalGameTime.TotalMilliseconds - closingTime) / (float)Settings.CALLOUT_CLOSING_TIME;
                    if (close_progress >= 1)
                    {
                        this.CurrentState = GuideState.CLOSED;
                        x_current = x_closed;
                        //selectedInfoPanel = 5; // Show "open guide" tab
                        this.IsUpsideDown = false;
                    }
                    else
                    {
                        x_current = (int)MathHelper.SmoothStep(x_open, x_closed, close_progress);
                        //x = (int)((float)x - progress * (float)(x - LivingLiquid.CALLOUT_HORIZONTAL_HIDE));
                        //Debug.WriteLine(x);
                    }
                    break;
            }

            // Create a callout texture with the circle cut out               
            GraphicsDevice graphicsDevice = PlanktonPopulations.graphicsDeviceManager.GraphicsDevice;
            renderTarget = new RenderTarget2D(graphicsDevice, calloutWidth + openButtonWidth, calloutHeight);
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);
            PlanktonPopulations.spriteBatch.Begin();
            // DEBUG
            //this.IsUpsideDown = true;
            if (this.IsLeftward)
            {
                if (this.CurrentState != GuideState.CLOSED)
                {
                    if (this.IsUpsideDown)
                    {
                        PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.guideImagesRight[selectedInfoPanel], new Vector2(guideTabRightOffset.X, Settings.CALLOUT_VERTICAL_ADJUST), null, Color.White, (float)Math.PI, new Vector2(calloutWidth, calloutHeight), 1f, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.guideImagesLeft[selectedInfoPanel], new Vector2(guideTabRightOffset.X, 0), Color.White);
                    }
                    PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.CloseTabLeftImage, guideTabLeftOffset, Color.White);
                    //PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.closeTabLeftImage, PlanktonPopulations.ArrowsOffset, Color.White);
                }
                else
                {
                    PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.OpenTabLeftImage, guideTabLeftOffset, Color.White);
                    //PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.openTabLeftImage, PlanktonPopulations.ArrowsOffset, Color.White);
                }
                PlanktonPopulations.spriteBatch.End();
                PlanktonPopulations.spriteBatch.Begin(SpriteSortMode.Deferred, PlanktonPopulations.subtractAlpha);
                PlanktonPopulations.spriteBatch.DrawCircle(new Vector2(-x_current + guideTabRightOffset.X, calloutVerticalOffset), Settings.CIRCLE_RADIUS, 64, Color.White, Settings.CIRCLE_RADIUS);
                PlanktonPopulations.spriteBatch.End();
                // Calculate source rectangle
                guideSourceRect = new Rectangle(0, 0, -Settings.CALLOUT_HORIZONTAL_HIDE - x_current, calloutHeight);
                // Position to draw callout
                guidePosition = new Vector2(this.ParentZoomCircle.position.X + x_current - guideTabRightOffset.X, this.ParentZoomCircle.position.Y - calloutVerticalOffset);
            }
            else
            {
                if (this.CurrentState != GuideState.CLOSED)
                {
                    if (this.IsUpsideDown)
                    {
                        PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.guideImagesLeft[selectedInfoPanel], new Vector2(0, Settings.CALLOUT_VERTICAL_ADJUST), null, Color.White, (float)Math.PI, new Vector2(calloutWidth, calloutHeight), 1f, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.guideImagesRight[selectedInfoPanel], Vector2.Zero, Color.White);
                    }
                    PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.CloseTabRightImage, new Vector2(PlanktonPopulations.guideImagesRight[selectedInfoPanel].Width - PlanktonPopulations.CloseTabRightImage.Width, 0f) + guideTabRightOffset, Color.White);
                    //PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.closeTabRightImage, new Vector2(PlanktonPopulations.guideImagesRight[selectedInfoPanel].Width - PlanktonPopulations.closeTabRightImage.Width, 0f) + PlanktonPopulations.ArrowsOffset, Color.White);
                }
                else
                {
                    PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.OpenTabRightImage, new Vector2(calloutWidth - PlanktonPopulations.OpenTabRightImage.Width, 0f) + guideTabRightOffset, Color.White);
                    //PlanktonPopulations.spriteBatch.Draw(PlanktonPopulations.openTabRightImage, new Vector2(calloutWidth - PlanktonPopulations.openTabRightImage.Width, 0f) + PlanktonPopulations.ArrowsOffset, Color.White);
                }
                PlanktonPopulations.spriteBatch.End();
                PlanktonPopulations.spriteBatch.Begin(SpriteSortMode.Deferred, PlanktonPopulations.subtractAlpha);
                PlanktonPopulations.spriteBatch.DrawCircle(new Vector2(x_current - Settings.CALLOUT_HORIZONTAL_HIDE, calloutVerticalOffset), Settings.CIRCLE_RADIUS, 64, Color.White, Settings.CIRCLE_RADIUS);
                PlanktonPopulations.spriteBatch.End();
                // Calculate source rectangle
                guideSourceRect = new Rectangle(x_current, 0, calloutWidth - x_current + openButtonWidth, calloutHeight);
                // Position to draw callout
                guidePosition = new Vector2(this.ParentZoomCircle.position.X + Settings.CALLOUT_HORIZONTAL_HIDE - 0 * Settings.CIRCLE_RADIUS, this.ParentZoomCircle.position.Y - calloutVerticalOffset);
            }
            graphicsDevice.SetRenderTarget(null);
            this.texture = (Texture2D)renderTarget;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(this.texture, guidePosition, guideSourceRect, Color.White);
            //spriteBatch.Draw(this.texture, guidePosition, Color.White);
            
            // DEBUG: Draw locations of buttons and tabs
            if (Settings.SHOW_HITBOXES)
            {
                Vector2 openButtonPosition, closeButtonPosition, tab1, tab2, tab3, tab4, tabSize;
                tabSize = new Vector2(Settings.CALLOUT_TAB_WIDTH, Settings.CALLOUT_TAB_HEIGHT);
                if (this.IsLeftward)
                {
                    openButtonPosition = Settings.CALLOUT_OPEN_BUTTON_LEFT;
                    closeButtonPosition = Settings.CALLOUT_CLOSE_BUTTON_LEFT;
                    if (this.IsUpsideDown)
                    {
                        tab1 = -Settings.CALLOUT_TAB1_BUTTON;
                        tab2 = -Settings.CALLOUT_TAB2_BUTTON;
                        tab3 = -Settings.CALLOUT_TAB3_BUTTON;
                        tab4 = -Settings.CALLOUT_TAB4_BUTTON;
                    }
                    else
                    {
                        tab1 = Settings.CALLOUT_TAB1_BUTTON_LEFT;
                        tab2 = Settings.CALLOUT_TAB2_BUTTON_LEFT;
                        tab3 = Settings.CALLOUT_TAB3_BUTTON_LEFT;
                        tab4 = Settings.CALLOUT_TAB4_BUTTON_LEFT;
                    }
                }
                else
                {
                    openButtonPosition = Settings.CALLOUT_OPEN_BUTTON;
                    closeButtonPosition = Settings.CALLOUT_CLOSE_BUTTON;
                    if (this.IsUpsideDown)
                    {
                        tab1 = -Settings.CALLOUT_TAB1_BUTTON_LEFT;
                        tab2 = -Settings.CALLOUT_TAB2_BUTTON_LEFT;
                        tab3 = -Settings.CALLOUT_TAB3_BUTTON_LEFT;
                        tab4 = -Settings.CALLOUT_TAB4_BUTTON_LEFT;
                    }
                    else
                    {
                        tab1 = Settings.CALLOUT_TAB1_BUTTON;
                        tab2 = Settings.CALLOUT_TAB2_BUTTON;
                        tab3 = Settings.CALLOUT_TAB3_BUTTON;
                        tab4 = Settings.CALLOUT_TAB4_BUTTON;
                    }
                }
                if (this.CurrentState == Guide.GuideState.CLOSED)
                {
                    spriteBatch.DrawCircle(this.ParentZoomCircle.position + openButtonPosition, Settings.CALLOUT_DETECTION_RADIUS, 64, Color.Pink);
                }
                else if (this.CurrentState == Guide.GuideState.OPEN)
                {
                    spriteBatch.DrawCircle(this.ParentZoomCircle.position + closeButtonPosition, Settings.CALLOUT_DETECTION_RADIUS, 64, Color.Pink);
                    spriteBatch.DrawRectangle(this.ParentZoomCircle.position + new Vector2(tab1.X - tabSize.X / 2, tab1.Y - tabSize.Y / 2), tabSize, Color.Pink, 1f);
                    spriteBatch.DrawRectangle(this.ParentZoomCircle.position + new Vector2(tab2.X - tabSize.X / 2, tab2.Y - tabSize.Y / 2), tabSize, Color.Pink, 1f);
                    spriteBatch.DrawRectangle(this.ParentZoomCircle.position + new Vector2(tab3.X - tabSize.X / 2, tab3.Y - tabSize.Y / 2), tabSize, Color.Pink, 1f);
                    spriteBatch.DrawRectangle(this.ParentZoomCircle.position + new Vector2(tab4.X - tabSize.X / 2, tab4.Y - tabSize.Y / 2), tabSize, Color.Pink, 1f);
                }
            }
        }

        public void OpenButtonPressed(bool upsideDown)
        {
            if (this.CurrentState == GuideState.CLOSED)
            {
                Debug.WriteLine("Open button pressed!");
                CurrentState = GuideState.OPENING;
                openingTime = PlanktonPopulations.gameTime.TotalGameTime.TotalMilliseconds;
                selectedInfoPanel = 0;
                this.IsUpsideDown = upsideDown;
                //this.upsideDownCallouts = true; // DEBUG
            }
        }
        public void CloseButtonPressed()
        {
            if (CurrentState == GuideState.OPEN)
            {
                Debug.WriteLine("Close button pressed!");
                CurrentState = GuideState.CLOSING;
                closingTime = PlanktonPopulations.gameTime.TotalGameTime.TotalMilliseconds;
            }
        }
        public void Tab1ButtonPressed()
        {
            if (CurrentState == GuideState.OPEN)
            {
                Debug.WriteLine("Tab1 pressed!");
                selectedInfoPanel = 1;
            }
        }
        public void Tab2ButtonPressed()
        {
            if (CurrentState == GuideState.OPEN)
            {
                Debug.WriteLine("Tab2 pressed!");
                selectedInfoPanel = 2;
            }
        }
        public void Tab3ButtonPressed()
        {
            if (CurrentState == GuideState.OPEN)
            {
                Debug.WriteLine("Tab3 pressed!");
                selectedInfoPanel = 3;
            }
        }
        public void Tab4ButtonPressed()
        {
            if (CurrentState == GuideState.OPEN)
            {
                Debug.WriteLine("Tab4 pressed!");
                selectedInfoPanel = 4;
            }
        }

        public void CheckButtonSwipe(string buttonName, float x, float y, bool isUpsideDownTouch)
        {
            // Check to see if the average of the path is towards the swipe target direction
            if (this.CurrentState == GuideState.OPEN && buttonName == "close")
            {
                Vector2 closeButtonPosition;
                if (this.IsLeftward)
                {
                    closeButtonPosition = this.ParentZoomCircle.position + Settings.CALLOUT_CLOSE_BUTTON_LEFT;
                    if (x > closeButtonPosition.X + Settings.CALLOUT_DETECTION_RADIUS)
                        this.CloseButtonPressed();
                }
                else
                {
                    closeButtonPosition = this.ParentZoomCircle.position + Settings.CALLOUT_CLOSE_BUTTON;
                    if (x < closeButtonPosition.X - Settings.CALLOUT_DETECTION_RADIUS)
                        this.CloseButtonPressed();
                }
            }
            else if (this.CurrentState == GuideState.CLOSED && buttonName == "open")
            {
                Vector2 openButtonPosition;
                if (this.IsLeftward)
                {
                    openButtonPosition = this.ParentZoomCircle.position + Settings.CALLOUT_OPEN_BUTTON_LEFT;
                    if (x < openButtonPosition.X - Settings.CALLOUT_DETECTION_RADIUS)
                        this.OpenButtonPressed(isUpsideDownTouch);
                }
                else
                {
                    openButtonPosition = this.ParentZoomCircle.position + Settings.CALLOUT_OPEN_BUTTON;
                    if (x > openButtonPosition.X + Settings.CALLOUT_DETECTION_RADIUS)
                        this.OpenButtonPressed(isUpsideDownTouch);
                }
            }
        }
    }
}
