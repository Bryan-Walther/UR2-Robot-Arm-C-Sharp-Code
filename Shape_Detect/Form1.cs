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
                /* Edge detection, credit emguCV wiki 
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

                        pixInch = roiImg.Height / 8.5;
                        botPixLocation = CalculatePixInch(roiImg, pixInch);

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
                            //edge detection within ROI size
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
                                                    if (angle < 80 || angle > 100)
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
                            int ij = 0;
                            foreach (Triangle2DF triangle in triangleList)
                            {
                                if (ij > 0) //Duplicate prevention
                                {
                                    if (((Math.Abs(triangle.Centeroid.X - triangleList[ij - 1].Centeroid.X) > 5) || (Math.Abs(triangle.Centeroid.Y - triangleList[ij - 1].Centeroid.Y) > 5)))
                                    {
                                        triangleRectangleImage.Draw(triangle, new Bgr(Color.LightBlue), 2);

                                        triangleRectangleImage.Draw(new CircleF(new Point((int)triangle.Centeroid.X, (int)triangle.Centeroid.Y), 1), new Bgr(Color.Blue), 2);
                                    }
                                }
                                else if (ij == 0)
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
                                ij++;
                            }
                            //ij lists total number of shapes found (x2 due to interior/exterior detected)
                            Invoke(new Action(() => { labelTriagNumb.Text = $"Number of Triangles: {ij / 2}"; }));

                            ij = 0;
                            foreach (RotatedRect box in boxList)
                            {
                                if (ij > 0)
                                {
                                    if (((Math.Abs(box.Center.X - boxList[ij - 1].Center.X) > 5) || (Math.Abs(box.Center.Y - boxList[ij - 1].Center.Y) > 5)))
                                    {
                                        triangleRectangleImage.Draw(box, new Bgr(Color.DarkOrange), 2);

                                        triangleRectangleImage.Draw(new CircleF(new Point((int)box.Center.X, (int)box.Center.Y), 1), new Bgr(Color.Orange), 2);
                                    }
                                }
                                else if (ij == 0)
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
                                ij++;
                            }
                            Invoke(new Action(() => { labelBoxNumber.Text = $"Number of Boxes: {ij / 2}"; }));
                            #endregion

                            #region drawCenterline
                            triangleRectangleImage.Draw(new LineSegment2D(new Point((int)triangleRectangleImage.Width / 2, 0), new Point((int)triangleRectangleImage.Width / 2, (int)triangleRectangleImage.Height)), new Bgr(Color.LightCyan), 1);
                            #endregion


                            Image_Shapes.Image = triangleRectangleImage.Bitmap;
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

                Plain_Image.Image = Default.Bitmap;
                roiBox.Image = roiImg.Bitmap;
            }
        }


        #region SendCommands
        private void SendDropCommand()
        {
            switch (selectedShape)
            {
                case 't':
                    sendCoords(8, -90);
                    break;
                case 'b':
                    sendCoords(8, 90);
                    break;
                default:
                    break;
            }
        }

        private void SendHomeCommand()
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

        private int CalculatePixInch(Image<Bgr, byte> input, double pixInch)
        {
            double pixToBot = (7.4) * pixInch; //7.75 Original = too long
            return (int)(pixToBot + input.Height);
        }

        private void calculateCoords(double distVert, double distHorz, double pixInch)
        {
            double pixDist = Math.Sqrt(Math.Pow(distVert,2) + Math.Pow(distHorz, 2));
            double inchDist = pixDist / pixInch;

            double Theta = -Math.Atan2(distHorz,distVert) * (180/Math.PI);

            Invoke(new Action(() => {
                labelAngleToBot.Text = $"Target Angle to Bot (deg): {(int)Theta}";
                labelDistFromBot.Text = $"Target Dist to Bot (in): {(int)inchDist}";
            }));
            sendCoords(inchDist, Theta);
        }

        //Send Coords to Arm
        private void sendCoords(double targetDist, double targetTheta)
        {
            string temp;
            double targetY = 2.5;

            if (serialPort.IsOpen)
            {
                temp = $"<{Math.Round(targetDist,1)} {targetY} {Math.Round(targetTheta,1)}>";
                Console.WriteLine(temp);
                
                byte[] buffer = Encoding.ASCII.GetBytes(temp);
                serialPort.Write(buffer, 0, buffer.Length);
            }
        }

        private void sendMagnetCommand()
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
                lock (serialLockObj)
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
                    if (msg.Length == 0)
                    {
                        continue;
                    }
                    else if (msg.Substring(0,1) == "W")
                    {
                        requestedCommand = false;
                    }
                    else if (msg.Substring(0, 1) == "N")
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
        private void comboCOMList_DropDown(object sender, EventArgs e)
        {
            ports = SerialPort.GetPortNames();

            comboCOMList.Items.Clear();
            foreach (string port in ports)
            {
                comboCOMList.Items.Add(port);
            }
        }

        private void comboCOMList_SelectedIndexChanged(object sender, EventArgs e)
        {
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
        {
            OverrideMath = boxOverRideCommand.Checked;
            motionIndex = 1;
        }

        private void buttonHome_Click(object sender, EventArgs e)
        {
            SendHomeCommand();
        }

        private void buttonCalcROI_Click(object sender, EventArgs e)
        {
            reCalculateROI = true;
        }

        private void checkBoxRunRobot_CheckedChanged(object sender, EventArgs e)
        {
            RunRobot = checkBoxRunRobot.Checked;
            motionIndex = 0;
        }

        #endregion UI_Elements
    }
}
