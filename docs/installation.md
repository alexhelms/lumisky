# Installation

It is recommended to run LumiSky as a docker container, but it also run as a native application.

## Raspi

?> If you are using a raspberry pi camera, see [this guide](/raspi-camera).

LumiSky and INDI server both run as containers on a raspi.

Create a clean raspi image following [the official instructions](https://www.raspberrypi.com/software/).
Install Raspberry Pi OS 64-bit, full or lite. Use the full version if you are going to use a mouse, keyboard,
and monitor on the raspi. Use the lite version if you are going to use SSH.

?> If you are on Windows and unfamiliar with Linux, ssh is built into Windows (since 2018). You should install
   and use Windows Terminal from the Microsoft Store (or [github](https://github.com/microsoft/terminal/releases)).
   If you'd like to use an SCP GUI for editing files, [WinSCP](https://winscp.net) is a great option.

SSH into your raspi, or if you are using a mouse and keyboard, run the following commands in a terminal.

1. Install docker.

```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh ./get-docker.sh
```

2. Allow non-sudo docker usage.

```bash
sudo usermod -aG docker $USER
newgrp docker
```

3. Enable docker to start on boot.

```bash
sudo systemctl enable docker.service
sudo systemctl enable containerd.service
```

4. Setup Lumisky.

```bash
sudo groupadd -g 1654 lumisky && sudo useradd -u 1654 -g lumisky lumisky
mkdir -p ~/lumisky/container
cd ~/lumisky
sudo chown lumisky:lumisky container
touch docker-compose.yml
```

?> You can run `nano docker-compose.yml` to edit the docker compose file.
   Use `shift+insert` to paste. When you are ready to save and exit, press
   `ctrl+x`, press `y`, then `enter`. Or, you can use an SCP client to
   download, edit, and upload the file.

Copy the following into `docker-compose.yml` and change the following as needed:
* Environment variable `TZ` to your local [timezone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones).
* INDI camera driver. See the [INDI devices](https://www.indilib.org/devices.html)
  page to find the name of the INDI driver.
  Common driver names:
    * ZWO - `indi_asi_ccd`
    * QHY - `indi_qhy_ccd`
    * Player One - `indi_playerone_ccd`
    * Touptek - `indi_toupcam_ccd`

?> If you already have a web server listening on 8080, you must change the LumiSky port.
   To change the port to `8081`, in the `docker-compose.yml` file in the `ports` section,
   change `8080:8080` to `8081:8080`. See [docker docs](https://docs.docker.com/engine/network/#published-ports)
   for more information.

[docker-compose.yml](/examples/raspi.docker-compose.yml ':include :type=code') 

5. Start LumiSky.

```bash
docker compose up -d
```

Go to the LumiSky dashboard on port 8080.

?> You can stop LumiSky by going to the directory that contains `docker-compose.yml` and running `docker compose down`.

## Docker

LumiSky can run on a server as docker container and connect to a raspi or other computer running `indiserver`.

It is assumed your server is already running docker and you can add a new `docker-compose.yml` file.

Change the following as needed:
* Environment variable `TZ` to your local [timezone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones).

[docker-compose.yml](examples/server.docker-compose.yml ':include :type=code')

## Debian

While it is recommended to use a docker container, you can run LumiSky as a native application in Debian 12 (bookworm).

Install dependencies.

```bash
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt upgrade -y aspnetcore-runtime-9.0 python3 python3-pip python3-pil ffmpeg libgeotiff5 libgtk-3-0 vtk9
```

Download LumiSky

```bash
sudo mkdir -p /var/lib/lumisky/app
cd /var/lib/lumisky/app
# TODO: wget lumisky-linux-x64-*.tar.gz
sudo tar zxvf lumisky-linux-x64-*.tar.gz
rm lumisky-*.tar.gz
chown www-data:www-data -R /var/lib/lumisky/
```

Create a systemd service

```bash
sudo nano /etc/systemd/system/lumisky.service
```

```ini
[Unit]
Description=LumiSky

[Service]
WorkingDirectory=/var/lib/lumisky/app
ExecStart=/usr/bin/dotnet /var/lib/lumisky/app/LumiSky.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=lumisky
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://*:8080
Environment=DOTNET_NOLOGO=true
Environment=LUMISKY_PATH=/var/lib/lumisky

[Install]
WantedBy=multi-user.target
```

Enable and start LumiSky

```bash
sudo systemctl daemon-reload
sudo systemctl enable lumisky
sudo systemctl start lumisky
```

Go to the LumiSky dashboard on port 8080.

## Windows

Download and install the following dependencies:
* [`ffmpeg-release-full.7z`](https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-full.7z)
  * Extract to `C:\ffmpeg`
* [Python 3.12](https://www.python.org/ftp/python/3.12.8/python-3.12.8-amd64.exe)

Download LumiSky for Windows (TODO: add download link) and extract to `C:\lumisky`.

Create an entry in the Windows Task Scheduler so LumiSky will automatically start on boot.

1. Open the Task Scheduler.
2. On the right, select `Create Basic Task...`.
3. Set the name to `LumiSky` and select Next.
4. Select `When the computer starts` and select Next.
5. Select `Start a program` and select Next.
6. Select `Browse` and navigate to `C:\lumisky\LumiSky.exe`
7. Enter `--urls=http://*:8080` for `Add arguments (optional)`.
8. Select Next.
9. Check the box for `Open the Properties dialog for this task when I click Finish` and select Finish.
10. Select the `General` tab and select `Run whether user is logged on or not`. You may have to enter your credentials.
11. Select the `Settings` tab and uncheck `Stop the task if it runs longer than:` option.
12. Check `If the task fails, restart every:` and select `1 minute` and `3 times`.
13. Select OK.
14. In the Task Scheduler, on the left, select `Task Scheduler Library` and press F5 to refresh.
15. Right click the `LumiSky` task and select `Run`.

Go to the LumiSky dashboard on at [http://localhost:8080](http://localhost:8080).
