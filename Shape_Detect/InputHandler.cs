using System;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;


namespace Shape_Detect
{
    class InputHandler
    {

        private bool loop = false;
        private String origPath = null;
        private InputObject reader = null;
        private bool isPaused = false;
        private Stopwatch timer;
        private OpenFileDialog dialog;

        public InputHandler()
        {
            timer = new Stopwatch();
            dialog = new OpenFileDialog();
        }

        ~InputHandler()
        {
            if (reader != null)
            {
                reader.Dispose();
            }
        }

        ///<summary>
        ///<para>Deletes the input and any dependencies.</para>
        ///</summary>
        public void Dispose()
        {
            if (reader != null)
            {
                reader.Dispose();
            }
        }

        ///<summary>
        ///<para>Opens a dialog for the user to select a video to load.</para>
        ///</summary>
        ///<returns>Video loaded.</returns>
        public bool requestLoadVideo()
        {
            dialog.Filter = "RawCV Video (*.rawcv)|*.rawcv";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                String path = dialog.FileName;
                return loadVideoFile(path);
            }

            return false;
        }

        ///<summary>
        ///<para>Opens a dialog for the user to select an image to load.</para>
        ///</summary>
        ///<returns>Image loaded.</returns>
        public bool requestLoadImage()
        {
            dialog.Filter = "RawCV Image (*.rawcvimg)|*.rawcvimg";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                String path = dialog.FileName;
                return loadImageFile(path);
            }

            return false;
        }

        ///<summary>
        ///<para>Opens a dialog for the user to select an input file to load.</para>
        ///</summary>
        ///<returns>Input loaded.</returns>
        public bool requestLoadInput()
        {
            dialog.Filter = "RawCV File (*.rawcv, *.rawcvimg)|*.rawcv;*.rawcvimg|RawCV Video (*.rawcv)|*.rawcv|RawCV Image (*.rawcvimg)|*.rawcvimg";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                String path = dialog.FileName;
                String extension = Path.GetExtension(path);
                if (extension == ".rawcv")
                {
                    return loadVideoFile(path);
                }else if(extension == ".rawcvimg")
                {
                    return loadImageFile(path);
                }
            }

            return false;
        }

        ///<summary>
        ///<para>Sets the input to a local video file.</para>
        ///<param name="filename">File name of the local file to load.</param>
        ///</summary>
        ///<returns>File was loaded.</returns>
        public bool setVideo(String filename)
        {
            String path = System.IO.Directory.GetCurrentDirectory() + "\\" + filename + ".rawcv";
            return loadVideoFile(path);
        }

        private bool loadVideoFile(String path)
        {
            Console.WriteLine("Loading file: " + path);
            if (!File.Exists(path))
            {
                Console.WriteLine("Could not find file.");
                return false;
            }

            Console.WriteLine("File found, loading stream.");
            loadVideoStream(path);

            return true;
        }

        ///<summary>
        ///<para>Sets the input to a local image file.</para>
        ///<param name="filename">File name of local image file.</param>
        ///</summary>
        ///<returns>Image loaded.</returns>
        public bool setImage(String filename)
        {
            String path = System.IO.Directory.GetCurrentDirectory() + "\\" + filename;
            return loadImageFile(path);
        }

        private bool loadImageFile(String path)
        {
            Console.WriteLine("Loading file: " + path);
            if (!File.Exists(path))
            {
                Console.WriteLine("Could not find file.");
                return false;
            }

            Console.WriteLine("File found, loading stream.");
            loadImage(path);

            return true;
        }

        ///<summary>
        ///<para>Sets the input to a camera stream.</para>
        ///<param name="cameraId">The id of the camera to be loaded.</param>
        ///</summary>
        ///<returns>Camera connected.</returns>
        public bool setCamera(int cameraId)
        {
            loadCameraStream(cameraId);
            if (reader.isFrameAvailable())
            {
                return true;
            }

            return false;
        }

        ///<summary>
        ///<para>Reads the raw bytes of the last loaded frame.</para>
        ///</summary>
        ///<returns>Bytes in [y,x, channel] format.</returns>
        public byte[,,] readRawData()
        {
            if (reader != null)
            {
                //Block read like a webcam. until 60fps has passed
                while (!(reader is CameraStream) && (timer.ElapsedMilliseconds < 33))
                {
                    Thread.Sleep(1);
                }
                timer.Restart(); //Set elapsed time to 0.

                if (reader.isFrameAvailable())
                {
                    return reader.readRawData();
                }
                else if (loop && (reader is VideoStream)) //Not available, assume video ended and need to loop again
                {
                    reader.Dispose();
                    reader = new VideoStream(origPath);
                    if (isPaused) //If somehow video ended while paused...
                    {
                        if (reader.isFrameAvailable())
                        {
                            reader.readImage();
                        }
                        reader.pause();
                        if (isFrameAvailable())
                        {
                            return reader.readRawData();
                        }
                    }
                    if (reader.isFrameAvailable())
                    {
                        return reader.readRawData();
                    }
                }
            }

            return null;
        }

        ///<summary>
        ///<para>Reads the next frame of the current input, if available.</para>
        ///<para>Note, that if a camera, this method blocks until the camera returns a new frame, which may differ between cameras.</para>
        ///<para>If a video for image file is loaded, the method will block to match 30fps.</para>
        ///</summary>
        ///<returns>The next frame, or null if no frame was available.</returns>
        public Image<Bgr, byte> readFrame()
        {
            if (reader != null)
            {
                //Block read like a webcam. until 60fps has passed
                while(!(reader is CameraStream) && (timer.ElapsedMilliseconds < 33))
                {
                    Thread.Sleep(1);
                }
                timer.Restart(); //Set elapsed time to 0.

                if (reader.isFrameAvailable())
                {
                    return reader.readImage();
                }
                else if (loop && (reader is VideoStream)) //Not available, assume video ended and need to loop again
                {
                    reader.Dispose();
                    reader = new VideoStream(origPath);
                    //reader.pause();
                    if (isPaused) //If somehow video ended while paused...
                    {
                        if (!reader.isFrameAvailable())
                        {
                            reader.readImage();
                        }
                        reader.pause();
                        if (isFrameAvailable())
                        {
                            return reader.readImage();
                        }
                    }
                    if (reader.isFrameAvailable())
                    {
                        return reader.readImage();
                    }
                }
            }

            return null;
        }

        private void loadVideoStream(String path)
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            origPath = path;
            reader = new VideoStream(path);
            if (isPaused)
            {
                if (reader.isFrameAvailable())
                {
                    reader.readImage();
                }
                reader.pause();
            }
            timer.Restart();
        }

        private void loadImage(String path)
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            origPath = path;
            reader = new ImageStream(path);
            timer.Restart();
        }

        private void loadCameraStream(int cameraId)
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            origPath = cameraId.ToString();
            reader = new CameraStream(cameraId);
            timer.Restart();
        }

        public void setLoop(bool loop)
        {
            this.loop = loop;
        }

        ///<summary>
        ///<para>Continues or starts the input.</para>
        ///</summary>
        public void play()
        {
            if (reader != null)
            {
                reader.play();
            }
            isPaused = false;
        }

        ///<summary>
        ///<para>Pauses the current input so no new frames are read.</para>
        ///</summary>
        public void pause()
        {
            if (reader != null)
            {
                reader.pause();
            }
            isPaused = true;
        }

        ///<summary>
        ///<returns>If the current input is playing.</returns>
        ///</summary>
        public bool isPlaying()
        {
            return !isPaused;
        }

        ///<summary>
        ///<returns>The current input width (might be broken with camera, maybe)</returns>
        ///</summary>
        public int getWidth()
        {
            if (reader == null)
            {
                return 0;
            }
            else
            {
                return reader.getWidth();
            }
        }

        /// <returns>The current input height (might be broken with camera, maybe)</returns>
        public int getHeight()
        {
            if (reader == null)
            {
                return 0;
            }
            else
            {
                return reader.getHeight();
            }
        }

        ///<summary>
        /// <returns>If there is a frame available to be read. 
        /// <para>For images always returns true.</para>
        /// <para>Videos return false at the end of the stream.</para>
        /// <para>Cameras return true if they are open.</para>
        /// </returns>
        /// </summary>
        public bool isFrameAvailable()
        {
            if (reader == null)
            {
                return false;
            }
            else
            {
                if (timer.ElapsedMilliseconds < 33)
                    return false;

                return reader.isFrameAvailable();
            }
        }

        private static void readFrame(BinaryReader reader, int width, int height, ref byte[,,] buffer) //Given a reader and a frame width/height, will load the next frame into the buffer
        {
            for (int channel = 0; channel < 3; channel++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        buffer[y, x, channel] = reader.ReadByte();
                    }
                }
            }
        }

        private interface InputObject
        {
            void Dispose();
            void play();
            void pause();
            Image<Bgr, byte> readImage();
            byte[,,] readRawData();
            int getWidth();
            int getHeight();
            bool isFrameAvailable();
        }

        private class ImageStream : InputObject
        {
            private int width = 0;
            private int height = 0;
            private byte[,,] buffer = null;
            public ImageStream(String path) //Assumes .rawcvimg file type
            {
                if (File.Exists(path))
                {
                    String extension = Path.GetExtension(path);

                    if(extension == ".rawcvimg")
                    {
                        BinaryReader reader = new BinaryReader(File.OpenRead(path));
                        width = reader.ReadInt32();
                        height = reader.ReadInt32();
                        buffer = new byte[height, width, 3];
                        InputHandler.readFrame(reader, width, height, ref buffer);
                        reader.Close();
                        reader.Dispose();
                    }else if (extension == ".png" || extension == ".jpg")
                    {
                        Bitmap img = new Bitmap(path);
                        width = img.Width;
                        height = img.Height;
                        buffer = new byte[height, width, 3];
                        for (int y = 0; y < img.Height; y++)
                        {
                            for (int x = 0; x < img.Width; x++)
                            {
                                Color pixel = img.GetPixel(x, y);
                                buffer[y, x, 0] = pixel.B;
                                buffer[y, x, 1] = pixel.G;
                                buffer[y, x, 2] = pixel.R;
                            }
                        }

                        img.Dispose();
                    }
                    
                }
            }

            ~ImageStream()
            {
                buffer = null;
            }

            void InputObject.Dispose()
            {
                buffer = null;
            }

            int InputObject.getHeight()
            {
                return width;
            }

            int InputObject.getWidth()
            {
                return height;
            }

            bool InputObject.isFrameAvailable()
            {
                return (buffer != null); //As long as it was initialized correctly.
            }

            void InputObject.pause()
            {
                //Do nothing, it's an image.
            }

            void InputObject.play()
            {
                //Do nothing, it's an image.
            }

            Image<Bgr, byte> InputObject.readImage()
            {
                if (buffer == null)
                {
                    return null;
                }
                else
                {
                    return new Image<Bgr, byte>(buffer);
                }
            }

            byte[,,] InputObject.readRawData()
            {
                if (buffer == null)
                {
                    return null;
                }
                else
                {
                    return buffer;
                }
            }
        }

        private class VideoStream : InputObject
        {
            //NOTE: read at least one frame before pausing, or else may not load correctly.
            private BinaryReader reader;
            private int width = 0;
            private int height = 0;
            private byte[,,] buffer = null;
            private bool frameAvailable = false;
            private bool status = false; //True = play, False = pause

            public VideoStream(String path)
            {
                if (File.Exists(path))
                {
                    reader = new BinaryReader(File.OpenRead(path));
                    width = reader.ReadInt32();
                    height = reader.ReadInt32();
                    buffer = new byte[height, width, 3];
                    frameAvailable = reader.ReadBoolean();
                    status = true;
                }
            }

            ~VideoStream()
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }

                buffer = null;
            }

            void InputObject.Dispose()
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }

                buffer = null;
            }

            int InputObject.getHeight()
            {
                return height;
            }

            int InputObject.getWidth()
            {
                return width;
            }

            bool InputObject.isFrameAvailable()
            {
                return frameAvailable;
            }

            void InputObject.pause()
            {
                status = false;
            }

            void InputObject.play()
            {
                status = true;
            }

            Image<Bgr, byte> InputObject.readImage()
            {

                if (loadIntoBuffer())
                {
                    return new Image<Bgr, byte>(buffer);
                }

                return null;
            }

            byte[,,] InputObject.readRawData()
            {
                if (loadIntoBuffer())
                {
                    return buffer;
                }

                return null;
            }

            private bool loadIntoBuffer()
            {
                if (!status && buffer != null)//Paused, reuse last framw
                {
                    return true;
                }
                else
                {//Playing, load next frame
                    if (frameAvailable)
                    {
                        reader.ReadDouble(); //Dispose of timing info.
                        InputHandler.readFrame(reader, width, height, ref buffer);
                        frameAvailable = reader.ReadBoolean(); //Check for another frame
                        if (!frameAvailable)
                        {//End of file, dispose of resources.
                            reader.Close();
                            reader.Dispose();
                            reader = null;
                        }

                        return true;
                    }

                }

                return false;
            }
        }

        private class CameraStream : InputObject
        {
            private VideoCapture captureDevice;
            private Image<Bgr, byte> buffer;
            private int width = 0;
            private int height = 0;
            private bool frameAvailable = false;
            private bool status = false; //True = play, False = pause

            public CameraStream(int cameraId)
            {
                captureDevice = new VideoCapture(cameraId);
                this.width = captureDevice.Width;
                this.height = captureDevice.Height;
                status = captureDevice.IsOpened;
                frameAvailable = status;
            }

            ~CameraStream()
            {
                captureDevice.Stop();
                captureDevice.Dispose();
                buffer = null;
            }

            void InputObject.Dispose()
            {
                captureDevice.Stop();
                captureDevice.Dispose();
                buffer = null;
            }

            int InputObject.getHeight()
            {
                return height;
            }

            int InputObject.getWidth()
            {
                return width;
            }

            bool InputObject.isFrameAvailable()
            {
                return captureDevice.IsOpened;
            }

            void InputObject.pause()
            {
                status = false;
            }

            void InputObject.play()
            {
                status = true;
            }

            Image<Bgr, byte> InputObject.readImage()
            {
                if (loadIntoBuffer())
                {
                    return buffer;
                }

                return null;
            }

            byte[,,] InputObject.readRawData()
            {
                if (buffer == null)
                    return null;

                return buffer.Data;
            }

            private bool loadIntoBuffer()
            {
                if (!status && buffer != null)//Paused, reuse last framw
                {
                    return true;
                }
                else
                {//Playing, load next frame
                    if (frameAvailable)
                    {
                        buffer = captureDevice.QueryFrame().ToImage<Bgr, byte>();

                        return true;
                    }

                }

                return false;
            }
        }

    }
}
