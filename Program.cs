using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IoTHubDeviceManager
{
	class Program
	{
		static async Task Main(string[] args)
		{
			try
			{
				ShowBanner();

				if (args.Length > 0 && (args[0] == "-h" || args[0] == "--help"))
				{
					ShowHelp();
					return;
				}

				bool exit = false;
				while (!exit)
				{
					Console.WriteLine("\nIoT Hub Device Manager - Main Menu");
					Console.WriteLine("1. Export devices from IoT Hub");
					Console.WriteLine("2. Import devices to IoT Hub");
					Console.WriteLine("3. Exit");
					Console.Write("\nSelect an option (1-3): ");

					string option = Console.ReadLine();

					switch (option)
					{
						case "1":
							await ExportDevicesMenu();
							break;
						case "2":
							await ImportDevicesMenu();
							break;
						case "3":
							exit = true;
							break;
						default:
							Console.WriteLine("Invalid option. Please try again.");
							break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\nError occurred: {ex.Message}");
				Console.WriteLine(ex.StackTrace);
			}
		}

		static void ShowBanner()
		{
			Console.WriteLine("╔══════════════════════════════════════════════╗");
			Console.WriteLine("║             IoT Hub Device Manager           ║");
			Console.WriteLine("║           Export and Import Utility          ║");
			Console.WriteLine("╚══════════════════════════════════════════════╝");
		}

		static void ShowHelp()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  IoTHubDeviceManager.exe                   - Run in interactive mode");
			Console.WriteLine("  IoTHubDeviceManager.exe -h/--help         - Show this help message");
			Console.WriteLine("\nDescription:");
			Console.WriteLine("  This utility allows exporting devices from an IoT Hub to a JSON file");
			Console.WriteLine("  and importing devices from a JSON file to another IoT Hub.");
		}

		static async Task ExportDevicesMenu()
		{
			Console.WriteLine("\n--- Export Devices ---");

			// Get connection string
			Console.Write("Enter source IoT Hub Connection String: ");
			string connectionString = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(connectionString))
			{
				Console.WriteLine("Connection string cannot be empty.");
				return;
			}

			// Get output file name
			Console.Write("Enter output file name (default: export.json): ");
			string fileName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(fileName))
			{
				fileName = "export.json";
			}

			try
			{
				Console.WriteLine("Connecting to IoT Hub...");

				// Create IoT Hub service client
				var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

				// Export device data
				await ExportDevicesToJsonFile(registryManager, fileName);

				Console.WriteLine("Export completed successfully!");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Export error: {ex.Message}");
			}
		}

		static async Task ImportDevicesMenu()
		{
			Console.WriteLine("\n--- Import Devices ---");

			// Get input file name
			Console.Write("Enter input file name (default: export.json): ");
			string fileName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(fileName))
			{
				fileName = "export.json";
			}

			if (!File.Exists(fileName))
			{
				Console.WriteLine($"File not found: {fileName}");
				return;
			}

			// Get destination connection string
			Console.Write("Enter destination IoT Hub Connection String: ");
			string connectionString = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(connectionString))
			{
				Console.WriteLine("Connection string cannot be empty.");
				return;
			}

			try
			{
				// Load devices from file
				string jsonContent = File.ReadAllText(fileName);
				var exportObject = JObject.Parse(jsonContent);
				var devices = exportObject["devices"].ToObject<JArray>();

				if (devices.Count == 0)
				{
					Console.WriteLine("No devices found in the export file.");
					return;
				}

				Console.WriteLine($"\nFound {devices.Count} devices in the export file.");

				// Allow selecting devices
				var selectedDevices = SelectDevicesToImport(devices);

				if (selectedDevices.Count == 0)
				{
					Console.WriteLine("No devices selected for import.");
					return;
				}

				Console.WriteLine($"\nSelected {selectedDevices.Count} devices for import.");
				Console.Write("Proceed with import? (y/n): ");
				string confirm = Console.ReadLine().ToLower();

				if (confirm != "y" && confirm != "yes")
				{
					Console.WriteLine("Import canceled.");
					return;
				}

				Console.WriteLine("Connecting to destination IoT Hub...");
				var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

				await ImportDevicesToIoTHub(registryManager, selectedDevices);

				Console.WriteLine("Import completed successfully!");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Import error: {ex.Message}");
			}
		}

		static List<JObject> SelectDevicesToImport(JArray devices)
		{
			var selectedDevices = new List<JObject>();
			bool selectionComplete = false;

			while (!selectionComplete)
			{
				Console.WriteLine("\nSelect devices to import:");
				Console.WriteLine("1. Select all devices");
				Console.WriteLine("2. Select by prefix");
				Console.WriteLine("3. Select by status (enabled/disabled)");
				Console.WriteLine("4. Select individual devices");
				Console.WriteLine("5. Done selecting");

				Console.Write("\nSelect an option (1-5): ");
				string option = Console.ReadLine();

				switch (option)
				{
					case "1":
						selectedDevices = devices.Select(d => (JObject)d).ToList();
						Console.WriteLine($"Selected all {selectedDevices.Count} devices.");
						break;

					case "2":
						Console.Write("Enter device ID prefix: ");
						string prefix = Console.ReadLine();

						if (!string.IsNullOrWhiteSpace(prefix))
						{
							var prefixDevices = devices
								.Where(d => d["deviceId"].ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
								.Select(d => (JObject)d)
								.ToList();

							foreach (var device in prefixDevices)
							{
								if (!selectedDevices.Any(d => d["deviceId"].ToString() == device["deviceId"].ToString()))
								{
									selectedDevices.Add(device);
								}
							}

							Console.WriteLine($"Selected {prefixDevices.Count} devices with prefix '{prefix}'.");
						}
						break;

					case "3":
						Console.Write("Enter status (enabled/disabled): ");
						string status = Console.ReadLine().ToLower();

						if (status == "enabled" || status == "disabled")
						{
							var statusDevices = devices
								.Where(d => d["status"].ToString().ToLower() == status)
								.Select(d => (JObject)d)
								.ToList();

							foreach (var device in statusDevices)
							{
								if (!selectedDevices.Any(d => d["deviceId"].ToString() == device["deviceId"].ToString()))
								{
									selectedDevices.Add(device);
								}
							}

							Console.WriteLine($"Selected {statusDevices.Count} {status} devices.");
						}
						break;

					case "4":
						ListDevices(devices, selectedDevices);

						Console.Write("Enter device IDs to select (comma-separated, or 'all' for all): ");
						string input = Console.ReadLine();

						if (input.ToLower() == "all")
						{
							selectedDevices = devices.Select(d => (JObject)d).ToList();
							Console.WriteLine($"Selected all {selectedDevices.Count} devices.");
						}
						else if (!string.IsNullOrWhiteSpace(input))
						{
							var deviceIds = input.Split(',').Select(id => id.Trim()).ToList();

							foreach (var deviceId in deviceIds)
							{
								var device = devices.FirstOrDefault(d => d["deviceId"].ToString() == deviceId);

								if (device != null && !selectedDevices.Any(d => d["deviceId"].ToString() == deviceId))
								{
									selectedDevices.Add((JObject)device);
									Console.WriteLine($"Selected device: {deviceId}");
								}
							}
						}
						break;

					case "5":
						selectionComplete = true;
						break;

					default:
						Console.WriteLine("Invalid option. Please try again.");
						break;
				}

				Console.WriteLine($"\nCurrently selected: {selectedDevices.Count} devices");
			}

			return selectedDevices;
		}

		static void ListDevices(JArray devices, List<JObject> selectedDevices)
		{
			Console.WriteLine("\nDevice List (first 20 shown):");
			Console.WriteLine("-----------------------------------------------");
			Console.WriteLine("| ID                 | Status   | Selected    |");
			Console.WriteLine("-----------------------------------------------");

			int count = 0;
			foreach (var device in devices.Take(20))
			{
				string deviceId = device["deviceId"].ToString();
				string status = device["status"].ToString();
				bool isSelected = selectedDevices.Any(d => d["deviceId"].ToString() == deviceId);

				Console.WriteLine($"| {deviceId.PadRight(18)} | {status.PadRight(8)} | {(isSelected ? "Yes" : "No").PadRight(10)} |");
				count++;
			}

			Console.WriteLine("-----------------------------------------------");

			if (devices.Count > 20)
			{
				Console.WriteLine($"... and {devices.Count - 20} more devices.");
			}
		}

		static async Task ExportDevicesToJsonFile(RegistryManager registryManager, string fileName)
		{
			Console.WriteLine("Retrieving all devices...");

			// Get all devices (up to 1000 - adjust as needed)
			var devices = await registryManager.GetDevicesAsync(1000);

			// Create list to hold full device information
			var deviceExportList = new List<JObject>();

			int count = 0;
			int total = devices.Count();

			Console.WriteLine($"Found {total} devices. Starting export...");

			foreach (var device in devices)
			{
				count++;
				Console.WriteLine($"Processing device {count}/{total}: {device.Id}");

				try
				{
					// Get full device twin (contains desired/reported properties)
					Twin deviceTwin = await registryManager.GetTwinAsync(device.Id);

					// Get device modules if any
					var modules = await registryManager.GetModulesOnDeviceAsync(device.Id);
					var moduleList = new List<JObject>();

					foreach (var module in modules)
					{
						try
						{
							// Get module twin
							Twin moduleTwin = await registryManager.GetTwinAsync(device.Id, module.Id);

							var moduleObject = JObject.FromObject(module);
							moduleObject["twin"] = moduleTwin != null ? JObject.FromObject(moduleTwin) : null;
							moduleList.Add(moduleObject);
						}
						catch (Exception moduleEx)
						{
							Console.WriteLine($"Warning: Could not process module {module.Id} for device {device.Id}: {moduleEx.Message}");
							// Add basic module info without twin
							var moduleObject = JObject.FromObject(module);
							moduleObject["twin"] = null;
							moduleObject["exportError"] = moduleEx.Message;
							moduleList.Add(moduleObject);
						}
					}

					// Create comprehensive device object
					var deviceObject = new JObject
					{
						["deviceId"] = device.Id,
						["etag"] = device.ETag,
						["status"] = device.Status.ToString(),
						["statusReason"] = device.StatusReason,
						["connectionState"] = device.ConnectionState.ToString(),
						["lastActivityTime"] = device.LastActivityTime,
						["cloudToDeviceMessageCount"] = device.CloudToDeviceMessageCount,
						["authentication"] = device.Authentication != null ? JObject.FromObject(device.Authentication) : null,
						["capabilities"] = device.Capabilities != null ? JObject.FromObject(device.Capabilities) : null,
						//["deviceScope"] = device.DeviceScope,
						["parentScopes"] = new JArray(device.ParentScopes ?? Array.Empty<string>()),
						["twin"] = deviceTwin != null ? JObject.FromObject(deviceTwin) : null,
						["modules"] = new JArray(moduleList)
					};

					// Add custom properties present in the device object
					foreach (var property in device.GetType().GetProperties())
					{
						try
						{
							var value = property.GetValue(device);
							if (value != null && !deviceObject.ContainsKey(property.Name))
							{
								deviceObject[property.Name] = JToken.FromObject(value);
							}
						}
						catch (Exception propEx)
						{
							Console.WriteLine($"Warning: Could not process property {property.Name} for device {device.Id}: {propEx.Message}");
						}
					}

					deviceExportList.Add(deviceObject);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error processing device {device.Id}: {ex.Message}");
				}
			}

			// Create result object
			var exportObject = new JObject
			{
				["exportDate"] = DateTime.UtcNow,
				["devices"] = new JArray(deviceExportList)
			};

			// Save to file
			Console.WriteLine($"Saving to {fileName}...");

			File.WriteAllText(fileName, exportObject.ToString(Formatting.Indented));

			Console.WriteLine($"Successfully exported {deviceExportList.Count} device(s) to {fileName}");
		}

		static async Task ImportDevicesToIoTHub(RegistryManager registryManager, List<JObject> deviceList)
		{
			int total = deviceList.Count;
			int successCount = 0;
			int failureCount = 0;
			int moduleSuccessCount = 0;
			int moduleFailureCount = 0;

			Console.WriteLine($"Starting import of {total} devices...");

			foreach (var deviceJson in deviceList)
			{
				string deviceId = deviceJson["deviceId"].ToString();
				Console.WriteLine($"Processing device {successCount + failureCount + 1}/{total}: {deviceId}");

				try
				{
					// Check if device already exists
					var existingDevice = await registryManager.GetDeviceAsync(deviceId);
					if (existingDevice != null)
					{
						Console.WriteLine($"Device {deviceId} already exists. Skipping device creation.");
					}
					else
					{
						// Create device with authentication
						var device = new Device(deviceId);

						// Check if it's an IoT Edge device
						bool isIotEdgeDevice = false;
						
						// Check capabilities
						if (deviceJson["capabilities"] != null && deviceJson["capabilities"].Type != JTokenType.Null)
						{
							var capabilitiesJson = deviceJson["capabilities"];
							isIotEdgeDevice = capabilitiesJson["iotEdge"]?.ToObject<bool>() ?? false;
							
							device.Capabilities = new DeviceCapabilities
							{
								IotEdge = isIotEdgeDevice
							};
							
							Console.WriteLine($"Device {deviceId} is {(isIotEdgeDevice ? "an IoT Edge" : "a regular IoT")} device.");
						}
						
						// Check twin for edge modules (alternative way to detect edge devices)
						if (!isIotEdgeDevice && deviceJson["modules"] != null && deviceJson["modules"].Type == JTokenType.Array)
						{
							var modules = deviceJson["modules"].ToObject<JArray>();
							
							// Check if any of the modules are edge modules ($edgeAgent or $edgeHub)
							foreach (var moduleJson in modules)
							{
								string moduleId = moduleJson["moduleId"]?.ToString();
								if (!string.IsNullOrEmpty(moduleId) && 
									(moduleId == "$edgeAgent" || moduleId == "$edgeHub"))
								{
									isIotEdgeDevice = true;
									device.Capabilities = new DeviceCapabilities
									{
										IotEdge = true
									};
									Console.WriteLine($"Device {deviceId} detected as IoT Edge device based on modules.");
									break;
								}
							}
						}

						// Set device authentication if available
						if (deviceJson["authentication"] != null && deviceJson["authentication"].Type != JTokenType.Null)
						{
							var authJson = deviceJson["authentication"];
							var authType = authJson["type"]?.ToString();

							if (authType == "sas")
							{
								// SAS authentication
								var primaryKey = authJson["symmetricKey"]?["primaryKey"]?.ToString();
								var secondaryKey = authJson["symmetricKey"]?["secondaryKey"]?.ToString();

								if (!string.IsNullOrEmpty(primaryKey) && !string.IsNullOrEmpty(secondaryKey))
								{
									device.Authentication = new AuthenticationMechanism
									{
										Type = AuthenticationType.Sas,
										SymmetricKey = new SymmetricKey
										{
											PrimaryKey = primaryKey,
											SecondaryKey = secondaryKey
										}
									};
								}
							}
							else if (authType == "selfSigned" || authType == "certificateAuthority")
							{
								// X.509 certificate authentication
								var thumbprint = authJson["x509Thumbprint"];
								var primaryThumbprint = thumbprint?["primaryThumbprint"]?.ToString();
								var secondaryThumbprint = thumbprint?["secondaryThumbprint"]?.ToString();

								device.Authentication = new AuthenticationMechanism
								{
									Type = authType == "selfSigned" ? 
										AuthenticationType.SelfSigned : 
										AuthenticationType.CertificateAuthority,
									X509Thumbprint = new X509Thumbprint
									{
										PrimaryThumbprint = primaryThumbprint,
										SecondaryThumbprint = secondaryThumbprint
									}
								};
							}
						}

						// Set device status
						if (deviceJson["status"] != null)
						{
							if (Enum.TryParse<DeviceStatus>(deviceJson["status"].ToString(), out var status))
							{
								device.Status = status;
							}
						}

						// Set status reason if available
						if (deviceJson["statusReason"] != null && deviceJson["statusReason"].Type != JTokenType.Null)
						{
							device.StatusReason = deviceJson["statusReason"].ToString();
						}

						// Create the device
						Console.WriteLine($"Creating device {deviceId}...");
						await registryManager.AddDeviceAsync(device);
						Console.WriteLine($"Device {deviceId} created successfully.");
					}

					// Update device twin if available
					if (deviceJson["twin"] != null && deviceJson["twin"].Type != JTokenType.Null)
					{
						var twinJson = deviceJson["twin"];
						
						// Create a new twin with the device ID
						var twin = new Twin(deviceId);
						
						// Set desired properties if available
						if (twinJson["properties"]?["desired"] != null)
						{
							var desiredProps = twinJson["properties"]["desired"];
							foreach (JProperty prop in desiredProps)
							{
								// Skip metadata and version
								if (prop.Name != "$metadata" && prop.Name != "$version")
								{
									twin.Properties.Desired[prop.Name] = prop.Value;
								}
							}
						}
						
						// Set tags if available
						if (twinJson["tags"] != null && twinJson["tags"].Type != JTokenType.Null)
						{
							foreach (JProperty tag in twinJson["tags"])
							{
								// Skip metadata
								if (tag.Name != "$metadata")
								{
									twin.Tags[tag.Name] = tag.Value;
								}
							}
						}
						
						// Update the twin
						Console.WriteLine($"Updating twin for device {deviceId}...");
						await registryManager.UpdateTwinAsync(deviceId, twin, "*");
						Console.WriteLine($"Twin for device {deviceId} updated successfully.");
					}

					// Process modules if available
					if (deviceJson["modules"] != null && deviceJson["modules"].Type == JTokenType.Array)
					{
						var modules = deviceJson["modules"].ToObject<JArray>();
						Console.WriteLine($"Found {modules.Count} modules for device {deviceId}");
						
						// Debug: Print the first module's structure if available
						if (modules.Count > 0)
						{
							Console.WriteLine("First module structure:");
							Console.WriteLine(modules[0].ToString(Formatting.Indented));
						}
						
						foreach (var moduleJson in modules)
						{
							string moduleId = moduleJson["moduleId"]?.ToString();
							
							if (string.IsNullOrEmpty(moduleId))
							{
								// Try alternative property names if "moduleId" is not found
								moduleId = moduleJson["Id"]?.ToString() ?? 
									moduleJson["id"]?.ToString() ?? 
									moduleJson["ModuleId"]?.ToString();
									
								if (string.IsNullOrEmpty(moduleId))
								{
									Console.WriteLine("Module ID is missing. Skipping module.");
									Console.WriteLine($"Available properties: {string.Join(", ", ((JObject)moduleJson).Properties().Select(p => p.Name))}");
									moduleFailureCount++;
									continue;
								}
							}
							
							try
							{
								// Check if module already exists
								var existingModule = await registryManager.GetModuleAsync(deviceId, moduleId);
								if (existingModule != null)
								{
									Console.WriteLine($"Module {moduleId} already exists on device {deviceId}. Skipping module creation.");
								}
								else
								{
									// Create module
									var module = new Module(deviceId, moduleId);
									
									// Set module authentication if available
									if (moduleJson["authentication"] != null && moduleJson["authentication"].Type != JTokenType.Null)
									{
										var authJson = moduleJson["authentication"];
										var authType = authJson["type"]?.ToString();
										
										if (authType == "sas")
										{
											// SAS authentication
											var primaryKey = authJson["symmetricKey"]?["primaryKey"]?.ToString();
											var secondaryKey = authJson["symmetricKey"]?["secondaryKey"]?.ToString();
											
											if (!string.IsNullOrEmpty(primaryKey) && !string.IsNullOrEmpty(secondaryKey))
											{
												module.Authentication = new AuthenticationMechanism
												{
													Type = AuthenticationType.Sas,
													SymmetricKey = new SymmetricKey
													{
														PrimaryKey = primaryKey,
														SecondaryKey = secondaryKey
													}
												};
											}
										}
									}
									
									// Create the module
									Console.WriteLine($"Creating module {moduleId} for device {deviceId}...");
									await registryManager.AddModuleAsync(module);
									Console.WriteLine($"Module {moduleId} created successfully for device {deviceId}.");
								}
								
								// Update module twin if available
								if (moduleJson["twin"] != null && moduleJson["twin"].Type != JTokenType.Null)
								{
									var twinJson = moduleJson["twin"];
									
									// Create a new twin with the device ID and module ID
									var twin = new Twin(deviceId);
									twin.ModuleId = moduleId;
									
									// Set desired properties if available
									if (twinJson["properties"]?["desired"] != null)
									{
										var desiredProps = twinJson["properties"]["desired"];
										foreach (JProperty prop in desiredProps)
										{
											// Skip metadata and version
											if (prop.Name != "$metadata" && prop.Name != "$version")
											{
												twin.Properties.Desired[prop.Name] = prop.Value;
											}
										}
									}
									
									// Set tags if available
									if (twinJson["tags"] != null && twinJson["tags"].Type != JTokenType.Null)
									{
										foreach (JProperty tag in twinJson["tags"])
										{
											// Skip metadata
											if (tag.Name != "$metadata")
											{
												twin.Tags[tag.Name] = tag.Value;
											}
										}
									}
									
									// Update the module twin
									Console.WriteLine($"Updating twin for module {moduleId} on device {deviceId}...");
									await registryManager.UpdateTwinAsync(deviceId, moduleId, twin, "*");
									Console.WriteLine($"Twin for module {moduleId} on device {deviceId} updated successfully.");
								}
								
								moduleSuccessCount++;
							}
							catch (Exception moduleEx)
							{
								Console.WriteLine($"Error processing module {moduleId} for device {deviceId}: {moduleEx.Message}");
								moduleFailureCount++;
							}
						}
					}
					
					successCount++;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error processing device {deviceId}: {ex.Message}");
					failureCount++;
				}
			}
			
			Console.WriteLine("\nImport Summary:");
			Console.WriteLine($"Devices: {successCount} succeeded, {failureCount} failed");
			Console.WriteLine($"Modules: {moduleSuccessCount} succeeded, {moduleFailureCount} failed");
			Console.WriteLine($"Total: {successCount + moduleSuccessCount} entities imported successfully");
		}
	}
}