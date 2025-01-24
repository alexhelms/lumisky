using FluentFTP;
using LumiSky.Core.Profile;
using System.Diagnostics;

namespace LumiSky.Core.Services;

public class FtpService
{
    private readonly IProfileProvider _profile;

    public FtpService(IProfileProvider profile)
    {
        _profile = profile;
    }

    public async Task UploadFiles(IEnumerable<string> filenames, CancellationToken token = default)
    {
        var start = Stopwatch.GetTimestamp();

        using var ftp = CreateFtpClient();
        await ftp.Connect(token);

        if (!string.IsNullOrWhiteSpace(_profile.Current.Export.FtpRemotePath))
        {
            var remotePath = GetRemotePath();
            await ftp.SetWorkingDirectory(remotePath, token);
        }

        foreach (var localFilename in filenames)
        {
            await UploadFile(ftp, localFilename, token);
        }

        await ftp.Disconnect(token);

        var elapsed = Stopwatch.GetElapsedTime(start);
        Log.Information("FTP upload completed in {Elapsed:F3} seconds", elapsed.TotalSeconds);
    }

    public async Task TryConnect(CancellationToken token = default)
    {
        using var ftp = CreateFtpClient();
        await ftp.Connect(token);
        await ftp.Disconnect();
    }

    private AsyncFtpClient CreateFtpClient()
    {
        return new AsyncFtpClient(
            host: _profile.Current.Export.FtpHostname,
            user: _profile.Current.Export.FtpUsername,
            pass: _profile.Current.Export.FtpPassword,
            port: _profile.Current.Export.FtpPort,
            config: new FtpConfig
            {
                ClientTimeZone = TimeZoneInfo.Local,
                ConnectTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds,
                ValidateAnyCertificate = !_profile.Current.Export.EnableCertificateValidation,
            });
    }

    private async Task UploadFile(AsyncFtpClient ftp, string localFilename, CancellationToken token)
    {
        var remoteFilename = localFilename.Replace(_profile.Current.App.ImageDataPath, string.Empty);
        var start = Stopwatch.GetTimestamp();

        // Make path relative
        remoteFilename = remoteFilename.TrimStart('/').TrimStart('\\');

        await ftp.UploadFile(localFilename, remoteFilename,
            existsMode: FtpRemoteExists.Overwrite,
            createRemoteDir: true,
            token: token);

        var elapsed = Stopwatch.GetElapsedTime(start);

        // Fixup the remote filename for logging
        if (!string.IsNullOrWhiteSpace(_profile.Current.Export.FtpRemotePath))
        {
            remoteFilename = Path.Join(GetRemotePath(), remoteFilename);
        }
        else
        {
            remoteFilename = Path.DirectorySeparatorChar + remoteFilename;
        }

        Log.Information("Uploaded raw {LocalFilename} to {RemoteFilename} in {Elapsed:F3} sec",
            localFilename, remoteFilename, elapsed.TotalSeconds);
    }

    private string GetRemotePath()
    {
        if (!string.IsNullOrWhiteSpace(_profile.Current.Export.FtpRemotePath))
        {
            var remotePath = _profile.Current.Export.FtpRemotePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (!remotePath.EndsWith(Path.DirectorySeparatorChar))
                remotePath += Path.DirectorySeparatorChar;

            return remotePath;
        }

        return Path.DirectorySeparatorChar.ToString();
    }
}
