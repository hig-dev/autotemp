using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;

namespace autotemp;

partial class Program
{
    // Configuration Variables
    static int interval = 2500; // Delay in milliseconds between loop iterations
    static int floorFanSpeed = 35; // Minimum fan speed (0-100)
    static int rampUpThresholdTemp = 66; // Temperature at which fan speed increases (°C)
    static int maxTemp = 90; // Temperature at which fan speed is max (100%)
    static int fanSpeedStep = 5; // Fan speed step increment (in percentage points)
    static bool isVerbose = false; // Enable verbose logging
    static string ipmicfgPath = @"C:\tools\supermicro\IPMICFG-Win.exe"; // Path to IPMICFG-Win.exe

    // Internal Variables
    static int lastFanSpeed = -1;
    static Computer? computer;

    static void Main(string[] args)
    {
        ParseArguments(args);
        ValidateConfiguration();

        Init();

        try
        {
            while (true)
            {
                Tick();
            }
        }
        catch (Exception ex)
        {
            Log(
                $"An error occurred: {ex.Message}. Setting fan speed to max (100%) and terminating the program."
            );
        }
        finally
        {
            SetFanSpeed(100);
            Reset();
            Environment.Exit(1);
        }
    }

    static void Reset()
    {
        computer?.Close();
        computer = null;
        lastFanSpeed = -1;
        if (OperatingSystem.IsWindows())
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }
    }

    static void Init()
    {
        Reset();

        Log("Initializing hardware sensors...");
        computer = new Computer { IsCpuEnabled = true };

        computer.Open();

        if (OperatingSystem.IsWindows())
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }
    }

    static void Tick()
    {
        int? cpuTemp = GetCpuTemp();
        if (cpuTemp.HasValue)
        {
            Log($"Current CPU Temperature: {cpuTemp.Value} C");
            int fanSpeed = floorFanSpeed;

            if (cpuTemp.Value >= maxTemp)
            {
                fanSpeed = 100;
            }
            else if (cpuTemp.Value >= rampUpThresholdTemp)
            {
                double rawSpeed =
                    floorFanSpeed
                    + (
                        (cpuTemp.Value - rampUpThresholdTemp)
                        / (double)(maxTemp - rampUpThresholdTemp)
                    ) * (100 - floorFanSpeed);
                // Round rawSpeed to the nearest fanSpeedStep multiple to reduce changes
                int steppedSpeed = (int)(
                    Math.Round(rawSpeed / (double)fanSpeedStep) * fanSpeedStep
                );
                fanSpeed = Math.Min(steppedSpeed, 100);
            }
            // Otherwise, fanSpeed remains at floorFanSpeed

            SetFanSpeed(fanSpeed);
        }
        else
        {
            Log("Failed to get a valid CPU temperature. Setting fan speed to max (100%).");
            SetFanSpeed(100);
        }
        Thread.Sleep(interval);
    }

    static void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            Log($"Power mode changed: {e.Mode}");
            if (e.Mode == PowerModes.Resume)
            {
                Console.WriteLine("System resumed from sleep. Reinitializing...");
                Init();
            }
        }
    }

    /// <summary>
    /// Parses command-line arguments to override default configuration values.
    /// Supported parameters:
    ///   -i, --interval   : Loop delay in milliseconds.
    ///   -f, --floor      : Floor fan speed (0–100).
    ///   -r, --ramp       : Ramp-up threshold temperature (°C).
    ///   -m, --max        : Maximum temperature (°C).
    ///   -s, --step       : Fan speed step increment.
    ///   -p, --path       : Path to IPMICFG-Win.exe.
    /// </summary>
    static void ParseArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();
            switch (arg)
            {
                case "-i":
                case "--interval":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedInterval))
                    {
                        interval = parsedInterval;
                    }
                    break;

                case "-f":
                case "--floor":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedFloor))
                    {
                        floorFanSpeed = parsedFloor;
                    }
                    break;

                case "-r":
                case "--ramp":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedRamp))
                    {
                        rampUpThresholdTemp = parsedRamp;
                    }
                    break;

                case "-m":
                case "--max":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedMax))
                    {
                        maxTemp = parsedMax;
                    }
                    break;

                case "-s":
                case "--step":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedStep))
                    {
                        fanSpeedStep = parsedStep;
                    }
                    break;

                case "-p":
                case "--path":
                    if (i + 1 < args.Length)
                    {
                        ipmicfgPath = args[++i];
                    }
                    break;
                case "-v":
                case "--verbose":
                    isVerbose = true;
                    break;
                default:
                    // Unrecognized parameters are ignored.
                    break;
            }
        }
    }

    /// <summary>
    /// Validates the configuration variables and resets them to defaults if necessary.
    /// </summary>
    static void ValidateConfiguration()
    {
        if (interval <= 0)
        {
            Console.WriteLine($"Invalid interval: {interval}. Setting to default (1000ms).");
            interval = 1000;
        }
        Console.WriteLine($"Interval: {interval}ms");
        if (floorFanSpeed < 0 || floorFanSpeed > 100)
        {
            Console.WriteLine(
                $"Invalid floor fan speed: {floorFanSpeed}. Setting to default (40%)."
            );
            floorFanSpeed = 40;
        }
        Console.WriteLine($"Floor Fan Speed: {floorFanSpeed}%");
        if (rampUpThresholdTemp < floorFanSpeed || rampUpThresholdTemp > 80)
        {
            Console.WriteLine(
                $"Invalid ramp up threshold temp: {rampUpThresholdTemp}. Setting to default (60°C)."
            );
            rampUpThresholdTemp = 60;
        }
        Console.WriteLine($"Ramp Up Threshold Temp: {rampUpThresholdTemp}°C");
        if (maxTemp < rampUpThresholdTemp || maxTemp > 100)
        {
            Console.WriteLine($"Invalid max temp: {maxTemp}. Setting to default (80°C).");
            maxTemp = 80;
        }
        Console.WriteLine($"Max Temp: {maxTemp}°C");
        if (fanSpeedStep <= 0 || fanSpeedStep > 100)
        {
            Console.WriteLine($"Invalid fan speed step: {fanSpeedStep}. Setting to default (5%).");
            fanSpeedStep = 5;
        }
        Console.WriteLine($"Fan Speed Step: {fanSpeedStep}%");
    }

    static void Log(string message)
    {
        if (isVerbose)
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Gets the current CPU temperature using the LibreHardwareMonitor library.
    /// Requires the LibreHardwareMonitorLib NuGet package.
    /// </summary>
    /// <returns>CPU temperature in Celsius or null if not found.</returns>
    static int? GetCpuTemp()
    {
        Log("Reading CPU temperature sensors...");
        float? maxTempValue = null;

        // Iterate through the hardware components
        var localComputer = computer;
        if (localComputer == null)
        {
            Log("Computer instance is null. Skipping sensor reading.");
            return null;
        }
        foreach (IHardware hardware in localComputer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                hardware.Update(); // Refresh sensor data

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        // Optionally, you can check sensor.Name to target a specific sensor (e.g., "CPU Package")
                        if (sensor.Value.HasValue)
                        {
                            Log($"Sensor: {sensor.Name}, Value: {sensor.Value.Value} C");
                            // Use the highest temperature reported among the sensors.
                            if (!maxTempValue.HasValue || sensor.Value.Value > maxTempValue.Value)
                            {
                                maxTempValue = sensor.Value.Value;
                            }
                        }
                    }
                }
            }
        }

        return maxTempValue.HasValue ? (int?)Math.Round(maxTempValue.Value) : null;
    }

    /// <summary>
    /// Sets the fan speed using the IPMICFG tool.
    /// </summary>
    /// <param name="speed">Desired fan speed (0-100).</param>
    static bool SetFanSpeed(int speed)
    {
        if (speed < 0 || speed > 100)
        {
            Log($"Invalid fan speed: {speed}. Setting fan speed to max (100%).");
            speed = 100;
        }

        if (speed != lastFanSpeed)
        {
            // Convert speed to a two-digit hex value (e.g., 35 -> "23")
            string hexSpeed = speed.ToString("x2");
            Log($"Setting fan speed to: {speed}%");

            // Build the argument string for the IPMICFG tool
            string arguments = $"-raw 0x30 0x70 0x66 0x01 0x00 0x{hexSpeed}";
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = ipmicfgPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };
                using Process? process = Process.Start(psi);
                if (process == null)
                {
                    Log("Failed to start the IPMICFG process.");
                    return false;
                }
                process.WaitForExit(10000); // Timeout after 10000 milliseconds (10 seconds)
                if (!process.HasExited)
                {
                    Log("IPMICFG process timed out. Killing the process.");
                    process.Kill();
                    throw new TimeoutException("IPMICFG process timed out.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting fan speed: {ex.Message}");
                return false;
            }
            lastFanSpeed = speed;
        }
        else
        {
            Log($"Fan speed already set to: {speed}%. No action taken.");
        }
        return true;
    }
}
