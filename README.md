# Unified Robotics II Robot Arm C# Code

Overview
-
Design, Build, and Program a small robotic arm capable of locating, picking up, and sorting triangles and squares.

I build my arm using two stepper motors and threaded rod to position the upper and lower portion of the arm. Limit switches and hoping that the steppers don't loose count keep the arm actuate enough as it moves to pick up the shapes.

Source Code for the Vision Processing and UI elements of UR2 Term Project. These sections were programmed in Visual Studio using C#.

Additional resources can be found in the other repositories on my account.

Launching the program and Run the Robot
-
Open the .stl and run as you would a normal C# program

Connect the external camera and select Camera under the Open tab at the top.

  P1-3 are just simple images that were used during the development of the code
  
  Make sure to orient the camera properly, more on that below
  
Connect the Arduino and select the serial port under the Open COM drop down

Enable the Run Robot check box

To begin the process, click the Home button

The Robot "should" now continue running until it runs out of shapes or the Run Robot checkbox was clicked again.

Contributor
-
Bryan Walther

  Student, Lawrence Technological University
