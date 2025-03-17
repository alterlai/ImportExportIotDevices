# IoT Hub Device Manager

This utility allows you to export devices from an IoT Hub to a JSON file and import devices from a JSON file to another IoT Hub. It supports both regular IoT devices and IoT Edge devices.

## Prerequisites

- .NET SDK installed
- Access to Azure IoT Hub

## Usage

### Export Devices

1. Run the program.
2. Select the option to export devices.
3. Enter the source IoT Hub connection string.
4. Specify the output file name (default is `export.json`).
5. The devices will be exported to the specified JSON file.

### Import Devices

1. Run the program.
2. Select the option to import devices.
3. Enter the input file name (default is `export.json`).
4. Enter the destination IoT Hub connection string.
5. Select the devices you want to import.
6. Confirm the import process.
7. The devices will be imported into the destination IoT Hub.

### IoT Edge Devices

After importing IoT Edge devices, you may need to reprovision them. Run the following command on each IoT Edge device:

```bash
sudo iotedge system reprovision
```

This command will ensure that the IoT Edge device is properly configured with the new IoT Hub settings.

## Notes

- Ensure that the connection strings are correct and have the necessary permissions.
- The program will log the progress and any errors encountered during the export and import processes. 