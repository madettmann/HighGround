Simulated annealing is a technique for finding the global minimum/maximum. It works by determining the cost of taking a random step and decides whether to take it or not based on the cost and the "Temperature" of the simulation which cools as the simulation proceeds. This means that costly steps are taken more often in the beginning than the end. I also shrank my search radius as the simulation proceeds.

The .exe file can be found in HighGroundWpf > bin > debug > HighGroundWpf.exe

The most important file is HighGroundWpf > MainWindow.xaml.cs This contains my
algorithm for simulated annealing and steepest ascent. The .xaml files just
contain code on the layout of the GUI.

I have included several excel files from real runs.

If you have visual studio, open HighGroundWpf.sln. This is the project file and will contain everything in one organized place.
