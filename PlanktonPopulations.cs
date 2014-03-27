using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using C3.XNA;       // https://bitbucket.org/jcpmcdonald/2d-xna-primitives/wiki/Home
using TUIO;

namespace PlanktonPopulations
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlanktonPopulations : Microsoft.Xna.Framework.Game, TuioListener
    {
        // Declare global constants
        long MOUSECLICK_ID = 0;
        public static int TEMPORAL_RESOLUTION = 72 * 15;

        // Declare some class and instance variables (pretty much the same thing since this is a singleton)
        public static SpriteBatch spriteBatch;
        public static GameTime gameTime;
        public static Texture2D[][] planktonImages = new Texture2D[4][];
        public static Texture2D[] guideImagesLeft = new Texture2D[6];
        public static Texture2D[] guideImagesRight = new Texture2D[6];
        public static Texture2D CloseTabLeftImage, CloseTabRightImage, OpenTabLeftImage, OpenTabRightImage;
        public static Texture2D currentFrame;
        public static Dictionary<string, Texture2D[]> readoutImages;
        public static Random rand = new Random();
        bool zoomsOpen = false; // Remembers whether there are currently magnified views open. Important for figuring out when state transitions happen.
        bool mouseWasPressed = false; // Helps determine whether the mouse is held continuously or if there is a new click
        double lastMousepress; // Remembers the time of the last mousepress in milliseconds
        double lastObjectTime; // Remembers the time the last object was seen in milliseconds
        public static Rectangle movieDestination;
        ConcurrentDictionary<long, ZoomCircle> zoomedCircles = new ConcurrentDictionary<long, ZoomCircle>();
        ConcurrentDictionary<long, ZoomCircle> queuedCircles = new ConcurrentDictionary<long, ZoomCircle>();
        ConcurrentDictionary<long, TuioObject> tuioObjects = new ConcurrentDictionary<long, TuioObject>();
        ConcurrentDictionary<long, TuioCursor> tuioCursors = new ConcurrentDictionary<long, TuioCursor>();
        ConcurrentQueue<TuioCursor> tuioCursorAddQueue = new ConcurrentQueue<TuioCursor>();
        ConcurrentQueue<TuioCursor> tuioCursorUpdateQueue = new ConcurrentQueue<TuioCursor>();
        ConcurrentQueue<TuioCursor> tuioCursorRemoveQueue = new ConcurrentQueue<TuioCursor>();
        ConcurrentDictionary<long, TuioHand> tuioHands = new ConcurrentDictionary<long, TuioHand>();
        ConcurrentQueue<TuioHand> tuioHandAddOrUpdateQueue = new ConcurrentQueue<TuioHand>();
        ConcurrentQueue<TuioHand> tuioHandRemoveQueue = new ConcurrentQueue<TuioHand>();
        ConcurrentQueue<TempTool> tempTools = new ConcurrentQueue<TempTool>();
        ConcurrentQueue<NutrientTool> nutrientTools = new ConcurrentQueue<NutrientTool>();
        public static Texture2D maskTexture;
        public static Texture2D tempIcon, hourglassIcon, questionButton;
        public static SpriteFont smallFont, mediumFont, largeFont, extraLargeFont;
        public static GraphicsDeviceManager graphicsDeviceManager;
        public static Color[] maskTextureArray;
        public static Dictionary<int, byte[][]> phygrpData = new Dictionary<int, byte[][]>();
        public static Dictionary<string, Dictionary<int, byte[]>> theData = new Dictionary<string, Dictionary<int, byte[]>>(); // A data structure to hold all plankton population and environmental data
        public static byte[] landmaskArray = new byte[583200]; // A byte array to hold landmask data
        public static List<string> dataNames;
        public static RenderTarget2D maskTarget;
        public static RenderTarget2D fullScreenTarget;
        public static BlendState subtractAlpha;
        public static bool showZoomedCircleContents = true;
        public static float movieVerticalOffset;
        public static float movieScale;
        public static Vector2 ArrowsOffset = new Vector2();
        public static Effect LensEffect;

        private VideoPlayer videoPlayer;
        private Video video;
        private Timeline timeline;
        private TuioClient tuioClient;
        private Texture2D continentsImage;
        private System.Windows.Forms.Form myForm;
        private bool readyForInput = false;

        public PlanktonPopulations()
        {
            Settings.LoadSettings("Settings.txt");

            // Possible extra debug settings here
#if DEBUG
#endif

            graphicsDeviceManager = new GraphicsDeviceManager(this);
            graphicsDeviceManager.PreferredBackBufferWidth = Settings.RESOLUTION_X;
            graphicsDeviceManager.PreferredBackBufferHeight = Settings.RESOLUTION_Y;
            graphicsDeviceManager.IsFullScreen = Settings.FULLSCREEN;
            graphicsDeviceManager.PreferMultiSampling = Settings.ANTIALIASING;
            graphicsDeviceManager.ApplyChanges();

            this.IsMouseVisible = Settings.SHOW_MOUSE;
            Content.RootDirectory = "Content";

            PlanktonPopulations.gameTime = new GameTime();

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Moves starting window location
            myForm = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(this.Window.Handle);
            myForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            myForm.Location = new System.Drawing.Point(0, 0);

            // Explicitly set number of samples for antialiasing; may not be necessary
            //GraphicsDevice.PresentationParameters.MultiSampleCount = 16;
            //Debug.WriteLine(graphicsDevice.PresentationParameters.MultiSampleCount);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //planktonTarget = new RenderTarget2D(GraphicsDevice, (int)(CIRCLE_RADIUS + CIRCLE_RADIUS_OVERSCAN) * 2, (int)(Game1.CIRCLE_RADIUS + Game1.CIRCLE_RADIUS_OVERSCAN) * 2);

            // Create a new renderTarget to prerender before drawing on screen.
            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            fullScreenTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, true, GraphicsDevice.DisplayMode.Format, DepthFormat.Depth24);

            maskTextureArray = new Color[(int)((Settings.CIRCLE_RADIUS + Settings.CIRCLE_RADIUS_OVERSCAN) * 2 * (Settings.CIRCLE_RADIUS + Settings.CIRCLE_RADIUS_OVERSCAN) * 2)];

            subtractAlpha = new BlendState();
            subtractAlpha.AlphaBlendFunction = BlendFunction.ReverseSubtract;
            subtractAlpha.AlphaSourceBlend = Blend.One;
            subtractAlpha.AlphaDestinationBlend = Blend.One;
            subtractAlpha.ColorBlendFunction = BlendFunction.ReverseSubtract;
            subtractAlpha.ColorSourceBlend = Blend.One;
            subtractAlpha.ColorDestinationBlend = Blend.One;

            // Initialize tools
            for (int i = 0; i < Settings.NUM_NUTRIENTTOOLS; i++)
            {
                nutrientTools.Enqueue(new NutrientTool(new Vector2(50, 350 + 100 * i)));
            }

            for (int i = 0; i < Settings.NUM_TEMPTOOLS; i++)
            {
                tempTools.Enqueue(new TempTool(new Vector2(50, 150 + i * 100)));
            }

            // Add datanames to the list 
            //"T", "SiO2", "POSi"
            dataNames = new List<string>() { "PhyGrp1", "PhyGrp3", "PhyGrp4", "PhyGrp5" }; // PhyGrp data needs to be the first four
            if (Settings.SHOW_LIGHT)
                dataNames.Add("PAR");
            if (Settings.SHOW_NITRATE)
                dataNames.Add("NO3");
            if (Settings.SHOW_TEMP || Settings.NUM_TEMPTOOLS > 0)
                dataNames.Add("T");
            if (Settings.SHOW_SILICA || Settings.NUM_NUTRIENTTOOLS > 0)
                dataNames.Add("SiO2");

            // Set up data structure for all the data
            for (int i = 0; i < dataNames.Count; i++)
                theData.Add(dataNames[i], new Dictionary<int, byte[]>());

            // Draw a loading rectangle
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            spriteBatch.DrawRectangle(new Rectangle(0, 0, 100, 100), Color.White);
            spriteBatch.End();
            base.Draw(new GameTime());

            // Load data from files
            FileStream[] fileStreams = new FileStream[dataNames.Count];
            for (int timestamp = 52704; timestamp <= 210384; timestamp += 1080)
            {
                Debug.WriteLine(string.Format("Reading data, {0}% complete...", ((float)timestamp - 52704.0) * 100.0 / (210384.0 - 52704.0)));

                for (int i = 0; i < dataNames.Count; i++)
                {
                    //// Read data from file into a float array
                    //float[] floats = new float[583200/4];
                    //byte[] fourBytes = new byte[4];
                    //fileStreams[i] = new FileStream(string.Format("Data\\{0}_{1}.data", dataNames[i], timestamp), FileMode.Open);
                    //Debug.WriteLine(string.Format("Reading {0}_{1}.data...", dataNames[i], timestamp));

                    //for (int j = 0; j < 583200/4; j++)
                    //{
                    //    // Read four bytes from the file
                    //    for (int k = 0; k < 4; k++)
                    //        fourBytes[k] = (byte)fileStreams[i].ReadByte();

                    //    // Convert four bytes to float (big-endian, so reverse it)
                    //    floats[j] = BitConverter.ToSingle(fourBytes.Reverse().ToArray(), 0);

                    //    // Check for NaN's
                    //    if (float.IsNaN(floats[j]))
                    //        floats[j] = 0;
                    //}

                    //// Add float data to the data structure
                    //theData[dataNames[i]].Add(timestamp, floats);

                    //// Done with this file
                    //fileStreams[i].Close();

                    // Read data from file
                    byte[] bytes = new byte[583200];
                    float[] floats = new float[583200 / 4];
                    byte[] fourBytes = new byte[4];
                    fileStreams[i] = new FileStream(string.Format("Data\\{0}_{1}.data", dataNames[i], timestamp), FileMode.Open);

                    // Read data from file into a byte array
                    fileStreams[i].Read(bytes, 0, 583200);

                    // Done with this file
                    fileStreams[i].Close();

                    //// Convert byte array to float array
                    //for (int j = 0; j < 583200 / 4; j++)
                    //{
                    //    // Read four bytes from the file
                    //    for (int k = 0; k < 4; k++)
                    //        fourBytes[k] = bytes[j*4+k];

                    //    // Convert four bytes to float (big-endian, so reverse it)
                    //    floats[j] = BitConverter.ToSingle(fourBytes.Reverse().ToArray(), 0);

                    //    // Check for NaN's
                    //    if (float.IsNaN(floats[j]))
                    //        floats[j] = 0;
                    //}

                    // Add byte array to the data structure
                    theData[dataNames[i]].Add(timestamp, bytes);

                }
                //Debug.WriteLine(timestamp);

                //// Save phytoplankton group distributions in a dictionary of timestamps
                //byte[][] values = new byte[4][];
                //for (int i = 0; i < 4; i++)
                //{
                //    values[i] = new byte[583200];
                //    fileStreams[i].Read(values[i], 0, 583200);
                //    fileStreams[i].Close();
                //}

                //phygrpData.Add(timestamp, values);

                // Load landmask data from file
                FileStream landmaskFS = new FileStream("Data\\landmask-2-540x270.data", FileMode.Open);
                landmaskFS.Read(landmaskArray, 0, 583200);
                landmaskFS.Close();
            }

            base.Initialize();

            // Initialize a TUIO client and set this LivingLiquid instance as a listener
            tuioClient = new TuioClient(3333);
            //Debug.WriteLine("Removing all TUIO listeners...");
            //tuioClient.removeAllTuioListeners();
            //Debug.WriteLine("Disconnecting TUIO client...is connected: " + tuioClient.isConnected());
            //tuioClient.disconnect();
            Debug.WriteLine("Adding TUIO listener......is connected: " + tuioClient.isConnected());
            tuioClient.addTuioListener(this);
            Debug.WriteLine("Connecting TUIO client...is connected: " + tuioClient.isConnected());
            tuioClient.connect();
            Debug.WriteLine("TUIO client connected: " + tuioClient.isConnected());

            // Initialize Tuio Time
            TuioTime.initSession();

            // Flag ready for input
            this.readyForInput = true;


            // Because the movie has greater than a 2:1 aspect ratio, need to vertically center on screen
            // This section locates a rectangle on screen where the movie should be displayed
            // (unless we're using a really weird narrow screen, in which case, redo this section)
            movieScale = (float)GraphicsDevice.Viewport.Width / (float)(video.Width);
            float movieScaledHeight = (float)(video.Height * movieScale);
            movieVerticalOffset = (GraphicsDevice.Viewport.Height - movieScaledHeight) / 2;
            movieDestination = new Rectangle(GraphicsDevice.Viewport.X,
                (int)(GraphicsDevice.Viewport.Y + movieVerticalOffset),
                GraphicsDevice.Viewport.Width,
                (int)movieScaledHeight);

            InitializeCircles();
        }

        private void InitializeCircles()
        {
            freeObjects = new ConcurrentDictionary<long, TuioObject>();
            draggedObjects = new ConcurrentDictionary<long, TuioObject>();
            zoomedCircles = new ConcurrentDictionary<long, ZoomCircle>();
            tuioObjects.Clear();

            // If touch_only, add 3 tuio objects that will stay forever
            if (Settings.TOUCHONLY)
            {
                for (int i = 0; i < 3; i++)
                {
                    TuioObject tobj = new TuioObject(new TuioTime(), rand.Next(), i, (float)rand.Next(movieDestination.Left, movieDestination.Right) / Settings.RESOLUTION_X, (float)rand.Next(movieDestination.Top, movieDestination.Bottom) / Settings.RESOLUTION_Y, 0f);
                    this.addTuioObject(tobj);
                    if (!freeObjects.TryAdd(tobj.getSymbolID(), tobj))
                    {
                        Debug.WriteLine("Failed to add to freeObjects in InitializeCircles()");
                    };
                }
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load lens shader
            LensEffect = Content.Load<Effect>("Lens");
            LensEffect.CurrentTechnique = LensEffect.Techniques["Technique1"];

            // Load zoom circle image for touch-only interaction
            ZoomCircle.ZoomCircleImage = Content.Load<Texture2D>("ZoomCircle");

            // Load question button
            questionButton = Content.Load<Texture2D>("Question_button");

            // Load hourglass icon
            hourglassIcon = Content.Load<Texture2D>("hourglass");

            // Load thermometer icon
            tempIcon = Content.Load<Texture2D>("thermometer");

            // Load callouts
            for (int i = 0; i < 5; i++)
            {
                guideImagesLeft[i] = Content.Load<Texture2D>("Guide" + i + "L");
            }
            for (int i = 0; i < 5; i++)
            {
                guideImagesRight[i] = Content.Load<Texture2D>("Guide" + i + "R");
            }

            // Load callout tabs
            CloseTabLeftImage = Content.Load<Texture2D>("GuideCloseTabLeft");
            CloseTabRightImage = Content.Load<Texture2D>("GuideCloseTabRight");
            OpenTabLeftImage = Content.Load<Texture2D>("GuideOpenTabLeft");
            OpenTabRightImage = Content.Load<Texture2D>("GuideOpenTabRight");

            // Load readout icons
            readoutImages = new Dictionary<string, Texture2D[]>();
            readoutImages["T"] = new Texture2D[2];
            readoutImages["T"][0] = Content.Load<Texture2D>("Temperature_empty");
            readoutImages["T"][1] = Content.Load<Texture2D>("Temperature_fill");
            readoutImages["SiO2"] = new Texture2D[2];
            readoutImages["SiO2"][0] = Content.Load<Texture2D>("Silica_empty");
            readoutImages["SiO2"][1] = Content.Load<Texture2D>("Silica_fill");
            readoutImages["NO3"] = new Texture2D[2];
            readoutImages["NO3"][0] = Content.Load<Texture2D>("Nitrogen_empty");
            readoutImages["NO3"][1] = Content.Load<Texture2D>("Nitrogen_fill");
            readoutImages["PAR"] = new Texture2D[2];
            readoutImages["PAR"][0] = Content.Load<Texture2D>("Sunlight_empty");
            readoutImages["PAR"][1] = Content.Load<Texture2D>("Sunlight_fill");

            // Load plankton textures
            planktonImages[0] = new Texture2D[1];
            planktonImages[0][0] = Content.Load<Texture2D>("prochlorococcus");  // PhyGrp1
            planktonImages[1] = new Texture2D[1];
            planktonImages[1][0] = Content.Load<Texture2D>("synechococcus");    // PhyGrp3
            planktonImages[2] = new Texture2D[2];
            planktonImages[2][0] = Content.Load<Texture2D>("Dinoflagellate1");  // PhyGrp4
            planktonImages[2][1] = Content.Load<Texture2D>("Dinoflagellate2");
            // planktonImages[2,2] = Content.Load<Texture2D>("Dinoflagellate3"); // This is actually a diatom, don't use for now
            planktonImages[3] = new Texture2D[3];
            planktonImages[3][0] = Content.Load<Texture2D>("Diatom1");          // PhyGrp5
            planktonImages[3][1] = Content.Load<Texture2D>("Diatom2");
            planktonImages[3][2] = Content.Load<Texture2D>("Diatom3");
            //planktonImages[3,3] = Content.Load<Texture2D>("Dinoflagellate3"); // This is actually a diatom, don't use for now

            // Load continent textures
            continentsImage = Content.Load<Texture2D>("Continents02");

            // Load fonts
            smallFont = Content.Load<SpriteFont>("Explo16");
            mediumFont = Content.Load<SpriteFont>("Explo20");
            largeFont = Content.Load<SpriteFont>("Explo32");
            extraLargeFont = Content.Load<SpriteFont>("Explo48");

            // Load movie images into the texture collection
            //movieFrames = new Texture2D[1000];
            //for (int i = 1; i <= 1000; i++)
            //{
            //    movieFrames[i - 1] = Content.Load<Texture2D>(i.ToString("D4"));
            //}

            // Create the mask texture
            CreateMaskTexture();

            // Load movie file            
            if (Settings.MOVIE_BLUE_WATER)
            {
                if (Settings.MOVIE_BLUE_WATER_SATURATED)
                    video = Content.Load<Video>("overviewMovieBlueSaturated");
                else
                    video = Content.Load<Video>("overviewMovieBlue");
            }
            else
            {
                if (Settings.MOVIE_SLOWER)
                    video = Content.Load<Video>("overviewMovieSlow");
                else
                    video = Content.Load<Video>("overviewMovieFast");
            }
            videoPlayer = new VideoPlayer();
            videoPlayer.IsLooped = true;
            videoPlayer.Play(video);

            // Initialize timeline
            this.timeline = new Timeline(GraphicsDevice, video, videoPlayer);

            //this.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Unload any non ContentManager content here
            this.readyForInput = false;
            //Debug.WriteLine("Disconnecting TUIO client...");
            //tuioClient.removeAllTuioListeners();
            tuioClient.disconnect();
            //Debug.WriteLine("TUIO client connected: " + tuioClient.isConnected());
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        bool keyboardIsReady = true;
        private int lastCheckedGameTime = 0;
        protected override void Update(GameTime gameTime)
        {
            // Update this object's gameTime
            PlanktonPopulations.gameTime = gameTime;

            // Get current movie frame as a texture
            currentFrame = videoPlayer.GetTexture();
            //currentFrame = (Texture2D)movieFrames[timestep-1];

            // Ensure at least one frame with no keys pressed before accepting further keyboard input
            if (Keyboard.GetState().GetPressedKeys().Count() == 0)
                keyboardIsReady = true;
            else if (keyboardIsReady)
            {
                keyboardIsReady = false;

                // Exit if escape key is pressed
                if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    this.UnloadContent();
                    this.Exit();
                }
                // Toggle fullscreen if alt+enter or F is pressed
                if (Keyboard.GetState().IsKeyDown(Keys.F) || ((Keyboard.GetState().IsKeyDown(Keys.LeftAlt) || (Keyboard.GetState().IsKeyDown(Keys.RightAlt))) && Keyboard.GetState().IsKeyDown(Keys.Enter)))
                {
                    graphicsDeviceManager.ToggleFullScreen();
                    //CreateMaskTexture();
                    //Game1.maskTexture.SetData<Color>(Game1.maskTextureArray);
                }
                // Toggle showing circle contents if space key is pressed
                //if (Keyboard.GetState().IsKeyDown(Keys.Space))
                //{
                //    if (showZoomedCircleContents)
                //        showZoomedCircleContents = false;
                //    else
                //        showZoomedCircleContents = true;
                //}
                // Toggle touch-only if T key is pressed
                if (Keyboard.GetState().IsKeyDown(Keys.T))
                {
                    if (Settings.TOUCHONLY)
                    {
                        Settings.TOUCHONLY = false;
                    }
                    else
                    {
                        Settings.TOUCHONLY = true;
                    }
                    this.CreateMaskTexture();
                    this.InitializeCircles();
                }
                // Move something using keyboard arrows
                if (Keyboard.GetState().IsKeyDown(Keys.Up))
                {
                    //myForm.Location = new System.Drawing.Point(myForm.Location.X, myForm.Location.Y - 1);
                    ArrowsOffset.Y--;
                    Debug.WriteLine("ArrowsOffset: " + ArrowsOffset.X + ", " + ArrowsOffset.Y);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Down))
                {
                    //myForm.Location = new System.Drawing.Point(myForm.Location.X, myForm.Location.Y + 1);
                    ArrowsOffset.Y++;
                    Debug.WriteLine("ArrowsOffset: " + ArrowsOffset.X + ", " + ArrowsOffset.Y);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                {
                    //myForm.Location = new System.Drawing.Point(myForm.Location.X - 1, myForm.Location.Y);
                    ArrowsOffset.X--;
                    Debug.WriteLine("ArrowsOffset: " + ArrowsOffset.X + ", " + ArrowsOffset.Y);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                {
                    //myForm.Location = new System.Drawing.Point(myForm.Location.X + 1, myForm.Location.Y);
                    ArrowsOffset.X++;
                    Debug.WriteLine("ArrowsOffset: " + ArrowsOffset.X + ", " + ArrowsOffset.Y);
                }
                // DEBUG
                if (Keyboard.GetState().IsKeyDown(Keys.E))
                {
                    throw new Exception("You pushed E!");
                }
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    if (Settings.SHOW_TOUCHES)
                    {
                        Settings.SHOW_TOUCHES = false;
                        Settings.SHOW_HITBOXES = false;
                    }
                    else
                    {
                        Settings.SHOW_TOUCHES = true;
                        Settings.SHOW_HITBOXES = true;
                    }
                }
            }

            // Recreate the mask texture if the content is lost (for example, if switched to fullscreen)
            if (maskTarget.IsContentLost)
                CreateMaskTexture();

            if (Settings.INPUT_USE_MOUSE)
                processMouseInput(gameTime);

            processTuioObjects(gameTime);

            ConsumeCursorAndHandEvents();

            if (zoomsOpen)
            {
                // Update each zoomed circle
                foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
                {
                    zoomedCircle.LoadDataAt((int)zoomedCircle.position.X, (int)zoomedCircle.position.Y, this.GetMovieTimestamp(), this.GetMovieTimestampRaw());
                }

                // Update each temperature tool
                foreach (TempTool tempTool in tempTools)
                {
                    tempTool.Update(this.GetMovieTimestamp(), movieDestination);
                }

                // Update each nutrient tool
                foreach (NutrientTool nutrientTool in nutrientTools)
                {
                    nutrientTool.Update(this.GetMovieTimestamp(), movieDestination);
                }
            }

            // Update all ZoomedCircles
            //if (!maskCreated)
            //{
            //    CreateMaskTexture();
            //    maskCreated = true;
            //    maskTexture.GetData<Color>(Game1.maskTextureArray, 0, maskTextureArray.Length);
            //}
            //CreateMaskTexture();
            //maskTexture.GetData<Color>(Game1.maskTextureArray, 0, maskTextureArray.Length);
            //maskTexture.SetData<Color>(maskTextureArray);
            foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
            {
                zoomedCircle.Update(gameTime);

                // Update readout textures
                foreach (Readout readout in zoomedCircle.readoutList)
                {
                    // Retrieve appropriate images
                    Texture2D empty = readoutImages[readout.dataName][0];
                    Texture2D full = readoutImages[readout.dataName][1];

                    // Calculate how full the readout display should be
                    double readoutRatio = (readout.value - readout.min) / (readout.max - readout.min);
                    //double scaleFactor = (double)DASHBOARD_READOUT_SIZE / (double)empty.Height;
                    int bottomSourcePixels = (int)(readoutRatio * (readout.top - readout.bottom) + readout.bottom);
                    int topSourcePixels = empty.Height - bottomSourcePixels;
                    //int bottomDestPixels = bottomSourcePixels;                    
                    //int topDestPixels = empty.Height - bottomDestPixels;

                    //Debug.WriteLine("{0}: {1}", readout.dataName, bottomDestPixelsRaw);

                    // Create rectangles representing readout sources and destinations
                    //Rectangle destTop = new Rectangle((int)(readoutPosition.X - DASHBOARD_READOUT_SIZE / 2), (int)(readoutPosition.Y - DASHBOARD_READOUT_SIZE / 2), (int)DASHBOARD_READOUT_SIZE, (int)topDestPixels);
                    //Rectangle destBottom = new Rectangle((int)(readoutPosition.X - DASHBOARD_READOUT_SIZE / 2), (int)(readoutPosition.Y - DASHBOARD_READOUT_SIZE / 2 + topDestPixels), (int)DASHBOARD_READOUT_SIZE, (int)bottomDestPixels);
                    Rectangle sourceTop = new Rectangle(0, 0, empty.Width, topSourcePixels);
                    Rectangle sourceBottom = new Rectangle(0, topSourcePixels, empty.Width, bottomSourcePixels);

                    // Draw top and bottom parts of readout
                    //spriteBatch.Draw(empty, destTop, sourceTop, Color.White);
                    //spriteBatch.Draw(full, destBottom, sourceBottom, Color.White);

                    // Center readout position
                    //Vector2 drawPosition = readoutPosition - new Vector2(DASHBOARD_READOUT_SIZE / 2, DASHBOARD_READOUT_SIZE / 2);

                    GraphicsDevice.SetRenderTarget(readout.target);
                    GraphicsDevice.Clear(Color.Transparent);
                    spriteBatch.Begin();
                    spriteBatch.Draw(empty, Vector2.Zero, sourceTop, Color.White);
                    spriteBatch.Draw(full, new Vector2(0, topSourcePixels), sourceBottom, Color.White);
                    spriteBatch.End();
                    GraphicsDevice.SetRenderTarget(null);
                    readout.texture = (Texture2D)readout.target;
                }
            }

            this.timeline.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// Use TUIO cursors (touches) to move TuioObjects. Only runs if touch_only is true, in the [Input] section of Settings.txt.
        /// </summary>
        /// <param name="gameTime"></param>        
        private ConcurrentDictionary<long, TuioObject> draggedObjects = new ConcurrentDictionary<long, TuioObject>();   // key is ID of dragging cursor's session ID
        private ConcurrentDictionary<long, Vector2> dragDiffVectors = new ConcurrentDictionary<long, Vector2>();        // key is ID of dragging cursor's session ID
        private ConcurrentDictionary<long, TuioObject> freeObjects = new ConcurrentDictionary<long, TuioObject>();      // key is TuioObject symbol ID
        private void processTouchOnlyUpdateEvent(TuioCursor tcur)
        {
            lock (touchOnlyLock)
            {
                if (draggedObjects.ContainsKey(tcur.getSessionID()))
                {
                    // Move the currently dragged TuioObject according to its dragging cursor
                    Vector2 tcurPosition = new Vector2(tcur.getScreenX(Settings.RESOLUTION_X), tcur.getScreenY(Settings.RESOLUTION_Y));
                    TuioObject tobj = draggedObjects[tcur.getSessionID()];
                    Vector2 dragDiffVector = dragDiffVectors[tcur.getSessionID()];
                    Vector2 newObjPos = new Vector2((tcurPosition.X - dragDiffVector.X) / (float)Settings.RESOLUTION_X, (tcurPosition.Y - dragDiffVector.Y) / (float)Settings.RESOLUTION_Y);
                    // Don't let objects move off screen
                    if (newObjPos.X >= 0 && newObjPos.X <= 1 && newObjPos.Y >= 0 && newObjPos.Y <= 1)
                    {

                        //tobj.update(TuioTime.getSessionTime(), (tcurPosition.X - dragDiffVector.X) / (float)Settings.RESOLUTION_X, (tcurPosition.Y - dragDiffVector.Y) / (float)Settings.RESOLUTION_Y, tcur.getXSpeed(), tcur.getYSpeed(), tcur.getMotionAccel());
                        //tobj.update(TuioTime.getSessionTime(), newObjPos.X, newObjPos.Y);
                        tobj.update(TuioTime.getSessionTime(), newObjPos.X, newObjPos.Y, tcur.getXSpeed(), tcur.getYSpeed(), tcur.getMotionAccel());
                        draggedObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                        //tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                        this.updateTuioObject(tobj);
                        Debug.WriteLine("Moving " + tobj.getSymbolID() + " with " + tcur.getSessionID() + " at speed " + tcur.getXSpeed() + ", " + tcur.getYSpeed());
                    }

                    /*
                    // Update corresponding ZoomCircle
                    // Calculate speed from x and y speeds
                    float totalSpeed = (float)Math.Sqrt(Math.Pow(tobj.getXSpeed() * Settings.RESOLUTION_X, 2) + Math.Pow(tobj.getYSpeed() * Settings.RESOLUTION_Y, 2));

                    // Ignore implausibly high values because Multitaction is giving us speeds of "Infinity" every couple updates
                    if (totalSpeed > 1000f)
                        totalSpeed = 0f;

                    //Debug.WriteLine(totalSpeed);
                    zoomedCircles[tobj.getSymbolID()].MoveTo(tobj.getScreenX(Settings.RESOLUTION_X), tobj.getScreenY(Settings.RESOLUTION_Y), tobj.getAngleDegrees(), this.GetMovieTimestamp(), this.GetMovieTimestampRaw(), totalSpeed);
                    */
                }
            }
        }

        private void processTouchOnlyRemoveEvent(long id)
        {
            lock (touchOnlyLock)
            {
                // Release a dragged TuioObject when the touch point is removed
                if (draggedObjects.ContainsKey(id))
                {
                    TuioObject outTobj;
                    draggedObjects.TryRemove(id, out outTobj);
                    Vector2 outVector;
                    dragDiffVectors.TryRemove(id, out outVector);

                    // Set object speed to 0
                    outTobj.update(outTobj.getX(), outTobj.getY(), 0, 0, 0);
                    this.updateTuioObject(outTobj);

                    if (freeObjects.TryAdd(outTobj.getSymbolID(), outTobj))
                        Debug.WriteLine("Releasing " + outTobj.getSymbolID() + " with " + id);
                    else
                        Debug.WriteLine("Failed to add back to freeObjects in processTouchOnlyRemoveEvent()");
                    
                }

                // Check if currently dragged TuioObjects are still being dragged; if not, release them
                /*
                foreach (long tcurSessionID in draggedObjects.Keys)
                {

                    bool contains = false;
                    foreach (TuioCursor tcur in tuioCursorsCopy)
                    {
                        if (tcur.getSessionID() == tcurSessionID)
                        {
                            contains = true;
                        }
                    }
                    if (!contains)
                    {
                        freeObjects.Add(draggedObjects[tcurSessionID]);
                        draggedObjects.Remove(tcurSessionID);
                        dragDiffVectors.Remove(tcurSessionID);
                        Debug.WriteLine("Releasing " + tcurSessionID);
                    }
                }
                 */
            }
        }

        /// <summary>
        /// Check if this newly added cursor should start dragging an object
        /// </summary>
        /// <param name="tcur"></param>
        private static Object touchOnlyLock = new Object();
        private void processTouchOnlyAddEvent(TuioCursor tcur)
        {
            lock (touchOnlyLock)
            {
                // Make sure touch point isn't already dragging something else
                if (!draggedObjects.Keys.Contains(tcur.getSessionID()))
                {
                    // Find the closest touched freeObject to this touch point
                    Vector2 tcurStartPos = new Vector2(tcur.getPath()[0].getScreenX(Settings.RESOLUTION_X), tcur.getPath()[0].getScreenY(Settings.RESOLUTION_Y));
                    float closestDistance = float.PositiveInfinity;
                    TuioObject closestTobj = null;

                    foreach (TuioObject tobj in freeObjects.Values)
                    //foreach (TuioObject tobj in tuioObjects.Values)
                    {
                        if (!draggedObjects.ContainsKey(tobj.getSessionID()))
                        {
                            Vector2 tobjPosition = new Vector2(tobj.getScreenX(Settings.RESOLUTION_X), tobj.getScreenY(Settings.RESOLUTION_Y));
                            float distance = Vector2.Distance(tcurStartPos, tobjPosition);
                            if (distance < closestDistance && distance < Settings.CIRCLE_RADIUS)
                            {
                                // Found a closer freeObject
                                closestDistance = distance;
                                closestTobj = tobj;
                            }
                        }
                    }
                    if (closestTobj != null)
                    {
                        Vector2 closestTobjPosition = new Vector2(closestTobj.getScreenX(Settings.RESOLUTION_X), closestTobj.getScreenY(Settings.RESOLUTION_Y));
                        draggedObjects.TryAdd(tcur.getSessionID(), closestTobj);
                        dragDiffVectors.TryAdd(tcur.getSessionID(), tcurStartPos - closestTobjPosition);
                        TuioObject outTobj;
                        freeObjects.TryRemove(closestTobj.getSymbolID(), out outTobj);
                        Debug.WriteLine("Grabbing  " + closestTobj.getSymbolID() + " with " + +tcur.getSessionID());
                    }
                }
            }
        }

        /// <summary>
        /// Use mouse input to simulate TUIO object input. Only runs if mouse_input is true, in the [Input] section of Settings.txt.
        /// </summary>
        /// <param name="gameTime"></param>
        private TuioObject draggedObject;
        private TuioCursor draggedCursor;
        private Vector2 dragDiffVector;
        private void processMouseInput(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();
            float mouseX = (float)mouseState.X / (float)Settings.RESOLUTION_X;
            float mouseY = (float)mouseState.Y / (float)Settings.RESOLUTION_Y;

            // If left button not clicked, release objects and cursors
            if (mouseState.LeftButton != ButtonState.Pressed)
            {
                draggedCursor = null;
                draggedObject = null;
                dragDiffVector = Vector2.Zero;
                tuioCursors.Clear();

                /*
                // Update objects
                foreach (TuioObject tobj in tuioObjects.Values) {
                    this.updateTuioObject(new TuioObject(new TuioTime(), tobj.getSessionID(), tobj.getSymbolID(),tobj.getX()+0.00001f,tobj.getY(),tobj.getAngle()));
                }
                 */
            }
            else if (mouseState.LeftButton == ButtonState.Pressed)
            {
                // If we're already dragging an object or cursor, just update that object's position
                if (draggedCursor != null)
                {
                    draggedCursor.update(TuioTime.getSessionTime(), mouseX, mouseY);
                    this.updateTuioCursor(draggedCursor);
                    return;
                }
                else if (draggedObject != null)
                {
                    draggedObject.update(TuioTime.getSessionTime(), mouseX - dragDiffVector.X / (float)Settings.RESOLUTION_X, mouseY - dragDiffVector.Y / (float)Settings.RESOLUTION_Y);
                    //draggedObject.update(new TuioTime(), mouseX, mouseY);
                    this.updateTuioObject(draggedObject);
                    return;
                }

                // If click is on a guide tab, make it a cursor
                TuioCursor tcur = new TuioCursor(TuioTime.getSessionTime(), 0, 0, mouseX, mouseY);
                ZoomCircle outZoomCircle;
                string outButtonName;
                this.WouldPushButton(tcur, out outZoomCircle, out outButtonName);
                if (outZoomCircle != null)
                {
                    draggedCursor = tcur;
                    this.addTuioCursor(tcur);
                    return;
                }

                // If click is on an open zoomCircle, start dragging its object

                // Find nearest object
                TuioObject closestTobj = null;
                float closestDistance = 1000f;
                Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                Vector2 tobjPosition = new Vector2();
                Vector2 closestDiffVector = Vector2.Zero;
                foreach (TuioObject tobj in tuioObjects.Values)
                {
                    tobjPosition.X = tobj.getScreenX(Settings.RESOLUTION_X);
                    tobjPosition.Y = tobj.getScreenY(Settings.RESOLUTION_Y);
                    Vector2 diffVector = Vector2.Subtract(mousePosition, tobjPosition);
                    float distance = diffVector.Length();
                    if (closestTobj == null)
                    {
                        closestDistance = distance;
                        closestTobj = tobj;
                        closestDiffVector = diffVector;
                    }
                    else if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTobj = tobj;
                        closestDiffVector = diffVector;
                    }
                }

                if (closestTobj != null)
                {
                    if (closestDistance <= Settings.CIRCLE_RADIUS)
                    {
                        // On left click, start dragging it
                        if (mouseState.LeftButton == ButtonState.Pressed)
                        {
                            draggedObject = closestTobj;
                            dragDiffVector = closestDiffVector;
                            return;
                        }
                    }
                }
                // If we're not maxed out on objects, place a new object and start dragging it
                if (tuioObjects.Count < Settings.MAX_CIRCLES)
                {
                    // Find a new id that's not taken yet
                    int id = 0;
                    while (tuioObjects.Keys.Contains(id))
                    {
                        id++;
                    }

                    TuioObject tobj = new TuioObject(new TuioTime(), rand.Next(), id, mouseX, mouseY, 0f);
                    draggedObject = tobj;
                    this.addTuioObject(tobj);
                    return;
                }

                // Otherwise, it's a cursor
                draggedCursor = tcur;
                this.addTuioCursor(tcur);
                return;
            }

            // If right clicked on a circle, remove it
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                TuioObject closestTobj = null;
                float closestDistance = 1000f;
                Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                Vector2 tobjPosition = new Vector2();
                foreach (TuioObject tobj in tuioObjects.Values)
                {
                    tobjPosition.X = tobj.getScreenX(Settings.RESOLUTION_X);
                    tobjPosition.Y = tobj.getScreenY(Settings.RESOLUTION_Y);
                    Vector2 diffVector = Vector2.Subtract(mousePosition, tobjPosition);
                    float distance = diffVector.Length();
                    if (closestTobj == null)
                    {
                        closestDistance = distance;
                        closestTobj = tobj;
                    }
                    else if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTobj = tobj;
                    }
                }

                if (closestTobj != null)
                {
                    if (closestDistance <= Settings.CIRCLE_RADIUS)
                    {
                        this.removeTuioObject(closestTobj);
                        return;
                    }
                }
            }

            // If middle clicked on a circle, flip the orientation
            if (mouseState.MiddleButton == ButtonState.Pressed)
            {
                TuioObject closestTobj = null;
                float closestDistance = 1000f;
                Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                Vector2 tobjPosition = new Vector2();
                foreach (TuioObject tobj in tuioObjects.Values)
                {
                    tobjPosition.X = tobj.getScreenX(Settings.RESOLUTION_X);
                    tobjPosition.Y = tobj.getScreenY(Settings.RESOLUTION_Y);
                    Vector2 diffVector = Vector2.Subtract(mousePosition, tobjPosition);
                    float distance = diffVector.Length();
                    if (closestTobj == null)
                    {
                        closestDistance = distance;
                        closestTobj = tobj;
                    }
                    else if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTobj = tobj;
                    }
                }

                if (closestTobj != null)
                {
                    if (closestDistance <= Settings.CIRCLE_RADIUS)
                    {
                        if (this.zoomedCircles[closestTobj.getSymbolID()].AttachedGuide.IsUpsideDown)
                            this.zoomedCircles[closestTobj.getSymbolID()].AttachedGuide.IsUpsideDown = false;
                        else
                            this.zoomedCircles[closestTobj.getSymbolID()].AttachedGuide.IsUpsideDown = true;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Update game state based on TUIO object inputs.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void processTuioObjects(GameTime gameTime)
        {
            // Tag ZoomedCircles without corresponding tuioObjects as expiring, or unexpire those that do have corresponding tuioObjects
            foreach (long id in zoomedCircles.Keys)
            {
                if (!tuioObjects.Keys.Contains(id))
                {
                    if (!zoomedCircles[id].isExpiring)
                        // Only set this when circle first starts expiring
                        zoomedCircles[id].expirationStartTime = gameTime.TotalGameTime.TotalMilliseconds;
                    zoomedCircles[id].isExpiring = true;
                }
                else
                {
                    zoomedCircles[id].isExpiring = false;
                }

                // Remove expired ZoomedCircles
                if (zoomedCircles[id].isExpiring && (gameTime.TotalGameTime.TotalMilliseconds - zoomedCircles[id].expirationStartTime > Settings.CROSSHAIRS_RING_UP_DELAY_TIME + Settings.CROSSHAIRS_ON_RING_UP_ZOOM_TIME))
                {
                    ZoomCircle removedCircle;
                    if (zoomedCircles.TryRemove(id, out removedCircle))
                    {
                        removedCircle.ReturnPlankton();
                    }
                }
            }

            // NOTE: This section not needed any more because ZoomCircles now manage their own state AND video no longer pauses when circles are open

            // Check and update application state
            //if (!zoomsOpen && tuioObjects.IsEmpty)
            //{
            //    // Keep calm and carry on
            //}
            //else if (!zoomsOpen && !tuioObjects.IsEmpty)
            //{
            //    // Transition from mapview to open circles
            //    // Pause movie
            //    if (videoPlayer.State == MediaState.Playing && MOVIE_PAUSE_WHEN_CIRCLES_SHOWN)
            //        videoPlayer.Pause();

            //    // Flag state change
            //    zoomsOpen = true;
            //}
            //else if (zoomsOpen && tuioObjects.IsEmpty)
            //{
            //    // Check if circles have been open for long enough without seeing objects                    
            //    if (gameTime.TotalGameTime.TotalMilliseconds - lastObjectTime >= CIRCLE_OPEN_TIME)
            //    {
            //        // If so, close them
            //        zoomedCircles = new ConcurrentDictionary<long, ZoomCircle>();

            //        // If so, resume movie playback
            //        if (videoPlayer.State == MediaState.Paused)
            //            videoPlayer.Resume();

            //        // Flag state change
            //        zoomsOpen = false;
            //    }
            //    // Check if we should start fading circles out
            //    else if (gameTime.TotalGameTime.TotalMilliseconds - lastObjectTime + CIRCLE_FADEOUT_TIME >= CIRCLE_OPEN_TIME)
            //    {
            //        foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
            //        {
            //            zoomedCircle.FadeOut(gameTime);
            //        }
            //    }
            //}
            //else if (zoomsOpen && !tuioObjects.IsEmpty)
            //{
            //    // Stop fading circles and reset to full opacity
            //    foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
            //    {
            //        if (zoomedCircle.fadeType == "out")
            //        {
            //            zoomedCircle.fadeType = "none";
            //        }
            //    }

            //    // Remember last time there was an object to determine whether a state transition occurs
            //    lastObjectTime = gameTime.TotalGameTime.TotalMilliseconds;
            //}

            // Handle all TUIO objects
            foreach (KeyValuePair<long, TuioObject> entry in tuioObjects)
            {
                long tKey = entry.Key;
                TuioObject tuioObject = entry.Value;

                // If it's new, create a new circle
                if (!zoomedCircles.Keys.Contains(tKey))
                {
                    addZoomedCircle(tuioObject.getScreenX(Settings.RESOLUTION_X), tuioObject.getScreenY(Settings.RESOLUTION_Y), tKey, tuioObject.getAngleDegrees());
                }
                else
                {
                    // It already exists, so just update the current corresponding circle
                    //Debug.WriteLine(tuioObject.getVelocityFromPath(RESOLUTION_X, RESOLUTION_Y));
                    //zoomedCircles[tKey].MoveTo(tuioObject.getScreenX(RESOLUTION_X), tuioObject.getScreenY(RESOLUTION_Y), tuioObject.getAngleDegrees(), this.GetMovieTimestamp(), this.GetMovieTimestampRaw(), tuioObject.getVelocityFromPath(RESOLUTION_X, RESOLUTION_Y));

                    // Calculate speed from x and y speeds
                    float totalSpeed = (float)Math.Sqrt(Math.Pow(tuioObject.getXSpeed() * Settings.RESOLUTION_X, 2) + Math.Pow(tuioObject.getYSpeed() * Settings.RESOLUTION_Y, 2));

                    // Ignore implausibly high values because Multitaction is giving us speeds of "Infinity" every couple updates
                    if (totalSpeed > 1000f)
                        totalSpeed = 0f;

                    //Debug.WriteLine(totalSpeed);
                    zoomedCircles[tKey].MoveTo(tuioObject.getScreenX(Settings.RESOLUTION_X), tuioObject.getScreenY(Settings.RESOLUTION_Y), tuioObject.getAngleDegrees(), this.GetMovieTimestamp(), this.GetMovieTimestampRaw(), totalSpeed);
                }
            }
        }

        private void ConsumeCursorAndHandEvents()
        {
            while (!tuioCursorAddQueue.IsEmpty)
            {
                TuioCursor tcur;
                if (tuioCursorAddQueue.TryDequeue(out tcur))
                {
                    if (Settings.TOUCHONLY)
                        processTouchOnlyAddEvent(tcur);

                    tuioCursors.AddOrUpdate(tcur.getCursorID(), tcur, (key, oldValue) => { oldValue.update(TuioTime.getSessionTime(), tcur.getX(), tcur.getY(), tcur.getXSpeed(), tcur.getYSpeed(), tcur.getMotionAccel()); return oldValue; });
                }
            }
            while (!tuioCursorUpdateQueue.IsEmpty)
            {
                TuioCursor tcur;
                if (tuioCursorUpdateQueue.TryDequeue(out tcur))
                {
                    if (Settings.TOUCHONLY)
                        processTouchOnlyUpdateEvent(tcur);

                    tuioCursors.AddOrUpdate(tcur.getCursorID(), tcur, (key, oldValue) => { oldValue.update(TuioTime.getSessionTime(), tcur.getX(), tcur.getY(), tcur.getXSpeed(), tcur.getYSpeed(), tcur.getMotionAccel()); return oldValue; });
                }
            }

            while (!tuioHandAddOrUpdateQueue.IsEmpty)
            {
                TuioHand thand;
                tuioHandAddOrUpdateQueue.TryDequeue(out thand);
                tuioHands.AddOrUpdate(thand.getHandID(), thand, updateTHand);
            }

            this.CheckTuioCursors();

            while (!tuioCursorRemoveQueue.IsEmpty)
            {
                TuioCursor tcur;
                if (tuioCursorRemoveQueue.TryDequeue(out tcur))
                {
                    if (Settings.TOUCHONLY)
                        processTouchOnlyRemoveEvent(tcur.getSessionID());

                    tuioCursors.TryRemove(tcur.getCursorID(), out tcur);
                }
            }

            while (!tuioHandRemoveQueue.IsEmpty)
            {
                TuioHand thand;
                tuioHandRemoveQueue.TryDequeue(out thand);
                tuioHands.TryRemove(thand.getHandID(), out thand);
            }

            if (Settings.TOUCHONLY)
                this.CleanUpDraggedCircles();
        }

        /// <summary>
        /// Makes sure the touch points corresponding to dragged zoomCircles still exist.
        /// </summary>
        private double secondsSinceLastCheck = 0;
        private void CleanUpDraggedCircles()
        {
            if (gameTime.TotalGameTime.TotalSeconds - secondsSinceLastCheck > 10)
            {
                lock (touchOnlyLock)
                {
                    secondsSinceLastCheck = gameTime.TotalGameTime.TotalSeconds;

                    // Check for touches that have dragged objects for too long (currently 30 seconds) and release their dragged objects.
                    foreach (long id in draggedObjects.Keys)
                    {
                        TuioCursor matchedTcur = null;
                        foreach (TuioCursor tcur in tuioCursors.Values)
                        {
                            if (tcur.getSessionID() == id)
                            {
                                matchedTcur = tcur;
                            }
                        }
                        if (matchedTcur == null)
                        {
                            //Debug.WriteLine("Releasing dragged object with no corresponding touch point in CleanUpDraggedCircles()");
                            //processTouchOnlyRemoveEvent(id);
                        }
                        else
                        {
                            TuioTime tcurLifespan = TuioTime.getSessionTime() - matchedTcur.getStartTime();
                            if (tcurLifespan.getSeconds() > 30)
                            {
                                Debug.WriteLine("Releasing dragged object with expired touch point in CleanUpDraggedCircles()");
                                processTouchOnlyRemoveEvent(id);
                            }
                        }
                    }

                    // Check for tuio objects that are no longer in draggedObjects or freeObjects and add them back to freeObjects.
                    foreach (TuioObject tobj in tuioObjects.Values)
                    {
                        if (!draggedObjects.ContainsKey(tobj.getSymbolID()) && !freeObjects.ContainsKey(tobj.getSymbolID()))
                        {
                            freeObjects.TryAdd(tobj.getSymbolID(), tobj);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks all current TuioCursors for button presses, and invokes actions if needed.
        /// </summary>
        private void CheckTuioCursors()
        {
            foreach (TuioCursor tcur in tuioCursors.Values)
            {
                int pathCount = tcur.getPath().Count();
                // Ignore abnormally long paths
                if (pathCount < 100)
                {
                    List<TuioPoint> path;
                    path = tcur.getPath();
                    //Debug.WriteLine("Path count: " + path.Count());

                    // Check to see if cursor has been around long enough, if it started on a button, and is currently on a button
                    TuioPoint firstPoint = path.First();
                    TuioPoint lastPoint = path.Last();
                    long firstTime = firstPoint.getTuioTime().getTotalMilliseconds();
                    long lastTime;

                    //// Use  the last point in the path as the time of the last point
                    //lastTime = lastPoint.getTuioTime().getTotalMilliseconds();

                    //// If tuio cursor is about to be removed, use the current time as the time of the last point
                    //if (tuioCursorRemoveQueue.Contains(tcur))
                    //{
                    //    lastTime = TuioTime.getSystemTime().getTotalMilliseconds();
                    //}

                    // Use the current time as the time of the last point
                    lastTime = TuioTime.getSystemTime().getTotalMilliseconds();

                    //Debug.WriteLine("Tuio cursor path count: " + path.Count());

                    ZoomCircle firstButtonZoomCircle, lastButtonZoomCircle;
                    string firstButtonName, lastButtonName;
                    WouldPushButton(firstPoint, out firstButtonZoomCircle, out firstButtonName);
                    WouldPushButton(lastPoint, out lastButtonZoomCircle, out lastButtonName);

                    // Calculate average of all path points
                    float averageX = 0, averageY = 0;
                    TuioPoint[] pathArray = new TuioPoint[pathCount];
                    path.CopyTo(0, pathArray, 0, pathCount); // Copy to a new array before enumerating to avoid concurrency issues
                    foreach (TuioPoint tpoint in pathArray)
                    {
                        averageX += tpoint.getScreenX(Settings.RESOLUTION_X);
                        averageY += tpoint.getScreenY(Settings.RESOLUTION_Y);
                    }
                    averageX /= path.Count;
                    averageY /= path.Count;

                    // Check to see if touch starts and ends on a button and lasted long enough
                    if (firstButtonZoomCircle == lastButtonZoomCircle && firstButtonName == lastButtonName && lastTime - firstTime > Settings.INPUT_TOUCH_TIME)
                    {
                        switch (firstButtonName)
                        {
                            case "tab1":
                                firstButtonZoomCircle.AttachedGuide.
                                    Tab1ButtonPressed();
                                break;
                            case "tab2":
                                firstButtonZoomCircle.AttachedGuide.Tab2ButtonPressed();
                                break;
                            case "tab3":
                                firstButtonZoomCircle.AttachedGuide.Tab3ButtonPressed();
                                break;
                            case "tab4":
                                firstButtonZoomCircle.AttachedGuide.Tab4ButtonPressed();
                                break;
                            case "close":
                                if (!Settings.INPUT_SWIPES_ONLY)
                                    firstButtonZoomCircle.AttachedGuide.CloseButtonPressed();
                                break;
                            case "open":
                                if (!Settings.INPUT_SWIPES_ONLY)
                                    firstButtonZoomCircle.AttachedGuide.OpenButtonPressed(IsUpsideDown(tcur));
                                break;
                        }
                    }
                    // Check if touch started on a swipable button, and was swiped in the appropriate direction
                    else if (firstButtonName == "close" || firstButtonName == "open")
                    {
                        // Pass average of path to ZoomCircle for it to check
                        firstButtonZoomCircle.AttachedGuide.CheckButtonSwipe(firstButtonName, averageX, averageY, IsUpsideDown(tcur));
                    }
                }
            }
        }

        public ZoomCircle addZoomedCircle(int mouseX, int mouseY, long id, float angle = 0f)
        {
            ZoomCircle zoomedCircle = new ZoomCircle(gameTime, id);
            zoomedCircle.OpenAt(mouseX, mouseY, angle, this.GetMovieTimestamp(), this.GetMovieTimestampRaw());
            zoomedCircle.UpdateTextures();
            zoomedCircles.AddOrUpdate(id, zoomedCircle, (k, v) => v);
            return zoomedCircle;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Update this object's gameTime
            PlanktonPopulations.gameTime = gameTime;

            // Update circle textures
            foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
            {
                //Debug.WriteLine("Updating circle texture...");
                zoomedCircle.UpdateTextures();
            }

            // Get ready to draw
            GraphicsDevice.SetRenderTarget(fullScreenTarget);
            GraphicsDevice.Clear(Color.Black);

            // Draw the movie texture to the scaled, vertically centered rectangle
            //movieDestination = new Rectangle(GraphicsDevice.Viewport.X, GraphicsDevice.Viewport.Y, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, null, null);
            spriteBatch.Draw(currentFrame, movieDestination, Color.White);

            // Draw the continents
            spriteBatch.Draw(continentsImage, movieDestination, Settings.CONTINENT_COLOR);

            // DEBUG: Draw object locations directly
            //foreach (TuioObject tobj in tuioObjects.Values)
            //{
            //    spriteBatch.DrawCircle(tobj.getScreenX(RESOLUTION_X), tobj.getScreenY(RESOLUTION_Y), 100, 64, Color.White);
            //}

            this.timeline.Draw(spriteBatch);

            if (zoomsOpen)
            {
                // Draw each temperature tool
                foreach (TempTool tempTool in tempTools)
                {
                    spriteBatch.Draw(tempIcon, tempTool.position - new Vector2(tempIcon.Width / 2, tempIcon.Height - 15), new Color(255, 255, 255, 80));
                    string tempString = Math.Round((double)tempTool.temperature, 1).ToString() + (char)176;
                    Vector2 tempStringBounds = mediumFont.MeasureString(tempString);
                    Vector2 tempStringPosition = new Vector2(tempTool.position.X - tempStringBounds.X / 2 + 7, tempTool.position.Y + 10);
                    spriteBatch.DrawString(mediumFont, tempString, tempStringPosition, Color.White);

                    // Draw a red line
                    spriteBatch.DrawLine(new Vector2(tempTool.position.X - 2, tempTool.position.Y), new Vector2(tempTool.position.X - 2, tempTool.position.Y - tempTool.temperature - 5), Color.Red, 5.0F);

                    // Draw a red circle
                    spriteBatch.DrawCircle(new Vector2(tempTool.position.X + 0.5F, tempTool.position.Y), 7.0F * tempTool.temperature / 28.0F, 32, Color.Red, 7.0F * tempTool.temperature / 28.0F);
                }

                // Draw each nutrient tool
                foreach (NutrientTool nutrientTool in nutrientTools)
                {
                    //spriteBatch.Draw(tempIcon, tempTool.position - new Vector2(tempIcon.Width / 2, tempIcon.Height - 15), Color.White);

                    // Draw nutrient circle
                    float nutrientRadius = 10 * (float)Math.Sqrt(nutrientTool.SiO2) + 30;
                    spriteBatch.DrawCircle(nutrientTool.position, nutrientRadius, 128, new Color(169, 169, 169, 80), nutrientRadius);

                    // Draw nutrient text
                    string nutrientString = "SiO2";
                    Vector2 nutrientStringBounds = mediumFont.MeasureString(nutrientString);
                    Vector2 nutrientStringPosition = new Vector2(nutrientTool.position.X - nutrientStringBounds.X / 2, nutrientTool.position.Y - nutrientStringBounds.Y / 2 - 10);
                    spriteBatch.DrawString(mediumFont, nutrientString, nutrientStringPosition, Color.White);

                    nutrientString = Math.Round((double)nutrientTool.SiO2, 1).ToString();
                    nutrientStringBounds = mediumFont.MeasureString(nutrientString);
                    nutrientStringPosition = new Vector2(nutrientTool.position.X - nutrientStringBounds.X / 2, nutrientTool.position.Y - nutrientStringBounds.Y / 2 + 10);
                    spriteBatch.DrawString(mediumFont, nutrientString, nutrientStringPosition, Color.White);

                    // Draw a red line
                    //spriteBatch.DrawLine(new Vector2(tempTool.position.X - 2, tempTool.position.Y), new Vector2(tempTool.position.X - 2, tempTool.position.Y - tempTool.temperature - 5), Color.Red, 5.0F);

                    // Draw a red circle
                    //spriteBatch.DrawCircle(new Vector2(tempTool.position.X + 0.5F, tempTool.position.Y), 7.0F * tempTool.temperature / 28.0F, 32, Color.Red, 7.0F * tempTool.temperature / 28.0F);
                }
            }

            // DEBUG: Draw an icon if the app is running slowly
            if (gameTime.IsRunningSlowly && Settings.SHOW_RUNNING_SLOWLY)
            {
                spriteBatch.Draw(hourglassIcon, new Rectangle(0, 0, 100, 100), Color.OrangeRed);
            }

            // DEBUG: Draw touch locations
            if (Settings.SHOW_TOUCHES)
            {
                foreach (TuioCursor tcur in tuioCursors.Values)
                {
                    spriteBatch.DrawCircle(new Vector2(tcur.getScreenX(Settings.RESOLUTION_X), tcur.getScreenY(Settings.RESOLUTION_Y)), 10f, 32, Color.LightYellow, 3f);
                }
            }


            spriteBatch.End();

            // Draw everything so far to the back buffer
            GraphicsDevice.SetRenderTarget(null);
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, null, null);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
            spriteBatch.Draw((Texture2D)fullScreenTarget, new Vector2(0, 0), Color.White);

            // Draw each zoomed circle
            foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
            {
                zoomedCircle.Draw(spriteBatch);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private int GetMovieTimestamp()
        {
            // Return current timestamp of movie playback
            double playPositionNormalized = (double)videoPlayer.PlayPosition.Ticks / (double)video.Duration.Ticks;
            // 52704 to 210384+1080=211464
            int timestamp = ((int)(playPositionNormalized * (211463.0 - 52704.0) / 1080.0)) * 1080 + 52704;
            return timestamp;
        }

        private double GetMovieTimestampRaw()
        {
            // Return current timestamp of movie playback
            double playPositionNormalized = (double)videoPlayer.PlayPosition.Ticks / (double)video.Duration.Ticks;
            // 52704 to 210384+1080=211464
            double timestamp = playPositionNormalized * (211463.0 - 52704.0) + 52704.0;
            return timestamp;
        }

        private void CreateMaskTexture()
        {
            if (Settings.TOUCHONLY)
            {
                maskTarget = new RenderTarget2D(GraphicsDevice, (int)(Settings.CIRCLE_RADIUS - ZoomCircle.ZoomCircleWidth) * 2, (int)(Settings.CIRCLE_RADIUS - ZoomCircle.ZoomCircleWidth) * 2);
            }
            else
            {
                maskTarget = new RenderTarget2D(GraphicsDevice, (int)(Settings.CIRCLE_RADIUS + Settings.CIRCLE_RADIUS_OVERSCAN) * 2, (int)(Settings.CIRCLE_RADIUS + Settings.CIRCLE_RADIUS_OVERSCAN) * 2);
            }

            // Create a circular mask texture
            //RenderTarget2D renderTargetTemp = new RenderTarget2D(GraphicsDevice, (int)(Game1.CIRCLE_RADIUS + Game1.CIRCLE_RADIUS_OVERSCAN) * 2, (int)(Game1.CIRCLE_RADIUS + Game1.CIRCLE_RADIUS_OVERSCAN) * 2,false,SurfaceFormat.Color,DepthFormat.None,0,RenderTargetUsage.PreserveContents);
            Vector2 center = new Vector2(maskTarget.Width / 2, maskTarget.Height / 2);

            //Vector2 cente rna = new Vector2(Game1.CIRCLE_RADIUS + 25, Game1.CIRCLE_RADIUS + 25);
            //RenderTarget2D renderTargetTemp = new RenderTarget2D(GraphicsDevice, (int)Game1.CIRCLE_RADIUS * 2 + 50, (int)Game1.CIRCLE_RADIUS * 2 + 50);
            GraphicsDevice.SetRenderTarget(maskTarget);
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            if (Settings.TOUCHONLY)
            {
                spriteBatch.DrawCircle(center, Settings.CIRCLE_RADIUS - ZoomCircle.ZoomCircleWidth, 64, Color.Transparent, Settings.CIRCLE_RADIUS - ZoomCircle.ZoomCircleWidth);
            }
            else
            {
                spriteBatch.DrawCircle(center, Settings.CIRCLE_RADIUS, 64, Color.Transparent, Settings.CIRCLE_RADIUS);
            }
            spriteBatch.End();
            //maskTexture = new Texture2D(GraphicsDevice, renderTargetTemp.Width, renderTargetTemp.Height);
            GraphicsDevice.SetRenderTarget(null);
            maskTexture = (Texture2D)maskTarget;
            //maskTarget.GetData<Color>(Game1.maskTextureArray,0,maskTextureArray.Length);
        }

        // Check each ZoomedCircle to see if cursor pressed a button
        /// <summary>
        /// Check all ZoomCircles to see if cursor pressed a button
        /// </summary>
        /// <param name="tcur">The cursor to check against all ZoomCircles and their buttons</param>
        /// <returns>Returns a tuple consisting of a ZoomCircle, and a text description of which button on that ZoomCircle the cursor would press. Otherwise, null and emptry string.</returns>
        public void WouldPushButton(TuioPoint tPoint, out ZoomCircle outZoomCircle, out string outButtonName)
        {
            // Check each ZoomedCircle to see if cursor pressed a button
            Vector2 cursorPosition = new Vector2(tPoint.getScreenX(Settings.RESOLUTION_X), tPoint.getScreenY(Settings.RESOLUTION_Y));
            foreach (ZoomCircle zoomedCircle in zoomedCircles.Values)
            {
                Vector2 openButtonPosition, closeButtonPosition, tab1, tab2, tab3, tab4;
                if (zoomedCircle.AttachedGuide.IsLeftward)
                {
                    openButtonPosition = Settings.CALLOUT_OPEN_BUTTON_LEFT;
                    closeButtonPosition = Settings.CALLOUT_CLOSE_BUTTON_LEFT;
                    if (zoomedCircle.AttachedGuide.IsUpsideDown)
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
                    if (zoomedCircle.AttachedGuide.IsUpsideDown)
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
                if (zoomedCircle.AttachedGuide.CurrentState == Guide.GuideState.CLOSED)
                {
                    Vector2 differenceVector = cursorPosition - zoomedCircle.position - openButtonPosition;
                    // If within a certain radius, trigger callout opening for the matched circle
                    if ((int)differenceVector.Length() <= Settings.CALLOUT_DETECTION_RADIUS)
                    {
                        outZoomCircle = zoomedCircle;
                        outButtonName = "open";
                        return;
                    }
                    //Debug.WriteLine(differenceVector.Length());
                }
                else if (zoomedCircle.AttachedGuide.CurrentState == Guide.GuideState.OPEN)
                {
                    Vector2 differenceVector = cursorPosition - zoomedCircle.position - closeButtonPosition;
                    if ((int)differenceVector.Length() <= Settings.CALLOUT_DETECTION_RADIUS)
                    {
                        outZoomCircle = zoomedCircle;
                        outButtonName = "close";
                        return;
                    }
                    differenceVector = cursorPosition - zoomedCircle.position - tab1;
                    if ((Math.Abs(differenceVector.X) <= (Settings.CALLOUT_TAB_WIDTH / 2)) && (Math.Abs(differenceVector.Y) <= (Settings.CALLOUT_TAB_HEIGHT / 2)))
                    {
                        outZoomCircle = zoomedCircle;
                        outButtonName = "tab1";
                        return;
                    }
                    differenceVector = cursorPosition - zoomedCircle.position - tab2;
                    if ((Math.Abs(differenceVector.X) <= (Settings.CALLOUT_TAB_WIDTH / 2)) && (Math.Abs(differenceVector.Y) <= (Settings.CALLOUT_TAB_HEIGHT / 2)))
                    {
                        outZoomCircle = zoomedCircle;
                        outButtonName = "tab2";
                        return;
                    }
                    differenceVector = cursorPosition - zoomedCircle.position - tab3;
                    if ((Math.Abs(differenceVector.X) <= (Settings.CALLOUT_TAB_WIDTH / 2)) && (Math.Abs(differenceVector.Y) <= (Settings.CALLOUT_TAB_HEIGHT / 2)))
                    {
                        outZoomCircle = zoomedCircle;
                        outButtonName = "tab3";
                        return;
                    }
                    differenceVector = cursorPosition - zoomedCircle.position - tab4;
                    if ((Math.Abs(differenceVector.X) <= (Settings.CALLOUT_TAB_WIDTH / 2)) && (Math.Abs(differenceVector.Y) <= (Settings.CALLOUT_TAB_HEIGHT / 2)))
                    {
                        outZoomCircle = zoomedCircle;
                        outButtonName = "tab4";
                        return;
                    }
                }
            }
            outZoomCircle = null;
            outButtonName = "";
        }

        private bool IsUpsideDown(TuioCursor tcur)
        {
            bool upsideDown = false;
            //int cursorID2 = tcur.getPointsorID();
            int cursorID = (int)tcur.getSessionID();
            //Debug.WriteLine("Cursor session ID: " + cursorID);
            //Debug.WriteLine("Cursor ID: " + cursorID2);
            // Search all currently known hands for the cursor ID
            foreach (TuioHand thand in tuioHands.Values)
            {
                if (thand.getFingerID1() == cursorID || thand.getFingerID2() == cursorID || thand.getFingerID3() == cursorID || thand.getFingerID4() == cursorID || thand.getFingerID5() == cursorID)
                {
                    if (thand.getY() < tcur.getY())
                        upsideDown = true;
                }
            }
            return upsideDown;
        }

        // TuioListener interface methods

        // this is called when an object becomes visible
        public void addTuioObject(TuioObject tobj)
        {
            this.updateTuioObject(tobj);
            //if (readyForInput)
            //{
            //    // If using calibration values, modify TuioObject using calibration values before passing it on
            //    if (USE_FIDUCIALS && USE_INDIVIDUAL_CALIBRATION)
            //        //ApplyCalibrations(tobj);
            //        // ERICSOCO ADDED:
            //        try
            //        {
            //            ApplyIndividualCalibrations(tobj);
            //        }
            //        catch (ArgumentOutOfRangeException aoore)
            //        {
            //            return;
            //        }

            //    // Only accept new objects if coordinates are within movieDestination
            //    if (movieDestination.Contains(tobj.getScreenX(RESOLUTION_X), tobj.getScreenY(RESOLUTION_Y) - 1))
            //    {
            //        //Debug.WriteLine("Object added!, x=" + tobj.getScreenX(RESOLUTION_X) + ", y=" + tobj.getScreenY(RESOLUTION_Y) + ", angle=" + tobj.getAngleDegrees() + ", symbolID=" + tobj.getSymbolID() + ", sessionID=" + tobj.getSessionID() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);
            //        //if (USE_FIDUCIALS) // Use symbol ID's from fiducials instead of session ID's
            //        //{
            //        //   tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
            //        //}
            //        //else
            //        //{
            //        //    tuioObjects.AddOrUpdate(tobj.getSessionID(), tobj, updateTObj);
            //        //}
            //        if (USE_GLOBAL_CALIBRATION)
            //            tobj.update(((tobj.getX() * RESOLUTION_X + GLOBAL_X_SHIFT) / (float)RESOLUTION_X), ((tobj.getY() * RESOLUTION_Y + GLOBAL_Y_SHIFT) / (float)RESOLUTION_Y));
            //        tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
            //    }
            //}
        }

        // an object was removed from the table
        public void removeTuioObject(TuioObject tobj)
        {
            if (readyForInput)
            {
                //Debug.WriteLine("Object removed!");
                TuioObject dummy;
                //if (USE_FIDUCIALS) // Use symbol ID's from fiducials instead of session ID's
                //{
                //    tuioObjects.TryRemove(tobj.getSymbolID(), out dummy);
                //}
                //else
                //{
                //    tuioObjects.TryRemove(tobj.getSessionID(), out dummy);
                //}
                tuioObjects.TryRemove(tobj.getSymbolID(), out dummy);
            }
        }

        // an object was moved on the table surface
        int secondsRunning = 0;
        int updatesCount = 0;
        public void updateTuioObject(TuioObject tobj)
        {
            if (readyForInput)
            {
                // If using calibration values, modify TuioObject using calibration values before passing it on
                if (Settings.INPUT_USE_FIDUCIALS && Settings.USE_INDIVIDUAL_CALIBRATION)
                    ApplyIndividualCalibrations(tobj);
                tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                /*
                // Only accept updates if coordinates are within movieDestination
                if (movieDestination.Contains(tobj.getScreenX(Settings.RESOLUTION_X), tobj.getScreenY(Settings.RESOLUTION_Y) - 1))
                {
                    //Debug.WriteLine("Object update!, x=" + tobj.getScreenX(RESOLUTION_X) + ", y=" + tobj.getScreenY(RESOLUTION_Y) + ", speed=" + tobj.getMotionSpeed() + ", accel=" + tobj.getMotionAccel() + ", angle=" + tobj.getAngleDegrees() + ", symbolID=" + tobj.getSymbolID() + ", sessionID=" + tobj.getSessionID() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);
                    //Debug.WriteLine("Oject velocity: " + tobj.getVelocityFromPath(RESOLUTION_X,RESOLUTION_Y));
                    //if (USE_FIDUCIALS) // Use symbol ID's from fiducials instead of session ID's
                    //{
                    //    //if (!USE_CALIBRATION || (USE_CALIBRATION && tobj.getSymbolID() < 10))
                    //        tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                    //}
                    //else
                    //{
                    //    tuioObjects.AddOrUpdate(tobj.getSessionID(), tobj, updateTObj);
                    //}
                    if (Settings.INPUT_USE_GLOBAL_CALIBRATION)
                        tobj.update(((tobj.getX() * Settings.RESOLUTION_X + Settings.INPUT_GLOBAL_X_SHIFT) / (float)Settings.RESOLUTION_X), ((tobj.getY() * Settings.RESOLUTION_Y + Settings.INPUT_GLOBAL_Y_SHIFT) / (float)Settings.RESOLUTION_Y));
                    if (zoomedCircles.ContainsKey(tobj.getSymbolID()))
                    {
                        // If this is an update, smooth motion by ignoring small changes
                        ZoomCircle oldZoomCircle = zoomedCircles[tobj.getSymbolID()];
                        Vector2 oldPosition = oldZoomCircle.position;
                        //Debug.WriteLine(oldPosition.X + ", " + oldPosition.Y + "\t" + tobj.getScreenX(RESOLUTION_X) + ", " + tobj.getScreenY(RESOLUTION_Y));

                        //Debug.WriteLineIf((Math.Abs(oldZoomCircle.position.X - tobj.getScreenX(RESOLUTION_X)) > 0), Math.Abs(oldZoomCircle.position.X - tobj.getScreenX(RESOLUTION_X)));
                        //Debug.WriteLine(oldPosition.X + ", " + oldPosition.Y + "\t" + tobj.getScreenX(RESOLUTION_X) + ", " + tobj.getScreenY(RESOLUTION_Y));
                        List<TuioPoint> path = tobj.getPath();
                        int pathCount = path.Count;
                        if (pathCount >= 10)
                        {
                            List<TuioPoint> lastPoints = path.GetRange(pathCount - 10, 10);
                            float yDiff = 0f;
                            float xDiff = 0f;
                            foreach (TuioPoint tpoint in lastPoints)
                            {
                                xDiff += Math.Abs(tpoint.getX() * Settings.RESOLUTION_X - oldPosition.X);
                                yDiff += Math.Abs(tpoint.getY() * Settings.RESOLUTION_Y - oldPosition.Y);
                            }
                            if (xDiff < 10f * Settings.CIRCLE_POSITION_CHANGE_THRESHOLD || yDiff < 10f * Settings.CIRCLE_POSITION_CHANGE_THRESHOLD)
                            {
                                tobj.update(oldPosition.X / (float)Settings.RESOLUTION_X, oldPosition.Y / (float)Settings.RESOLUTION_Y);
                            }
                            else
                            {
                                tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                            }

                            //if ((Math.Abs(oldPosition.X - tobj.getScreenX(RESOLUTION_X)) < CIRCLE_POSITION_CHANGE_THRESHOLD) ||
                            //    (Math.Abs(oldPosition.Y - tobj.getScreenY(RESOLUTION_Y)) < CIRCLE_POSITION_CHANGE_THRESHOLD))
                            //{
                            //    tobj.update(oldPosition.X / RESOLUTION_X, oldPosition.Y / RESOLUTION_Y);
                            //}
                            //else
                            //{
                            //    tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                            //}
                        }
                        else
                        {
                            tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                        }
                    }
                    else
                    {
                        // Otherwise just add it
                        tuioObjects.AddOrUpdate(tobj.getSymbolID(), tobj, updateTObj);
                    }                 
                }
                */

                // Measure how many total updates / second we're getting
                //if ((int)gameTime.TotalGameTime.TotalSeconds > secondsRunning)
                //{
                //    Debug.WriteLine("Object updates per second:" + updatesCount);
                //    secondsRunning = (int)gameTime.TotalGameTime.TotalSeconds;
                //    updatesCount = 0;
                //}
                //else
                //{
                //    updatesCount++;
                //}
            }
        }

        private static void ApplyIndividualCalibrations(TuioObject tobj)
        {
            if (Settings.USE_INDIVIDUAL_CALIBRATION) // Modify object using calibration values before passing it on
            {
                int symbolID = tobj.getSymbolID();

                // ERICSOCO ADDED
                if (symbolID > 4095)
                {
                    throw new ArgumentOutOfRangeException();
                }

                // Retrieve calibration values for this object
                float calA1 = MathHelper.ToRadians(Settings.CALIBRATIONS[symbolID, 0]);
                float calD = (float)Settings.CALIBRATIONS[symbolID, 1];
                float calA2 = MathHelper.ToRadians(Settings.CALIBRATIONS[symbolID, 2]);

                // Assume that the tuioObject is the origin, and that its orientation vector points up (along the y-axis).

                // Create a position vector based on the calibration information that is offset from the tuioObject/origin.
                Vector2 offsetVector = new Vector2((float)Math.Cos(calA1), (float)Math.Sin(calA1));
                offsetVector = Vector2.Multiply(offsetVector, calD);

                // Rotate vector around the origin by the TUIO object's orientation.
                Matrix tMatrix = Matrix.CreateRotationZ(tobj.getAngle());

                // Translate vector by the TUIO object's position.
                tMatrix *= Matrix.CreateTranslation(tobj.getScreenX(Settings.RESOLUTION_X), tobj.getScreenY(Settings.RESOLUTION_Y), 0f);

                // Apply matrix transforms.
                offsetVector = Vector2.Transform(offsetVector, tMatrix);

                // Normalize to screen resolution.
                float offsetX = offsetVector.X / Settings.RESOLUTION_X;
                float offsetY = offsetVector.Y / Settings.RESOLUTION_Y;

                // Add other calibration angle to tuioObject angle.                
                float offsetA = tobj.getAngle() + calA2;

                // Update the TUIO object.
                tobj.update(tobj.getTuioTime(), offsetX, offsetY, offsetA);

                /*
                // Add offset angle to tuioObject's rotation
                float offsetA = tobj.getAngle() + calA1;
                Vector2 offsetV = new Vector2((float)Math.Cos(offsetA), (float)Math.Sin(offsetA));

                // Move fiducial point to data point
                // offsetV.Normalize();
                Vector2 dataPoint = Vector2.Multiply(offsetV, calD);

                tobj.update(tobj.getTuioTime(), tobj.getX() + dataPoint.X / RESOLUTION_X, tobj.getY() + dataPoint.Y / RESOLUTION_Y, tobj.getAngle() + calA2);
                 */
            }
        }

        // this is called when a new cursor is detected 
        public void addTuioCursor(TuioCursor tcur)
        {
            if (readyForInput)
            {
                tuioCursorAddQueue.Enqueue(tcur);
                //Debug.WriteLine("Cursor added, x=" + tcur.getScreenX(Settings.RESOLUTION_X) + ", y=" + tcur.getScreenY(Settings.RESOLUTION_Y) + ", sessionID=" + tcur.getSessionID() + ", cursorID=" + tcur.getCursorID() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);
            }
        }

        // a cursor was removed from the table
        public void removeTuioCursor(TuioCursor tcur)
        {
            if (readyForInput)
            {
                //Debug.WriteLine("Cursor removed, x=" + tcur.getScreenX(Settings.RESOLUTION_X) + ", y=" + tcur.getScreenY(Settings.RESOLUTION_Y) + ", sessionID=" + tcur.getSessionID() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);
                tuioCursorRemoveQueue.Enqueue(tcur);

                /* Uncomment for checking cursors on remove
                TuioCursor outCursor;
                if (tuioCursors.TryRemove(tcur.getSessionID(), out outCursor))
                {
                    Debug.WriteLine("Cursor removed, x=" + tcur.getScreenX(RESOLUTION_X) + ", y=" + tcur.getScreenY(RESOLUTION_Y) + ", sessionID=" + tcur.getSessionID() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);

                    // Register button presses on cursor remove

                    // If necessary: check lifetime of cursor so that only short touches are registered
                    // Can use tcur.getTuioTime() - tcur.getStartTime()

                    // Compare removed cursor's location to initial cursor location
                    Vector2 initialPosition = new Vector2(outCursor.getScreenX(RESOLUTION_X), outCursor.getScreenY(RESOLUTION_Y));
                    Vector2 finalPosition = new Vector2(tcur.getScreenX(RESOLUTION_X), tcur.getScreenY(RESOLUTION_Y));
                    Vector2 differenceVector = initialPosition - finalPosition;

                    // If within a certain radius, check all zoomedCircle's button locations
                    if ((int)differenceVector.Length() <= (CIRCLE_INFO_BUTTON_SIZE / 2))
                    {
                        foreach (ZoomedCircle zoomedCircle in zoomedCircles.Values)
                        {
                            Vector2 buttonDifferenceVector = zoomedCircle.buttonPosition - finalPosition;

                            // If within a certain radius, trigger callout opening for the matched circle
                            if ((int)buttonDifferenceVector.Length() <= (CIRCLE_INFO_BUTTON_SIZE / 2))
                            {
                                zoomedCircle.InfoButtonPressed();
                            }
                        }
                    }
                }
              */
            }
        }

        // a cursor is moving on the table surface
        public void updateTuioCursor(TuioCursor tcur)
        {
            if (readyForInput)
            {
                //Debug.WriteLine("Cursor update, x=" + tcur.getScreenX(Settings.RESOLUTION_X) + ", y=" + tcur.getScreenY(Settings.RESOLUTION_Y) + ", sessionID=" + tcur.getSessionID() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);

                // Only update cursor locations if showing touches...will have other effects since we're not doing any sliding interactions yet
                //if (Settings.SHOW_TOUCHES)
                tuioCursorUpdateQueue.Enqueue(tcur);

                // Measure how many total updates / second we're getting
                //if ((int)gameTime.TotalGameTime.TotalSeconds > secondsRunning)
                //{
                //    Debug.WriteLine("Cursor updates per second:" + updatesCount);
                //    secondsRunning = (int)gameTime.TotalGameTime.TotalSeconds;
                //    updatesCount = 0;
                //}
                //else
                //{
                //    updatesCount++;
                //}
            }
        }

        // this method is called after each bundle, use it to repaint your screen for example  
        int refreshCount = 0;
        public void refresh(TuioTime bundleTime)
        {

            // Measure how many total updates / second we're getting
            //if ((int)gameTime.TotalGameTime.TotalSeconds > secondsRunning)
            //{
            //    Debug.WriteLine("Refreshes per second:" + refreshCount);
            //    secondsRunning = (int)gameTime.TotalGameTime.TotalSeconds;
            //    refreshCount = 0;
            //}
            //else
            //{
            //    refreshCount++;
            //}
        }

        // Update function for Tuio methods
        public TuioObject updateTObj(long key, TuioObject value)
        {
            return value;
        }

        // Update function for Tuio methods
        public TuioHand updateTHand(long key, TuioHand value)
        {
            return value;
        }

        // Update function for Tuio methods
        public TuioCursor updateTCur(long key, TuioCursor value)
        {
            return value;
        }

        public void addTuioHand(TuioHand thand)
        {
            if (readyForInput)
            {
                //Debug.WriteLine("Hand added!, x=" + thand.getScreenX(RESOLUTION_X) + ", y=" + thand.getScreenY(RESOLUTION_Y) + ", handID=" + thand.getHandID() + ", sessionID=" + thand.getSessionID() + ", finger1ID = " + thand.getFingerID1() + ", finger2ID = " + thand.getFingerID2() + ", finger3ID = " + thand.getFingerID3() + ", finger4ID = " + thand.getFingerID4() + ", finger5ID = " + thand.getFingerID5() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);
                tuioHandAddOrUpdateQueue.Enqueue(thand);
            }
        }
        public void updateTuioHand(TuioHand thand)
        {
            if (readyForInput)
            {
                //Debug.WriteLine("Hand update!, x=" + thand.getScreenX(RESOLUTION_X) + ", y=" + thand.getScreenY(RESOLUTION_Y) + ", handID=" + thand.getHandID() + ", sessionID=" + thand.getSessionID() + ", finger1ID = " + thand.getFingerID1() + ", finger2ID = " + thand.getFingerID2() + ", finger3ID = " + thand.getFingerID3() + ", finger4ID = " + thand.getFingerID4() + ", finger5ID = " + thand.getFingerID5() + ", gameTime=" + gameTime.TotalGameTime.TotalMilliseconds);
                tuioHandAddOrUpdateQueue.Enqueue(thand);
            }
        }
        public void removeTuioHand(TuioHand thand)
        {
            if (readyForInput)
            {
                //Debug.WriteLine("Hand removed!");
                tuioHandRemoveQueue.Enqueue(thand);
            }
        }
    }
}
