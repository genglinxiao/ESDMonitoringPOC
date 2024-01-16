DataCollector Documentation
Overview

    This is a monitoring program that collects performance data from modbus slave devices, and then publish it in Prometheus or into InfluxDB.
    The program runs on Linux with MS dotnet runtime 7.0 or later.

Installation

    You can simply copy the excutable into a system that meets the prerequisites and run the excutable.
    Run "datacollector --version", if the version is printed, then the program is correctly installed.

Configuration

    The program has a 2-level configuration system. On the first level, you need a .conf file, either specified in your command line, or by default "collector.conf", to specify the global configurations and the list of slave devices. On the 2nd level, for each slave device, you need a .json file to specify its modbus memory details.

Usage
Starting the Program

    Basic command to start the program.
    Description of different modes the program can operate in.

Command Line Arguments

    Detailed list of available command line arguments and their functions.
    Examples of common command line usages.

Monitoring and Data Collection

    Explanation of how the program collects performance data.
    Information on the data that can be collected and how it's processed.

Publishing Data in Prometheus Format

    Details on how the program formats data for Prometheus.
    Instructions on integrating with a Prometheus server.

Logs and Debugging

    Location and format of log files.
    How to interpret common log entries.
    Tips for troubleshooting common issues using logs.

Advanced Features

    Description of any advanced features or capabilities.
    Additional configuration options for power users.

Best Practices

    Recommendations for effective use of the program.
    Tips for maintaining and updating the configuration.

FAQ

    Answers to frequently asked questions.

Troubleshooting

    Common issues and their solutions.
    Contact information for further support.

Appendix

    Any additional reference material.
    Glossary of terms.
