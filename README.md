# UDP JSON Dashboard (RxGUI)

A professional, cross-platform telemetry dashboard built with **Avalonia UI** and **.NET 9**. This tool provides a real-time interface for monitoring, testing, and interacting with telemetry systems using JSON-formatted data over UDP.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![AvaloniaUI](https://img.shields.io/badge/AvaloniaUI-11.3-red.svg)

## 🎥 Demo


https://github.com/user-attachments/assets/df0cef9c-ba75-4b53-8a8b-bd483e303150



---

## 🚀 Key Features

- **Real-time Telemetry:** Seamlessly send and receive telemetry data via UDP.
- **Multi-Instance Management:** Easily configure and switch between different network profiles (Local/Remote IP and Ports).
- **Flexible JSON Editor:** Edit telemetry payloads with a built-in editor using string literals for maximum flexibility, bypassing the need for fixed data classes.
- **Comprehensive Logging:** View incoming (RX) and outgoing (TX) messages with timestamps and endpoint details.
- **Persistent Configuration:** All settings are stored in an organized `Config.inf` file for easy portability.
- **Modern UI/UX:** Leveraging **Actipro Software** controls and **Avalonia UI** for a high-performance, themed user experience.
- **Data Persistence:** Integrated SQLite support for long-term data management.

## 🛠️ Technology Stack

- **Framework:** .NET 9.0
- **UI Framework:** [Avalonia UI](https://avaloniaui.net/) (v11.3.12)
- **Networking:** [NetCoreServer](https://github.com/chronoxor/NetCoreServer) for high-performance UDP communication.
- **JSON Handling:** [Newtonsoft.Json](https://www.newtonsoft.com/json) for robust serialization/deserialization.
- **UI Components:** [Actipro Software Avalonia Controls](https://www.actiprosoftware.com/products/controls/avalonia) for professional-grade UI elements.
- **Database:** [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) for lightweight data storage.
- **Configuration:** Salaros.ConfigParser for INI-based configuration management.

## 🏁 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed on your machine.

### Installation & Run

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd udp-json-dashboard
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run --project "ModelUI-master (Copy)/RxGUI/RxGUI.csproj"
   ```

## ⚙️ Configuration

The application uses a `Config.inf` file located in the root directory to manage instances and network settings. While these can be modified via the UI, the structure is as follows:

```ini
[Network]
LocalIp=127.0.0.1
LocalPort=9000
RemoteIp=127.0.0.1
RemotePort=9001

[App]
InstanceId=InstanceA

[Instances]
Names=InstanceA, InstanceB
```

## 📖 Usage

1. **Setup Instances:** Use the sidebar to add or select a network instance.
2. **Start Server:** Click **Start Server** to begin listening for incoming telemetry on your local port.
3. **Send Telemetry:**
   - Prepare your JSON payload in the editor.
   - Click **Send JSON** to transmit the data to the configured remote endpoint.
4. **Monitor Logs:** Switch to the **Logs** view to inspect real-time traffic, including raw JSON payloads and metadata.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request or open an issue for any bugs or feature requests.

## 🎖️ Credits

Special thanks to **[@uxmanz](https://github.com/uxmanz)** for creating the **Base Model UI ViewPanels**. His template significantly streamlined the development process, allowing for easy expansion and customization of the dashboard components.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
