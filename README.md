# MariaDB-Control
MariaDB WPF App to Control Windows Service

This is a simple learning/testing project. 

It's a Windows Presentation Foundation (WPF) that detects a MariaDB service, presents it's status (Running, Stopped) and allows you to control it via buttons.

[Download](https://github.com/lucianosaito/MariaDB-Control/releases) the current release at the releases page.

## Learning Objectives
While making this project, my focus was learning:
- Visual Studio, C# and WPF, which I never coded before
- Controling Windows Services in C#
- XAML, WPF Layouts, customizing components, Metro UI Style
- Reading .INI Files
- Threading and Updating the UI with Threads

I apologize for the lack of model classes and the poor coding.

## TODO
- Un-hardcode "MySQL" service name, mysqld.exe call and my.ini read, possibly making an option menu to customize the paths and commands.

## Screenshot

![MariaDB Control Screenshot](https://lucianosaito.github.io/mdb_screenshot.png)



This project is licensed under the terms of the MIT license.
