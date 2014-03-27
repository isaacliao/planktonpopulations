/*
    TUIO Hand Tracker extension for Multitaction
*/

using System;
using System.Collections.Generic;

namespace TUIO
{

    /**
     * The TuioHand class encapsulates /tuio/_hand TUIO objects.
     *
     * @author Isaac Liao, based on C# TUIO Library by Martin Kaltenbrunner
     * @version 1.0
     */
    public class TuioHand : TuioContainer
    {

        /**
         * The individual ID number that is assigned to each TuioHand.
         */
        protected int hand_id;
        /**
         * The finger ID numbers that this hand belongs to.
         */
        protected int finger_id1, finger_id2, finger_id3, finger_id4, finger_id5;

        /**
         * This constructor takes a TuioTime argument and assigns it along with the provided
         * Hand ID, X and Y coordinates, and Finger ID's to the newly created TuioHand.
         *
         * @param	ttime	the TuioTime to assign
         * @param	h_id	the Hand ID to assign
         * @param	xp	the X coordinate to assign
         * @param	yp	the Y coordinate to assign
         * @param	f_id1	the Finger ID #1 to assign
         * @param	f_id2	the Finger ID #2 to assign
         * @param	f_id3	the Finger ID #3 to assign
         * @param	f_id4	the Finger ID #4 to assign
         * @param	f_id5	the Finger ID #5 to assign
         */
        public TuioHand(TuioTime ttime, int h_id, float xp, float yp, int f_id1, int f_id2, int f_id3, int f_id4, int f_id5)
            : base(ttime, h_id, xp, yp)
        {
            hand_id = h_id;
            finger_id1 = f_id1;
            finger_id2 = f_id2;
            finger_id3 = f_id3;
            finger_id4 = f_id4;
            finger_id5 = f_id5;
        }

        /**
         * This constructor assigns the provided Hand ID, X and Y coordinates, and Finger ID's to the newly created TuioHand.
         *
         * @param	h_id	the Hand ID to assign
         * @param	xp	the X coordinate to assign
         * @param	yp	the Y coordinate to assign
         * @param	f_id1	the Finger ID #1 to assign
         * @param	f_id2	the Finger ID #2 to assign
         * @param	f_id3	the Finger ID #3 to assign
         * @param	f_id4	the Finger ID #4 to assign
         * @param	f_id5	the Finger ID #5 to assign
         */
        public TuioHand(int h_id, float xp, float yp, int f_id1, int f_id2, int f_id3, int f_id4, int f_id5)
            : base(h_id, xp, yp)
        {
            hand_id = h_id;
            finger_id1 = f_id1;
            finger_id2 = f_id2;
            finger_id3 = f_id3;
            finger_id4 = f_id4;
            finger_id5 = f_id5;
        }

        /**
         * This constructor takes the provided TUIO Time, Hand ID, X and Y coordinates,
         * and assigns these values to the newly created TuioHand.
         *
         * @param	ttime	the TuioTime to assign
         * @param	h_id	the Hand ID to assign
         * @param	xp	the X coordinate to assign
         * @param	yp	the Y coordinate to assign
         * @param	a	the angle to assign
         */
        public TuioHand(TuioTime ttime, int h_id, float xp, float yp)
            : base(ttime, h_id, xp, yp)
        {
            hand_id = h_id;
        }

        /**
         * This constructor takes the provided Hand ID, X and Y coordinates,
         * and assigs these values to the newly created TuioHand.
         *
         * @param	h_id	the Hand ID to assign
         * @param	xp	the X coordinate to assign
         * @param	yp	the Y coordinate to assign
         * @param	a	the angle to assign
         */
        public TuioHand(int h_id, float xp, float yp)
            : base(h_id, xp, yp)
        {
            hand_id = h_id;
        }

        /**
         * This constructor takes the atttibutes of the provided TuioHand
         * and assigs these values to the newly created TuioHand.
         *
         * @param	thand	the TuioHand to assign
         */
        public TuioHand(TuioHand thand)
            : base(thand)
        {
            hand_id = thand.getHandID();
            finger_id1 = thand.getFingerID1();
            finger_id2 = thand.getFingerID2();
            finger_id3 = thand.getFingerID3();
            finger_id4 = thand.getFingerID4();
            finger_id5 = thand.getFingerID5();
        }

        /**
         * Takes a TuioTime argument and assigns it along with the provided
         * X and Y coordinate, angle, X and Y velocity, motion acceleration,
         * rotation speed and rotation acceleration to the private TuioHand attributes.
         *
         * @param	ttime	the TuioTime to assign
         * @param	xp	the X coordinate to assign
         * @param	yp	the Y coordinate to assign
         * @param	xs	the X velocity to assign
         * @param	ys	the Y velocity to assign
         * @param	f_id1	the Finger ID #1 to assign
         * @param	f_id2	the Finger ID #2 to assign
         * @param	f_id3	the Finger ID #3 to assign
         * @param	f_id4	the Finger ID #4 to assign
         * @param	f_id5	the Finger ID #5 to assign
         */
        public void update(TuioTime ttime, float xp, float yp, float xs, float ys, int f_id1, int f_id2, int f_id3, int f_id4, int f_id5)
        {
            base.update(ttime, xp, yp, xs, ys, 0f);
            finger_id1 = f_id1;
            finger_id2 = f_id2;
            finger_id3 = f_id3;
            finger_id4 = f_id4;
            finger_id5 = f_id5;
        }

        /**
         * Assigns the provided X and Y coordinates, X and Y velocities, and finger ID's to the private TuioHand attributes.
         *
         * @param	xp	the X coordinate to assign
         * @param	yp	the Y coordinate to assign
         * @param	xs	the X velocity to assign
         * @param	ys	the Y velocity to assign
         * @param	f_id1	the Finger ID #1 to assign
         * @param	f_id2	the Finger ID #2 to assign
         * @param	f_id3	the Finger ID #3 to assign
         * @param	f_id4	the Finger ID #4 to assign
         * @param	f_id5	the Finger ID #5 to assign
         */
        public void update(float xp, float yp, float xs, float ys, int f_id1, int f_id2, int f_id3, int f_id4, int f_id5)
        {
            base.update(xp, yp, xs, ys, 0f);
            finger_id1 = f_id1;
            finger_id2 = f_id2;
            finger_id3 = f_id3;
            finger_id4 = f_id4;
            finger_id5 = f_id5;
        }

        /**
         * Takes the atttibutes of the provided TuioHand
         * and assigns these values to this TuioHand.
         * The TuioTime time stamp of this TuioContainer remains unchanged.
         *
         * @param	tobj	the TuioContainer to assign
         */
        public void update(TuioHand thand)
        {
            base.update(thand);
            hand_id = thand.getHandID();
            finger_id1 = thand.getFingerID1();
            finger_id2 = thand.getFingerID2();
            finger_id3 = thand.getFingerID3();
            finger_id4 = thand.getFingerID4();
            finger_id5 = thand.getFingerID5();
        }

        /**
         * This method is used to calculate the speed and acceleration values of a
         * TuioObject with unchanged position and angle.
         */
        public new void stop(TuioTime ttime)
        {
            update(ttime, this.xpos, this.ypos);
        }

        /**
         * Returns the hand ID of this TuioHand.
         * @return  the hand ID of this TuioHand
         */
        public int getHandID()
        {
            return hand_id;
        }

        /**
         * Returns the finger ID #1 of this TuioHand.
         * @return  the finger ID #1 of this TuioHand
         */
        public int getFingerID1()
        {
            return finger_id1;
        }

        /**
         * Returns the finger ID #2 of this TuioHand.
         * @return  the finger ID #2 of this TuioHand
         */
        public int getFingerID2()
        {
            return finger_id2;
        }

        /**
         * Returns the finger ID #3 of this TuioHand.
         * @return  the finger ID #3 of this TuioHand
         */
        public int getFingerID3()
        {
            return finger_id3;
        }

        /**
         * Returns the finger ID #4 of this TuioHand.
         * @return  the finger ID #4 of this TuioHand
         */
        public int getFingerID4()
        {
            return finger_id4;
        }

        /**
         * Returns the finger ID #5 of this TuioHand.
         * @return  the finger ID #5 of this TuioHand
         */
        public int getFingerID5()
        {
            return finger_id5;
        }

        /**
         * Returns true of this TuioObject is moving.
         * @return	true of this TuioObject is moving
         */
        public new bool isMoving()
        {
            if ((state == TUIO_ACCELERATING) || (state == TUIO_DECELERATING)) return true;
            else return false;
        }

    }

}
