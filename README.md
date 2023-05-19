# Skia Twitch Chat

![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)

Twitch Chat Interface is a graphical application built using SKIA in .NET that provides a interface for interacting with Twitch chat. With this tool, you can easily view and participate in chat conversations.

![Screenshot](img/Twitch%20Chat.png)

## Features

- **Real-time Chat**: Interact with Twitch chat in real-time, receiving and sending messages seamlessly.

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Silk.NET.Core v2.17.1
- Silk.NET.Input.Common v2.17.1
- Silk.NET.Input.Glfw v2.17.1
- Silk.NET.Maths v2.17.1
- Silk.NET.Windowing.Common v2.17.1
- Silk.NET.Windowing.Glfw v2.17.1
- SkiaSharp v2.88.3
- SkiaSharp.NativeAssets.Win32 v2.88.3

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/assasinos/Skia-Twitch-Chat.git
   ```

2. Change into the project directory:

   ```bash
   cd Skia Twitch Chat
   ```

3. Set the following environment variables:
   - `TwitchOathPassword`: Your Twitch OAuth password.
   - `TwitchOathNickname`: Your Twitch OAuth nickname.

4. Build the application:

   ```bash
   dotnet build
   ```

## Usage

1. Launch the application:

   ```bash
   dotnet run
   ```


2. Use the following commands (prefixed with `\`) for functionality:
   - `\help`: Display the list of available commands and their usage.
   - `\join [<channel>]`: Join the channel


3. The chat interface will be displayed, showing real-time messages from the Twitch chat.


4. Type your messages in the input field at the bottom and press Enter to send them.


5. Enjoy interacting with Twitch chat in a graphical interface!


## Contributing

Contributions are welcome! If you find any issues or have suggestions for improvements, please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.
