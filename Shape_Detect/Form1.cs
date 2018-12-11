/*
 * Bryan Walther
 * Unified Robotics 2 Robot Arm
 * Due Date: Demcember 12th 2018
 * 
 * Take camera input and locate type and center location of boxes and triangles
 * Send and Keep track of command order for robot
 * * Use serial to send commands to Arduino
 * UI to display current camera view, ROI boundary, and shape field
 * * button commands to start and home robot
 * * Display position and types of shapes
 * monitor serial communications for arduino's command requests
 * 
 */


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.IO.Ports;
using Emgu.CV.Util;
using System.IO;

namespace Shape_Detect
{
    public partial class Form1 : Form
    {
        //Video Handeling thread setup
        private Thread _captureThread;
        private InputHandler videoIn = new InputHandler();

        //Serial communication thread and accessory setup
        private const int baudRate = 9600;
        private SerialPort serialPort = new SerialPort();
        string[] ports;
        private Thread serialMonitoringThread;

        //Thread managing setup
        private static readonly object serialLockObj = new object();
        volatile bool allowRun;

        //UI global variables
        bool reCalculateROI = true;
        bool RunRobot = false;
        bool OverrideMath = false;

        //communication based global variables
        bool requestedCommand = false;
        bool commandCompleted = false;
        char selectedShape;
        char[] motionList = {'h','p','m','h','d','m' };  //motion set for picking up a shape (Home, Position, Magnet (on), Home, Drop off point, Magnet (off))
        int motionIndex = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) //Upon loading window, begin camera thread
        {
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) //when window closed: end Serial communication, end Monitor Thread, end Camera thread
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                lock (serialLockObj) //wait for Monitor thread to finish and close safely
                {
                    allowRun = false;
                }
                serialMonitoringThread.Join();
            }
            _captureThread.Abort();

        }

        private void DisplayWebcam() //main function: responible for viewing camera input, ROI selection, shape detection and location, and command braining
        {
            double cannyThreshold = 120;
            int botPixLocation = 0; //position of robot base given ~7.5in distance from bottom edge of paper
            double pixInch = 0; //pixels per inch value;
            Rectangle Resize = new Rectangle();

            while (true) //do this ALOT!!
            {
                if (videoIn == null || !videoIn.isFrameAvailable()) //if camera input not work, SKIP!
                {
                    continue;
                }

                #region Basic Image Handling
                /*
                 * Read input from Input Handler (Credit to Zack Carey)
                 * Basic noise and orientation manipuations
                 */ 

                Image<Bgr, Byte> Default = videoIn.readFrame().Convert<Bgr, Byte>().Resize(Plain_Image.Width, Plain_Image.Height, Emgu.CV.CvEnum.Inter.Linear).Flip(FlipType.Vertical).Flip(FlipType.Horizontal);

                Image<Bgr, Byte> roiImg = Default.Copy();

                //Convert the image to grayscale and filter out the noise
                UMat uimage1 = new UMat();
                CvInvoke.CvtColor(roiImg, uimage1, ColorConversion.Bgr2Gray);

                //use image pyr to remove noise
                UMat pyrDown = new UMat();
                CvInvoke.PyrDown(uimage1, pyrDown);
                CvInvoke.PyrUp(pyrDown, uimage1);

                #endregion Basic Image Handling

                #region Canny and edge detection for ROI
                /* 
                 * Edge detection, credit emguCV wiki 
                 * These shapes are used to find the ROI, which will choose the largest from those selected
                 */
                double cannyThresholdLinking1 = 120.0;
                UMat cannyEdges1 = new UMat();
                CvInvoke.Canny(uimage1, cannyEdges1, cannyThreshold, cannyThresholdLinking1);
                #endregion

                #region Make ROI for shapes
                /* Shape detection part 1:
                 * Look for largest shape on view (Sheet of paper) and scale image acordingly
                 * If no shape large enough found, does not resize image.
                 * * assumes page takes up entire view and does not have enough edge to make out page
                 * 
                 * Done in a two stage process so shapes are found in reference to ROI image size, not original size
                 */
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {

                    if (reCalculateROI) //UI element to reset ROI Image, so that calculations dont have to be run each time. (Camera does not move after all)
                    {
                        CvInvoke.FindContours(cannyEdges1, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                        int chosen = 0;
                        int edgeExtra = 5;

                        for (int i = 0; i < contours.Size; i++) //pick the largest shape of those found
                        {
                            using (VectorOfPoint contour = contours[i])
                            {
                                double maxArea = 0;
                                double area = CvInvoke.ContourArea(contour);
                                if (area > maxArea)
                                {
                                    maxArea = area;
                                    chosen = i;
                                }
                            }
                        }

                        // Getting minimal rectangle which contains the contour
                        Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[chosen]);

                        Resize = new Rectangle(boundingBox.X + edgeExtra, boundingBox.Y + edgeExtra, boundingBox.Width - 2 * edgeExtra, boundingBox.Height - 2 * edgeExtra);

                        pixInch = roiImg.Height / 8.5;  //This assumes 8.5x11 sheet of paper, then using the image height (assumed to be the paper) to be get a pixels per inch value
                        botPixLocation = CalculatePixInch(roiImg, pixInch); //the position of the forward middle point of the robot in pixels, used to calculate some things later on

                        reCalculateROI = false;
                    }
                    if (Resize.Width * Resize.Height > 10000) //resize ROIimage to be size found above
                        roiImg.ROI = Resize;
                }

                #endregion

                #region Run Robot
                if (RunRobot && (requestedCommand || OverrideMath)) //Run this part if User and Arduino ask for (or user overrides for debug and testing perposes)
                {
                    switch (motionList[motionIndex]) //next command list
                    {
                        case 'h': //Home
                            SendHomeCommand();
                            break;
                        case 'm': //Magnet
                            sendMagnetCommand();
                            break;
                        case 'p': //Position
                            //Load the image from file and resize it for display
                            Image<Bgr, Byte> img = roiImg;

                            //Convert the image to grayscale and filter out the noise
                            UMat uimage = new UMat();
                            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);


                            #region Canny and edge detection
                            /*
                             * edge detection within ROI size
                             * Again credit to emguCV wiki for the edge and shape detection
                            */
                            double cannyThresholdLinking = 120.0;
                            UMat cannyEdges = new UMat();
                            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

                            LineSegment2D[] lines = CvInvoke.HoughLinesP(
                                cannyEdges,
                                1, //Distance resolution in pixel-related units
                                Math.PI / 45.0, //Angle resolution measured in radians.
                                20, //threshold
                                30, //min Line width
                                10); //gap between lines
                            #endregion

                            #region Find triangles and rectangles
                            /*
                             * find shapes (and identify type) within ROI of paper
                             */
                            List<Triangle2DF> triangleList = new List<Triangle2DF>();
                            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle

                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                            {
                                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                                int count = contours.Size;

                                for (int i = 0; i < count; i++)
                                {
                                    using (VectorOfPoint contour = contours[i])
                                    using (VectorOfPoint approxContour = new VectorOfPoint())
                                    {
                                        if (CvInvoke.ContourArea(contour) > 100)
                                        {
                                            CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                                            if (approxContour.Size == 3) //The contour has 3 vertices, it is a triangle
                                            {
                                                Point[] pts = approxContour.ToArray();
                                                triangleList.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
                                            }
                                            else if (approxContour.Size == 4) //The contour has 4 vertices.
                                            {
                                                #region determine if all the angles in the contour are within [80, 100] degree
                                                bool isRectangle = true;
                                                Point[] pts = approxContour.ToArray();
                                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                                for (int j = 0; j < edges.Length; j++)
                                                {
                                                    double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                                    if (angle < 80 || angle > 100) //ensure that the angles of the quadralateral are (roughly) 90deg
                                                    {
                                                        isRectangle = false;
                                                        break;
                                                    }
                                                }
                                                #endregion

                                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region draw triangles and rectangles/ locate center and target
                            bool targetPoint = true;

                            Image<Bgr, Byte> triangleRectangleImage = img.CopyBlank();
                            int triangleCount = 0;
                            foreach (Triangle2DF triangle in triangleList)
                            {
                                if (triangleCount > 0) //Duplicate prevention
                                /* 
                                 * For all shapes, check to see if the center is too close to the previous shape's center (if its the same shape, just inside/outside, the centers should be the same)
                                 * ignore the ones that are too close together
                                 * for all other shaopes, outline and mark the center point
                                 */
                                {

                                    if (((Math.Abs(triangle.Centeroid.X - triangleList[triangleCount - 1].Centeroid.X) > 5) || (Math.Abs(triangle.Centeroid.Y - triangleList[triangleCount - 1].Centeroid.Y) > 5)))
                                    {
                                        triangleRectangleImage.Draw(triangle, new Bgr(Color.LightBlue), 2);

                                        triangleRectangleImage.Draw(new CircleF(new Point((int)triangle.Centeroid.X, (int)triangle.Centeroid.Y), 1), new Bgr(Color.Blue), 2);
                                    }
                                }
                                else if (triangleCount == 0)
                                /*
                                 * The code chooses the first shape in the list as the target (and as the first shape to reference back duplicate prevention)
                                 * Then, if the first shape is the Target to be picked up, it colors the center dot a different color and passes on the center coords to be calculated and sent to the Arduino
                                 * also draws a pretty line from the front center of the 'robot' to the center line
                                 * 
                                 * If the first shape is not the target (should never happen for triangles, but eh, better safe than confused as to why it broke), treat as the other shapes above
                                 */
                                {
                                    triangleRectangleImage.Draw(triangle, new Bgr(Color.LightBlue), 2);

                                    if (targetPoint)
                                    {
                                        targetPoint = false;

                                        triangleRectangleImage.Draw(new CircleF(new Point((int)triangle.Centeroid.X, (int)triangle.Centeroid.Y), 1), new Bgr(Color.Brown), 2);
                                        triangleRectangleImage.Draw(new LineSegment2D(new Point((int)triangle.Centeroid.X, (int)triangle.Centeroid.Y), new Point(triangleRectangleImage.Width / 2, botPixLocation)), new Bgr(Color.LightGreen), 1);
                                        calculateCoords((botPixLocation - (double)triangleRectangleImage.Height) + (double)(triangleRectangleImage.Height - triangle.Centeroid.Y), triangleRectangleImage.Width / 2 - (double)triangle.Centeroid.X, pixInch);
                                        selectedShape = 't';
                                    }
                                    else
                                    {
                                        triangleRectangleImage.Draw(new CircleF(new Point((int)triangle.Centeroid.X, (int)triangle.Centeroid.Y), 1), new Bgr(Color.Blue), 2);
                                    }
                                }
                                triangleCount++;
                            }
                            //triangleCount lists total number of triangle found (x2 due to interior/exterior detected)
                            Invoke(new Action(() => { labelTriagNumb.Text = $"Number of Triangles: {triangleCount / 2}"; }));

                            int boxCount = 0;
                            foreach (RotatedRect box in boxList)
                            {
                                if (boxCount > 0)
                                /* 
                                 * For all shapes, check to see if the center is too close to the previous shape's center (if its the same shape, just inside/outside, the centers should be the same)
                                 * ignore the ones that are too close together
                                 * for all other shaopes, outline and mark the center point
                                 */
                                {
                                    if (((Math.Abs(box.Center.X - boxList[boxCount - 1].Center.X) > 5) || (Math.Abs(box.Center.Y - boxList[boxCount - 1].Center.Y) > 5)))
                                    {
                                        triangleRectangleImage.Draw(box, new Bgr(Color.DarkOrange), 2);

                                        triangleRectangleImage.Draw(new CircleF(new Point((int)box.Center.X, (int)box.Center.Y), 1), new Bgr(Color.Orange), 2);
                                    }
                                }
                                else if (boxCount == 0)
                                /*
                                * The code chooses the first shape in the list as the target (and as the first shape to reference back duplicate prevention)
                                * Then, if the first shape is the Target to be picked up, it colors the center dot a different color and passes on the center coords to be calculated and sent to the Arduino
                                * also draws a pretty line from the front center of the 'robot' to the center line
                                * 
                                * If the first shape is not the target (pretty much any time there is a triangle on the board as well), treat as the other shapes above
                                */
                                {
                                    triangleRectangleImage.Draw(box, new Bgr(Color.DarkOrange), 2);

                                    if (targetPoint)
                                    {
                                        targetPoint = false;

                                        triangleRectangleImage.Draw(new CircleF(new Point((int)box.Center.X, (int)box.Center.Y), 1), new Bgr(Color.Brown), 2);
                                        triangleRectangleImage.Draw(new LineSegment2D(new Point((int)box.Center.X, (int)box.Center.Y), new Point(triangleRectangleImage.Width / 2, botPixLocation)), new Bgr(Color.LightGreen), 1);
                                        calculateCoords((botPixLocation - (double)triangleRectangleImage.Height) + (double)(triangleRectangleImage.Height - box.Center.Y), triangleRectangleImage.Width / 2 - (double)box.Center.X, pixInch);
                                        selectedShape = 'b';
                                    }

                                    else
                                    {
                                        triangleRectangleImage.Draw(new CircleF(new Point((int)box.Center.X, (int)box.Center.Y), 1), new Bgr(Color.Orange), 2);
                                    }
                                }
                                boxCount++;
                            }
                            //once again, boxCount counst the inside and outside contours, thus the total number of boxes is half boxCount
                            Invoke(new Action(() => { labelBoxNumber.Text = $"Number of Boxes: {boxCount / 2}"; }));
                            #endregion

                            #region drawCenterline
                            /*
                             * The most complex and super nessesary part of this whole code!!
                             * No really!
                             * 
                             * yeah... it just draws a line down the center of the image so that debugging was easier.
                             * It doesn't do much
                             * I should call it Perry!
                             */
                            triangleRectangleImage.Draw(new LineSegment2D(new Point((int)triangleRectangleImage.Width / 2, 0), new Point((int)triangleRectangleImage.Width / 2, (int)triangleRectangleImage.Height)), new Bgr(Color.LightCyan), 1);
                            #endregion


                            Image_Shapes.Image = triangleRectangleImage.Bitmap;//Draw the picture!
                            targetPoint = true;
                            break;
                        case 'd'://drop of points
                            SendDropCommand();
                            break;
                        default:
                            break;
                    }
                    requestedCommand = false;
                    commandCompleted = true;
                }
                #endregion Run Robot

                Plain_Image.Image = Default.Bitmap; //draw the default and ROI images
                roiBox.Image = roiImg.Bitmap;
            }
        }


        #region SendCommands
        /*
         * Anything and Everything to do with sending the commands to the robot
         * Includes the Drop, Home and Magnet Commands as well as coordinate calculator and sender
         */
        private void SendDropCommand()
        {
            switch (selectedShape)
            {
                case 't'://trianngles
                    sendCoords(8, -90); //90deg to the left of center
                    break;
                case 'b'://boxes
                    sendCoords(8, 90); //90deg to the right of center
                    break;
                default:
                    break;
            }
        }

        private void SendHomeCommand() //probably over complicated, but hey, I like that it works. Just send the command <H>
        {
            string temp;

            if (serialPort.IsOpen)
            {

                temp = $"<H>";
                Console.WriteLine(temp);

                byte[] buffer = Encoding.ASCII.GetBytes(temp);
                serialPort.Write(buffer, 0, buffer.Length);
            }
        }

        private int CalculatePixInch(Image<Bgr, byte> input, double pixInch) //used in finding the robot's location in pixels from way above
        {
            double pixToBot = (7.75) * pixInch; //robot should be around 7.75 inches away from the bottom of the page, If the robot is consistently missing, this is a good number to take a look at
            return (int)(pixToBot + input.Height);
        }

        private void calculateCoords(double distVert, double distHorz, double pixInch)//Alot of the math was already done when the function was called, but this takes the distance from the robot in x and y and gives a dist and angle
        {
            double pixDist = Math.Sqrt(Math.Pow(distVert,2) + Math.Pow(distHorz, 2));
            double inchDist = pixDist / pixInch;

            double Theta = -Math.Atan2(distHorz,distVert) * (180/Math.PI);

            Invoke(new Action(() => { //just to see what it is thinking
                labelAngleToBot.Text = $"Target Angle to Bot (deg): {Math.Round(Theta, 1)}";
                labelDistFromBot.Text = $"Target Dist to Bot (in): {Math.Round(inchDist, 1)}";
            }));
            sendCoords(inchDist, Theta); //Calls next function to actually send the commands onwards!
        }

        //Send Coords to Arm
        private void sendCoords(double targetDist, double targetTheta)
        {
            string temp;
            double targetY = 2.5; // the pick up and drop off heights dont need to be different, so they arn't. If it is too low or too high for any reason, change this number here

            if (serialPort.IsOpen)
            {
                temp = $"<{Math.Round(targetDist,1)} {targetY} {Math.Round(targetTheta,1)}>";//sends the command in the format <XXX.x YYY.y TTT.t>
                Console.WriteLine(temp);
                
                byte[] buffer = Encoding.ASCII.GetBytes(temp);
                serialPort.Write(buffer, 0, buffer.Length);
            }
        }

        private void sendMagnetCommand() //back to the easy ones, just sends the command <M>
        {
            if (serialPort.IsOpen)
            {
                string temp = $"<M>";
                byte[] buffer = Encoding.ASCII.GetBytes(temp);
                serialPort.Write(buffer, 0, buffer.Length);
            }
        }
        #endregion SendCommands

        #region SerialReciveing
        private void MonitorSerialData()
        {
            while (true)
            {
                lock (serialLockObj) //Thread managing
                {
                    if (!allowRun)
                    {
                        return;
                    }
                }

                try
                {
                    //block until \n character is received, extract command data
                    string msg = serialPort.ReadLine();
                    //confirm the string has both < and > character
                    if (msg.IndexOf("<") == -1 || msg.IndexOf(">") == -1)
                    {
                        continue;
                    }
                    //remove everything before the < character
                    msg = msg.Substring(msg.IndexOf("<") + 1);
                    //remove everything after the > character
                    msg = msg.Remove(msg.IndexOf(">"));
                    //if the resulting string is empty, disregard and move on
                    Invoke(new Action(() =>
                    {
                        serialReturn.Text = $"Returned Point Data: {msg}";
                    }));
                    Console.WriteLine(msg);
                    if (msg.Length == 0) //arduino sent empty command or gibberish
                    {
                        continue;
                    }
                    else if (msg.Substring(0,1) == "W") //I don't think this is even still in use, but I don't remember. So it stays!
                    {
                        requestedCommand = false;
                    }
                    else if (msg.Substring(0, 1) == "N")//Arduino sent next command request
                    {
                        requestedCommand = true;
                        if (commandCompleted)
                        {
                            commandCompleted = false;
                            if (++motionIndex == motionList.Length)
                            {
                                motionIndex = 0;
                            }
                        }
                        Console.WriteLine($"=========={motionIndex}");
                    }
                }
                catch (TimeoutException timeex)
                {
                    
                }
                catch(IOException ioex)
                {

                }
            }
        }
        #endregion SerialReciving

        #region InputHandling
        /*
         * Credit to Zack Carey
         * Though he informed me that this was outdated as soon as he sent it too me... Thanks pal
         */
        private void p1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (videoIn.setImage("Video Files\\P1.png"))
            {
                videoIn.play();
            }
        }

        private void p2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (videoIn.setImage("Video Files\\P2.png"))
            {
                videoIn.play();
            }
        }

        private void camera1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (videoIn.setCamera(0))
            {
                videoIn.play();
            }
        }

        private void p3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (videoIn.setImage("Video Files\\P3.png"))
            {
                videoIn.play();
            }
        }
        #endregion InoutHandling

        #region UI_Elements
        /*
         * Contains all the buttons and switches that the User can play wis
         */
        private void comboCOMList_DropDown(object sender, EventArgs e)
        {
            //update the drop down list everytime it is open to get the most recent list of serial ports
            ports = SerialPort.GetPortNames();

            comboCOMList.Items.Clear();
            foreach (string port in ports)
            {
                comboCOMList.Items.Add(port);
            }
        }

        private void comboCOMList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //If an existing communication is open, close it; then open a new com on selected port
            if (serialPort.IsOpen)
            {
                lock (serialLockObj)
                {
                    allowRun = false;
                }
                serialMonitoringThread.Join();
                serialPort.Close();
            }

            string selectedCom = comboCOMList.Text;

            if (selectedCom != "0")
            {
                //MessageBox.Show($"It is sending {selectedCom}");
                try
                {
                    serialPort.PortName = selectedCom;
                    serialPort.BaudRate = baudRate;
                    serialPort.Open();
                    serialPort.ReadTimeout = 1000;

                    serialPort.DiscardOutBuffer();
                    serialPort.DiscardInBuffer();

                    allowRun = true;
                    serialMonitoringThread = new Thread(MonitorSerialData);
                    serialMonitoringThread.Name = "Serial Monitoring";
                    serialMonitoringThread.Start();

                    //SendHomeCommand();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error initializing COM port");
                    Close();
                    throw;
                }
            }
        }

        private void boxOverRideCommand_CheckedChanged(object sender, EventArgs e)
        {//user may or may not have robot attached. They just wanted to test the shape detection
            OverrideMath = boxOverRideCommand.Checked;
            motionIndex = 1;
        }

        private void buttonHome_Click(object sender, EventArgs e)
        { //send home command; Also used to start the robot sequence (as homing the robot tells it to prompt next command)
            SendHomeCommand();
        }

        private void buttonCalcROI_Click(object sender, EventArgs e)
        {//Does what it says on the tin...
            reCalculateROI = true;
        }

        private void checkBoxRunRobot_CheckedChanged(object sender, EventArgs e)
        {//Enables the robot to be run, acts as a safty switch to prevent new commands from being sent
            RunRobot = checkBoxRunRobot.Checked;
            motionIndex = 0;
        }

        #endregion UI_Elements
    }
}
