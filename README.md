# Knaeckebot - Desktop Automation Tool

Knaeckebot is a powerful desktop automation tool developed with .NET 8 and WPF. It allows you to record, create, and execute sequences of mouse and keyboard inputs to automate repetitive tasks on your computer.

## ✨ Features

- **Record and Playback**: Record mouse clicks, keyboard inputs, and more with just a few clicks
- **Intelligent Sequences**: Create complex automation workflows with variables and conditional logic
- **Loop Actions**: Repeat sequences with configurable conditions and counters
- **Clipboard Operations**: Automate copying and pasting of text
- **Browser Integration**: Interact with web browsers for web automation
- **JSON Processing**: Process JSON data for advanced automation workflows
- **Variable Support**: Use dynamic variables for flexible automations
- **Custom Delays**: Precise timing control between actions

## 🖼️ Screenshots
![image](https://github.com/user-attachments/assets/762acec4-b98a-401a-ab96-786dd1b94479)
![image](https://github.com/user-attachments/assets/53ded1aa-ea39-4629-a07e-6ad3a048ebfe)


## 🔧 Installation

### Prerequisites

- Windows operating system (Windows 10 or higher recommended)
- .NET 8 Runtime
- For development: Visual Studio 2022 or higher

### Installation via Release Files

1. Download the latest version from the [Releases page](https://github.com/Pinkognito/knaeckebot/releases)
2. Extract the ZIP file to a folder of your choice
3. Start the application via `Knaeckebot.exe`

### Installation from Source Files

1. Clone this repository:
   ```
   git clone https://github.com/Pinkognito/knaeckebot.git
   ```
2. Open the solution in Visual Studio
3. Build the solution (Ctrl+Shift+B)
4. Start the application (F5)

## 🚀 Usage

### Getting Started

1. Start Knaeckebot
2. Create a new sequence via the "File" → "New Sequence" menu
3. Give the sequence a name and description

### Recording a Sequence

1. Select the desired sequence
2. Click "Start Rec" in the toolbar
3. Perform the actions you want to automate
4. Click "Stop Rec" to end the recording

### Playing a Sequence

1. Select the desired sequence
2. Click "Play (F5)" in the toolbar or press the F5 key
3. The sequence will now be executed automatically
4. Press "Stop (F6/Esc)" or the Escape key to interrupt the execution

### Working with Variables

1. Expand the "Sequence Variables" section
2. Click "Add Variable" to create a new variable
3. Set the name, type, and initial value of the variable
4. Use the variable in your actions (e.g., in text inputs or conditions)

### Creating a Loop Action

1. Click "Add Action" and select "Loop Action"
2. Set the maximum number of iterations
3. Optional: Enable a condition for loop termination
4. Add actions to the loop (copy them from the main action list with Ctrl+C and paste them with Ctrl+V)

## ⌨️ Keyboard Shortcuts

- **F5**: Play sequence
- **F6** or **ESC**: Stop playback
- **Ctrl+C**: Copy actions / Duplicate sequence
- **Ctrl+V**: Paste actions
- **Del**: Delete selected actions or sequences

## 🛠️ For Developers

### Project Structure

- **Controls/**: WPF controls for the user interface
- **Converters/**: Value converters for XAML bindings
- **Models/**: Data models for sequences and actions
- **Services/**: Core functionality and services
- **ViewModels/**: ViewModels for MVVM architecture

### Contributions

Contributions to the project are welcome! Here's how you can help:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 🔒 Security Note

This application uses low-level keyboard and mouse hooks to provide its automation functions. These are used exclusively for legitimate automation purposes. Some security software might flag this behavior - please ensure you trust the source before running it.

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📋 Disclaimer

Knaeckebot was designed for automating repetitive tasks on your own computer. Use it responsibly and be aware that automating interactions with third-party services may be subject to their terms of service.
