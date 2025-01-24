# Updating

## Docker

1. SSH into your raspi or server.
2. Stop Lumisky.

  ```bash
  cd ~/lumisky
  docker compose down
  ```

3. Edit `docker-compose.yml` and change the version tag on the image.
4. Start Lumisky.

  ```bash
  docker compose up -d
  ```

## Linux

1. SSH into the server.
2. Stop the service.

  ```bash
  systemctl stop lumisky
  ```
3. Download and extract the new version of LumiSky.
  ```bash
  cd ~
  # TODO: wget lumisky-linux-x64-*.tar.gz
  tar zxvf lumisky-linux-x64-*.tar.gz
  ```
4. Copy the new version and set permissions.
  ```bash
  sudo rm -r /var/lib/lumisky/app
  sudo cp -r lumisky/ /var/lib/lumisky/app/
  chown www-data:www-data -R /var/lib/lumisky/app/
  ```
4. Start Lumisky.
  ```bash
  systemctl start lumisky
  ```

## Windows

1. Open Task Scheduler
2. On the left select `Task Scheduler Library`.
3. Right click the LumiSky task and select `End`.
4. Download the new version of lumisky.
5. Delete the previous version fo LumiSky at `C:\lumisky`.
6. Extract the zip to `C:\lumisky`.
7. Back in Task Scheduler, right click the LumiSky task and select `Run`.
