using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using C3.XNA;

namespace PlanktonPopulations
{
    class NutrientTool : MovableObject
    {
        private Vector2 _position;
        public Vector2 position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }
        public float SiO2;

        public NutrientTool(Vector2 position)
        {
            _position = position;
        }

        // Get temperature at this tool's position
        public void Update(int timestamp, Rectangle movieDestination)
        {
            // Calculate file data position based on x,y coordinates. Data stored is stored in a 540x270 grid.
            double movieX = position.X - movieDestination.X;
            double movieY = movieDestination.Height - (position.Y - movieDestination.Y) - 1; // Data is vertically flipped

            // However, since we've cropped the bottom 60 lines from the 2160x1080 video, this translates to a 540x255 grid.
            long offset = (int)(movieY / (double)movieDestination.Height * 255.0) * 540;
            offset += (int)(movieX / (double)movieDestination.Width * 540);

            // Add an offset to the beginning of the file read reflecting the inaccessible cropped area (15 lines)
            offset += 15 * 540;

            // Seek to position in file (4-byte floats)
            byte[] bytes = new byte[4];
            byte[] bytes2 = new byte[4];
            for (int j = 0; j < 4; j++)
            {
                //bytes[j] = Game1.theData["SiO2"][timestamp][offset * 4 + j];
                //bytes2[j] = Game1.theData["POSi"][timestamp][offset * 4 + j];
                bytes[j] = PlanktonPopulations.theData["SiO2"][timestamp][offset * 4 + j];
            }
            // Convert from big-endian
            SiO2 = BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);
            //POSi = BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);
        }
    }
}
