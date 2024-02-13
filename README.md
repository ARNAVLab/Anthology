# Anthology
The Anthology Framework 

Contains all the Anthology Controller and Model files, and new developments on the framework itself. 

## Importing and Running the Anthology Framework
There are currently two ways to run the Anthology simulation framework: 

### Using the Anthology-CLI Project 

The Anthology framework is a submodule in the Anthology-ClI proejct. 
You can clone the Anthology-CLI project here: https://github.com/ARNAVLab/Anthology-CLI

Note: The CLI doesn't have a working UI. Instead, follow the run instructions in the CLI project to edit the simulation start state, run the the simulation engine, and view the output on the browser using the Client API mappings. 

### Using the Multipurpose Social Simulation (MPSS) Unity Project

The Anthology framework is imported in the Scripts folder as a git submodule in the MPSS Unity project repo. 
You can clone the MPSS Unity project here: https://github.com/ARNAVLab/multipurpose-social-sim.git

Note: The MPSS Unity project provides a generic social simulation GUI and architecture that any Simulation Framework can plug into. For the purpose of this dissertation/research and to demonstrate how a Simulation Framework can plug into the MPSS, we have imported these Anthology framework files into the MPSS repo. 

To run the Anthology framework on Unity, follow the run instructions provided in the MPSS repo ReadMe.md file. 