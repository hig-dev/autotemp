# AutoTemp for SuperMicro IPMI mainboards

AutoTemp is a .NET 9 console application designed to manage fan speeds on your SuperMicro motherboard based on CPU temperatures.
The program uses [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) to read hardware sensor data and the IPMICFG tool to set the fan speeds.

## Tested Hardware
- SuperMicro H13SAE-MF motherboard
- AMD Ryzen 7950x CPU
- Windows 11

## Features

- **Temperature-Based Fan Control:**  
  Dynamically adjusts fan speeds based on CPU temperature thresholds:
  - Uses a minimum ("floor") fan speed.
  - Gradually ramps up the fan speed between a specified temperature threshold and a maximum temperature.
  - Sets the fan to 100% once the maximum temperature is reached.
  
- **Stepped Fan Speed Adjustments:**  
  Fan speed changes are rounded to discrete steps to reduce frequent small changes.

- **Resilient to Sleep/Resume Cycles:**  
  The application resets its internal state upon system resume to ensure fan speed updates occur correctly after waking from sleep.


## Installation

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/hig-dev/autotemp.git
   cd FanControl
   ```

2. **Build the Project:**

   ```bash
   dotnet build -c Release
   ```

You need to have the IPMICFG tool on your system. You can download it from the [SuperMicro website](https://www.supermicro.com/en/solutions/management-software/ipmi-utilities).

## Usage

Run the compiled executable from a command prompt. All parameters are optional. If no parameters are provided, the application will use its default settings.

Example command:

```bash
autotemp.exe --interval 2500 --floor 35 --ramp 66 --max 90 --step 5 --path "C:\tools\supermicro\IPMICFG-Win.exe"
```

### Command-Line Parameters

- **`--interval [milliseconds]`**  
  Delay between loop iterations. Default is `2500`.

- **`--floor [0-100]`**  
  Minimum fan speed percentage. Default is `35`.

- **`--ramp [°C]`**  
  CPU temperature at which fan speed begins to increase. Default is `66`.

- **`--max [°C]`**  
  CPU temperature at which fan speed is set to 100%. Default is `90`.

- **`--step [percentage]`**  
  The increment step for fan speed adjustments. Default is `5`.

- **`--path [file path]`**  
  Full path to the `IPMICFG-Win.exe` executable. Default is `C:\tools\supermicro\IPMICFG-Win.exe`.

- **`--verbose`**
  Enable verbose logging.
	

## Running Automatically Using Windows Task Scheduler

The IPMICFG tool requires administrative privileges to send raw IPMI commands.

   - Create a task that runs with highest privileges.
   - Set a trigger for "At startup".
   - Set action to "Start a program" and point to the compiled executable.

## License

This project is provided under the MPL 2.0. License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- **LibreHardwareMonitor:**  
  Thanks to the LibreHardwareMonitor project for providing easy access to hardware sensor data.
- **SuperMicro:**  
  For the IPMICFG tool.

