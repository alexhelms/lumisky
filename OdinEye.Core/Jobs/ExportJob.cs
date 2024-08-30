using FluentFTP;
using OdinEye.Core.Profile;
using Quartz;
using System.Diagnostics;

namespace OdinEye.Core.Jobs;

public class ExportJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Export, JobConstants.Groups.Allsky);
    
    private readonly IProfileProvider _profile;

    public ExportJob(IProfileProvider profile)
    {
        _profile = profile;
    }

    public string? RawFilename { get; set; }
    public string? ImageFilename { get; set; }
    public string? PanoramaFilename { get; set; }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        if (!_profile.Current.Export.EnableExport) return;

        var localFilenames = new List<string>();

        if (_profile.Current.Export.ExportRaws &&
            RawFilename is not null &&
            File.Exists(RawFilename))
        {
            localFilenames.Add(RawFilename);
        }

        if (_profile.Current.Export.ExportImages &&
            ImageFilename is not null &&
            File.Exists(ImageFilename))
        {
            localFilenames.Add(ImageFilename);
        }

        if (_profile.Current.Export.ExportPanoramas &&
            PanoramaFilename is not null &&
            File.Exists(PanoramaFilename))
        {
            localFilenames.Add(PanoramaFilename);
        }
        
        if (localFilenames.Count > 0)
        {
            var start = Stopwatch.GetTimestamp();

            using var ftp = new AsyncFtpClient(
                host: _profile.Current.Export.FtpHostname,
                user: _profile.Current.Export.FtpUsername,
                pass: _profile.Current.Export.FtpPassword,
                port: _profile.Current.Export.FtpPort,
                config: new FtpConfig
                {
                    ValidateAnyCertificate = _profile.Current.Export.EnableCertificateValidation,
                });

            await ftp.AutoConnect();

            if (!string.IsNullOrWhiteSpace(_profile.Current.Export.FtpRemotePath))
            {
                var remotePath = GetRemotePath();
                await ftp.SetWorkingDirectory(remotePath, context.CancellationToken);
            }

            foreach (var localFilename in localFilenames)
            {
                await UploadFile(ftp, localFilename, context.CancellationToken);
            }

            await ftp.Disconnect();

            var elapsed = Stopwatch.GetElapsedTime(start);
            Log.Information("FTP upload completed in {Elapsed:F3} seconds", elapsed.TotalSeconds);
        }
    }

    private async Task UploadFile(AsyncFtpClient ftp, string localFilename, CancellationToken token)
    {
        var remoteFilename = localFilename.Replace(_profile.Current.Capture.DataDirectory, string.Empty);
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
